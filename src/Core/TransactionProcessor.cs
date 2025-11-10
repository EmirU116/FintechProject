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
            
            // In real scenario: Send to payment gateway (Stripe, PayPal, etc.)
            // They handle authorization and return approve/decline
            var authorizationResult = await SimulatePaymentGatewayCall(transaction);
            
            if (!authorizationResult.IsApproved)
            {
                return new TransactionResult
                {
                    IsSuccessful = false,
                    Status = authorizationResult.DeclineReason,
                    Message = $"Transaction declined: {authorizationResult.DeclineReason}",
                    RemainingBalance = 0 // We don't know or need the actual balance
                };
            }

            // Transaction approved by payment gateway
            return new TransactionResult
            {
                IsSuccessful = true,
                Status = "AUTHORIZED",
                Message = $"Transaction authorized successfully. Amount: {transaction.Amount} {transaction.Currency}",
                RemainingBalance = 0 // Not applicable - we don't track customer balances
            };
        }

        private static async Task<AuthorizationResult> SimulatePaymentGatewayCall(Source.Core.Transaction.Transaction transaction)
        {
            // Simulate call to payment gateway (Stripe, Square, PayPal, etc.)
            await Task.Delay(50);
            
            // Simulate different authorization outcomes based on card number
            // In reality, this comes from the actual payment processor
            var lastDigit = int.Parse(transaction.CardNumber[^1..]);
            
            return lastDigit switch
            {
                0 or 1 => new AuthorizationResult { IsApproved = false, DeclineReason = "INSUFFICIENT_FUNDS" },
                2 => new AuthorizationResult { IsApproved = false, DeclineReason = "CARD_DECLINED" },
                3 => new AuthorizationResult { IsApproved = false, DeclineReason = "EXPIRED_CARD" },
                _ => new AuthorizationResult { IsApproved = true, DeclineReason = "" }
            };
        }

        private class AuthorizationResult
        {
            public bool IsApproved { get; init; }
            public string DeclineReason { get; init; } = "";
        }
    }
}