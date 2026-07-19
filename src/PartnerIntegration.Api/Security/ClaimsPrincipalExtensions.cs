using System.Security.Claims;

namespace PartnerIntegration.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static string GetRequiredPartnerId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated partner identity is missing.");
}
