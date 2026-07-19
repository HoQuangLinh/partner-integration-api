namespace PartnerIntegration.Domain.Transactions;

public sealed record PartnerId
{
    public const int MaxLength = 50;

    public string Value { get; }

    private PartnerId(string value) => Value = value;

    public static PartnerId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Partner ID is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Partner ID must not exceed {MaxLength} characters.",
                nameof(value));
        }

        return new PartnerId(normalized);
    }

    public override string ToString() => Value;
}
