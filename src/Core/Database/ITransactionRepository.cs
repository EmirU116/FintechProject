using Source.Core.Transaction;

namespace Source.Core.Database
{
    public interface ITransactionRepository
    {
        Task SaveProcessedTransactionAsync(ProcessedTransaction transaction);
        Task<IEnumerable<ProcessedTransaction>> GetAllProcessedTransactionsAsync();
        Task<ProcessedTransaction?> GetProcessedTransactionByIdAsync(string transactionId);
    }
}
