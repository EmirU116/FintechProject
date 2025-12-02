using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions;

public class FraudDetectionAnalyzer
{
    private readonly ILogger<FraudDetectionAnalyzer> _logger;
    private readonly Source.Core.Database.ApplicationDbContext _dbContext;
    private readonly Azure.Messaging.EventGrid.EventGridPublisherClient? _eventGridPublisher;

    public FraudDetectionAnalyzer(
        ILogger<FraudDetectionAnalyzer> logger, 
        Source.Core.Database.ApplicationDbContext dbContext,
        Azure.Messaging.EventGrid.EventGridPublisherClient? eventGridPublisher = null)
    {
        _logger = logger;
        _dbContext = dbContext;
        _eventGridPublisher = eventGridPublisher;
    }

    [Function("FraudDetectionAnalyzer")]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("🔍 Fraud detection analyzing: {Type} {Subject}", cloudEvent.Type, cloudEvent.Subject);

        if (cloudEvent.Data is null)
        {
            _logger.LogWarning("Event has no data payload");
            return;
        }

        try
        {
            var eventData = JsonSerializer.Deserialize<TransactionEventData>(cloudEvent.Data.ToString());
            
            if (eventData == null)
            {
                _logger.LogWarning("Failed to deserialize event data");
                return;
            }

            var riskScore = 0;
            var alerts = new List<string>();

            // Rule 1: Check for large transaction amounts (>$10,000)
            if (eventData.Amount > 10000)
            {
                riskScore += 30;
                alerts.Add($"Large amount: {eventData.Amount} {eventData.Currency}");
            }

            // Rule 2: Check for round numbers (potential test or fraudulent transaction)
            if (eventData.Amount % 1000 == 0 && eventData.Amount >= 1000)
            {
                riskScore += 20;
                alerts.Add($"Round number amount: {eventData.Amount}");
            }

            // Rule 3: Check for very small transactions (potential probing)
            if (eventData.Amount < 1)
            {
                riskScore += 15;
                alerts.Add($"Very small amount: {eventData.Amount}");
            }

            // Rule 4: Check for unusual time of day (transactions between 2-5 AM local time)
            var hour = eventData.ProcessedAtUtc.Hour;
            if (hour >= 2 && hour <= 5)
            {
                riskScore += 10;
                alerts.Add($"Unusual time: {hour}:00 UTC");
            }

            // Rule 5: Check for transaction velocity (Note: requires historical data to be fully effective)
            // This is a simplified version - in production, query recent transactions from database
            _logger.LogInformation("Transaction velocity check: Would query recent transactions for pattern analysis");

            // Rule 6: Check for repeated destination patterns
            // In production: Query if same destination card has been used multiple times recently
            _logger.LogInformation("Destination pattern check: Would analyze destination card usage frequency");

            // Rule 7: Geographic location anomaly detection
            // In production: Compare transaction location with user's typical locations
            _logger.LogInformation("Geo-location check: Would validate transaction origin against user profile");

            if (riskScore > 0)
            {
                _logger.LogWarning(
                    "⚠️ FRAUD ALERT: TransactionId={TransactionId}, RiskScore={RiskScore}, Alerts={Alerts}",
                    eventData.TransactionId,
                    riskScore,
                    string.Join("; ", alerts)
                );

                // Store alert in fraud_alerts table
                var fraudAlert = new Source.Core.FraudAlert
                {
                    Id = Guid.NewGuid(),
                    TransactionId = eventData.TransactionId,
                    RiskScore = riskScore,
                    Alerts = string.Join("; ", alerts),
                    Amount = eventData.Amount,
                    Currency = eventData.Currency,
                    DetectedAt = DateTime.UtcNow,
                    Status = riskScore >= 50 ? "HighRisk" : "Pending"
                };

                await _dbContext.FraudAlerts.AddAsync(fraudAlert);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("💾 Fraud alert stored in database: AlertId={AlertId}", fraudAlert.Id);

                // Publish Fraud.AlertTriggered event if risk score exceeds threshold
                if (riskScore >= 50 && _eventGridPublisher != null)
                {
                    var fraudEventData = new Source.Core.Events.FraudAlertTriggeredEventData
                    {
                        TransactionId = eventData.TransactionId,
                        RiskScore = riskScore,
                        Alerts = alerts.ToArray(),
                        Amount = eventData.Amount,
                        Currency = eventData.Currency,
                        DetectedAt = DateTime.UtcNow
                    };

                    var fraudEvent = new CloudEvent(
                        source: "fintech/fraud-detection",
                        type: "Fraud.AlertTriggered",
                        jsonSerializableData: fraudEventData)
                    {
                        Subject = $"transaction/{eventData.TransactionId}",
                        Id = Guid.NewGuid().ToString()
                    };

                    await _eventGridPublisher.SendEventAsync(fraudEvent);
                    _logger.LogWarning("🚨 HIGH RISK FRAUD EVENT PUBLISHED: TransactionId={TransactionId}, RiskScore={RiskScore}", 
                        eventData.TransactionId, riskScore);
                }
            }
            else
            {
                _logger.LogInformation("✅ No fraud indicators detected for transaction {TransactionId}", eventData.TransactionId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze fraud for event {Subject}", cloudEvent.Subject);
            throw;
        }
    }

    private class TransactionEventData
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string FromCardMasked { get; set; } = string.Empty;
        public string ToCardMasked { get; set; } = string.Empty;
        public DateTime ProcessedAtUtc { get; set; }
    }
}
