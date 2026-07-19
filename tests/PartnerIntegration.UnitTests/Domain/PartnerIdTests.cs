using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.UnitTests.Domain;

public class PartnerIdTests
{
    [Test]
    public void Create_WhenValueHasWhitespace_ShouldTrim()
    {
        var partnerId = PartnerId.Create(" P-1001 ");

        Assert.That(partnerId.Value, Is.EqualTo("P-1001"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WhenValueIsMissing_ShouldThrow(string value)
    {
        Action create = () => PartnerId.Create(value);

        Assert.Throws<ArgumentException>(create);
    }

    [Test]
    public void Create_WhenValueExceedsMaxLength_ShouldThrow()
    {
        Action create = () => PartnerId.Create(new string('P', PartnerId.MaxLength + 1));

        Assert.Throws<ArgumentException>(create);
    }
}
