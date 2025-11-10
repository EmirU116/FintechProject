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

        public static async Task<TransactionResult> ProcessTransaction(Source.Core.Transaction.Transaction transaction)
        {
            // Simulate payment gateway call (Stripe, Square, etc.)
            await Task.Delay(100); // Simulate network call to payment processor
            
            // Check if card exists in our dummy system
            var card = DummyCreditCardService.GetCard(transaction.CardNumber);
            if (card == null)
            {
                return new TransactionResult
                {
                    IsSuccessful = false,
                    Status = "INVALID_CARD",
                    Message = "Card number not found in system",
                    RemainingBalance = 0
                };
            }

            // Check card validity (active, not expired)
            var declineReason = DummyCreditCardService.GetDeclineReason(transaction.CardNumber);
            if (declineReason != "APPROVED")
            {
                return new TransactionResult
                {
                    IsSuccessful = false,
                    Status = declineReason,
                    Message = $"Transaction declined: {declineReason}",
                    RemainingBalance = card.Balance
                };
            }

            // Check sufficient funds
            if (card.Balance < transaction.Amount)
            {
                return new TransactionResult
                {
                    IsSuccessful = false,
                    Status = "INSUFFICIENT_FUNDS",
                    Message = $"Insufficient funds. Available: {card.Balance:C}, Required: {transaction.Amount:C}",
                    RemainingBalance = card.Balance
                };
            }

            // Transaction approved - simulate processing
            var remainingBalance = card.Balance - transaction.Amount;
            
            return new TransactionResult
            {
                IsSuccessful = true,
                Status = "AUTHORIZED",
                Message = $"Transaction authorized for {card.CardHolderName}. Amount: {transaction.Amount:C}",
                RemainingBalance = remainingBalance
            };
        }


    }
}