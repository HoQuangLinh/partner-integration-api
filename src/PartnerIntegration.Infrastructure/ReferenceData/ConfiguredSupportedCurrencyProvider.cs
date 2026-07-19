using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions.ReferenceData;
using PartnerIntegration.Infrastructure.Configuration;

namespace PartnerIntegration.Infrastructure.ReferenceData;

public class ConfiguredSupportedCurrencyProvider : ISupportedCurrencyProvider
{
    private readonly HashSet<string> _supportedCurrencyCodes;

    public ConfiguredSupportedCurrencyProvider(IOptions<SupportedCurrenciesOptions> options)
    {
        _supportedCurrencyCodes = options.Value.CurrencyCodes
            .Select(currencyCode => currencyCode.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public bool IsSupported(string currencyCode) =>
        !string.IsNullOrWhiteSpace(currencyCode) &&
        _supportedCurrencyCodes.Contains(currencyCode.Trim().ToUpperInvariant());
}
