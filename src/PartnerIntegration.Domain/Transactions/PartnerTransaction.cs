namespace PartnerIntegration.Domain.Transactions;

public sealed record PartnerTransaction(
    PartnerId PartnerId,
    TransactionReference TransactionReference,
    Money Money,
    DateTimeOffset Timestamp)
{
    public static PartnerTransaction Create(
        string partnerId,
        string transactionReference,
        decimal amount,
        string currency,
        DateTimeOffset timestamp)
    {
        if (timestamp == default)
        {
            throw new ArgumentException("Timestamp is required.", nameof(timestamp));
        }

        return new PartnerTransaction(
            PartnerId.Create(partnerId),
            TransactionReference.Create(transactionReference),
            Money.Create(amount, currency),
            timestamp);
    }
}
