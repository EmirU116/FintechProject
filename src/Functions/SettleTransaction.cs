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

            // Get buyer's current balance
            var buyerBalance = await TransactionProcessor.GetBuyerBalance(transaction.CardNumber);
            _logger.LogInformation($"Retrieved buyer balance: {buyerBalance} {transaction.Currency}");

            // Process the business logic for the transaction
            var transactionResult = await TransactionProcessor.ProcessTransaction(transaction, buyerBalance);
            
            if (transactionResult.IsSuccessful)
            {
                _logger.LogInformation($"Transaction {transaction.Id} completed successfully: {transactionResult.Message}");
                _logger.LogInformation($"Buyer's remaining balance: {transactionResult.RemainingBalance} {transaction.Currency}");
                
                // Proceed with settlement processing
                await ProcessSettlement(transaction, transactionResult);
            }
            else
            {
                _logger.LogError($"Transaction {transaction.Id} failed: {transactionResult.Message}");
                // In a real scenario, you might want to send this to a failed transactions queue
                // or notify the buyer about insufficient funds
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
        _logger.LogInformation($"Settlement Status: {result.Status}");
        _logger.LogInformation($"Amount transferred: {transaction.Amount} {transaction.Currency}");
        _logger.LogInformation($"Buyer's remaining balance: {result.RemainingBalance} {transaction.Currency}");
        
        // Add your actual settlement logic here (e.g., database updates, audit logs, notifications)
        await Task.Delay(100); // Simulate processing time
        
        _logger.LogInformation($"Successfully settled transaction {transaction.Id} with status: {result.Status}");
    }
}