using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace FintechProject.IntegrationTests
{
    /// <summary>
    /// Integration tests for the complete payment flow.
    /// These tests verify the end-to-end functionality from HTTP request to database updates.
    /// 
    /// NOTE: These tests require:
    /// 1. Azure Functions running locally (func start)
    /// 2. PostgreSQL database running and accessible
    /// 3. Azure Service Bus emulator or real Service Bus namespace
    /// 4. Azure Event Grid emulator or real Event Grid topic (optional)
    /// </summary>
    public class PaymentFlowIntegrationTests : IAsyncLifetime
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://localhost:7071";

        public PaymentFlowIntegrationTests()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl)
            };
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", "test-key");
        }

        public async Task InitializeAsync()
        {
            // Setup: Seed test data before running tests
            var response = await _httpClient.PostAsync("/api/seed-cards", null);
            response.EnsureSuccessStatusCode();
        }

        public Task DisposeAsync()
        {
            _httpClient.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ProcessPayment_WithValidRequest_ShouldReturn202Accepted()
        {
            // ARRANGE
            var transferRequest = new
            {
                fromCardNumber = "4111111111111111",
                toCardNumber = "5555555555554444",
                amount = 100.00m,
                currency = "USD"
            };

            // ACT
            var response = await _httpClient.PostAsJsonAsync("/api/ProcessPayment", transferRequest);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            var result = await response.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.TransactionId.Should().NotBeEmpty();
            result.Message.Should().Contain("queued for processing");
        }

        [Fact]
        public async Task ProcessPayment_WithInvalidAmount_ShouldReturn400BadRequest()
        {
            // ARRANGE
            var transferRequest = new
            {
                fromCardNumber = "4111111111111111",
                toCardNumber = "5555555555554444",
                amount = -50.00m, // Invalid: negative amount
                currency = "USD"
            };

            // ACT
            var response = await _httpClient.PostAsJsonAsync("/api/ProcessPayment", transferRequest);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Contain("greater than zero");
        }

        [Fact]
        public async Task ProcessPayment_WithMissingCardNumber_ShouldReturn400BadRequest()
        {
            // ARRANGE
            var transferRequest = new
            {
                fromCardNumber = "", // Missing
                toCardNumber = "5555555555554444",
                amount = 100.00m,
                currency = "USD"
            };

            // ACT
            var response = await _httpClient.PostAsJsonAsync("/api/ProcessPayment", transferRequest);

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetCreditCards_ShouldReturnCardList()
        {
            // ACT
            var response = await _httpClient.GetAsync("/api/cards");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var result = await response.Content.ReadFromJsonAsync<GetCardsResponse>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Count.Should().BeGreaterThan(0);
            result.Cards.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetProcessedTransactions_ShouldReturnTransactionList()
        {
            // ACT
            var response = await _httpClient.GetAsync("/api/processed-transactions");

            // ASSERT
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var transactions = await response.Content.ReadFromJsonAsync<List<ProcessedTransaction>>();
            transactions.Should().NotBeNull();
        }

        [Fact(Skip = "Requires async processing - wait time needed")]
        public async Task CompletePaymentFlow_ShouldProcessSuccessfully()
        {
            // ARRANGE
            var transferRequest = new
            {
                fromCardNumber = "4111111111111111",
                toCardNumber = "5555555555554444",
                amount = 50.00m,
                currency = "USD"
            };

            // Get initial balances
            var initialCardsResponse = await _httpClient.GetAsync("/api/cards");
            var initialCards = await initialCardsResponse.Content.ReadFromJsonAsync<GetCardsResponse>();
            var fromCardInitial = initialCards!.Cards.First(c => c.CardNumberMasked.EndsWith("1111"));
            var toCardInitial = initialCards.Cards.First(c => c.CardNumberMasked.EndsWith("4444"));

            // ACT
            // Step 1: Initiate payment
            var paymentResponse = await _httpClient.PostAsJsonAsync("/api/ProcessPayment", transferRequest);
            paymentResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            var paymentResult = await paymentResponse.Content.ReadFromJsonAsync<ProcessPaymentResponse>();
            var transactionId = paymentResult!.TransactionId;

            // Step 2: Wait for async processing (Service Bus + SettleTransaction)
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Step 3: Verify transaction was processed
            var transactionsResponse = await _httpClient.GetAsync("/api/processed-transactions");
            var transactions = await transactionsResponse.Content.ReadFromJsonAsync<List<ProcessedTransaction>>();
            var processedTxn = transactions!.FirstOrDefault(t => t.Id == transactionId);

            // Step 4: Verify balances updated
            var finalCardsResponse = await _httpClient.GetAsync("/api/cards");
            var finalCards = await finalCardsResponse.Content.ReadFromJsonAsync<GetCardsResponse>();
            var fromCardFinal = finalCards!.Cards.First(c => c.CardNumberMasked.EndsWith("1111"));
            var toCardFinal = finalCards.Cards.First(c => c.CardNumberMasked.EndsWith("4444"));

            // ASSERT
            processedTxn.Should().NotBeNull();
            processedTxn!.Status.Should().Be("Success");
            processedTxn.Amount.Should().Be(50.00m);

            fromCardFinal.Balance.Should().Be(fromCardInitial.Balance - 50.00m);
            toCardFinal.Balance.Should().Be(toCardInitial.Balance + 50.00m);
        }

        // Response DTOs
        private class ProcessPaymentResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string TransactionId { get; set; } = string.Empty;
            public string TraceId { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
        }

        private class ErrorResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }

        private class GetCardsResponse
        {
            public bool Success { get; set; }
            public int Count { get; set; }
            public List<CreditCard> Cards { get; set; } = new();
        }

        private class CreditCard
        {
            public string CardNumberMasked { get; set; } = string.Empty;
            public string CardHolderName { get; set; } = string.Empty;
            public decimal Balance { get; set; }
            public string CardType { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        private class ProcessedTransaction
        {
            public string Id { get; set; } = string.Empty;
            public string CardNumberMasked { get; set; } = string.Empty;
            public string ToCardNumberMasked { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? ValidationMessage { get; set; }
            public DateTime ProcessedAt { get; set; }
        }
    }
}
