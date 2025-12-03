using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Source.Core.Middleware;

namespace Functions.Middleware
{
    /// <summary>
    /// Middleware to enforce rate limiting on Azure Functions HTTP requests.
    /// Uses sliding window algorithm to track requests per client.
    /// </summary>
    public class RateLimitMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly RateLimiter _rateLimiter;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitOptions _options;

        public RateLimitMiddleware(
            ILogger<RateLimitMiddleware> logger,
            RateLimiter rateLimiter,
            RateLimitOptions options)
        {
            _logger = logger;
            _rateLimiter = rateLimiter;
            _options = options;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            // Skip rate limiting if disabled
            if (!_options.Enabled)
            {
                await next(context);
                return;
            }

            // Only apply to HTTP-triggered functions
            var requestData = await context.GetHttpRequestDataAsync();
            if (requestData == null)
            {
                await next(context);
                return;
            }

            // Get client identifier (prefer API key, fallback to IP address)
            var identifier = GetClientIdentifier(requestData);

            // Check rate limit
            if (!_rateLimiter.IsRequestAllowed(identifier))
            {
                var requestCount = _rateLimiter.GetRequestCount(identifier);
                var resetTime = _rateLimiter.GetTimeUntilReset(identifier);

                _logger.LogWarning(
                    "Rate limit exceeded for client {Identifier}. Request count: {Count}/{Limit}. Reset in: {ResetTime}",
                    MaskIdentifier(identifier),
                    requestCount,
                    _options.MaxRequestsPerWindow,
                    resetTime?.ToString(@"mm\:ss") ?? "unknown"
                );

                // Create rate limit response
                var response = requestData.CreateResponse();
                response.StatusCode = HttpStatusCode.TooManyRequests;
                await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "Rate limit exceeded",
                    message = _options.RateLimitExceededMessage,
                    limit = _options.MaxRequestsPerWindow,
                    windowDuration = _options.WindowDuration.TotalSeconds,
                    retryAfter = resetTime?.TotalSeconds ?? 60
                }));

                // Add rate limit headers
                response.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerWindow.ToString());
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("X-RateLimit-Reset", ((int)(resetTime?.TotalSeconds ?? 60)).ToString());
                response.Headers.Add("Retry-After", ((int)(resetTime?.TotalSeconds ?? 60)).ToString());

                context.GetInvocationResult().Value = response;
                return;
            }

            // Add rate limit info headers to successful requests
            var remaining = _options.MaxRequestsPerWindow - _rateLimiter.GetRequestCount(identifier);
            
            _logger.LogInformation(
                "Rate limit check passed for client {Identifier}. Remaining: {Remaining}/{Limit}",
                MaskIdentifier(identifier),
                remaining,
                _options.MaxRequestsPerWindow
            );

            // Continue to the function
            await next(context);

            // Add headers to response after function execution
            var httpResponseData = context.GetHttpResponseData();
            if (httpResponseData != null)
            {
                httpResponseData.Headers.Add("X-RateLimit-Limit", _options.MaxRequestsPerWindow.ToString());
                httpResponseData.Headers.Add("X-RateLimit-Remaining", Math.Max(0, remaining - 1).ToString());
                
                var resetTime = _rateLimiter.GetTimeUntilReset(identifier);
                if (resetTime.HasValue)
                {
                    httpResponseData.Headers.Add("X-RateLimit-Reset", ((int)resetTime.Value.TotalSeconds).ToString());
                }
            }
        }

        /// <summary>
        /// Extracts client identifier from request.
        /// Priority: Function Key > API Key Header > IP Address
        /// </summary>
        private string GetClientIdentifier(HttpRequestData request)
        {
            // Try to get function key from query parameter
            var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
            var functionKey = query["code"];
            if (!string.IsNullOrWhiteSpace(functionKey))
            {
                return $"key:{functionKey.Substring(0, Math.Min(8, functionKey.Length))}";
            }

            // Try to get API key from header
            if (request.Headers.TryGetValues("x-functions-key", out var apiKeys))
            {
                var apiKey = apiKeys.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    return $"key:{apiKey.Substring(0, Math.Min(8, apiKey.Length))}";
                }
            }

            // Fallback to IP address
            var clientIp = GetClientIpAddress(request);
            return $"ip:{clientIp}";
        }

        /// <summary>
        /// Extracts client IP address from request headers.
        /// </summary>
        private string GetClientIpAddress(HttpRequestData request)
        {
            // Check for forwarded IP (behind proxy/load balancer)
            if (request.Headers.TryGetValues("X-Forwarded-For", out var forwardedIps))
            {
                var ip = forwardedIps.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    return ip;
                }
            }

            // Check for real IP header
            if (request.Headers.TryGetValues("X-Real-IP", out var realIps))
            {
                var ip = realIps.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    return ip;
                }
            }

            // Fallback to unknown
            return "unknown";
        }

        /// <summary>
        /// Masks the identifier for logging purposes.
        /// </summary>
        private string MaskIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return "unknown";
            }

            var parts = identifier.Split(':');
            if (parts.Length == 2)
            {
                var prefix = parts[0];
                var value = parts[1];
                
                if (prefix == "key" && value.Length > 4)
                {
                    return $"{prefix}:****{value.Substring(value.Length - 4)}";
                }
                
                return identifier;
            }

            return identifier;
        }
    }

    /// <summary>
    /// Extension methods for FunctionContext to simplify rate limit middleware.
    /// </summary>
    public static class FunctionContextExtensions
    {
        public static HttpResponseData? GetHttpResponseData(this FunctionContext context)
        {
            var invocationResult = context.GetInvocationResult();
            if (invocationResult?.Value is HttpResponseData responseData)
            {
                return responseData;
            }
            return null;
        }
    }
}
