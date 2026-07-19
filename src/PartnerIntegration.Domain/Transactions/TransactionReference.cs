namespace PartnerIntegration.Domain.Transactions;

public sealed record TransactionReference
{
    public const int MaxLength = 100;

    public string Value { get; }

    private TransactionReference(string value) => Value = value;

    public static TransactionReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Transaction reference is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"Transaction reference must not exceed {MaxLength} characters.",
                nameof(value));
        }

        return new TransactionReference(normalized);
    }

    public override string ToString() => Value;
}
