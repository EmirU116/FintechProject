using System.Collections.Generic;

namespace Source.Core
{
    public class DummyCreditCard
    {
        public int Id { get; set; }
        public string CardNumber { get; init; } = "";
        public string CardHolderName { get; init; } = "";
        public decimal Balance { get; set; }
        public string CardType { get; init; } = "";
        public DateTime ExpiryDate { get; init; }
        public bool IsActive { get; init; } = true;
        
        public string CardNumberMasked => $"****-****-****-{CardNumber[^4..]}";
    }

    public static class DummyCreditCardService
    {
        private static readonly Dictionary<string, DummyCreditCard> _dummyCards = new()
        {
            // High Balance Cards (Successful transactions)
            ["4111111111111111"] = new DummyCreditCard 
            { 
                CardNumber = "4111111111111111", 
                CardHolderName = "John Doe", 
                Balance = 5000.00m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                IsActive = true
            },
            ["5555555555554444"] = new DummyCreditCard 
            { 
                CardNumber = "5555555555554444", 
                CardHolderName = "Jane Smith", 
                Balance = 3500.00m, 
                CardType = "Mastercard",
                ExpiryDate = DateTime.UtcNow.AddYears(3),
                IsActive = true
            },
            ["378282246310005"] = new DummyCreditCard 
            { 
                CardNumber = "378282246310005", 
                CardHolderName = "Bob Johnson", 
                Balance = 10000.00m, 
                CardType = "American Express",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },

            // Medium Balance Cards
            ["4000000000000002"] = new DummyCreditCard 
            { 
                CardNumber = "4000000000000002", 
                CardHolderName = "Alice Brown", 
                Balance = 250.00m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                IsActive = true
            },
            ["5105105105105100"] = new DummyCreditCard 
            { 
                CardNumber = "5105105105105100", 
                CardHolderName = "Charlie Wilson", 
                Balance = 750.00m, 
                CardType = "Mastercard",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },

            // Low Balance Cards (Insufficient funds)
            ["4000000000000010"] = new DummyCreditCard 
            { 
                CardNumber = "4000000000000010", 
                CardHolderName = "David Lee", 
                Balance = 25.00m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                IsActive = true
            },
            ["4000000000000051"] = new DummyCreditCard 
            { 
                CardNumber = "4000000000000051", 
                CardHolderName = "Emma Davis", 
                Balance = 10.50m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true
            },

            // Declined Cards (Various reasons)
            ["4000000000000002"] = new DummyCreditCard 
            { 
                CardNumber = "4000000000000002", 
                CardHolderName = "Frank Miller", 
                Balance = 1000.00m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddYears(2),
                IsActive = false  // Card blocked
            },
            ["4000000000000069"] = new DummyCreditCard 
            { 
                CardNumber = "4000000000000069", 
                CardHolderName = "Grace Taylor", 
                Balance = 500.00m, 
                CardType = "Visa",
                ExpiryDate = DateTime.UtcNow.AddMonths(-6), // Expired
                IsActive = true
            }
        };

        public static DummyCreditCard? GetCard(string cardNumber)
        {
            return _dummyCards.TryGetValue(cardNumber, out var card) ? card : null;
        }

        public static decimal GetBalance(string cardNumber)
        {
            var card = GetCard(cardNumber);
            return card?.Balance ?? 0m;
        }

        public static bool IsCardValid(string cardNumber)
        {
            var card = GetCard(cardNumber);
            return card != null && card.IsActive && card.ExpiryDate > DateTime.UtcNow;
        }

        public static string GetDeclineReason(string cardNumber)
        {
            var card = GetCard(cardNumber);
            
            if (card == null)
                return "INVALID_CARD";
            
            if (!card.IsActive)
                return "CARD_BLOCKED";
            
            if (card.ExpiryDate <= DateTime.UtcNow)
                return "EXPIRED_CARD";
            
            return "APPROVED";
        }

        public static List<DummyCreditCard> GetAllTestCards()
        {
            return _dummyCards.Values.ToList();
        }

        // For testing - simulate spending money
        public static bool ProcessPayment(string cardNumber, decimal amount)
        {
            var card = GetCard(cardNumber);
            if (card == null || !IsCardValid(cardNumber) || card.Balance < amount)
                return false;

            // In a real system, you'd update the balance in the database
            // For demo purposes, we'll just return success
            return true;
        }
    }
}