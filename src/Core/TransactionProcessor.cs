using Source.Core.Transaction;

namespace Source.Core
{
    public class TransactionProcessor
    {
        public class TransactionResult
        {
            public bool IsSuccessful { get; init; }
            public string Status { get; init; } = "";
            public string Message { get; init; } = "";
            public decimal RemainingBalance { get; init; }
        }

        public static async Task<TransactionResult> ProcessTransaction(Source.Core.Transaction.Transaction transaction, decimal buyerBalance = 1000m)
        {
            // Simulate async processing
            await Task.Delay(50);

            // Check if buyer has sufficient funds
            if (buyerBalance < transaction.Amount)
            {
                return new TransactionResult
                {
                    IsSuccessful = false,
                    Status = "INSUFFICIENT_FUNDS",
                    Message = $"Transaction failed: Insufficient funds. Required: {transaction.Amount} {transaction.Currency}, Available: {buyerBalance} {transaction.Currency}",
                    RemainingBalance = buyerBalance
                };
            }

            // Simulate processing the transaction
            // 1. Deduct money from buyer
            var newBalance = buyerBalance - transaction.Amount;
            
            // 2. Transfer money to company (simulated)
            // In a real scenario, this would involve actual payment processing
            
            // 3. Return successful result
            return new TransactionResult
            {
                IsSuccessful = true,
                Status = "SUCCESS",
                Message = $"Transaction completed successfully. Amount: {transaction.Amount} {transaction.Currency} transferred to company",
                RemainingBalance = newBalance
            };
        }

        public static async Task<decimal> GetBuyerBalance(string cardNumber)
        {
            // Simulate async database call to get buyer's balance
            await Task.Delay(25);
            
            // In a real scenario, this would query the actual balance from database
            // For demo purposes, we'll simulate different balances based on card number
            var lastDigit = int.Parse(cardNumber[^1..]);
            return lastDigit switch
            {
                0 or 1 or 2 => 50m,  // Low balance scenarios
                3 or 4 => 250m,      // Medium balance
                _ => 1500m           // High balance
            };
        }
    }
}