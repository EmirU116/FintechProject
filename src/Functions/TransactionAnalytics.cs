using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions;

public class TransactionAnalytics
{
    private readonly ILogger<TransactionAnalytics> _logger;

    public TransactionAnalytics(ILogger<TransactionAnalytics> logger)
    {
        _logger = logger;
    }

    [Function("TransactionAnalytics")]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("📊 Analytics processing: {Type} {Subject}", cloudEvent.Type, cloudEvent.Subject);

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

            // Calculate metrics
            var hour = eventData.ProcessedAtUtc.Hour;
            var dayOfWeek = eventData.ProcessedAtUtc.DayOfWeek;

            _logger.LogInformation(
                "📊 Transaction metrics: Amount={Amount} {Currency}, Hour={Hour}, DayOfWeek={DayOfWeek}",
                eventData.Amount,
                eventData.Currency,
                hour,
                dayOfWeek
            );

            // TODO: Update transaction_metrics table with:
            // - Increment transaction count for the hour/day
            // - Add to total volume for the currency
            // - Calculate success/failure rates
            // - Track average transaction amounts

            // TODO: Push custom metrics to Application Insights
            // _telemetryClient.TrackMetric("TransactionVolume", eventData.Amount);
            // _telemetryClient.TrackMetric("TransactionsPerHour", 1);

            _logger.LogInformation("✅ Analytics updated for transaction {TransactionId}", eventData.TransactionId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process analytics for event {Subject}", cloudEvent.Subject);
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
