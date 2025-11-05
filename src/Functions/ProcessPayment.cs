using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Functions;

public class ProcessPayment
{
    private readonly ILogger<ProcessPayment> _logger;

    public ProcessPayment(ILogger<ProcessPayment> logger)
    {
        _logger = logger;
    }

    [Function("ProcessPayment")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Received payment request");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var transaction = JsonSerializer.Deserialize<Transaction>(requestBody);

        if (transaction == null || transaction.Amount <= 0)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("Invalid transaction data");
            return badResponse;
        }

        _logger.LogInformation($"Transaction {transaction.Id} validated. Amount: {transaction.Amount}");

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync($"Payment processed for card: {transaction.CardNumberMasked}");
        return response;
    }
       public record Transaction
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string CardNumberMasked => $"****-****-****-{CardNumber[^4..]}";
        public string CardNumber { get; init; } = "";
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "USD";
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
