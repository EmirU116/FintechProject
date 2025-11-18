using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Source.Core.Database;

namespace Source.Functions;

public class GetProcessedTransactions
{
    private readonly ILogger<GetProcessedTransactions> _logger;
    private readonly ITransactionRepository _transactionRepository;

    public GetProcessedTransactions(ILogger<GetProcessedTransactions> logger, ITransactionRepository transactionRepository)
    {
        _logger = logger;
        _transactionRepository = transactionRepository;
    }

    [Function("GetProcessedTransactions")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("Retrieving all processed transactions from database");

        try
        {
            var transactions = await _transactionRepository.GetAllProcessedTransactionsAsync();
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            await response.WriteStringAsync(JsonSerializer.Serialize(transactions, jsonOptions));
            
            _logger.LogInformation($"Successfully retrieved {transactions.Count()} processed transactions");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processed transactions from database");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error retrieving transactions: {ex.Message}");
            return errorResponse;
        }
    }
}
