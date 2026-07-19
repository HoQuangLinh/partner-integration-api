using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Infrastructure.Configuration;
using PartnerIntegration.Infrastructure.ExternalServices;

namespace PartnerIntegration.UnitTests;

public class PartnerVerificationResilienceTests
{
    [Test]
    public async Task VerifyAsync_WhenTwoTransientFailuresThenSuccess_ShouldRetryAndReturnVerified()
    {
        var httpMessageHandler = new SequenceHttpMessageHandler(
            _ => CreateResponseAsync(HttpStatusCode.ServiceUnavailable),
            _ => CreateResponseAsync(HttpStatusCode.InternalServerError),
            _ => CreateResponseAsync(HttpStatusCode.OK, """{"partnerId":"P-1001","isVerified":true}"""));
        using var serviceProvider = BuildServiceProvider(httpMessageHandler, maxRetryAttempts: 3);
        var verificationService = serviceProvider.GetRequiredService<IPartnerVerificationService>();

        var result = await verificationService.VerifyAsync("P-1001", CancellationToken.None);

        Assert.That(result, Is.True);
        Assert.That(httpMessageHandler.CallCount, Is.EqualTo(3));
    }

    [Test]
    public async Task VerifyAsync_WhenFirstAttemptTimesOutThenSuccess_ShouldRetryAndReturnVerified()
    {
        var httpMessageHandler = new SequenceHttpMessageHandler(
            async cancellationToken =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return await CreateResponseAsync(HttpStatusCode.OK);
            },
            _ => CreateResponseAsync(HttpStatusCode.OK, """{"partnerId":"P-1001","isVerified":true}"""));
        await using var serviceProvider = BuildServiceProvider(
            httpMessageHandler,
            maxRetryAttempts: 1,
            attemptTimeoutMilliseconds: 30);
        var verificationService = serviceProvider.GetRequiredService<IPartnerVerificationService>();

        var result = await verificationService.VerifyAsync("P-1001", CancellationToken.None);

        Assert.That(result, Is.True);
        Assert.That(httpMessageHandler.CallCount, Is.EqualTo(2));
    }

    [Test]
    public void VerifyAsync_WhenHttp400IsReturned_ShouldFailWithoutRetrying()
    {
        var httpMessageHandler = new SequenceHttpMessageHandler(_ => CreateResponseAsync(HttpStatusCode.BadRequest));
        using var serviceProvider = BuildServiceProvider(httpMessageHandler, maxRetryAttempts: 3);
        var verificationService = serviceProvider.GetRequiredService<IPartnerVerificationService>();

        Assert.ThrowsAsync<PartnerVerificationUnavailableException>(
            (Func<Task>)(() => verificationService.VerifyAsync("P-1001", CancellationToken.None)));

        Assert.That(httpMessageHandler.CallCount, Is.EqualTo(1));
    }

    [Test]
    public void VerifyAsync_WhenAllAttemptsReturn503_ShouldThrowUnavailableAfterConfiguredRetries()
    {
        var httpMessageHandler = new SequenceHttpMessageHandler(
            _ => CreateResponseAsync(HttpStatusCode.ServiceUnavailable));
        using var serviceProvider = BuildServiceProvider(httpMessageHandler, maxRetryAttempts: 2);
        var verificationService = serviceProvider.GetRequiredService<IPartnerVerificationService>();

        Assert.ThrowsAsync<PartnerVerificationUnavailableException>(
            (Func<Task>)(() => verificationService.VerifyAsync("P-1001", CancellationToken.None)));

        Assert.That(httpMessageHandler.CallCount, Is.EqualTo(3));
    }

    [Test]
    public void VerifyAsync_WhenCallerCancels_ShouldPropagateCancellationWithoutRetrying()
    {
        var httpMessageHandler = new SequenceHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return await CreateResponseAsync(HttpStatusCode.OK);
        });
        using var serviceProvider = BuildServiceProvider(httpMessageHandler, maxRetryAttempts: 3);
        var verificationService = serviceProvider.GetRequiredService<IPartnerVerificationService>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.CatchAsync<OperationCanceledException>(
            (Func<Task>)(() => verificationService.VerifyAsync("P-1001", cancellation.Token)));

        Assert.That(httpMessageHandler.CallCount, Is.LessThanOrEqualTo(1));
    }

    private static ServiceProvider BuildServiceProvider(
        SequenceHttpMessageHandler httpMessageHandler,
        int maxRetryAttempts,
        int attemptTimeoutMilliseconds = 200)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new PartnerVerificationOptions
        {
            BaseUrl = httpMessageHandler.BaseAddress.AbsoluteUri,
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelayMilliseconds = 1,
            AttemptTimeoutMilliseconds = attemptTimeoutMilliseconds,
            TotalTimeoutMilliseconds = 2_000
        };

        services
            .AddHttpClient<IPartnerVerificationService, PartnerVerificationClient>(client =>
            {
                client.BaseAddress = httpMessageHandler.BaseAddress;
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => httpMessageHandler)
            .AddPartnerVerificationResilience(options);

        return services.BuildServiceProvider();
    }

    private static Task<HttpResponseMessage> CreateResponseAsync(
        HttpStatusCode statusCode,
        string? content = null) =>
        Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = content is null ? null : new StringContent(content)
        });

    private class SequenceHttpMessageHandler(
        params Func<CancellationToken, Task<HttpResponseMessage>>[] responseFactories)
        : HttpMessageHandler
    {
        private int _callCount;

        public Uri BaseAddress { get; } = new UriBuilder(
            Uri.UriSchemeHttp,
            IPAddress.Loopback.ToString()).Uri;

        public int CallCount => _callCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _callCount);
            var responseFactory = responseFactories[Math.Min(call - 1, responseFactories.Length - 1)];
            return responseFactory(cancellationToken);
        }
    }
}
