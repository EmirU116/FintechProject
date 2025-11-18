namespace Source.Core.Transaction
{
    public class ProcessedTransaction
    {
        public int Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string CardNumberMasked { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime TransactionTimestamp { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string AuthorizationStatus { get; set; } = string.Empty;
        public string ProcessingMessage { get; set; } = string.Empty;
    }
}
