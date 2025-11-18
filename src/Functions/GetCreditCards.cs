using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core.Database;

namespace Functions
{
    public class GetCreditCards
    {
        private readonly ILogger<GetCreditCards> _logger;
        private readonly ICreditCardRepository _cardRepository;

        public GetCreditCards(
            ILogger<GetCreditCards> logger,
            ICreditCardRepository cardRepository)
        {
            _logger = logger;
            _cardRepository = cardRepository;
        }

        [Function("GetCreditCards")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "cards")] HttpRequest req)
        {
            _logger.LogInformation("Retrieving all credit cards");

            try
            {
                var cards = await _cardRepository.GetAllCardsAsync();
                
                var cardList = cards.Select(c => new
                {
                    cardNumberMasked = c.CardNumberMasked,
                    cardHolderName = c.CardHolderName,
                    balance = c.Balance,
                    cardType = c.CardType,
                    expiryDate = c.ExpiryDate,
                    isActive = c.IsActive
                }).ToList();

                return new OkObjectResult(new
                {
                    success = true,
                    count = cardList.Count,
                    cards = cardList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit cards");
                return new ObjectResult(new
                {
                    success = false,
                    message = "Error retrieving cards",
                    error = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
