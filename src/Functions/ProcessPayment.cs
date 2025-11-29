using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Source.Core;

namespace Functions;

public class ProcessPayment
{
    private readonly ILogger<ProcessPayment> _logger;

    public ProcessPayment(ILogger<ProcessPayment> logger)
    {
        _logger = logger;
    }

    [Function("ProcessPayment")]
    public async Task<ProcessPaymentOutput> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var traceId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("🟢 [TRACE:{TraceId}] ═══ HTTP TRIGGER ENTRY POINT ═══", traceId);
        _logger.LogInformation("🟢 [TRACE:{TraceId}] Received payment/transfer request", traceId);

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        
        if(string.IsNullOrWhiteSpace(requestBody))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { success = false, message = "Empty request body" });
            return new ProcessPaymentOutput { HttpResponse = badResponse };
        }

        var transferRequest = JsonSerializer.Deserialize<TransferRequest>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Validate transfer request structure
        if (transferRequest == null || transferRequest.Amount <= 0)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { success = false, message = "Invalid transfer data or amount must be greater than zero" });
            return new ProcessPaymentOutput { HttpResponse = badResponse };
        }

        if (string.IsNullOrWhiteSpace(transferRequest.FromCardNumber) || 
            string.IsNullOrWhiteSpace(transferRequest.ToCardNumber))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { success = false, message = "Source and destination card numbers are required" });
            return new ProcessPaymentOutput { HttpResponse = badResponse };
        }

        // Basic validation will be done by SettleTransaction
        // We only do minimal pre-validation here to catch obvious errors
        _logger.LogInformation("🟢 [TRACE:{TraceId}] Basic validation passed, queuing for detailed processing", traceId);

        _logger.LogInformation(
            "🟢 [TRACE:{TraceId}] Transfer request validated: {Amount} {Currency} from ****{From} to ****{To}",
            traceId,
            transferRequest.Amount,
            transferRequest.Currency ?? "USD",
            transferRequest.FromCardNumber[^4..],
            transferRequest.ToCardNumber[^4..]
        );

        // Convert to Transaction format for SettleTransaction
        var transaction = new TransactionMessage
        {
            Id = Guid.NewGuid(),
            CardNumber = transferRequest.FromCardNumber,
            CardNumberMasked = $"****-****-****-{transferRequest.FromCardNumber[^4..]}",
            Amount = transferRequest.Amount,
            Currency = transferRequest.Currency ?? "USD",
            Timestamp = DateTime.UtcNow,
            ToCardNumber = transferRequest.ToCardNumber,
            ToCardNumberMasked = $"****-****-****-{transferRequest.ToCardNumber[^4..]}"
        };

        string messageBody = JsonSerializer.Serialize(transaction);
        
        _logger.LogInformation("🟢 [TRACE:{TraceId}] Transfer request queued with Transaction ID: {TransactionId}", traceId, transaction.Id);
        _logger.LogInformation("🟢 [TRACE:{TraceId}] Message sent to Azure Service Bus queue 'transactions'", traceId);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            success = true,
            message = "Transfer request queued for processing",
            transactionId = transaction.Id,
            traceId = traceId,
            fromCard = transaction.CardNumberMasked,
            toCard = transaction.ToCardNumberMasked,
            amount = transaction.Amount,
            currency = transaction.Currency
        });

        _logger.LogInformation("🟢 [TRACE:{TraceId}] HTTP response sent to client", traceId);

        return new ProcessPaymentOutput 
        { 
            HttpResponse = response,
            ServiceBusMessage = messageBody
        };
    }

    private class TransferRequest
    {
        public string FromCardNumber { get; set; } = string.Empty;
        public string ToCardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
    }

    private class TransactionMessage
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CardNumberMasked { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime Timestamp { get; set; }
        public string? ToCardNumber { get; set; }
        public string? ToCardNumberMasked { get; set; }
    }

    public class ProcessPaymentOutput
    {
        [HttpResult]
        public HttpResponseData? HttpResponse { get; set; }

        [ServiceBusOutput("transactions", Connection = "ServiceBusConnection")]
        public string? ServiceBusMessage { get; set; }
    }
}
