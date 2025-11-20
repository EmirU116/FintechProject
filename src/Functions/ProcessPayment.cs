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
    private readonly MoneyTransferService _transferService;

    public ProcessPayment(ILogger<ProcessPayment> logger, MoneyTransferService transferService)
    {
        _logger = logger;
        _transferService = transferService;
    }

    [Function("ProcessPayment")]
    public async Task<ProcessPaymentOutput> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Received payment/transfer request");

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
        _logger.LogInformation("Basic validation passed, queuing for detailed processing");

        _logger.LogInformation(
            "Transfer request validated: {Amount} {Currency} from ****{From} to ****{To}",
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
        
        _logger.LogInformation("Transfer request queued with ID: {TransactionId}", transaction.Id);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            success = true,
            message = "Transfer request queued for processing",
            transactionId = transaction.Id,
            fromCard = transaction.CardNumberMasked,
            toCard = transaction.ToCardNumberMasked,
            amount = transaction.Amount,
            currency = transaction.Currency
        });

        return new ProcessPaymentOutput 
        { 
            HttpResponse = response,
            ServiceBusMessage = messageBody
        };
    }

    // [Function("TransferMoney")]
    // public async Task TransferMoneyFromQueue(
    //     [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] string messageBody)
    // {
    //     _logger.LogInformation("Processing transfer from Service Bus queue");

    //     try
    //     {
    //         // Deserialize the transaction from queue message
    //         var transferRequest = JsonSerializer.Deserialize<TransferRequest>(messageBody, new JsonSerializerOptions
    //         {
    //             PropertyNameCaseInsensitive = true
    //         });

    //         if (transferRequest == null)
    //         {
    //             _logger.LogError("Failed to deserialize transfer request from queue");
    //             throw new InvalidOperationException("Invalid transfer request in queue message");
    //         }

    //         _logger.LogInformation(
    //             "Processing transfer: {Amount} {Currency} from {From} to {To}",
    //             transferRequest.Amount,
    //             transferRequest.Currency ?? "USD",
    //             $"****{transferRequest.FromCardNumber[^4..]}",
    //             $"****{transferRequest.ToCardNumber[^4..]}"
    //         );

    //         // Execute the money transfer and update database
    //         var result = await _transferService.TransferMoneyAsync(
    //             transferRequest.FromCardNumber,
    //             transferRequest.ToCardNumber,
    //             transferRequest.Amount,
    //             transferRequest.Currency ?? "USD"
    //         );

    //         if (result.Success)
    //         {
    //             _logger.LogInformation(
    //                 "Transfer completed successfully: {TransactionId} - From balance: {FromBalance}, To balance: {ToBalance}",
    //                 result.TransactionId,
    //                 result.FromAccountNewBalance,
    //                 result.ToAccountNewBalance
    //             );
    //         }
    //         else
    //         {
    //             _logger.LogWarning(
    //                 "Transfer failed: {TransactionId} - {Message}",
    //                 result.TransactionId,
    //                 result.Message
    //             );
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error processing transfer from queue: {Message}", ex.Message);
    //         throw; // Re-throw to allow Service Bus retry logic
    //     }
    // }

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
