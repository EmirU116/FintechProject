using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Source.Core;

namespace Functions;

public class GetTestCards
{
    private readonly ILogger<GetTestCards> _logger;

    public GetTestCards(ILogger<GetTestCards> logger)
    {
        _logger = logger;
    }

    [Function("GetTestCards")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
    {
        _logger.LogInformation("Retrieving test credit cards");

        var testCards = DummyCreditCardService.GetAllTestCards();
        
        // Create anonymous objects to avoid circular references and show only relevant info
        var cardInfo = testCards.Select(card => new
        {
            CardNumber = card.CardNumber,
            CardNumberMasked = card.CardNumberMasked,
            CardHolderName = card.CardHolderName,
            Balance = card.Balance,
            CardType = card.CardType,
            ExpiryDate = card.ExpiryDate.ToString("MM/yy"),
            IsActive = card.IsActive,
            Status = DummyCreditCardService.GetDeclineReason(card.CardNumber)
        }).ToList();

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        
        var jsonString = JsonSerializer.Serialize(cardInfo, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        await response.WriteStringAsync(jsonString);
        return response;
    }
}