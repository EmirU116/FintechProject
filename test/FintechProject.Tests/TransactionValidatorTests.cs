using Source.Core.Transaction;
using FluentAssertions;
using Xunit;

namespace FintechProject.Tests
{
    /// <summary>
    /// UNIT TEST EXPLANATION:
    /// This class tests the TransactionValidator which checks if transactions are valid.
    /// 
    /// Think of it like a bouncer at a club - they check if you meet the requirements
    /// (age, dress code, etc.) before letting you in. The validator checks transactions
    /// before processing them.
    /// 
    /// Each test method (marked with [Fact]) is a separate test case.
    /// </summary>
    public class TransactionValidatorTests
    {
        // HELPER METHOD - This creates a valid transaction we can use in tests
        // We'll modify specific parts of it in each test to test different scenarios
        private Source.Core.Transaction.Transaction CreateValidTransaction()
        {
            return new Source.Core.Transaction.Transaction
            {
                Id = Guid.NewGuid(), // A unique ID
                CardNumber = "1234-5678-9012-3456", // Valid 16-digit card
                Amount = 100.00m, // Positive amount
                Currency = "USD", // Valid 3-letter currency code
                Timestamp = DateTime.UtcNow // Current time
            };
        }

        /// <summary>
        /// TEST #1: Valid Transaction
        /// WHAT IT DOES: Checks that a completely valid transaction passes validation
        /// WHY: We need to make sure our validator approves good transactions
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithValidData_ShouldReturnTrue()
        {
            // ARRANGE: Set up the test data
            var transaction = CreateValidTransaction();

            // ACT: Run the code we're testing
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT: Check if the result is what we expected
            result.IsValid.Should().BeTrue(); // Should pass validation
            result.Errors.Should().BeEmpty(); // Should have no error messages
        }

        /// <summary>
        /// TEST #2: Null Transaction
        /// WHAT IT DOES: Tests what happens if someone passes null instead of a transaction
        /// WHY: We need to handle edge cases gracefully without crashing
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithNullTransaction_ShouldReturnFalse()
        {
            // ARRANGE
            Source.Core.Transaction.Transaction? transaction = null;

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction!);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Transaction cannot be null");
        }

        /// <summary>
        /// TEST #3: Invalid Card Number - Too Short
        /// WHAT IT DOES: Tests that card numbers must be exactly 16 digits
        /// WHY: Credit cards always have 16 digits, so we reject anything else
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithShortCardNumber_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.CardNumber = "1234-5678-9012"; // Only 12 digits - TOO SHORT!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Card number must be exactly 16 digits (4x4 format)");
        }

        /// <summary>
        /// TEST #4: Invalid Card Number - Contains Letters
        /// WHAT IT DOES: Tests that card numbers must only contain digits
        /// WHY: Real credit cards only have numbers, not letters
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithLettersInCardNumber_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.CardNumber = "1234-5678-ABCD-3456"; // Has letters!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Card number must be exactly 16 digits (4x4 format)");
        }

        /// <summary>
        /// TEST #5: Card Number Without Dashes (Should Still Work)
        /// WHAT IT DOES: Tests that card numbers work with or without dashes
        /// WHY: Users might enter "1234567890123456" OR "1234-5678-9012-3456"
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithCardNumberWithoutDashes_ShouldReturnTrue()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.CardNumber = "1234567890123456"; // No dashes, but still valid

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// TEST #6: Negative Amount
        /// WHAT IT DOES: Tests that amounts must be positive
        /// WHY: You can't charge someone -$50, amounts must be greater than zero
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithNegativeAmount_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Amount = -50.00m; // Negative amount!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Amount must be greater than 0");
        }

        /// <summary>
        /// TEST #7: Zero Amount
        /// WHAT IT DOES: Tests that amount cannot be zero
        /// WHY: A $0 transaction doesn't make sense
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithZeroAmount_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Amount = 0m; // Zero!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Amount must be greater than 0");
        }

        /// <summary>
        /// TEST #8: Invalid Currency Code - Too Short
        /// WHAT IT DOES: Tests that currency codes must be exactly 3 letters
        /// WHY: All currency codes (USD, EUR, GBP) are exactly 3 letters
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithInvalidCurrencyLength_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Currency = "US"; // Only 2 letters - INVALID!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Currency must be a valid 3-letter code (e.g., USD, EUR, GBP)");
        }

        /// <summary>
        /// TEST #9: Invalid Currency Code - Not Recognized
        /// WHAT IT DOES: Tests that the currency must be a real currency
        /// WHY: "XYZ" isn't a real currency, we only accept real ones like USD, EUR
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithUnknownCurrency_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Currency = "XYZ"; // Not a real currency!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Currency must be a valid 3-letter code (e.g., USD, EUR, GBP)");
        }

        /// <summary>
        /// TEST #10: Valid Different Currencies
        /// WHAT IT DOES: Tests that we accept multiple valid currencies
        /// WHY: Our system should work with USD, EUR, GBP, etc.
        /// 
        /// [Theory] and [InlineData] let us run the same test with different values
        /// </summary>
        [Theory]
        [InlineData("USD")] // US Dollar
        [InlineData("EUR")] // Euro
        [InlineData("GBP")] // British Pound
        [InlineData("JPY")] // Japanese Yen
        [InlineData("CAD")] // Canadian Dollar
        public void ValidateTransaction_WithValidCurrencies_ShouldReturnTrue(string currency)
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Currency = currency;

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        /// <summary>
        /// TEST #11: Empty Transaction ID
        /// WHAT IT DOES: Tests that transaction must have a valid ID
        /// WHY: Every transaction needs a unique identifier to track it
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithEmptyGuid_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Id = Guid.Empty; // Empty ID - INVALID!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Transaction ID cannot be empty");
        }

        /// <summary>
        /// TEST #12: Future Timestamp
        /// WHAT IT DOES: Tests that transactions can't be dated in the future
        /// WHY: You can't make a transaction tomorrow - it has to be now or in the past
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithFutureTimestamp_ShouldReturnFalse()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Timestamp = DateTime.UtcNow.AddHours(2); // 2 hours in the future!

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Transaction timestamp cannot be in the future");
        }

        /// <summary>
        /// TEST #13: Multiple Errors at Once
        /// WHAT IT DOES: Tests that validator catches ALL errors, not just the first one
        /// WHY: If multiple things are wrong, user should know about all of them
        /// </summary>
        [Fact]
        public void ValidateTransaction_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.CardNumber = "123"; // TOO SHORT
            transaction.Amount = -10m; // NEGATIVE
            transaction.Currency = "XX"; // INVALID

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);

            // ASSERT
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(3); // Should have 3 different errors
            result.Errors.Should().Contain("Card number must be exactly 16 digits (4x4 format)");
            result.Errors.Should().Contain("Amount must be greater than 0");
            result.Errors.Should().Contain("Currency must be a valid 3-letter code (e.g., USD, EUR, GBP)");
        }

        /// <summary>
        /// TEST #14: GetErrorMessage Method
        /// WHAT IT DOES: Tests that we can get all errors as a single string
        /// WHY: Sometimes we want to show all errors in one message
        /// </summary>
        [Fact]
        public void ValidationResult_GetErrorMessage_ShouldJoinAllErrors()
        {
            // ARRANGE
            var transaction = CreateValidTransaction();
            transaction.Amount = 0m;
            transaction.Currency = "XX";

            // ACT
            var result = TransactionValidator.ValidateTransaction(transaction);
            var errorMessage = result.GetErrorMessage();

            // ASSERT
            errorMessage.Should().Contain("Amount must be greater than 0");
            errorMessage.Should().Contain(";"); // Errors should be joined with semicolons
            errorMessage.Should().Contain("Currency must be a valid 3-letter code");
        }
    }
}
