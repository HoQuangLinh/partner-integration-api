using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.Infrastructure.Configuration;
using Polly;
using Polly.Timeout;

namespace PartnerIntegration.Infrastructure.ExternalServices;

public static class PartnerVerificationResilienceExtensions
{
    public static IHttpClientBuilder AddPartnerVerificationResilience(
        this IHttpClientBuilder builder,
        PartnerVerificationOptions options)
    {
        builder.AddStandardResilienceHandler(resilience =>
        {
            resilience.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(options.AttemptTimeoutMilliseconds);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMilliseconds(options.TotalTimeoutMilliseconds);

            resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
            resilience.Retry.Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds);
            resilience.Retry.BackoffType = DelayBackoffType.Exponential;
            resilience.Retry.UseJitter = true;
            resilience.Retry.ShouldHandle = CreateTransientPredicate();

            resilience.CircuitBreaker.ShouldHandle = CreateTransientPredicate();
        });

        return builder;
    }

    private static PredicateBuilder<HttpResponseMessage> CreateTransientPredicate() =>
        new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(response =>
                response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
                (int)response.StatusCode >= 500);
}
