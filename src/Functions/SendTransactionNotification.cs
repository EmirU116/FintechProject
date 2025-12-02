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

            // Integrate with notification services (simulated)
            // In production, replace with actual SendGrid/Twilio/Azure Communication Services
            var notificationTasks = new List<Task>();

            // Simulate Email notification
            notificationTasks.Add(SendEmailNotificationAsync(eventData, message));

            // Simulate SMS notification for high-value transactions
            if (eventData.Amount > 1000)
            {
                notificationTasks.Add(SendSmsNotificationAsync(eventData, message));
            }

            // Simulate Push notification
            notificationTasks.Add(SendPushNotificationAsync(eventData, message));

            await Task.WhenAll(notificationTasks);

            _logger.LogInformation("✅ Notification sent successfully for transaction {TransactionId}", eventData.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for event {Subject}", cloudEvent.Subject);
            throw; // Re-throw to trigger retry
        }
    }

    private async Task SendEmailNotificationAsync(TransactionEventData eventData, string message)
    {
        // Simulate email sending delay
        await Task.Delay(50);
        
        _logger.LogInformation(
            "📧 Email sent: To=cardholder@example.com, Subject=Transaction Alert, TransactionId={TransactionId}",
            eventData.TransactionId);
        
        // In production:
        // await _sendGridClient.SendEmailAsync(new SendGridMessage
        // {
        //     To = new[] { new EmailAddress(cardholderEmail) },
        //     Subject = "Transaction Alert",
        //     PlainTextContent = message,
        //     HtmlContent = $"<p>{message}</p>"
        // });
    }

    private async Task SendSmsNotificationAsync(TransactionEventData eventData, string message)
    {
        // Simulate SMS sending delay
        await Task.Delay(50);
        
        _logger.LogInformation(
            "📱 SMS sent: To=+1234567890, Message={Message}, TransactionId={TransactionId}",
            message.Substring(0, Math.Min(50, message.Length)),
            eventData.TransactionId);
        
        // In production:
        // await _twilioClient.SendMessageAsync(new CreateMessageOptions(new PhoneNumber(phoneNumber))
        // {
        //     From = new PhoneNumber(fromPhoneNumber),
        //     Body = message
        // });
    }

    private async Task SendPushNotificationAsync(TransactionEventData eventData, string message)
    {
        // Simulate push notification delay
        await Task.Delay(30);
        
        _logger.LogInformation(
            "🔔 Push notification sent: DeviceId=user_device_token, TransactionId={TransactionId}",
            eventData.TransactionId);
        
        // In production:
        // await _notificationHubClient.SendDirectNotificationAsync(new Dictionary<string, string>
        // {
        //     { "title", "Transaction Alert" },
        //     { "body", message },
        //     { "transactionId", eventData.TransactionId }
        // }, deviceToken);
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
