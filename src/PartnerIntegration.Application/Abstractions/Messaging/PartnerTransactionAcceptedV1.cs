namespace PartnerIntegration.Application.Abstractions.Messaging;

public sealed record PartnerTransactionAcceptedV1(
    Guid MessageId,
    string MessageType,
    int MessageVersion,
    string CorrelationId,
    string PartnerId,
    string TransactionReference,
    decimal Amount,
    string Currency,
    DateTimeOffset TransactionTimestamp,
    DateTimeOffset AcceptedAtUtc);
