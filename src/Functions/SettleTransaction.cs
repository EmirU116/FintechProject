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
        [ServiceBusTrigger("transactions", Connection = "ServiceBusConnection")] string message)
    {
        _logger.LogInformation($"Received transaction message from queue: {message}");

        try
        {
            var transaction = JsonSerializer.Deserialize<Transaction>(message);

            if (transaction == null)
            {
                _logger.LogWarning("Received invalid transaction payload.");
                return;
            }

            // Validate the transaction using the validation function
            var validationResult = TransactionValidator.ValidateTransaction(transaction);
            
            if (!validationResult.IsValid)
            {
                _logger.LogError($"Transaction validation failed for {transaction.Id}: {validationResult.GetErrorMessage()}");
                // In a real scenario, you might want to send this to a dead letter queue
                // or handle the validation failure appropriately
                return;
            }

            _logger.LogInformation($"Transaction {transaction.Id} passed validation");

            // Process the transaction through payment gateway (no balance checking needed)
            var transactionResult = await TransactionProcessor.ProcessTransaction(transaction);
            
            if (transactionResult.IsSuccessful)
            {
                _logger.LogInformation($"Transaction {transaction.Id} authorized successfully: {transactionResult.Message}");
                
                // Proceed with settlement processing
                await ProcessSettlement(transaction, transactionResult);
            }
            else
            {
                _logger.LogError($"Transaction {transaction.Id} declined: {transactionResult.Message}");
                // In a real scenario, you might want to send this to a declined transactions queue
                // or handle the decline reason appropriately
                return;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Failed to deserialize transaction message: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing transaction settlement");
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