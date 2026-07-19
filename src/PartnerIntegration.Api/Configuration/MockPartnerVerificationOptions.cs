using System.ComponentModel.DataAnnotations;

namespace PartnerIntegration.Api.Configuration;

public class MockPartnerVerificationOptions
{
    public const string SectionName = "MockPartnerVerification";

    [Range(0, 1)]
    public double TimeoutProbability { get; init; }

    public string[] UnverifiedPartnerIds { get; init; } = [];
}
