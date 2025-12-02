using System.Text.Json;
using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Communication.Email;
using Azure.Communication.Sms;
using Azure;

namespace Source.Functions;

public class SendTransactionNotification
{
    private readonly ILogger<SendTransactionNotification> _logger;
    private readonly EmailClient? _emailClient;
    private readonly SmsClient? _smsClient;
    private readonly string? _emailSender;
    private readonly string? _emailTo;
    private readonly string? _smsFrom;
    private readonly string? _smsTo;

    public SendTransactionNotification(ILogger<SendTransactionNotification> logger)
    {
        _logger = logger;
        var acsConn = Environment.GetEnvironmentVariable("ACS_CONNECTION_STRING");
        _emailSender = Environment.GetEnvironmentVariable("EMAIL_SENDER_ADDRESS");
        _emailTo = Environment.GetEnvironmentVariable("NOTIFY_EMAIL_TO");
        _smsFrom = Environment.GetEnvironmentVariable("SMS_SENDER_NUMBER");
        _smsTo = Environment.GetEnvironmentVariable("NOTIFY_SMS_TO");

        if (!string.IsNullOrWhiteSpace(acsConn))
        {
            try
            {
                _emailClient = new EmailClient(acsConn);
                _smsClient = new SmsClient(acsConn);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize ACS clients");
            }
        }
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

            var notificationTasks = new List<Task>();

            // Email notification (ACS if configured, else simulate)
            notificationTasks.Add(SendEmailNotificationAsync(eventData, message));

            // SMS notification for high-value transactions (ACS if configured, else simulate)
            if (eventData.Amount > 1000)
            {
                notificationTasks.Add(SendSmsNotificationAsync(eventData, message));
            }

            // Optional simulated push notification
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
        if (_emailClient != null && !string.IsNullOrWhiteSpace(_emailSender) && !string.IsNullOrWhiteSpace(_emailTo))
        {
            try
            {
                var subject = "Transaction Alert";
                var html = $"<p>{message}</p><p>TransactionId: {eventData.TransactionId}</p>";
                var sendResult = await _emailClient.SendAsync(
                    WaitUntil.Completed,
                    senderAddress: _emailSender,
                    recipientAddress: _emailTo,
                    subject: subject,
                    htmlContent: html,
                    plainTextContent: message);

                _logger.LogInformation("📧 Email sent via ACS: To={To}, Status={Status}, TransactionId={TransactionId}", _emailTo, sendResult.Value.Status, eventData.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via ACS");
            }
        }
        else
        {
            await Task.Delay(50);
            _logger.LogInformation("📧 Email simulated: To={To}, TransactionId={TransactionId}", _emailTo ?? "cardholder@example.com", eventData.TransactionId);
        }
    }

    private async Task SendSmsNotificationAsync(TransactionEventData eventData, string message)
    {
        if (_smsClient != null && !string.IsNullOrWhiteSpace(_smsFrom) && !string.IsNullOrWhiteSpace(_smsTo))
        {
            try
            {
                var resp = await _smsClient.SendAsync(from: _smsFrom, to: _smsTo, message: message);
                _logger.LogInformation("📱 SMS sent via ACS: To={To}, Success={Success}, TransactionId={TransactionId}", _smsTo, resp.Value.Successful, eventData.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS via ACS");
            }
        }
        else
        {
            await Task.Delay(50);
            _logger.LogInformation("📱 SMS simulated: To={To}, TransactionId={TransactionId}", _smsTo ?? "+1234567890", eventData.TransactionId);
        }
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
