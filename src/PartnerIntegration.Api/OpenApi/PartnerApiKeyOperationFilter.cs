using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using PartnerIntegration.Api.Security;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PartnerIntegration.Api.OpenApi;

public class PartnerApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true)
            .Union(context.MethodInfo.GetCustomAttributes(true))
            .ToArray() ?? [];

        if (endpointAttributes.OfType<AllowAnonymousAttribute>().Any() ||
            !endpointAttributes.OfType<AuthorizeAttribute>().Any())
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    PartnerApiKeyAuthenticationHandler.SchemeName,
                    context.Document)] = []
            }
        ];
    }
}
