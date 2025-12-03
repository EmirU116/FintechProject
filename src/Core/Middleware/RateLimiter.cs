using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Source.Core.Middleware
{
    /// <summary>
    /// In-memory rate limiter using sliding window algorithm.
    /// For production, consider Redis-based distributed rate limiting.
    /// </summary>
    public class RateLimiter
    {
        private readonly ConcurrentDictionary<string, RequestWindow> _requestWindows = new();
        private readonly int _maxRequestsPerWindow;
        private readonly TimeSpan _windowDuration;

        public RateLimiter(int maxRequestsPerWindow = 100, TimeSpan? windowDuration = null)
        {
            _maxRequestsPerWindow = maxRequestsPerWindow;
            _windowDuration = windowDuration ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Checks if the request is allowed for the given identifier (e.g., IP address or API key).
        /// </summary>
        /// <param name="identifier">Unique identifier for the client (IP, API key, user ID)</param>
        /// <returns>True if request is allowed, false if rate limit exceeded</returns>
        public bool IsRequestAllowed(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            var window = _requestWindows.GetOrAdd(identifier, _ => new RequestWindow());

            lock (window)
            {
                // Remove expired requests from sliding window
                window.RemoveExpiredRequests(now, _windowDuration);

                // Check if limit exceeded
                if (window.RequestCount >= _maxRequestsPerWindow)
                {
                    return false;
                }

                // Add new request
                window.AddRequest(now);
                return true;
            }
        }

        /// <summary>
        /// Gets the number of requests made by the identifier in the current window.
        /// </summary>
        public int GetRequestCount(string identifier)
        {
            if (_requestWindows.TryGetValue(identifier, out var window))
            {
                lock (window)
                {
                    window.RemoveExpiredRequests(DateTimeOffset.UtcNow, _windowDuration);
                    return window.RequestCount;
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the time until the rate limit resets for the identifier.
        /// </summary>
        public TimeSpan? GetTimeUntilReset(string identifier)
        {
            if (_requestWindows.TryGetValue(identifier, out var window))
            {
                lock (window)
                {
                    var oldestRequest = window.GetOldestRequestTime();
                    if (oldestRequest.HasValue)
                    {
                        var resetTime = oldestRequest.Value.Add(_windowDuration);
                        var timeUntilReset = resetTime - DateTimeOffset.UtcNow;
                        return timeUntilReset > TimeSpan.Zero ? timeUntilReset : TimeSpan.Zero;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Cleans up expired request windows to prevent memory leaks.
        /// Should be called periodically.
        /// </summary>
        public void CleanupExpiredWindows()
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = new System.Collections.Generic.List<string>();

            foreach (var kvp in _requestWindows)
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveExpiredRequests(now, _windowDuration);
                    if (kvp.Value.RequestCount == 0)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
            }

            foreach (var key in expiredKeys)
            {
                _requestWindows.TryRemove(key, out _);
            }
        }

        private class RequestWindow
        {
            private readonly System.Collections.Generic.Queue<DateTimeOffset> _requests = new();

            public int RequestCount => _requests.Count;

            public void AddRequest(DateTimeOffset timestamp)
            {
                _requests.Enqueue(timestamp);
            }

            public void RemoveExpiredRequests(DateTimeOffset now, TimeSpan windowDuration)
            {
                var cutoffTime = now.Subtract(windowDuration);
                while (_requests.Count > 0 && _requests.Peek() < cutoffTime)
                {
                    _requests.Dequeue();
                }
            }

            public DateTimeOffset? GetOldestRequestTime()
            {
                return _requests.Count > 0 ? _requests.Peek() : null;
            }
        }
    }

    /// <summary>
    /// Configuration options for rate limiting.
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// Maximum number of requests allowed per time window.
        /// Default: 100 requests per minute.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 100;

        /// <summary>
        /// Duration of the rate limit window.
        /// Default: 1 minute.
        /// </summary>
        public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Burst limit - maximum requests in a short burst.
        /// Default: 200 requests per minute.
        /// </summary>
        public int BurstLimit { get; set; } = 200;

        /// <summary>
        /// Whether to enable rate limiting.
        /// Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Custom message returned when rate limit is exceeded.
        /// </summary>
        public string? RateLimitExceededMessage { get; set; } = "Rate limit exceeded. Please try again later.";
    }
}
