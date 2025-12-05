using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Transaction;
using Source.Core;
using Source.Core.Database;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Functions;

public class SettleTransaction
{
    private readonly ILogger<SettleTransaction> _logger;
    private readonly ITransactionRepository _transactionRepository;
    private readonly MoneyTransferService _transferService;
    private readonly EventGridPublisherClient? _eventGrid;

    public SettleTransaction(
        ILogger<SettleTransaction> logger, 
        ITransactionRepository transactionRepository,
        MoneyTransferService transferService,
        EventGridPublisherClient? eventGrid = null)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _transferService = transferService;
        _eventGrid = eventGrid;
    }
    
    [Function("SettleTransaction")]
    public async Task Run(
        [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] string messageBody)
    {
        _logger.LogInformation("🔵 ═══ SERVICE BUS TRIGGER FIRED ═══");
        _logger.LogInformation("🔵 Processing transaction from Service Bus queue");

        // Audit: Service Bus message received
        AuditLogger.LogAuditToConsole("SERVICE BUS TRIGGERED", "PENDING", new Dictionary<string, object>
        {
            { "Function", "SettleTransaction" },
            { "Queue", "transactions" },
            { "Timestamp", DateTime.UtcNow }
        });

        try
        {
            var transaction = JsonSerializer.Deserialize<TransactionMessage>(messageBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (transaction == null)
            {
                _logger.LogError("Failed to deserialize transaction from queue");
                AuditLogger.LogAuditFailure("DESERIALIZATION", "UNKNOWN", "Failed to deserialize transaction message");
                throw new InvalidOperationException("Invalid transaction in queue message");
            }

            _logger.LogInformation("🔵 Received transaction message from queue");
            _logger.LogInformation("🔵 Transaction ID: {TransactionId}", transaction.Id);

            // Audit: Transaction received from queue
            AuditLogger.LogAuditToConsole("TRANSACTION RECEIVED", transaction.Id.ToString(), new Dictionary<string, object>
            {
                { "Amount", transaction.Amount },
                { "Currency", transaction.Currency },
                { "FromCard", transaction.CardNumberMasked },
                { "ToCard", transaction.ToCardNumberMasked ?? "N/A" }
            });

            // Check if this is a transfer (has destination card)
            if (!string.IsNullOrEmpty(transaction.ToCardNumber))
            {
                _logger.LogInformation(
                    "🔵 Processing money transfer: {Amount} {Currency} from {From} to {To}",
                    transaction.Amount,
                    transaction.Currency,
                    transaction.CardNumberMasked,
                    transaction.ToCardNumberMasked
                );

                // Audit: Starting transfer processing
                AuditLogger.LogAuditToConsole("PROCESSING TRANSFER", transaction.Id.ToString(), new Dictionary<string, object>
                {
                    { "Stage", "Executing Money Transfer" },
                    { "FromCard", transaction.CardNumberMasked },
                    { "ToCard", transaction.ToCardNumberMasked ?? "N/A" },
                    { "Amount", transaction.Amount },
                    { "Currency", transaction.Currency }
                });

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
                        "🔵 ✓ Transfer completed successfully: {TransactionId}",
                        result.TransactionId
                    );
                    _logger.LogInformation(
                        "🔵 ✓ From balance: {FromBalance} | To balance: {ToBalance}",
                        result.FromAccountNewBalance,
                        result.ToAccountNewBalance
                    );
                    _logger.LogInformation("🔵 ✓ Database updated successfully");

                    // Audit: Transfer succeeded
                    AuditLogger.LogAuditSuccess("TRANSFER COMPLETED", result.TransactionId.ToString(), "Money transfer successful");
                    AuditLogger.LogAuditToConsole("DATABASE UPDATED", result.TransactionId.ToString(), new Dictionary<string, object>
                    {
                        { "FromBalance", result.FromAccountNewBalance },
                        { "ToBalance", result.ToAccountNewBalance },
                        { "Amount", transaction.Amount },
                        { "Currency", transaction.Currency },
                        { "Status", "SUCCESS" }
                    });

                    // Publish Transaction.Settled event to Event Grid
                    try
                    {
                        if (_eventGrid is not null)
                        {
                            var evtData = new
                            {
                                transactionId = result.TransactionId,
                                amount = transaction.Amount,
                                currency = transaction.Currency,
                                fromCardMasked = transaction.CardNumberMasked,
                                toCardMasked = transaction.ToCardNumberMasked,
                                processedAtUtc = DateTime.UtcNow
                            };

                            var cloudEvent = new CloudEvent(
                                source: "urn:fintech:transactions",
                                type: "Transaction.Settled",
                                data: BinaryData.FromObjectAsJson(evtData),
                                dataContentType: "application/json",
                                dataFormat: CloudEventDataFormat.Json)
                            {
                                Subject = $"transactions/{result.TransactionId}"
                            };

                            await _eventGrid.SendEventAsync(cloudEvent);
                            _logger.LogInformation("🔵 Published Event Grid event Transaction.Settled for {TransactionId}", result.TransactionId);
                        }
                        else
                        {
                            _logger.LogWarning("Event Grid publisher not configured; skipping event publish.");
                        }
                    }
                    catch (Exception egx)
                    {
                        _logger.LogError(egx, "Failed to publish Event Grid event for {TransactionId}", result.TransactionId);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "🔵 ✗ Transfer failed: {TransactionId} - {Message}",
                        result.TransactionId,
                        result.Message
                    );

                    // Audit: Transfer failed
                    AuditLogger.LogAuditFailure("TRANSFER FAILED", result.TransactionId.ToString(), result.Message);
                    AuditLogger.LogAuditToConsole("TRANSACTION FAILED", result.TransactionId.ToString(), new Dictionary<string, object>
                    {
                        { "Reason", result.Message },
                        { "Amount", transaction.Amount },
                        { "Currency", transaction.Currency },
                        { "FromCard", transaction.CardNumberMasked },
                        { "ToCard", transaction.ToCardNumberMasked ?? "N/A" },
                        { "Status", "FAILED" }
                    });
                    
                    // Optionally publish a failure event
                    try
                    {
                        if (_eventGrid is not null)
                        {
                            var evtData = new
                            {
                                transactionId = result.TransactionId,
                                amount = transaction.Amount,
                                currency = transaction.Currency,
                                fromCardMasked = transaction.CardNumberMasked,
                                toCardMasked = transaction.ToCardNumberMasked,
                                reason = result.Message,
                                failedAtUtc = DateTime.UtcNow
                            };

                            var cloudEvent = new CloudEvent(
                                source: "urn:fintech:transactions",
                                type: "Transaction.Failed",
                                data: BinaryData.FromObjectAsJson(evtData),
                                dataContentType: "application/json",
                                dataFormat: CloudEventDataFormat.Json)
                            {
                                Subject = $"transactions/{result.TransactionId}"
                            };

                            await _eventGrid.SendEventAsync(cloudEvent);
                            _logger.LogInformation("🟠 Published Event Grid event Transaction.Failed for {TransactionId}", result.TransactionId);
                        }
                    }
                    catch (Exception egx)
                    {
                        _logger.LogError(egx, "Failed to publish failure Event Grid event for {TransactionId}", result.TransactionId);
                    }
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