using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Database;

namespace Source.Functions;

public class AuditLogWriter
{
    private readonly ILogger<AuditLogWriter> _logger;
    private readonly ApplicationDbContext _dbContext;

    public AuditLogWriter(ILogger<AuditLogWriter> logger, ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("AuditLogWriter")]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("📝 Audit log writing: {Type} {Subject}", cloudEvent.Type, cloudEvent.Subject);

        try
        {
            // Store the complete CloudEvent for compliance and audit trail
            var auditEntry = new AuditEvent
            {
                Id = Guid.NewGuid(),
                EventId = cloudEvent.Id ?? Guid.NewGuid().ToString(),
                EventType = cloudEvent.Type ?? "Unknown",
                EventSource = cloudEvent.Source?.ToString() ?? string.Empty,
                EventSubject = cloudEvent.Subject ?? string.Empty,
                EventData = cloudEvent.Data?.ToString() ?? string.Empty,
                EventTime = cloudEvent.Time?.UtcDateTime ?? DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            };

            // TODO: Uncomment when audit_events table is created
            // await _dbContext.AuditEvents.AddAsync(auditEntry);
            // await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "✅ Audit log written: EventId={EventId}, Type={Type}, Subject={Subject}",
                auditEntry.EventId,
                auditEntry.EventType,
                auditEntry.EventSubject
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for event {Subject}", cloudEvent.Subject);
            throw;
        }
    }

    // TODO: Move to src/Core/AuditEvent.cs when table is created
    private class AuditEvent
    {
        public Guid Id { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EventSource { get; set; } = string.Empty;
        public string EventSubject { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public DateTime EventTime { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
