using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PartnerIntegration.Infrastructure.ExternalServices;

public class PartnerVerificationClient(
    HttpClient httpClient,
    ILogger<PartnerVerificationClient> logger)
    : IPartnerVerificationService
{
    public async Task<bool> VerifyAsync(
        string partnerId,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var httpResponse = await httpClient
                .GetAsync(
                    $"api/v1/mock/partners/{Uri.EscapeDataString(partnerId)}/verification",
                    cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(
                    "Partner verification completed for {PartnerId} with result {IsVerified} in {ElapsedMilliseconds} ms",
                    partnerId,
                    false,
                    stopwatch.ElapsedMilliseconds);
                return false;
            }

            httpResponse.EnsureSuccessStatusCode();

            var verificationResponse = await httpResponse.Content
                .ReadFromJsonAsync<PartnerVerificationResponse>(cancellationToken);

            if (verificationResponse is null || string.IsNullOrWhiteSpace(verificationResponse.PartnerId))
            {
                throw new PartnerVerificationUnavailableException();
            }

            var isVerified = verificationResponse.IsVerified &&
                             string.Equals(
                                 verificationResponse.PartnerId,
                                 partnerId,
                                 StringComparison.OrdinalIgnoreCase);

            logger.LogInformation(
                "Partner verification completed for {PartnerId} with result {IsVerified} in {ElapsedMilliseconds} ms",
                partnerId,
                isVerified,
                stopwatch.ElapsedMilliseconds);

            return isVerified;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or OperationCanceledException or BrokenCircuitException or
                TimeoutRejectedException or JsonException)
        {
            logger.LogError(
                exception,
                "Partner verification failed for {PartnerId} after {ElapsedMilliseconds} ms with category {ErrorCategory}",
                partnerId,
                stopwatch.ElapsedMilliseconds,
                "ExternalDependency");
            throw new PartnerVerificationUnavailableException(exception);
        }
    }

    private record PartnerVerificationResponse(string PartnerId, bool IsVerified);
}
