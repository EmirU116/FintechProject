namespace Source.Core;

public class FraudAlert
{
    public Guid Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string Alerts { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Reviewed, Confirmed, Dismissed
}
