using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Transaction;
using Source.Core;
using Source.Core.Database;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Source.Functions;

public class SettleTransaction
{
    private readonly ILogger<SettleTransaction> _logger;
    private readonly ITransactionRepository _transactionRepository;
    private readonly MoneyTransferService _transferService;

    public SettleTransaction(
        ILogger<SettleTransaction> logger, 
        ITransactionRepository transactionRepository,
        MoneyTransferService transferService)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _transferService = transferService;
    }
    
    [Function("SettleTransaction")]
    public async Task Run(
        [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] string messageBody)
    {
        _logger.LogInformation("Processing transaction from Service Bus queue");

        try
        {
            var transaction = JsonSerializer.Deserialize<TransactionMessage>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (transaction == null)
            {
                _logger.LogError("Failed to deserialize transaction from queue");
                throw new InvalidOperationException("Invalid transaction in queue message");
            }

            _logger.LogInformation("Received transaction message from queue: {Message}", messageBody);

            // Check if this is a transfer (has destination card)
            if (!string.IsNullOrEmpty(transaction.ToCardNumber))
            {
                _logger.LogInformation(
                    "Processing money transfer: {Amount} {Currency} from {From} to {To}",
                    transaction.Amount,
                    transaction.Currency,
                    transaction.CardNumberMasked,
                    transaction.ToCardNumberMasked
                );

                // Execute the money transfer
                var result = await _transferService.TransferMoneyAsync(
                    transaction.CardNumber,
                    transaction.ToCardNumber,
                    transaction.Amount,
                    transaction.Currency
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
            else
            {
                _logger.LogInformation("Processing regular transaction for {Card}", transaction.CardNumberMasked);
                // Handle regular transactions here if needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction from queue: {Message}", ex.Message);
            throw; // Re-throw to allow Service Bus retry logic
        }
    } 

    // [Function("SettleTransaction")]
    // public async Task Run(
    //     [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
    //     ServiceBusMessageActions messageActions)
    // {
    //            _logger.LogInformation("Received payment/transfer request");

    //     string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        
    //     if(string.IsNullOrWhiteSpace(requestBody))
    //     {
    //         _logger.LogWarning("Empty request body received");
    //         return string.Empty;
    //     }

    //     var transferRequest = JsonSerializer.Deserialize<TransferRequest>(requestBody, new JsonSerializerOptions
    //     {
    //         PropertyNameCaseInsensitive = true
    //     });

    //     if (transferRequest == null || transferRequest.Amount <= 0)
    //     {
    //         _logger.LogWarning("Invalid transfer request or amount");
    //         return string.Empty;
    //     }

    //     if (string.IsNullOrWhiteSpace(transferRequest.FromCardNumber) || 
    //         string.IsNullOrWhiteSpace(transferRequest.ToCardNumber))
    //     {
    //         _logger.LogWarning("Missing card numbers in request");
    //         return string.Empty;
    //     }

    //     // Validate and check balances
    //     var validationResult = await _transferService.ValidateTransferAsync(
    //         transferRequest.FromCardNumber,
    //         transferRequest.ToCardNumber,
    //         transferRequest.Amount
    //     );

    //     if (!validationResult.Success)
    //     {
    //         _logger.LogWarning($"Transfer validation failed: {validationResult.Message}");
    //         return string.Empty;
    //     }

    //     // Convert TransferRequest to Transaction format for SettleTransaction
    //     var transaction = new Transaction
    //     {
    //         Id = Guid.NewGuid(),
    //         CardNumber = transferRequest.FromCardNumber,
    //         CardNumberMasked = $"****-****-****-{transferRequest.FromCardNumber[^4..]}",
    //         Amount = transferRequest.Amount,
    //         Currency = transferRequest.Currency ?? "USD",
    //         Timestamp = DateTime.UtcNow,
    //         ToCardNumber = transferRequest.ToCardNumber, // Add destination card
    //         ToCardNumberMasked = $"****-****-****-{transferRequest.ToCardNumber[^4..]}"
    //     };

    //     var message = JsonSerializer.Serialize(transaction);
        
    //     _logger.LogInformation($"Transfer validated and queued: {transferRequest.Amount} {transferRequest.Currency ?? "USD"} from {transaction.CardNumberMasked} to {transaction.ToCardNumberMasked}");

    //     return message; 
    // }

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
}