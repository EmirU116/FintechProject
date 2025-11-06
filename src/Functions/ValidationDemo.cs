using System;
using Source.Core.Transaction;

namespace Source.Functions.Tests
{
    public static class ValidationDemo
    {
        public static void TestValidation()
        {
            Console.WriteLine("=== Transaction Validation Demo ===\n");

            // Test 1: Valid transaction
            var validTransaction = new Transaction
            {
                CardNumber = "1234567890123456", // Exactly 16 digits (4x4 format)
                Amount = 100.50m,
                Currency = "USD"
            };

            var result1 = TransactionValidator.ValidateTransaction(validTransaction);
            Console.WriteLine($"Valid Transaction Test: {(result1.IsValid ? "PASSED" : "FAILED")}");
            if (!result1.IsValid)
                Console.WriteLine($"Errors: {result1.GetErrorMessage()}");

            // Test 2: Invalid card number (too short)
            var invalidTransaction1 = new Transaction
            {
                CardNumber = "123456789012", // Only 12 digits - should fail (needs 16)
                Amount = 100.50m,
                Currency = "USD"
            };

            var result2 = TransactionValidator.ValidateTransaction(invalidTransaction1);
            Console.WriteLine($"\nInvalid Card Number (too short) Test: {(result2.IsValid ? "FAILED" : "PASSED")}");
            Console.WriteLine($"Errors: {result2.GetErrorMessage()}");

            // Test 3: Invalid card number (too long)
            var invalidTransaction2 = new Transaction
            {
                CardNumber = "12345678901234567890", // 20 digits - should fail (needs exactly 16)
                Amount = 100.50m,
                Currency = "USD"
            };

            var result3 = TransactionValidator.ValidateTransaction(invalidTransaction2);
            Console.WriteLine($"\nInvalid Card Number (too long) Test: {(result3.IsValid ? "FAILED" : "PASSED")}");
            Console.WriteLine($"Errors: {result3.GetErrorMessage()}");

            // Test 4: Invalid amount
            var invalidTransaction3 = new Transaction
            {
                CardNumber = "1234567890123456",
                Amount = -50.00m, // Negative amount - should fail
                Currency = "USD"
            };

            var result4 = TransactionValidator.ValidateTransaction(invalidTransaction3);
            Console.WriteLine($"\nInvalid Amount Test: {(result4.IsValid ? "FAILED" : "PASSED")}");
            Console.WriteLine($"Errors: {result4.GetErrorMessage()}");

            // Test 5: Invalid currency
            var invalidTransaction4 = new Transaction
            {
                CardNumber = "1234567890123456",
                Amount = 100.50m,
                Currency = "INVALID" // Invalid currency - should fail
            };

            var result5 = TransactionValidator.ValidateTransaction(invalidTransaction4);
            Console.WriteLine($"\nInvalid Currency Test: {(result5.IsValid ? "FAILED" : "PASSED")}");
            Console.WriteLine($"Errors: {result5.GetErrorMessage()}");
        }
    }
}