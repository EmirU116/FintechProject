namespace Source.Core;

public class TransactionMetric
{
    public Guid Id { get; set; }
    public DateTime MetricDate { get; set; }
    public int Hour { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageAmount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
