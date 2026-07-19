using System.Diagnostics;
using System.Security.Claims;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace PartnerIntegration.Api.Logging;

public static class SerilogConfiguration
{
    private const string ServiceName = "partner-integration-bff";

    public static WebApplicationBuilder AddApplicationLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", ServiceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new JsonFormatter(renderMessage: true));
        });

        return builder;
    }

    public static void UseApplicationRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<RequestLogContextMiddleware>();
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
            {
                if (httpContext.Request.Path.StartsWithSegments("/health"))
                {
                    return LogEventLevel.Debug;
                }

                if (exception is not null || httpContext.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                return httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(
                    "TraceId",
                    Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);

                var partnerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(partnerId))
                {
                    diagnosticContext.Set("PartnerId", partnerId);
                }
            };
        });
    }
}
