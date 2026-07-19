using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using PartnerIntegration.Api.Configuration;
using PartnerIntegration.Api.ErrorHandling;
using PartnerIntegration.Api.OpenApi;
using PartnerIntegration.Api.Security;

namespace PartnerIntegration.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Partner Integration API",
                Version = "v1"
            });
            options.AddSecurityDefinition(
                PartnerApiKeyAuthenticationHandler.SchemeName,
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header,
                    Name = PartnerApiKeyAuthenticationHandler.HeaderName,
                    Description = "Partner API key"
                });
            options.OperationFilter<PartnerApiKeyOperationFilter>();
        });

        var dataProtectionSection = configuration.GetSection(DataProtectionStorageOptions.SectionName);
        if (dataProtectionSection.Exists())
        {
            services
                .AddOptions<DataProtectionStorageOptions>()
                .Bind(dataProtectionSection)
                .ValidateDataAnnotations()
                .Validate(
                    options => Path.IsPathRooted(options.KeysPath),
                    "Data-protection key path must be absolute.")
                .ValidateOnStart();
            var dataProtectionOptions = dataProtectionSection.Get<DataProtectionStorageOptions>()
                ?? throw new OptionsValidationException(
                    DataProtectionStorageOptions.SectionName,
                    typeof(DataProtectionStorageOptions),
                    ["Data-protection configuration is invalid."]);

            services
                .AddDataProtection()
                .SetApplicationName(dataProtectionOptions.ApplicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeysPath));
        }

        services
            .AddOptions<MockPartnerVerificationOptions>()
            .Bind(configuration.GetSection(MockPartnerVerificationOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => options.UnverifiedPartnerIds.All(id => !string.IsNullOrWhiteSpace(id)),
                "Mock unverified partner IDs cannot be empty.")
            .ValidateOnStart();

        services
            .AddOptions<PartnerApiKeyOptions>()
            .Bind(configuration.GetSection(PartnerApiKeyOptions.SectionName))
            .Validate(
                options => options.Credentials.Count > 0 &&
                           options.Credentials.All(credential =>
                               !string.IsNullOrWhiteSpace(credential.PartnerId) &&
                               !string.IsNullOrWhiteSpace(credential.ApiKey)),
                "At least one non-empty partner API key must be configured.")
            .Validate(
                options => options.Credentials
                    .Select(credential => credential.PartnerId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() == options.Credentials.Count,
                "Partner IDs in API-key configuration must be unique.")
            .ValidateOnStart();

        services
            .AddAuthentication(PartnerApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, PartnerApiKeyAuthenticationHandler>(
                PartnerApiKeyAuthenticationHandler.SchemeName,
                _ => { });
        services.AddAuthorization();

        services.AddHealthChecks();

        return services;
    }
}
