using System.Diagnostics;
using Serilog.Context;

namespace PartnerIntegration.Api.Logging;

public class RequestLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            await next(context);
        }
    }
}
