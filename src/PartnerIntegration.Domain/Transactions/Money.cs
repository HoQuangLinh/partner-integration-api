namespace PartnerIntegration.Domain.Transactions;

public sealed record Money
{
    public const int CurrencyCodeLength = 3;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != CurrencyCodeLength
            || !normalizedCurrency.All(char.IsAsciiLetter))
        {
            throw new ArgumentException(
                "Currency must be a three-letter code.",
                nameof(currency));
        }

        return new Money(amount, normalizedCurrency);
    }
}
