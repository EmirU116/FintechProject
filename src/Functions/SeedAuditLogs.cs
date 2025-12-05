using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Source.Core.Database;
using Source.Core;
using System.Text.Json;

namespace Functions;

public class SeedAuditLogs
{
    private readonly ILogger<SeedAuditLogs> _logger;
    private readonly ApplicationDbContext _dbContext;

    public SeedAuditLogs(ILogger<SeedAuditLogs> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("SeedAuditLogs")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("🌱 Seeding audit log entries for testing");

        try
        {
            var transactionId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var auditLogs = new List<AuditEvent>
            {
                // Transaction.Queued event
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "Transaction.Queued",
                    EventSource = "urn:fintech:transactions",
                    EventSubject = $"transactions/{transactionId}",
                    EventData = JsonSerializer.Serialize(new
                    {
                        transactionId = transactionId.ToString(),
                        amount = 250.00m,
                        currency = "USD",
                        fromCardMasked = "****-****-****-9012",
                        toCardMasked = "****-****-****-1234",
                        queuedAtUtc = now
                    }),
                    EventTime = now,
                    RecordedAt = now.AddMilliseconds(50)
                },
                // Transaction.Settled event
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "Transaction.Settled",
                    EventSource = "urn:fintech:transactions",
                    EventSubject = $"transactions/{transactionId}",
                    EventData = JsonSerializer.Serialize(new
                    {
                        transactionId = transactionId.ToString(),
                        amount = 250.00m,
                        currency = "USD",
                        fromCardMasked = "****-****-****-9012",
                        toCardMasked = "****-****-****-1234",
                        processedAtUtc = now.AddSeconds(2)
                    }),
                    EventTime = now.AddSeconds(2),
                    RecordedAt = now.AddSeconds(2).AddMilliseconds(50)
                }
            };

            // Add another failed transaction for variety
            var failedTransactionId = Guid.NewGuid();
            auditLogs.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid().ToString(),
                EventType = "Transaction.Failed",
                EventSource = "urn:fintech:transactions",
                EventSubject = $"transactions/{failedTransactionId}",
                EventData = JsonSerializer.Serialize(new
                {
                    transactionId = failedTransactionId.ToString(),
                    amount = 100.00m,
                    currency = "USD",
                    fromCardMasked = "****-****-****-5678",
                    toCardMasked = "****-****-****-9999",
                    reason = "Insufficient funds",
                    failedAtUtc = now.AddSeconds(-60)
                }),
                EventTime = now.AddSeconds(-60),
                RecordedAt = now.AddSeconds(-60).AddMilliseconds(50)
            });

            await _dbContext.AuditEvents.AddRangeAsync(auditLogs);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("✅ Seeded {Count} audit log entries", auditLogs.Count);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                success = true,
                message = $"Seeded {auditLogs.Count} audit log entries",
                sampleTransactionId = transactionId.ToString(),
                entries = auditLogs.Select(a => new
                {
                    eventType = a.EventType,
                    eventSubject = a.EventSubject,
                    eventTime = a.EventTime
                })
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding audit logs: {Message}", ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to seed audit logs",
                error = ex.Message
            });
            return errorResponse;
        }
    }
}
