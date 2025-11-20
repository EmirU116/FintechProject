
namespace Source.Core.Transaction
{
    public record Transaction
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CardNumberMasked { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime Timestamp { get; set; }
        
        // Add fields for transfers (destination card)
        public string? ToCardNumber { get; set; }
        public string? ToCardNumberMasked { get; set; }
    }
}
