using Source.Core;
using Source.Core.Database;
using Source.Core.Transaction;
using FluentAssertions;
using Moq;
using Xunit;

namespace FintechProject.Tests
{
    /// <summary>
    /// ADVANCED UNIT TEST EXPLANATION - MOCKING:
    /// 
    /// This class tests MoneyTransferService which handles transferring money between cards.
    /// 
    /// WHAT IS MOCKING?
    /// Mocking is creating "fake" versions of dependencies. Think of it like this:
    /// - If you're testing a car's steering wheel, you don't need a real engine
    /// - You can use a "fake" engine that just does what you tell it to
    /// 
    /// WHY USE MOCKS?
    /// 1. We don't want to touch a real database during tests (too slow, can break things)
    /// 2. We want to control exactly what happens (make a card exist, or not exist)
    /// 3. We want to test ONE thing at a time (just the transfer logic, not the database)
    /// 
    /// In these tests, we use Moq to create fake repositories that behave how we tell them to.
    /// </summary>
    public class MoneyTransferServiceTests
    {
        // These are our "fake" repositories
        private readonly Mock<ICreditCardRepository> _mockCardRepository;
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;
        private readonly MoneyTransferService _service;

        /// <summary>
        /// CONSTRUCTOR - Runs before EACH test
        /// This sets up fresh mocks for every test so tests don't affect each other
        /// </summary>
        public MoneyTransferServiceTests()
        {
            // Create fresh mocks for each test
            _mockCardRepository = new Mock<ICreditCardRepository>();
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            
            // Create the service with our fake repositories
            _service = new MoneyTransferService(
                _mockCardRepository.Object, 
                _mockTransactionRepository.Object
            );
        }

        /// <summary>
        /// HELPER: Create a test credit card
        /// </summary>
        private DummyCreditCard CreateTestCard(
            string cardNumber, 
            decimal balance, 
            bool isActive = true, 
            int yearsUntilExpiry = 2)
        {
            return new DummyCreditCard
            {
                CardNumber = cardNumber,
                CardHolderName = "Test User",
                Balance = balance,
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(yearsUntilExpiry),
                IsActive = isActive
            };
        }

        /// <summary>
        /// TEST #1: Successful Transfer
        /// WHAT IT DOES: Tests a normal, successful money transfer
        /// WHY: This is the "happy path" - everything works correctly
        /// 
        /// MOCK SETUP:
        /// - We tell the fake repository to return specific cards when asked
        /// - We verify that the repository's save methods are called
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithValidCards_ShouldSucceed()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
            var toCard = CreateTestCard("5555666677778888", balance: 500m);
            var transferAmount = 100m;

            // SETUP MOCKS: Tell the fake repository what to return
            // When someone calls GetCardByNumberAsync with "1111222233334444", return fromCard
            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
                .ReturnsAsync(toCard);

            // ACT: Perform the transfer
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                transferAmount
            );

            // ASSERT: Check the results
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Successfully transferred");
            result.FromAccountNewBalance.Should().Be(900m); // 1000 - 100
            result.ToAccountNewBalance.Should().Be(600m);   // 500 + 100

            // VERIFY: Make sure the repository methods were called
            // This checks that the balances were actually updated in the "database"
            _mockCardRepository.Verify(
                x => x.UpdateCardBalanceAsync(It.IsAny<DummyCreditCard>()), 
                Times.Exactly(2) // Should be called twice (once for each card)
            );

            // Verify that the transaction was saved
            _mockTransactionRepository.Verify(
                x => x.SaveProcessedTransactionAsync(It.IsAny<ProcessedTransaction>()), 
                Times.Once
            );
        }

        /// <summary>
        /// TEST #2: Transfer with Zero Amount
        /// WHAT IT DOES: Tests that you can't transfer $0
        /// WHY: Zero-dollar transfers don't make sense
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithZeroAmount_ShouldFail()
        {
            // ARRANGE
            var transferAmount = 0m;

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                transferAmount
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("must be greater than zero");

            // VERIFY: Repository methods should NOT be called for invalid amount
            _mockCardRepository.Verify(
                x => x.GetCardByNumberAsync(It.IsAny<string>()), 
                Times.Never // Should never check cards if amount is invalid
            );
        }

        /// <summary>
        /// TEST #3: Transfer with Negative Amount
        /// WHAT IT DOES: Tests that you can't transfer negative money
        /// WHY: Negative amounts don't make sense
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithNegativeAmount_ShouldFail()
        {
            // ARRANGE
            var transferAmount = -50m;

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                transferAmount
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("must be greater than zero");
        }

        /// <summary>
        /// TEST #4: Source Card Not Found
        /// WHAT IT DOES: Tests what happens when the source card doesn't exist
        /// WHY: Can't transfer from a card that doesn't exist
        /// 
        /// MOCK SETUP: Return NULL when asked for the source card
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithInvalidSourceCard_ShouldFail()
        {
            // ARRANGE
            // Setup mock to return null (card not found)
            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync((DummyCreditCard?)null);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Source card not found");

            // VERIFY: Failed transaction should still be saved for audit trail
            _mockTransactionRepository.Verify(
                x => x.SaveProcessedTransactionAsync(It.IsAny<ProcessedTransaction>()), 
                Times.Once
            );
        }

        /// <summary>
        /// TEST #5: Source Card is Blocked
        /// WHAT IT DOES: Tests that blocked cards can't send money
        /// WHY: Blocked cards are frozen for security reasons
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithBlockedSourceCard_ShouldFail()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m, isActive: false);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("blocked");

            // VERIFY: Balance should NOT be updated for blocked card
            _mockCardRepository.Verify(
                x => x.UpdateCardBalanceAsync(It.IsAny<DummyCreditCard>()), 
                Times.Never
            );
        }

        /// <summary>
        /// TEST #6: Source Card is Expired
        /// WHAT IT DOES: Tests that expired cards can't send money
        /// WHY: Expired cards are no longer valid
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithExpiredSourceCard_ShouldFail()
        {
            // ARRANGE
            var fromCard = CreateTestCard(
                "1111222233334444", 
                balance: 1000m, 
                yearsUntilExpiry: -1 // Expired 1 year ago
            );

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("expired");
        }

        /// <summary>
        /// TEST #7: Insufficient Funds
        /// WHAT IT DOES: Tests that you can't transfer more than you have
        /// WHY: Can't spend money you don't have
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithInsufficientFunds_ShouldFail()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 50m);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            // ACT - Try to transfer more than the balance
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m // Trying to transfer $100 when only $50 available
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Insufficient funds");
            result.FromAccountNewBalance.Should().Be(50m); // Balance unchanged
        }

        /// <summary>
        /// TEST #8: Destination Card Not Found
        /// WHAT IT DOES: Tests what happens when destination card doesn't exist
        /// WHY: Can't send money to a card that doesn't exist
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithInvalidDestinationCard_ShouldFail()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            // Destination card returns null
            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
                .ReturnsAsync((DummyCreditCard?)null);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Destination card not found");
        }

        /// <summary>
        /// TEST #9: Destination Card is Blocked
        /// WHAT IT DOES: Tests that blocked cards can't receive money
        /// WHY: Blocked cards shouldn't be used at all
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithBlockedDestinationCard_ShouldFail()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
            var toCard = CreateTestCard("5555666677778888", balance: 500m, isActive: false);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
                .ReturnsAsync(toCard);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Destination card is blocked");
        }

        /// <summary>
        /// TEST #10: Self-Transfer (Same Card)
        /// WHAT IT DOES: Tests that you can't transfer money to yourself
        /// WHY: Transferring to the same card doesn't make sense
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_ToSameCard_ShouldFail()
        {
            // ARRANGE
            var card = CreateTestCard("1111222233334444", balance: 1000m);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(card);

            // ACT - Try to transfer to the same card
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "1111222233334444", // Same card!
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("same card");
        }

        /// <summary>
        /// TEST #11: Transaction ID is Generated
        /// WHAT IT DOES: Tests that every transfer gets a unique ID
        /// WHY: We need to track each transaction uniquely
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_ShouldGenerateTransactionId()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
            var toCard = CreateTestCard("5555666677778888", balance: 500m);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
                .ReturnsAsync(toCard);

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.TransactionId.Should().NotBeNullOrEmpty();
            result.TransferTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// TEST #12: Multiple Transfers Don't Interfere
        /// WHAT IT DOES: Tests that we can do multiple transfers sequentially
        /// WHY: In real systems, many transfers happen at once
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_MultipleTransfers_ShouldAllSucceed()
        {
            // ARRANGE
            var card1 = CreateTestCard("1111111111111111", balance: 1000m);
            var card2 = CreateTestCard("2222222222222222", balance: 500m);
            var card3 = CreateTestCard("3333333333333333", balance: 300m);

            _mockCardRepository.Setup(x => x.GetCardByNumberAsync("1111111111111111"))
                .ReturnsAsync(card1);
            _mockCardRepository.Setup(x => x.GetCardByNumberAsync("2222222222222222"))
                .ReturnsAsync(card2);
            _mockCardRepository.Setup(x => x.GetCardByNumberAsync("3333333333333333"))
                .ReturnsAsync(card3);

            // ACT - Do multiple transfers
            var result1 = await _service.TransferMoneyAsync("1111111111111111", "2222222222222222", 100m);
            var result2 = await _service.TransferMoneyAsync("2222222222222222", "3333333333333333", 50m);

            // ASSERT
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            result1.TransactionId.Should().NotBe(result2.TransactionId); // Different IDs
        }

        /// <summary>
        /// TEST #13: Exception Handling
        /// WHAT IT DOES: Tests that errors are handled gracefully
        /// WHY: If something goes wrong, we shouldn't crash - we should return an error
        /// 
        /// MOCK SETUP: Make the repository throw an exception
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // ARRANGE
            // Setup mock to throw an exception
            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // ACT
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m
            );

            // ASSERT
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("system error");
            result.Message.Should().Contain("Database connection failed");
        }

        /// <summary>
        /// TEST #14: Custom Currency
        /// WHAT IT DOES: Tests transferring with different currency
        /// WHY: System should support multiple currencies
        /// </summary>
        [Fact]
        public async Task TransferMoneyAsync_WithCustomCurrency_ShouldSucceed()
        {
            // ARRANGE
            var fromCard = CreateTestCard("1111222233334444", balance: 1000m);
            var toCard = CreateTestCard("5555666677778888", balance: 500m);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("1111222233334444"))
                .ReturnsAsync(fromCard);

            _mockCardRepository
                .Setup(x => x.GetCardByNumberAsync("5555666677778888"))
                .ReturnsAsync(toCard);

            // ACT - Transfer in EUR instead of USD
            var result = await _service.TransferMoneyAsync(
                "1111222233334444", 
                "5555666677778888", 
                100m,
                "EUR"
            );

            // ASSERT
            result.Success.Should().BeTrue();

            // Verify that the saved transaction has the correct currency
            _mockTransactionRepository.Verify(
                x => x.SaveProcessedTransactionAsync(
                    It.Is<ProcessedTransaction>(t => t.Currency == "EUR")
                ), 
                Times.Once
            );
        }
    }
}
