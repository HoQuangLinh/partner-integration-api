using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.UnitTests.Domain;

public class TransactionReferenceTests
{
    [Test]
    public void Create_WhenValueHasWhitespace_ShouldTrim()
    {
        var reference = TransactionReference.Create(" TXN-99823 ");

        Assert.That(reference.Value, Is.EqualTo("TXN-99823"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WhenValueIsMissing_ShouldThrow(string value)
    {
        Action create = () => TransactionReference.Create(value);

        Assert.Throws<ArgumentException>(create);
    }

    [Test]
    public void Create_WhenValueExceedsMaxLength_ShouldThrow()
    {
        Action create = () =>
            TransactionReference.Create(new string('T', TransactionReference.MaxLength + 1));

        Assert.Throws<ArgumentException>(create);
    }
}
