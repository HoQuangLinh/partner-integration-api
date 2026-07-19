namespace PartnerIntegration.Application.Abstractions.ExternalServices;

public interface IPartnerVerificationService
{
    Task<bool> VerifyAsync(
        string partnerId,
        CancellationToken cancellationToken);
}
