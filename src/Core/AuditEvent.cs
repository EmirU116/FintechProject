namespace Source.Core;

public class AuditEvent
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
