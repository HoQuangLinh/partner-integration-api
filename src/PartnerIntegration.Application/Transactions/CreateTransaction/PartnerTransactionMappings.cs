using PartnerIntegration.Application.Abstractions.Messaging;
using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public static class PartnerTransactionMappings
{
    public const string MessageType = "partner.transaction.accepted";
    public const int MessageVersion = 1;

    public static PartnerTransactionAcceptedV1 ToIntegrationMessage(
        this PartnerTransaction transaction,
        string correlationId,
        DateTimeOffset acceptedAtUtc) =>
        new(
            Guid.NewGuid(),
            MessageType,
            MessageVersion,
            correlationId,
            transaction.PartnerId.Value,
            transaction.TransactionReference.Value,
            transaction.Money.Amount,
            transaction.Money.Currency,
            transaction.Timestamp,
            acceptedAtUtc);
}
