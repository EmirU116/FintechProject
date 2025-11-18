namespace Source.Core
{
    public class TransferResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal FromAccountNewBalance { get; set; }
        public decimal ToAccountNewBalance { get; set; }
        public DateTime TransferTimestamp { get; set; }
    }
}
