using Xunit;
using PaymentApi.Functions;

public class PaymentTests
{
    [Fact]
    public void Transaction_Should_Have_MaskedCardNumber()
    {
        var transaction = new Transaction { CardNumber = "1234567890123456", Amount = 10.0m };
        Assert.Equal("****-****-****-3456", transaction.CardNumberMasked);
    }
}