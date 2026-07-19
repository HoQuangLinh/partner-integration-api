using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.UnitTests.Domain;

public class MoneyTests
{
    [Test]
    public void Create_WhenCurrencyIsLowercaseWithWhitespace_ShouldNormalize()
    {
        var money = Money.Create(250m, " usd ");

        Assert.That(money.Amount, Is.EqualTo(250m));
        Assert.That(money.Currency, Is.EqualTo("USD"));
    }

    [TestCase(0)]
    [TestCase(-0.01)]
    public void Create_WhenAmountIsNotPositive_ShouldThrow(decimal amount)
    {
        Action create = () => Money.Create(amount, "USD");

        Assert.Throws<ArgumentException>(create);
    }

    [TestCase("")]
    [TestCase("US")]
    [TestCase("USDD")]
    [TestCase("12A")]
    public void Create_WhenCurrencyFormatIsInvalid_ShouldThrow(string currency)
    {
        Action create = () => Money.Create(1m, currency);

        Assert.Throws<ArgumentException>(create);
    }
}
