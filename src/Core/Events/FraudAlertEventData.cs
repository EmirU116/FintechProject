namespace Source.Core.Events;

public class FraudAlertEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string FromCardMasked { get; set; } = string.Empty;
    public string ToCardMasked { get; set; } = string.Empty;
    public DateTime TriggeredAtUtc { get; set; }
}
