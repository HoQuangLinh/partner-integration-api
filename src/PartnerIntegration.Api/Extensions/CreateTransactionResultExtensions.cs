using PartnerIntegration.Api.Contracts;
using PartnerIntegration.Application.Transactions.CreateTransaction;

namespace PartnerIntegration.Api.Extensions;

public static class CreateTransactionResultExtensions
{
    public static CreatePartnerTransactionResponse ToResponse(
        this CreateTransactionResult result) =>
        new(
            result.MessageId,
            result.TransactionReference,
            result.AcceptedAtUtc);
}
