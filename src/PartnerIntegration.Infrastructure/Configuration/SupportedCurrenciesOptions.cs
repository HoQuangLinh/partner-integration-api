using System.ComponentModel.DataAnnotations;

namespace PartnerIntegration.Infrastructure.Configuration;

public class SupportedCurrenciesOptions
{
    public const string SectionName = "SupportedCurrencies";

    [MinLength(1)]
    public required string[] CurrencyCodes { get; init; }
}
