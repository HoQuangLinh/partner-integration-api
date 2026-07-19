namespace PartnerIntegration.Application.Abstractions.ReferenceData;

public interface ISupportedCurrencyProvider
{
    bool IsSupported(string currencyCode);
}
