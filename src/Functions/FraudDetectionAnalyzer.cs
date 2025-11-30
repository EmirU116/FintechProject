using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions;

public class FraudDetectionAnalyzer
{
    private readonly ILogger<FraudDetectionAnalyzer> _logger;

    public FraudDetectionAnalyzer(ILogger<FraudDetectionAnalyzer> logger)
    {
        _logger = logger;
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

            // TODO: Add more sophisticated checks:
            // - Transaction velocity (multiple transfers in short time)
            // - Same destination card repeatedly
            // - Unusual time of day
            // - Geo-location checks
            // - Historical pattern analysis

            if (riskScore > 0)
            {
                _logger.LogWarning(
                    "⚠️ FRAUD ALERT: TransactionId={TransactionId}, RiskScore={RiskScore}, Alerts={Alerts}",
                    eventData.TransactionId,
                    riskScore,
                    string.Join("; ", alerts)
                );

                // TODO: Store alert in fraud_alerts table
                // TODO: Publish Fraud.AlertTriggered event if risk score > threshold
                // if (riskScore >= 50)
                // {
                //     await _eventGrid.SendEventAsync(new CloudEvent(...));
                // }
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
