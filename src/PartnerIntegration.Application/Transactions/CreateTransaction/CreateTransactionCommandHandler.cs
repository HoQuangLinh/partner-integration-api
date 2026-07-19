using MediatR;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Application.Abstractions.Messaging;
using PartnerIntegration.Application.Common.Results;
using PartnerIntegration.Domain.Transactions;

namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public class CreateTransactionCommandHandler(
    IPartnerVerificationService partnerVerificationService,
    ITransactionMessagePublisher messagePublisher,
    TimeProvider timeProvider)
    : IRequestHandler<CreateTransactionCommand, Result<CreateTransactionResult>>
{
    public async Task<Result<CreateTransactionResult>> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var partnerId = request.PartnerId.Trim();

        if (!string.Equals(
                partnerId,
                request.AuthenticatedPartnerId.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<CreateTransactionResult>.Failure(CreateTransactionErrors.PartnerMismatch);
        }

        var isVerified = await partnerVerificationService
            .VerifyAsync(partnerId, cancellationToken);

        if (!isVerified)
        {
            return Result<CreateTransactionResult>.Failure(CreateTransactionErrors.PartnerNotVerified);
        }

        var transaction = PartnerTransaction.Create(
            partnerId,
            request.TransactionReference,
            request.Amount,
            request.Currency,
            request.Timestamp);
        var acceptedAtUtc = timeProvider.GetUtcNow();
        var message = transaction.ToIntegrationMessage(request.CorrelationId, acceptedAtUtc);

        await messagePublisher.PublishAsync(message, cancellationToken);

        return Result<CreateTransactionResult>.Success(
            new CreateTransactionResult(
                message.MessageId,
                transaction.TransactionReference.Value,
                acceptedAtUtc));
    }
}
