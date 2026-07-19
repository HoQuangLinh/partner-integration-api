using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Application.Abstractions.Messaging;
using PartnerIntegration.Application.Abstractions.ReferenceData;
using PartnerIntegration.Infrastructure.Configuration;
using PartnerIntegration.Infrastructure.ExternalServices;
using PartnerIntegration.Infrastructure.Messaging;
using PartnerIntegration.Infrastructure.ReferenceData;

namespace PartnerIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<PartnerVerificationOptions>()
            .Bind(configuration.GetSection(PartnerVerificationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.TotalTimeoutMilliseconds > options.AttemptTimeoutMilliseconds,
                "Partner verification total timeout must exceed the per-attempt timeout.")
            .ValidateOnStart();
        services
            .AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<SupportedCurrenciesOptions>()
            .Bind(configuration.GetSection(SupportedCurrenciesOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.CurrencyCodes.All(currencyCode =>
                    currencyCode.Trim().Length == 3 &&
                    currencyCode.Trim().All(char.IsAsciiLetter)),
                "Every supported currency must be a three-letter code.")
            .ValidateOnStart();

        var verificationOptions = configuration
            .GetSection(PartnerVerificationOptions.SectionName)
            .Get<PartnerVerificationOptions>()
            ?? throw new OptionsValidationException(
                PartnerVerificationOptions.SectionName,
                typeof(PartnerVerificationOptions),
                ["Partner verification configuration is required."]);

        services
            .AddHttpClient<IPartnerVerificationService, PartnerVerificationClient>(client =>
            {
                client.BaseAddress = new Uri(verificationOptions.BaseUrl, UriKind.Absolute);
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddPartnerVerificationResilience(verificationOptions);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISupportedCurrencyProvider, ConfiguredSupportedCurrencyProvider>();
        services.AddSingleton<IRabbitMqConnectionManager, RabbitMqConnectionManager>();
        services.AddSingleton<ITransactionMessagePublisher, RabbitMqTransactionMessagePublisher>();

        return services;
    }
}
