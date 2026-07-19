namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public sealed record CreateTransactionResult(
    Guid MessageId,
    string TransactionReference,
    DateTimeOffset AcceptedAtUtc);
