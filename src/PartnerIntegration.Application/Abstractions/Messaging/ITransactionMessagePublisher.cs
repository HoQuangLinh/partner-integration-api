namespace PartnerIntegration.Application.Abstractions.Messaging;

public interface ITransactionMessagePublisher
{
    Task PublishAsync(
        PartnerTransactionAcceptedV1 message,
        CancellationToken cancellationToken);
}
