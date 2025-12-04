using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core;
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

            // Store audit event in database
            await _dbContext.AuditEvents.AddAsync(auditEntry);
            await _dbContext.SaveChangesAsync();

            // Log detailed audit information to console/terminal
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║ AUDIT LOG ENTRY                                                          ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ Event ID:      {auditEntry.EventId,-58} ║");
            Console.WriteLine($"║ Event Type:    {auditEntry.EventType,-58} ║");
            Console.WriteLine($"║ Subject:       {auditEntry.EventSubject,-58} ║");
            Console.WriteLine($"║ Source:        {auditEntry.EventSource,-58} ║");
            Console.WriteLine($"║ Event Time:    {auditEntry.EventTime:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
            Console.WriteLine($"║ Recorded At:   {auditEntry.RecordedAt:yyyy-MM-dd HH:mm:ss.fff} UTC{"",-38} ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ Event Data:                                                              ║");
            
            // Pretty print the event data
            try
            {
                var eventDataObj = JsonSerializer.Deserialize<object>(auditEntry.EventData);
                var prettyJson = JsonSerializer.Serialize(eventDataObj, new JsonSerializerOptions { WriteIndented = true });
                var lines = prettyJson.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Length > 70 ? line.Substring(0, 67) + "..." : line;
                    Console.WriteLine($"║ {trimmedLine,-72} ║");
                }
            }
            catch
            {
                // If JSON parsing fails, just print the raw data
                Console.WriteLine($"║ {auditEntry.EventData,-72} ║");
            }
            
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");

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

}
