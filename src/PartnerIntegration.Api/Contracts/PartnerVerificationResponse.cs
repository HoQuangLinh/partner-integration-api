namespace PartnerIntegration.Api.Contracts;

public sealed record PartnerVerificationResponse(string PartnerId, bool IsVerified);
