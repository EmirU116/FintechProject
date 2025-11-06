using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text;
using Source.Core.Transaction;

namespace Functions;

public class ProcessPayment
{
    private readonly ILogger<ProcessPayment> _logger;

    public ProcessPayment(ILogger<ProcessPayment> logger)
    {
        _logger = logger;
    }

    [Function("ProcessPayment")]
    [ServiceBusOutput("transactions", Connection = "ServiceBusConnection")]
    public async Task<string> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        // this handles basic validation, checking if the incoming data has the required data with values
        // if its either null or empty, the code will complain.
        _logger.LogInformation("Received payment request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        // checks if the request is null or has white space = fails
        if(string.IsNullOrWhiteSpace(requestBody))
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Empty request body");
            return null!;
        }

        var transaction = JsonSerializer.Deserialize<Transaction>(requestBody);
        // after packing out the json data, this checks if the transaction is null or if the amount cost is under
        // or equal to 0
        if (transaction == null || transaction.Amount <= 0)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid transaction data");
            return null!;
        }

        _logger.LogInformation($"Transaction {transaction.Id} validated. Amount: {transaction.Amount}");

        // Convert transaction object to JSON for the queue message
        string messageBody = JsonSerializer.Serialize(transaction);

        // sends the data to service bus queue
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Payment processed for card: {transaction.CardNumberMasked}");
        return messageBody;
    }
}
