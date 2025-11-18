using Source.Core.Database;
using Source.Core.Transaction;

namespace Source.Core
{
    public class MoneyTransferService
    {
        private readonly ICreditCardRepository _cardRepository;
        private readonly ITransactionRepository _transactionRepository;

        public MoneyTransferService(
            ICreditCardRepository cardRepository, 
            ITransactionRepository transactionRepository)
        {
            _cardRepository = cardRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<TransferResult> TransferMoneyAsync(
            string fromCardNumber, 
            string toCardNumber, 
            decimal amount,
            string currency = "USD")
        {
            var transactionId = Guid.NewGuid().ToString();
            var timestamp = DateTime.UtcNow;

            try
            {
                // Validate amount
                if (amount <= 0)
                {
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Transfer amount must be greater than zero",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Get source card
                var fromCard = await _cardRepository.GetCardByNumberAsync(fromCardNumber);
                if (fromCard == null)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "INVALID_SOURCE_CARD", "Source card not found");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Source card not found",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Validate source card
                if (!fromCard.IsActive)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "CARD_BLOCKED", "Source card is blocked");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Source card is blocked",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                if (fromCard.ExpiryDate <= DateTime.UtcNow)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "EXPIRED_CARD", "Source card has expired");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Source card has expired",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Check sufficient balance
                if (fromCard.Balance < amount)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "INSUFFICIENT_FUNDS", $"Insufficient funds. Available: {fromCard.Balance:C}");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = $"Insufficient funds. Available balance: {fromCard.Balance:C}",
                        TransactionId = transactionId,
                        FromAccountNewBalance = fromCard.Balance,
                        TransferTimestamp = timestamp
                    };
                }

                // Get destination card
                var toCard = await _cardRepository.GetCardByNumberAsync(toCardNumber);
                if (toCard == null)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "INVALID_DESTINATION_CARD", "Destination card not found");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Destination card not found",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Validate destination card
                if (!toCard.IsActive)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "DESTINATION_BLOCKED", "Destination card is blocked");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Destination card is blocked",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Prevent self-transfer
                if (fromCardNumber == toCardNumber)
                {
                    await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                        timestamp, "INVALID_TRANSFER", "Cannot transfer to the same card");
                    
                    return new TransferResult
                    {
                        Success = false,
                        Message = "Cannot transfer money to the same card",
                        TransactionId = transactionId,
                        TransferTimestamp = timestamp
                    };
                }

                // Perform the transfer
                fromCard.Balance -= amount;
                toCard.Balance += amount;

                // Update both cards in the database
                await _cardRepository.UpdateCardBalanceAsync(fromCard);
                await _cardRepository.UpdateCardBalanceAsync(toCard);

                // Save successful transaction record
                await SaveSuccessfulTransaction(transactionId, fromCardNumber, toCardNumber, 
                    amount, currency, timestamp, fromCard.Balance, toCard.Balance);

                return new TransferResult
                {
                    Success = true,
                    Message = $"Successfully transferred {amount:C} from {fromCard.CardNumberMasked} to {toCard.CardNumberMasked}",
                    TransactionId = transactionId,
                    FromAccountNewBalance = fromCard.Balance,
                    ToAccountNewBalance = toCard.Balance,
                    TransferTimestamp = timestamp
                };
            }
            catch (Exception ex)
            {
                await SaveFailedTransaction(transactionId, fromCardNumber, amount, currency, 
                    timestamp, "SYSTEM_ERROR", $"Transfer failed: {ex.Message}");
                
                return new TransferResult
                {
                    Success = false,
                    Message = $"Transfer failed due to system error: {ex.Message}",
                    TransactionId = transactionId,
                    TransferTimestamp = timestamp
                };
            }
        }

        private async Task SaveSuccessfulTransaction(
            string transactionId,
            string fromCardNumber,
            string toCardNumber,
            decimal amount,
            string currency,
            DateTime timestamp,
            decimal fromNewBalance,
            decimal toNewBalance)
        {
            var transaction = new ProcessedTransaction
            {
                TransactionId = transactionId,
                CardNumberMasked = $"****-{fromCardNumber[^4..]} → ****-{toCardNumber[^4..]}",
                Amount = amount,
                Currency = currency,
                TransactionTimestamp = timestamp,
                ProcessedAt = DateTime.UtcNow,
                AuthorizationStatus = "APPROVED",
                ProcessingMessage = $"Transfer successful. From balance: {fromNewBalance:C}, To balance: {toNewBalance:C}"
            };

            await _transactionRepository.SaveProcessedTransactionAsync(transaction);
        }

        private async Task SaveFailedTransaction(
            string transactionId,
            string cardNumber,
            decimal amount,
            string currency,
            DateTime timestamp,
            string status,
            string message)
        {
            var transaction = new ProcessedTransaction
            {
                TransactionId = transactionId,
                CardNumberMasked = $"****-{cardNumber[^4..]}",
                Amount = amount,
                Currency = currency,
                TransactionTimestamp = timestamp,
                ProcessedAt = DateTime.UtcNow,
                AuthorizationStatus = status,
                ProcessingMessage = message
            };

            await _transactionRepository.SaveProcessedTransactionAsync(transaction);
        }
    }
}
