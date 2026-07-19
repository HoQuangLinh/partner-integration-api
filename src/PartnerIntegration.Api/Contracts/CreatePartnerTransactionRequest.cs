using PartnerIntegration.Application.Transactions.CreateTransaction;

namespace PartnerIntegration.Api.Contracts;

public sealed record CreatePartnerTransactionRequest(
    string? PartnerId,
    string? TransactionReference,
    decimal Amount,
    string? Currency,
    DateTimeOffset Timestamp)
{
    public CreateTransactionCommand ToCommand(
        string authenticatedPartnerId,
        string correlationId) =>
        new(
            authenticatedPartnerId,
            correlationId,
            PartnerId ?? string.Empty,
            TransactionReference ?? string.Empty,
            Amount,
            Currency ?? string.Empty,
            Timestamp);
}
