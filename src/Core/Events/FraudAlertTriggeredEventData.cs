namespace Source.Core.Events;

public class FraudAlertTriggeredEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string[] Alerts { get; set; } = Array.Empty<string>();
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
