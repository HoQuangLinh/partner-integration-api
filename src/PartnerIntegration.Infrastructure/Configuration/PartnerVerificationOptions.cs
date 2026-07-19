using System.ComponentModel.DataAnnotations;

namespace PartnerIntegration.Infrastructure.Configuration;

public class PartnerVerificationOptions
{
    public const string SectionName = "PartnerVerification";

    [Required]
    [Url]
    public required string BaseUrl { get; init; }

    [Range(1, 10)]
    public required int MaxRetryAttempts { get; init; }

    [Range(1, 60_000)]
    public required int RetryDelayMilliseconds { get; init; }

    [Range(1, 60_000)]
    public required int AttemptTimeoutMilliseconds { get; init; }

    [Range(1, 120_000)]
    public required int TotalTimeoutMilliseconds { get; init; }
}
