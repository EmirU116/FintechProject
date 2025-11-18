using Microsoft.EntityFrameworkCore;

namespace Source.Core.Database
{
    public class CreditCardRepository : ICreditCardRepository
    {
        private readonly ApplicationDbContext _context;

        public CreditCardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DummyCreditCard?> GetCardByNumberAsync(string cardNumber)
        {
            return await _context.CreditCards
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<IEnumerable<DummyCreditCard>> GetAllCardsAsync()
        {
            return await _context.CreditCards.ToListAsync();
        }

        public async Task UpdateCardBalanceAsync(DummyCreditCard card)
        {
            _context.CreditCards.Update(card);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCardAsync(DummyCreditCard card)
        {
            await _context.CreditCards.AddAsync(card);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CardExistsAsync(string cardNumber)
        {
            return await _context.CreditCards
                .AnyAsync(c => c.CardNumber == cardNumber);
        }
    }
}
