using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Transaction;
using Source.Core;


namespace Source.Functions;

public class SettleTransaction
{
    private readonly ILogger<SettleTransaction> _logger;

    public SettleTransaction(ILogger<SettleTransaction> logger)
    {
        _logger = logger;
    }

    [Function("SettleTransaction")]
    public async Task Run(
        [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var messageBody = message.Body.ToString();
        _logger.LogInformation($"Received transaction message from queue: {messageBody}");

        try
        {
            var transaction = JsonSerializer.Deserialize<Transaction>(messageBody);

            if (transaction == null)
            {
                _logger.LogWarning("Received invalid transaction payload.");
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "InvalidPayload", deadLetterErrorDescription: "Transaction payload is null or invalid");
                return;
            }

            // Validate the transaction using the validation function
            var validationResult = TransactionValidator.ValidateTransaction(transaction);
            
            if (!validationResult.IsValid)
            {
                _logger.LogError($"Transaction validation failed for {transaction.Id}: {validationResult.GetErrorMessage()}");
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "ValidationFailed", deadLetterErrorDescription: validationResult.GetErrorMessage());
                return;
            }

            _logger.LogInformation($"Transaction {transaction.Id} passed validation");

            // Process the transaction through payment gateway (no balance checking needed)
            var transactionResult = await TransactionProcessor.ProcessTransaction(transaction);
            
            if (transactionResult.IsSuccessful)
            {
                _logger.LogInformation($"Transaction {transaction.Id} authorized successfully: {transactionResult.Message}");
                
                try
                {
                    // Proceed with settlement processing
                    await ProcessSettlement(transaction, transactionResult);
                    
                    // Complete message only after successful settlement
                    await messageActions.CompleteMessageAsync(message);
                    _logger.LogInformation($"Transaction {transaction.Id} completed and settled successfully");
                }
                catch (Exception settlementEx)
                {
                    _logger.LogError(settlementEx, $"Settlement processing failed for transaction {transaction.Id}");
                    await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "SettlementFailed", deadLetterErrorDescription: settlementEx.Message);
                }
            }
            else
            {
                _logger.LogError($"Transaction {transaction.Id} declined: {transactionResult.Message}");
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "TransactionDeclined", deadLetterErrorDescription: transactionResult.Message);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Failed to deserialize transaction message: {ex.Message}");
            await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "DeserializationError", deadLetterErrorDescription: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing transaction settlement");
            // Rethrow to let Azure Functions retry mechanism handle unexpected errors
            throw;
        }
    }

    private async Task ProcessSettlement(Transaction transaction, TransactionProcessor.TransactionResult result)
    {
        // Simulate async settlement processing
        _logger.LogInformation($"Processing settlement for transaction {transaction.Id}");
        
        // Log the transaction details
        _logger.LogInformation($"Authorization Status: {result.Status}");
        _logger.LogInformation($"Amount authorized: {transaction.Amount} {transaction.Currency}");
        
        // Add your actual settlement logic here:
        // - Save transaction to database
        // - Send confirmation to merchant
        // - Update audit logs
        // - Send receipt to customer
        await Task.Delay(100); // Simulate processing time
        
        _logger.LogInformation($"Successfully settled transaction {transaction.Id} with authorization status: {result.Status}");
    }
}