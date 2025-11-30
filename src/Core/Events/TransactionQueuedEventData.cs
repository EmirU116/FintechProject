namespace Source.Core.Events;

public class TransactionQueuedEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string FromCardMasked { get; set; } = string.Empty;
    public string ToCardMasked { get; set; } = string.Empty;
    public DateTime QueuedAtUtc { get; set; }
}
