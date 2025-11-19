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
    [ServiceBusOutput("transactions", Connection = "ServiceBusConnection")]
    public async Task<string> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // Validates request and queues to Service Bus for async processing
        _logger.LogInformation("Received payment/transfer request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        
        if(string.IsNullOrWhiteSpace(requestBody))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Empty request body");
            return null!;
        }

        var transferRequest = JsonSerializer.Deserialize<TransferRequest>(requestBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        // Validate transfer request
        if (transferRequest == null || transferRequest.Amount <= 0)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid transfer data or amount must be greater than zero");
            return null!;
        }

        if (string.IsNullOrWhiteSpace(transferRequest.FromCardNumber) || 
            string.IsNullOrWhiteSpace(transferRequest.ToCardNumber))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Source and destination card numbers are required");
            return null!;
        }

        _logger.LogInformation(
            "Transfer request validated: {Amount} {Currency} from ****{From} to ****{To}",
            transferRequest.Amount,
            transferRequest.Currency ?? "USD",
            transferRequest.FromCardNumber[^4..],
            transferRequest.ToCardNumber[^4..]
        );

        // Return message body - Service Bus output binding will queue it
        string messageBody = JsonSerializer.Serialize(transferRequest);
        
        _logger.LogInformation("Returning transfer request for Service Bus queue");

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            success = true,
            message = "Transfer request queued for processing",
            fromCard = $"****{transferRequest.FromCardNumber[^4..]}",
            toCard = $"****{transferRequest.ToCardNumber[^4..]}",
            amount = transferRequest.Amount,
            currency = transferRequest.Currency ?? "USD"
        });

        return messageBody;
    }

    [Function("TransferMoney")]
    public async Task TransferMoneyFromQueue(
        [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] string messageBody)
    {
        _logger.LogInformation("Processing transfer from Service Bus queue");

        try
        {
            // Deserialize the transaction from queue message
            var transferRequest = JsonSerializer.Deserialize<TransferRequest>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (transferRequest == null)
            {
                _logger.LogError("Failed to deserialize transfer request from queue");
                throw new InvalidOperationException("Invalid transfer request in queue message");
            }

            _logger.LogInformation(
                "Processing transfer: {Amount} {Currency} from {From} to {To}",
                transferRequest.Amount,
                transferRequest.Currency ?? "USD",
                $"****{transferRequest.FromCardNumber[^4..]}",
                $"****{transferRequest.ToCardNumber[^4..]}"
            );

            // Execute the money transfer and update database
            var result = await _transferService.TransferMoneyAsync(
                transferRequest.FromCardNumber,
                transferRequest.ToCardNumber,
                transferRequest.Amount,
                transferRequest.Currency ?? "USD"
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "Transfer completed successfully: {TransactionId} - From balance: {FromBalance}, To balance: {ToBalance}",
                    result.TransactionId,
                    result.FromAccountNewBalance,
                    result.ToAccountNewBalance
                );
            }
            else
            {
                _logger.LogWarning(
                    "Transfer failed: {TransactionId} - {Message}",
                    result.TransactionId,
                    result.Message
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transfer from queue: {Message}", ex.Message);
            throw; // Re-throw to allow Service Bus retry logic
        }
    }

    private class TransferRequest
    {
        public string FromCardNumber { get; set; } = string.Empty;
        public string ToCardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
    }
}
