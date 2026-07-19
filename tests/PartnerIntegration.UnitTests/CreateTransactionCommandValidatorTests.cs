using PartnerIntegration.Application.Abstractions.ReferenceData;
using PartnerIntegration.Application.Transactions.CreateTransaction;

namespace PartnerIntegration.UnitTests;

public class CreateTransactionCommandValidatorTests
{
    private static readonly DateTimeOffset ValidTimestamp = new(2026, 7, 17, 8, 0, 0, TimeSpan.Zero);
    private readonly CreateTransactionCommandValidator _validator = new(new SupportedCurrencyProvider());

    [Test]
    public async Task Validate_WhenCommandIsValid_ShouldSucceed()
    {
        var command = CreateValidCommand();

        var result = await _validator.ValidateAsync(command);

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(0, TestName = "Validate_WhenAmountIsZero_ShouldRejectAmount")]
    [TestCase(-0.01, TestName = "Validate_WhenAmountIsNegative_ShouldRejectAmount")]
    public async Task Validate_WhenAmountIsNotPositive_ShouldRejectAmount(decimal amount)
    {
        var command = CreateValidCommand() with { Amount = amount };

        var result = await _validator.ValidateAsync(command);

        Assert.That(result.Errors.Any(error => error.PropertyName == nameof(command.Amount)), Is.True);
    }

    [TestCase("", TestName = "Validate_WhenPartnerIdIsEmpty_ShouldRejectPartnerId")]
    [TestCase("   ", TestName = "Validate_WhenPartnerIdIsWhitespace_ShouldRejectPartnerId")]
    public async Task Validate_WhenPartnerIdIsMissing_ShouldRejectPartnerId(string partnerId)
    {
        var command = CreateValidCommand() with { PartnerId = partnerId };

        var result = await _validator.ValidateAsync(command);

        Assert.That(result.Errors.Any(error => error.PropertyName == nameof(command.PartnerId)), Is.True);
    }

    [Test]
    public async Task Validate_WhenPartnerIdExceedsMaximumLength_ShouldRejectPartnerId()
    {
        var command = CreateValidCommand() with { PartnerId = new string('P', 51) };

        var result = await _validator.ValidateAsync(command);

        Assert.That(result.Errors.Any(error => error.PropertyName == nameof(command.PartnerId)), Is.True);
    }

    [TestCase("", TestName = "Validate_WhenTransactionReferenceIsEmpty_ShouldRejectReference")]
    [TestCase("   ", TestName = "Validate_WhenTransactionReferenceIsWhitespace_ShouldRejectReference")]
    public async Task Validate_WhenTransactionReferenceIsMissing_ShouldRejectReference(string reference)
    {
        var command = CreateValidCommand() with { TransactionReference = reference };

        var result = await _validator.ValidateAsync(command);

        Assert.That(
            result.Errors.Any(error => error.PropertyName == nameof(command.TransactionReference)),
            Is.True);
    }

    [Test]
    public async Task Validate_WhenTransactionReferenceExceedsMaximumLength_ShouldRejectReference()
    {
        var command = CreateValidCommand() with { TransactionReference = new string('T', 101) };

        var result = await _validator.ValidateAsync(command);

        Assert.That(
            result.Errors.Any(error => error.PropertyName == nameof(command.TransactionReference)),
            Is.True);
    }

    [TestCase("", TestName = "Validate_WhenCurrencyIsEmpty_ShouldRejectCurrency")]
    [TestCase("US", TestName = "Validate_WhenCurrencyLengthIsInvalid_ShouldRejectCurrency")]
    [TestCase("JPY", TestName = "Validate_WhenCurrencyIsUnsupported_ShouldRejectCurrency")]
    public async Task Validate_WhenCurrencyIsInvalid_ShouldRejectCurrency(string currency)
    {
        var command = CreateValidCommand() with { Currency = currency };

        var result = await _validator.ValidateAsync(command);

        Assert.That(result.Errors.Any(error => error.PropertyName == nameof(command.Currency)), Is.True);
    }

    [Test]
    public async Task Validate_WhenTimestampIsMissing_ShouldRejectTimestamp()
    {
        var command = CreateValidCommand() with { Timestamp = default };

        var result = await _validator.ValidateAsync(command);

        var error = result.Errors.Single(error => error.PropertyName == nameof(command.Timestamp));
        Assert.That(error.ErrorMessage, Is.EqualTo("Timestamp is required."));
    }

    private static CreateTransactionCommand CreateValidCommand() =>
        new("P-1001", "correlation-001", "P-1001", "TXN-99823", 250m, "usd", ValidTimestamp);

    private class SupportedCurrencyProvider : ISupportedCurrencyProvider
    {
        public bool IsSupported(string currencyCode) =>
            new[] { "USD", "EUR", "GBP", "VND" }.Contains(currencyCode, StringComparer.OrdinalIgnoreCase);
    }
}
