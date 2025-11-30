using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Source.Functions;

public class SendTransactionNotification
{
    private readonly ILogger<SendTransactionNotification> _logger;

    public SendTransactionNotification(ILogger<SendTransactionNotification> logger)
    {
        _logger = logger;
    }

    [Function("SendTransactionNotification")]
    public async Task Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        _logger.LogInformation("📧 Notification trigger: {Type} {Subject}", cloudEvent.Type, cloudEvent.Subject);

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

            // Determine notification message based on event type
            var message = cloudEvent.Type switch
            {
                "Transaction.Settled" => $"✅ Transfer completed: {eventData.Amount} {eventData.Currency} from {eventData.FromCardMasked} to {eventData.ToCardMasked}",
                "Transaction.Failed" => $"❌ Transfer failed: {eventData.Amount} {eventData.Currency}. Reason: {eventData.Reason ?? "Unknown"}",
                _ => $"Transaction event: {cloudEvent.Type}"
            };

            _logger.LogInformation("Notification message: {Message}", message);

            // TODO: Integrate with SendGrid/Twilio/Azure Communication Services
            // For now, just log the notification
            // Example:
            // await _emailService.SendEmailAsync(toEmail, subject, message);
            // await _smsService.SendSmsAsync(phoneNumber, message);

            // Simulate notification delay
            await Task.Delay(100);

            _logger.LogInformation("✅ Notification sent successfully for transaction {TransactionId}", eventData.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for event {Subject}", cloudEvent.Subject);
            throw; // Re-throw to trigger retry
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
        public string? Reason { get; set; }
    }
}
