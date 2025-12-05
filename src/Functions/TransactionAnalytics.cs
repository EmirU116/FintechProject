using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Functions;

public class TransactionAnalytics
{
    private readonly ILogger<TransactionAnalytics> _logger;
    private readonly Source.Core.Database.ApplicationDbContext _dbContext;

    public TransactionAnalytics(
        ILogger<TransactionAnalytics> logger,
        Source.Core.Database.ApplicationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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

            // Update transaction_metrics table
            var metricDate = eventData.ProcessedAtUtc.Date;
            var metricKey = new { metricDate, hour, currency = eventData.Currency };

            var existingMetric = await _dbContext.TransactionMetrics
                .FirstOrDefaultAsync(m => 
                    m.MetricDate.Date == metricDate && 
                    m.Hour == hour && 
                    m.Currency == eventData.Currency);

            if (existingMetric != null)
            {
                // Update existing metric
                existingMetric.TransactionCount++;
                existingMetric.TotalVolume += eventData.Amount;
                existingMetric.AverageAmount = existingMetric.TotalVolume / existingMetric.TransactionCount;
                existingMetric.SuccessCount++; // Assuming this event means success
                existingMetric.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // Create new metric entry
                var newMetric = new Source.Core.TransactionMetric
                {
                    Id = Guid.NewGuid(),
                    MetricDate = metricDate,
                    Hour = hour,
                    DayOfWeek = dayOfWeek.ToString(),
                    Currency = eventData.Currency,
                    TransactionCount = 1,
                    TotalVolume = eventData.Amount,
                    AverageAmount = eventData.Amount,
                    SuccessCount = 1,
                    FailureCount = 0,
                    LastUpdated = DateTime.UtcNow
                };
                await _dbContext.TransactionMetrics.AddAsync(newMetric);
            }

            await _dbContext.SaveChangesAsync();

            // Push custom metrics to Application Insights
            var metrics = new Dictionary<string, double>
            {
                { "TransactionVolume", (double)eventData.Amount },
                { "TransactionsPerHour", 1 },
                { "Hour", hour },
                { "DayOfWeek", (int)dayOfWeek }
            };

            var properties = new Dictionary<string, object>
            {
                { "Currency", eventData.Currency },
                { "TransactionId", eventData.TransactionId },
                { "FromCard", eventData.FromCardMasked },
                { "ToCard", eventData.ToCardMasked }
            };

            foreach (var metric in metrics)
            {
                _logger.LogMetric(metric.Key, metric.Value, properties);
            }

            _logger.LogInformation(
                "✅ Analytics updated for transaction {TransactionId}: Count={Count}, Volume={Volume}, Avg={Avg}",
                eventData.TransactionId,
                existingMetric?.TransactionCount ?? 1,
                existingMetric?.TotalVolume ?? eventData.Amount,
                existingMetric?.AverageAmount ?? eventData.Amount);

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
