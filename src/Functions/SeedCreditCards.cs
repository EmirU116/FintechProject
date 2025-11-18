using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Source.Core;
using Source.Core.Database;

namespace Functions
{
    public class SeedCreditCards
    {
        private readonly ILogger<SeedCreditCards> _logger;
        private readonly ICreditCardRepository _cardRepository;

        public SeedCreditCards(
            ILogger<SeedCreditCards> logger,
            ICreditCardRepository cardRepository)
        {
            _logger = logger;
            _cardRepository = cardRepository;
        }

        [Function("SeedCreditCards")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "seed-cards")] HttpRequest req)
        {
            _logger.LogInformation("Seeding credit cards into database");

            try
            {
                var testCards = DummyCreditCardService.GetAllTestCards();
                var seededCount = 0;
                var skippedCount = 0;

                foreach (var card in testCards)
                {
                    // Check if card already exists
                    var exists = await _cardRepository.CardExistsAsync(card.CardNumber);
                    if (!exists)
                    {
                        await _cardRepository.SaveCardAsync(card);
                        seededCount++;
                        _logger.LogInformation("Seeded card: {CardNumber} - {CardHolder}", 
                            card.CardNumberMasked, card.CardHolderName);
                    }
                    else
                    {
                        skippedCount++;
                        _logger.LogInformation("Card already exists: {CardNumber}", card.CardNumberMasked);
                    }
                }

                return new OkObjectResult(new
                {
                    success = true,
                    message = $"Database seeding completed. {seededCount} cards added, {skippedCount} cards skipped (already exist).",
                    seededCount,
                    skippedCount,
                    totalCards = testCards.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding credit cards");
                return new ObjectResult(new
                {
                    success = false,
                    message = "Error seeding database",
                    error = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        }
    }
}
