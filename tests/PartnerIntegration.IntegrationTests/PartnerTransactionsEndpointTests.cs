using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PartnerIntegration.IntegrationTests;

public class PartnerTransactionsEndpointTests
{
    private PartnerIntegrationApiFactory _apiFactory = null!;
    private HttpClient _httpClient = null!;
    private string _apiKey = null!;
    private string _partnerId = null!;

    [SetUp]
    public void SetUp()
    {
        _partnerId = Environment.GetEnvironmentVariable("PARTNER_ID")
            ?? throw new InvalidOperationException("PARTNER_ID is required to run integration tests.");
        _apiKey = Environment.GetEnvironmentVariable("PARTNER_API_KEY")
            ?? throw new InvalidOperationException("PARTNER_API_KEY is required to run integration tests.");
        _apiFactory = new PartnerIntegrationApiFactory();
        _httpClient = _apiFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _apiFactory?.Dispose();
    }

    [Test]
    public async Task CreateTransaction_WhenRequestIsValid_ShouldReturn202AndPublishMessage()
    {
        using var request = CreateAuthenticatedRequest(CreateValidPayload());

        using var httpResponse = await _httpClient.SendAsync(request);
        var acceptedResponse = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(acceptedResponse.GetProperty("messageId").GetGuid(), Is.Not.EqualTo(Guid.Empty));
        Assert.That(
            acceptedResponse.GetProperty("transactionReference").GetString(),
            Is.EqualTo("TXN-99823"));
        Assert.That(
            acceptedResponse.GetProperty("acceptedAtUtc").GetDateTimeOffset(),
            Is.Not.EqualTo(default(DateTimeOffset)));
        Assert.That(_apiFactory.VerificationService.CallCount, Is.EqualTo(1));
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.EqualTo(1));
        Assert.That(_apiFactory.MessagePublisher.Message!.CorrelationId, Is.Not.Empty);
    }

    [Test]
    public async Task CreateTransaction_WhenPayloadIsInvalid_ShouldReturn400WithoutPublishing()
    {
        var invalidPayload = CreateValidPayload() with { Amount = 0, Currency = "JPY" };
        using var request = CreateAuthenticatedRequest(invalidPayload);

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.BadRequest);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("Validation failed"));
        Assert.That(problemDetails.GetProperty("errors").TryGetProperty("amount", out _), Is.True);
        Assert.That(problemDetails.GetProperty("errors").TryGetProperty("currency", out _), Is.True);
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.Zero);
    }

    [Test]
    public async Task CreateTransaction_WhenApiKeyIsMissing_ShouldReturn401ProblemDetails()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/partner/transactions")
        {
            Content = JsonContent.Create(CreateValidPayload())
        };

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.Unauthorized);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("Unauthorized"));
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.Zero);
    }

    [Test]
    public async Task CreateTransaction_WhenPartnerIdentityDoesNotMatchPayload_ShouldReturn403WithoutCallingDependencies()
    {
        var mismatchedPartnerPayload = CreateValidPayload() with { PartnerId = "P-OTHER" };
        using var request = CreateAuthenticatedRequest(mismatchedPartnerPayload);

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.Forbidden);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("Forbidden"));
        Assert.That(
            problemDetails.GetProperty("errorCode").GetString(),
            Is.EqualTo("partner.authorization_mismatch"));
        Assert.That(_apiFactory.VerificationService.CallCount, Is.Zero);
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.Zero);
    }

    [Test]
    public async Task CreateTransaction_WhenPartnerIsNotVerified_ShouldReturn422WithoutPublishing()
    {
        _apiFactory.VerificationService.IsPartnerVerified = false;
        using var request = CreateAuthenticatedRequest(CreateValidPayload());

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.UnprocessableEntity);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("Request rejected"));
        Assert.That(problemDetails.GetProperty("errorCode").GetString(), Is.EqualTo("partner.not_verified"));
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.Zero);
    }

    [Test]
    public async Task CreateTransaction_WhenVerificationServiceIsUnavailable_ShouldReturn503WithoutPublishing()
    {
        _apiFactory.VerificationService.SimulateUnavailableVerification = true;
        using var request = CreateAuthenticatedRequest(CreateValidPayload());

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.ServiceUnavailable);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("External dependency unavailable"));
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.Zero);
    }

    [Test]
    public async Task CreateTransaction_WhenRabbitMqIsUnavailable_ShouldReturn503ProblemDetails()
    {
        _apiFactory.MessagePublisher.SimulatePublishingFailure = true;
        using var request = CreateAuthenticatedRequest(CreateValidPayload());

        using var httpResponse = await _httpClient.SendAsync(request);
        var problemDetails = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        AssertProblemDetails(httpResponse, problemDetails, HttpStatusCode.ServiceUnavailable);
        Assert.That(problemDetails.GetProperty("title").GetString(), Is.EqualTo("Message broker unavailable"));
        Assert.That(_apiFactory.MessagePublisher.CallCount, Is.EqualTo(1));
        Assert.That(_apiFactory.MessagePublisher.Message, Is.Null);
    }

    [Test]
    public async Task GetMockVerification_WhenPartnerIsConfiguredAsUnverified_ShouldReturnNotVerified()
    {
        using var httpResponse = await _httpClient.GetAsync("/api/v1/mock/partners/P-INVALID/verification");
        var verificationResponse = await httpResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(verificationResponse.GetProperty("isVerified").GetBoolean(), Is.False);
    }

    private HttpRequestMessage CreateAuthenticatedRequest(PartnerTransactionPayload payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/partner/transactions")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-API-Key", _apiKey);

        return request;
    }

    private static void AssertProblemDetails(
        HttpResponseMessage response,
        JsonElement problemDetails,
        HttpStatusCode expectedStatus)
    {
        Assert.That(
            response.Content.Headers.ContentType?.MediaType,
            Is.EqualTo("application/problem+json"));
        Assert.That(problemDetails.GetProperty("status").GetInt32(), Is.EqualTo((int)expectedStatus));
        Assert.That(
            problemDetails.GetProperty("instance").GetString(),
            Is.EqualTo("/api/v1/partner/transactions"));
        Assert.That(problemDetails.GetProperty("traceId").GetString(), Is.Not.Empty);
        Assert.That(problemDetails.GetProperty("correlationId").GetString(), Is.Not.Empty);
    }

    private PartnerTransactionPayload CreateValidPayload() =>
        new(_partnerId, "TXN-99823", 250m, "USD", DateTimeOffset.UtcNow.AddMinutes(-1));

    private record PartnerTransactionPayload(
        string PartnerId,
        string TransactionReference,
        decimal Amount,
        string Currency,
        DateTimeOffset Timestamp);
}
