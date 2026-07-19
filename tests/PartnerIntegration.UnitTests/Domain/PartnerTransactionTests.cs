using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.UnitTests.Domain;

public class PartnerTransactionTests
{
    private static readonly DateTimeOffset ValidTimestamp = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WhenInputIsValid_ShouldNormalizeFields()
    {
        var transaction = PartnerTransaction.Create(
            " P-1001 ",
            " TXN-99823 ",
            250m,
            "usd",
            ValidTimestamp);

        Assert.That(transaction.PartnerId.Value, Is.EqualTo("P-1001"));
        Assert.That(transaction.TransactionReference.Value, Is.EqualTo("TXN-99823"));
        Assert.That(transaction.Money.Amount, Is.EqualTo(250m));
        Assert.That(transaction.Money.Currency, Is.EqualTo("USD"));
        Assert.That(transaction.Timestamp, Is.EqualTo(ValidTimestamp));
    }

    [Test]
    public void Create_WhenTimestampIsMissing_ShouldThrow()
    {
        Action create = () => PartnerTransaction.Create("P-1001", "TXN-99823", 250m, "USD", default);

        Assert.Throws<ArgumentException>(create);
    }
}
