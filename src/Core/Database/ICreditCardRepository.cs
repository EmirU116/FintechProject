namespace Source.Core.Database
{
    public interface ICreditCardRepository
    {
        Task<DummyCreditCard?> GetCardByNumberAsync(string cardNumber);
        Task<IEnumerable<DummyCreditCard>> GetAllCardsAsync();
        Task UpdateCardBalanceAsync(DummyCreditCard card);
        Task SaveCardAsync(DummyCreditCard card);
        Task<bool> CardExistsAsync(string cardNumber);
    }
}
