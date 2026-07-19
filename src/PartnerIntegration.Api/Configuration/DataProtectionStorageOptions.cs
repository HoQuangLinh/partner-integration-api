using System.ComponentModel.DataAnnotations;

namespace PartnerIntegration.Api.Configuration;

public class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";

    [Required]
    public required string ApplicationName { get; init; }

    [Required]
    public required string KeysPath { get; init; }
}
