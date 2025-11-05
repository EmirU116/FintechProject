
namespace Source.Core.Transaction
{
    public record Transaction
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string CardNumberMasked => $"****-****-****-{CardNumber[^4..]}";
        public string CardNumber { get; init; } = "";
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "USD";
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
