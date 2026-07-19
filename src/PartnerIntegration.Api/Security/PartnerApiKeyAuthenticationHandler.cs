using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PartnerIntegration.Api.ErrorHandling;

namespace PartnerIntegration.Api.Security;

public class PartnerApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptions<PartnerApiKeyOptions> partnerApiKeyOptions,
    IProblemDetailsService problemDetailsService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    public const string SchemeName = "PartnerApiKey";
    public const string HeaderName = "X-API-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues) || headerValues.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var presentedKey = headerValues[0];

        if (string.IsNullOrWhiteSpace(presentedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("The API key is empty."));
        }

        var partnerId = partnerApiKeyOptions.Value.Credentials
            .FirstOrDefault(credential => KeysMatch(credential.ApiKey, presentedKey))
            ?.PartnerId;

        if (string.IsNullOrWhiteSpace(partnerId))
        {
            return Task.FromResult(AuthenticateResult.Fail("The API key is invalid."));
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, partnerId) };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        var problemDetails = ApiProblemDetailsFactory.Create(
            Context,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "A valid partner API key is required.");

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = problemDetails
        });
    }

    private static bool KeysMatch(string configuredKey, string presentedKey)
    {
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var presentedBytes = Encoding.UTF8.GetBytes(presentedKey);

        return configuredBytes.Length == presentedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(configuredBytes, presentedBytes);
    }
}
