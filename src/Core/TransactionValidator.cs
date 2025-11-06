using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Source.Core.Transaction
{
    public static class TransactionValidator
    {
        public static ValidationResult ValidateTransaction(Transaction transaction)
        {
            var errors = new List<string>();

            if (transaction == null)
            {
                return new ValidationResult(false, new[] { "Transaction cannot be null" });
            }

            // Validate Card Number - must be exactly 16 digits (4x4 format)
            if (!ValidateCardNumber(transaction.CardNumber))
            {
                errors.Add("Card number must be exactly 16 digits (4x4 format)");
            }

            // Validate Amount - must be positive
            if (transaction.Amount <= 0)
            {
                errors.Add("Amount must be greater than 0");
            }

            // Validate Currency - must be valid 3-letter code
            if (!ValidateCurrency(transaction.Currency))
            {
                errors.Add("Currency must be a valid 3-letter code (e.g., USD, EUR, GBP)");
            }

            // Validate ID - must not be empty
            if (string.IsNullOrWhiteSpace(transaction.Id))
            {
                errors.Add("Transaction ID cannot be empty");
            }

            // Validate Timestamp - must not be in the future
            if (transaction.Timestamp > DateTime.UtcNow.AddMinutes(5)) // Allow 5 minutes tolerance for clock skew
            {
                errors.Add("Transaction timestamp cannot be in the future");
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        private static bool ValidateCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                return false;

            // Remove any spaces or dashes for validation
            var cleanCardNumber = cardNumber.Replace(" ", "").Replace("-", "");

            // Must be exactly 16 digits (4x4 format)
            return cleanCardNumber.Length == 16 && cleanCardNumber.All(char.IsDigit);
        }

        private static bool ValidateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return false;

            // Must be exactly 3 letters
            if (currency.Length != 3 || !currency.All(char.IsLetter))
                return false;

            // Common currency codes validation
            var validCurrencies = new HashSet<string>
            {
                "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NOK", "DKK", "PLN", "CZK", "HUF", "RON", "BGN", "HRK", "RUB", "TRY", "BRL", "MXN", "INR", "KRW", "SGD", "HKD", "NZD", "ZAR", "THB", "MYR", "IDR", "PHP", "VND"
            };

            return validCurrencies.Contains(currency.ToUpper());
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public ValidationResult(bool isValid, IEnumerable<string> errors)
        {
            IsValid = isValid;
            Errors = errors.ToList().AsReadOnly();
        }

        public string GetErrorMessage()
        {
            return string.Join("; ", Errors);
        }
    }
}