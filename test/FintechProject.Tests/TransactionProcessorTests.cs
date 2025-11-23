using Source.Core;
using FluentAssertions;
using Xunit;

namespace FintechProject.Tests
{
    /// <summary>
    /// UNIT TEST EXPLANATION - Testing Static Methods:
    /// 
    /// TransactionProcessor is different from MoneyTransferService because:
    /// 1. It uses STATIC methods (no need to create an instance)
    /// 2. It uses DummyCreditCardService (also static)
    /// 3. We don't need mocks here because we're testing the whole flow
    /// 
    /// This is more like an INTEGRATION test - testing multiple pieces together.
    /// But it's still a unit test because we're not touching a real database or API.
    /// </summary>
    public class TransactionProcessorTests
    {
        /// <summary>
        /// HELPER: Create a test transaction
        /// </summary>
        private Source.Core.Transaction.Transaction CreateTransaction(
            string cardNumber, 
            decimal amount, 
            string currency = "USD")
        {
            return new Source.Core.Transaction.Transaction
            {
                Id = Guid.NewGuid(),
                CardNumber = cardNumber,
                Amount = amount,
                Currency = currency,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// TEST #1: Successful Transaction with High Balance Card
        /// WHAT IT DOES: Tests a successful payment with a card that has enough money
        /// WHY: This is the normal case - everything works
        /// 
        /// NOTE: Uses real test cards from DummyCreditCardService
        /// Card "4111111111111111" has $5000 balance
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithValidCard_ShouldSucceed()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4111111111111111", // John Doe's card with $5000
                amount: 100m
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.Status.Should().Be("AUTHORIZED");
            result.Message.Should().Contain("authorized");
            result.RemainingBalance.Should().Be(4900m); // 5000 - 100
        }

        /// <summary>
        /// TEST #2: Transaction with Invalid Card
        /// WHAT IT DOES: Tests what happens with a card that doesn't exist
        /// WHY: System should reject cards that aren't in our database
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithInvalidCard_ShouldFail()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "9999999999999999", // This card doesn't exist
                amount: 50m
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeFalse();
            result.Status.Should().Be("INVALID_CARD");
            result.Message.Should().Contain("not found");
            result.RemainingBalance.Should().Be(0m);
        }

        /// <summary>
        /// TEST #3: Transaction with Insufficient Funds
        /// WHAT IT DOES: Tests when card doesn't have enough money
        /// WHY: Can't charge more than the card has
        /// 
        /// Card "4000000000000010" has only $25
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithInsufficientFunds_ShouldFail()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4000000000000010", // David Lee's card with only $25
                amount: 100m // Trying to charge $100
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeFalse();
            result.Status.Should().Be("INSUFFICIENT_FUNDS");
            result.Message.Should().Contain("Insufficient funds");
            result.Message.Should().Contain("25"); // Should mention available balance
            result.RemainingBalance.Should().Be(25m); // Balance unchanged
        }

        /// <summary>
        /// TEST #4: Transaction with Blocked Card
        /// WHAT IT DOES: Tests that blocked cards are rejected
        /// WHY: Blocked cards shouldn't be able to make purchases
        /// 
        /// Card "4000000000000002" is blocked (IsActive = false)
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithBlockedCard_ShouldFail()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4000000000000002", // Frank Miller's blocked card
                amount: 50m
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeFalse();
            result.Status.Should().Be("CARD_BLOCKED");
            result.Message.Should().Contain("declined");
        }

        /// <summary>
        /// TEST #5: Transaction with Expired Card
        /// WHAT IT DOES: Tests that expired cards are rejected
        /// WHY: Can't use a card past its expiration date
        /// 
        /// Card "4000000000000069" expired 6 months ago
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithExpiredCard_ShouldFail()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4000000000000069", // Grace Taylor's expired card
                amount: 50m
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeFalse();
            result.Status.Should().Be("EXPIRED_CARD");
            result.Message.Should().Contain("declined");
        }

        /// <summary>
        /// TEST #6: Large Amount Transaction
        /// WHAT IT DOES: Tests processing a large payment
        /// WHY: System should handle both small and large amounts
        /// 
        /// Card "378282246310005" has $10,000 balance
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithLargeAmount_ShouldSucceed()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "378282246310005", // Bob Johnson's AmEx with $10,000
                amount: 5000m // Large payment
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeTrue();
            result.Status.Should().Be("AUTHORIZED");
            result.RemainingBalance.Should().Be(5000m); // 10000 - 5000
        }

        /// <summary>
        /// TEST #7: Small Amount Transaction
        /// WHAT IT DOES: Tests processing a small payment
        /// WHY: System should handle small amounts like $0.01
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithSmallAmount_ShouldSucceed()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4111111111111111", // High balance card
                amount: 0.50m // 50 cents
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeTrue();
            result.RemainingBalance.Should().Be(4999.50m);
        }

        /// <summary>
        /// TEST #8: Exact Balance Transaction
        /// WHAT IT DOES: Tests charging exactly the card's balance
        /// WHY: Should be able to use all available funds
        /// 
        /// Card "4000000000000051" has exactly $10.50
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_WithExactBalance_ShouldSucceed()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4000000000000051", // Emma Davis's card with $10.50
                amount: 10.50m // Exact balance
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeTrue();
            result.RemainingBalance.Should().Be(0m); // Should have $0 left
        }

        /// <summary>
        /// TEST #9: Transaction Just Over Balance
        /// WHAT IT DOES: Tests charging $0.01 more than available
        /// WHY: Even $0.01 over should be rejected
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_JustOverBalance_ShouldFail()
        {
            // ARRANGE
            var transaction = CreateTransaction(
                cardNumber: "4000000000000051", // Card with $10.50
                amount: 10.51m // $0.01 too much
            );

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeFalse();
            result.Status.Should().Be("INSUFFICIENT_FUNDS");
        }

        /// <summary>
        /// TEST #10: Different Card Types Work
        /// WHAT IT DOES: Tests that Visa, Mastercard, and AmEx all work
        /// WHY: System should support multiple card types
        /// 
        /// [Theory] lets us run the same test with different inputs
        /// </summary>
        [Theory]
        [InlineData("4111111111111111", "Visa")]      // Visa
        [InlineData("5555555555554444", "Mastercard")] // Mastercard
        [InlineData("378282246310005", "American Express")] // AmEx
        public async Task ProcessTransaction_WithDifferentCardTypes_ShouldSucceed(
            string cardNumber, 
            string expectedCardType)
        {
            // ARRANGE
            var transaction = CreateTransaction(cardNumber, amount: 50m);

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeTrue();
            result.Message.Should().NotBeNullOrEmpty();
            
            // Verify the card type is correct
            var card = DummyCreditCardService.GetCard(cardNumber);
            card.Should().NotBeNull();
            card!.CardType.Should().Be(expectedCardType);
        }

        /// <summary>
        /// TEST #11: Multiple Sequential Transactions
        /// WHAT IT DOES: Tests multiple transactions on the same card
        /// WHY: In real systems, cards are used multiple times
        /// 
        /// NOTE: Balance calculations don't persist in our test service,
        /// so each transaction sees the original balance
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_MultipleTransactions_ShouldAllProcess()
        {
            // ARRANGE
            var cardNumber = "4111111111111111";
            var transaction1 = CreateTransaction(cardNumber, 100m);
            var transaction2 = CreateTransaction(cardNumber, 200m);
            var transaction3 = CreateTransaction(cardNumber, 50m);

            // ACT
            var result1 = await TransactionProcessor.ProcessTransaction(transaction1);
            var result2 = await TransactionProcessor.ProcessTransaction(transaction2);
            var result3 = await TransactionProcessor.ProcessTransaction(transaction3);

            // ASSERT - All should succeed
            result1.IsSuccessful.Should().BeTrue();
            result2.IsSuccessful.Should().BeTrue();
            result3.IsSuccessful.Should().BeTrue();

            // Each should have unique calculated balances
            result1.RemainingBalance.Should().Be(4900m);
            result2.RemainingBalance.Should().Be(4800m);
            result3.RemainingBalance.Should().Be(4950m);
        }

        /// <summary>
        /// TEST #12: Transaction Simulation Delay
        /// WHAT IT DOES: Tests that processor simulates network delay
        /// WHY: Real payment processors take time to respond
        /// 
        /// The processor has a Task.Delay(100) to simulate this
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_ShouldTakeTime()
        {
            // ARRANGE
            var transaction = CreateTransaction("4111111111111111", 100m);
            var startTime = DateTime.UtcNow;

            // ACT
            await TransactionProcessor.ProcessTransaction(transaction);
            var endTime = DateTime.UtcNow;

            // ASSERT - Should take at least 100ms
            var duration = endTime - startTime;
            duration.TotalMilliseconds.Should().BeGreaterOrEqualTo(95); // Allow small margin
        }

        /// <summary>
        /// TEST #13: Transaction Result Contains Card Holder Name
        /// WHAT IT DOES: Tests that result includes cardholder info
        /// WHY: Confirmation should show who the card belongs to
        /// </summary>
        [Fact]
        public async Task ProcessTransaction_ResultContainsCardHolderName()
        {
            // ARRANGE
            var transaction = CreateTransaction("4111111111111111", 100m);

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.Message.Should().Contain("John Doe"); // Card holder name
        }

        /// <summary>
        /// TEST #14: Transaction with Different Currencies
        /// WHAT IT DOES: Tests transactions in different currencies
        /// WHY: System should support USD, EUR, GBP, etc.
        /// 
        /// NOTE: Current processor doesn't validate currency,
        /// but it accepts it as part of the transaction
        /// </summary>
        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("GBP")]
        [InlineData("JPY")]
        public async Task ProcessTransaction_WithDifferentCurrencies_ShouldAccept(string currency)
        {
            // ARRANGE
            var transaction = CreateTransaction("4111111111111111", 100m, currency);

            // ACT
            var result = await TransactionProcessor.ProcessTransaction(transaction);

            // ASSERT
            result.IsSuccessful.Should().BeTrue();
            // Verify currency was preserved
            transaction.Currency.Should().Be(currency);
        }
    }
}
