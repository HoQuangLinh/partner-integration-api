using MediatR;
using PartnerIntegration.Application.Common.Results;

namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public sealed record CreateTransactionCommand(
    string AuthenticatedPartnerId,
    string CorrelationId,
    string PartnerId,
    string TransactionReference,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp)
    : IRequest<Result<CreateTransactionResult>>;
