using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Source.Core.Transaction;

namespace Source.Functions;

public class SendCriticalPayment
{
    private readonly ILogger<SendCriticalPayment> _logger;
    private readonly IConfiguration _config;

    public SendCriticalPayment(ILogger<SendCriticalPayment> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    [Function("SendCriticalPayment")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "critical-payment")] HttpRequestData req)
    {
        _logger.LogInformation("🔴 ═══ HTTP POST /critical-payment RECEIVED ═══");

        // Deserialize the request body
        Transaction? transaction;
        try
        {
            transaction = await JsonSerializer.DeserializeAsync<Transaction>(req.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize critical payment request");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid request body");
            return badResponse;
        }

        if (transaction == null || transaction.Amount <= 0 || string.IsNullOrWhiteSpace(transaction.CardNumber))
        {
            _logger.LogWarning("Invalid critical payment request");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid transaction data");
            return badResponse;
        }

        // Set defaults
        transaction.Id = transaction.Id == Guid.Empty ? Guid.NewGuid() : transaction.Id;
        transaction.Timestamp = transaction.Timestamp == DateTime.MinValue ? DateTime.UtcNow : transaction.Timestamp;
        transaction.Currency = string.IsNullOrWhiteSpace(transaction.Currency) ? "USD" : transaction.Currency;

        // Mask card numbers for logging
        transaction.CardNumberMasked = MaskCardNumber(transaction.CardNumber);
        if (!string.IsNullOrWhiteSpace(transaction.ToCardNumber))
        {
            transaction.ToCardNumberMasked = MaskCardNumber(transaction.ToCardNumber);
        }

        _logger.LogInformation("Queueing critical payment: {TransactionId}, Amount: {Amount} {Currency}", 
            transaction.Id, transaction.Amount, transaction.Currency);

        try
        {
            // Send to Service Bus for guaranteed delivery
            var connectionString = _config["ServiceBusConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError("ServiceBusConnectionString not configured");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Service Bus not configured");
                return errorResponse;
            }

            await using var client = new ServiceBusClient(connectionString);
            await using var sender = client.CreateSender("critical-payments");

            var messageBody = JsonSerializer.Serialize(transaction);
            var message = new ServiceBusMessage(messageBody)
            {
                MessageId = transaction.Id.ToString(),
                ContentType = "application/json",
                Subject = "CriticalPayment"
            };

            await sender.SendMessageAsync(message);

            _logger.LogInformation("Critical payment queued successfully: {TransactionId}", transaction.Id);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                message = "Critical payment queued for processing",
                transactionId = transaction.Id,
                queueType = "ServiceBus",
                queue = "critical-payments"
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue critical payment {TransactionId}", transaction.Id);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Failed to queue payment: {ex.Message}");
            return errorResponse;
        }
    }

    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 4)
            return "****";

        return "****-****-****-" + cardNumber[^4..];
    }
}
