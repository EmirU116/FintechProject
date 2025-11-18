using Microsoft.EntityFrameworkCore;
using Source.Core.Transaction;

namespace Source.Core.Database
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveProcessedTransactionAsync(ProcessedTransaction transaction)
        {
            await _context.ProcessedTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProcessedTransaction>> GetAllProcessedTransactionsAsync()
        {
            return await _context.ProcessedTransactions
                .OrderByDescending(t => t.ProcessedAt)
                .ToListAsync();
        }

        public async Task<ProcessedTransaction?> GetProcessedTransactionByIdAsync(string transactionId)
        {
            return await _context.ProcessedTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }
    }
}
