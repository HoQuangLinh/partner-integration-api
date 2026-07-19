namespace PartnerIntegration.Application.Abstractions.ExternalServices;

public class PartnerVerificationUnavailableException(Exception? innerException = null)
    : Exception("Partner verification is temporarily unavailable.", innerException);
