using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Source.Core.Database;
using Microsoft.EntityFrameworkCore;
using Source.Core;
using System.Text.Json;

namespace Source.Functions;

public class GetAuditLogs
{
    private readonly ILogger<GetAuditLogs> _logger;
    private readonly ApplicationDbContext _dbContext;

    public GetAuditLogs(ILogger<GetAuditLogs> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("GetAuditLogs")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("📋 Retrieving audit logs");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            
            var eventType = query["eventType"];
            var transactionId = query["transactionId"];
            var fromDateStr = query["fromDate"];
            var toDateStr = query["toDate"];
            var limitStr = query["limit"] ?? "100";

            // Build query
            var auditQuery = _dbContext.AuditEvents.AsQueryable();

            // Filter by event type
            if (!string.IsNullOrWhiteSpace(eventType))
            {
                auditQuery = auditQuery.Where(a => a.EventType == eventType);
                _logger.LogInformation("Filtering by event type: {EventType}", eventType);
            }

            // Filter by transaction ID (in subject)
            if (!string.IsNullOrWhiteSpace(transactionId))
            {
                auditQuery = auditQuery.Where(a => a.EventSubject.Contains(transactionId));
                _logger.LogInformation("Filtering by transaction ID: {TransactionId}", transactionId);
            }

            // Filter by date range
            if (DateTime.TryParse(fromDateStr, out var fromDate))
            {
                auditQuery = auditQuery.Where(a => a.EventTime >= fromDate);
                _logger.LogInformation("Filtering from date: {FromDate}", fromDate);
            }

            if (DateTime.TryParse(toDateStr, out var toDate))
            {
                auditQuery = auditQuery.Where(a => a.EventTime <= toDate);
                _logger.LogInformation("Filtering to date: {ToDate}", toDate);
            }

            // Apply limit
            if (!int.TryParse(limitStr, out var limit) || limit <= 0 || limit > 1000)
            {
                limit = 100;
            }

            // Execute query
            var auditLogs = await auditQuery
                .OrderByDescending(a => a.EventTime)
                .Take(limit)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} audit log entries", auditLogs.Count);

            // Format response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var responseData = new
            {
                success = true,
                count = auditLogs.Count,
                limit = limit,
                filters = new
                {
                    eventType = eventType ?? "none",
                    transactionId = transactionId ?? "none",
                    fromDate = fromDateStr ?? "none",
                    toDate = toDateStr ?? "none"
                },
                auditLogs = auditLogs.Select(a => new
                {
                    id = a.Id,
                    eventId = a.EventId,
                    eventType = a.EventType,
                    eventSource = a.EventSource,
                    eventSubject = a.EventSubject,
                    eventData = JsonSerializer.Deserialize<object>(a.EventData),
                    eventTime = a.EventTime,
                    recordedAt = a.RecordedAt
                })
            };
            
            await response.WriteStringAsync(
                JsonSerializer.Serialize(responseData, new JsonSerializerOptions { WriteIndented = true })
            );

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs: {Message}", ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to retrieve audit logs",
                error = ex.Message
            });
            return errorResponse;
        }
    }
}
