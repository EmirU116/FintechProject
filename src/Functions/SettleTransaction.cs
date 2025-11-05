using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Transaction;


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

            // Simulating business logic - like marking as settled, write to DB, audit log
            _logger.LogInformation($"Settling transaction: {transaction.Id} for amount {transaction.Amount} {transaction.Currency}");

            // Add your settlement logic here
            await ProcessSettlement(transaction);
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

    private async Task ProcessSettlement(Transaction transaction)
    {
        // Simulate async settlement processing
        _logger.LogInformation($"Processing settlement for transaction {transaction.Id}");
        
        // Add your actual settlement logic here
        await Task.Delay(100); // Simulate processing time
        
        _logger.LogInformation($"Successfully settled transaction {transaction.Id}");
    }
}