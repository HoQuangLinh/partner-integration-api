namespace PartnerIntegration.Api.Security;

public class PartnerApiKeyOptions
{
    public const string SectionName = "PartnerApiKeys";

    public List<PartnerApiCredential> Credentials { get; init; } = [];
}

public class PartnerApiCredential
{
    public required string PartnerId { get; init; }

    public required string ApiKey { get; init; }
}
