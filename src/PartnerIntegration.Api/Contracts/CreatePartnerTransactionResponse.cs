namespace PartnerIntegration.Api.Contracts;

public sealed record CreatePartnerTransactionResponse(
    Guid MessageId,
    string TransactionReference,
    DateTimeOffset AcceptedAtUtc);
