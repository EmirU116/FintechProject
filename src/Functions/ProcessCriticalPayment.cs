using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Source.Core;
using Source.Core.Database;
using Source.Core.Eventing;
using Source.Core.Transaction;

namespace Source.Functions;

public class ProcessCriticalPayment
{
    private readonly ILogger<ProcessCriticalPayment> _logger;
    private readonly ITransactionRepository _transactionRepository;
    private readonly MoneyTransferService _moneyTransferService;
    private readonly IEventGridPublisher _eventPublisher;

    public ProcessCriticalPayment(
        ILogger<ProcessCriticalPayment> logger,
        ITransactionRepository transactionRepository,
        MoneyTransferService moneyTransferService,
        IEventGridPublisher eventPublisher)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
        _moneyTransferService = moneyTransferService;
        _eventPublisher = eventPublisher;
    }

    [Function(nameof(ProcessCriticalPayment))]
    public async Task Run(
        [ServiceBusTrigger("critical-payments", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var transaction = JsonSerializer.Deserialize<Transaction>(message.Body);

        if (transaction == null)
        {
            _logger.LogError("Failed to deserialize critical payment message");
            var dlqProperties = new Dictionary<string, object>
            {
                { "Reason", "DeserializationError" },
                { "Description", "Unable to deserialize message body" }
            };
            await messageActions.DeadLetterMessageAsync(message, dlqProperties);
            return;
        }

        _logger.LogInformation("Processing critical payment for TransactionId: {TransactionId}, Amount: {Amount}", 
            transaction.Id, transaction.Amount);

        try
        {
            // Execute money transfer
            var result = await _moneyTransferService.TransferMoneyAsync(
                transaction.CardNumber,
                transaction.ToCardNumber ?? string.Empty,
                transaction.Amount
            );

            // Store processed transaction
            var processedTransaction = new ProcessedTransaction
            {
                TransactionId = transaction.Id.ToString(),
                CardNumberMasked = transaction.CardNumberMasked,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                TransactionTimestamp = transaction.Timestamp,
                ProcessedAt = DateTime.UtcNow,
                AuthorizationStatus = result.Success ? "Completed" : "Failed",
                ProcessingMessage = result.Message ?? string.Empty
            };

            await _transactionRepository.SaveProcessedTransactionAsync(processedTransaction);

            // Complete the message
            await messageActions.CompleteMessageAsync(message);

            // Publish domain event
            if (result.Success)
            {
                _logger.LogInformation("Critical payment processed successfully: {TransactionId}", transaction.Id);
                var eventData = new
                {
                    TransactionId = transaction.Id.ToString(),
                    Type = "CriticalPayment",
                    CardNumber = transaction.CardNumberMasked,
                    ToCardNumber = transaction.ToCardNumberMasked ?? string.Empty,
                    Amount = transaction.Amount,
                    Status = "Completed",
                    ProcessedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishTransactionProcessedAsync(eventData, $"critical-payment/{transaction.Id}");
            }
            else
            {
                _logger.LogWarning("Critical payment failed: {TransactionId}, Reason: {Reason}", 
                    transaction.Id, result.Message);
                var eventData = new
                {
                    TransactionId = transaction.Id.ToString(),
                    Type = "CriticalPayment",
                    CardNumber = transaction.CardNumberMasked,
                    ToCardNumber = transaction.ToCardNumberMasked ?? string.Empty,
                    Amount = transaction.Amount,
                    Status = "Failed",
                    Reason = result.Message ?? "Unknown error",
                    FailedAt = DateTime.UtcNow
                };
                await _eventPublisher.PublishTransactionFailedAsync(eventData, $"critical-payment/{transaction.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing payment {TransactionId}", transaction.Id);

            // Check delivery count for retry logic
            if (message.DeliveryCount >= 10)
            {
                _logger.LogError("Critical payment {TransactionId} exceeded max retries, moving to DLQ", 
                    transaction.Id);
                var dlqProperties = new Dictionary<string, object>
                {
                    { "Reason", "MaxRetriesExceeded" },
                    { "Description", $"Failed after {message.DeliveryCount} attempts: {ex.Message}" }
                };
                await messageActions.DeadLetterMessageAsync(message, dlqProperties);
            }
            else
            {
                // Abandon to retry (will use exponential backoff configured on queue)
                _logger.LogWarning("Critical payment {TransactionId} failed, will retry (attempt {Attempt}/10)", 
                    transaction.Id, message.DeliveryCount);
                await messageActions.AbandonMessageAsync(message);
            }

            throw;
        }
    }
}
