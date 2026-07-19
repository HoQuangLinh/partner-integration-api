using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PartnerIntegration.Api.Configuration;
using PartnerIntegration.Api.Contracts;

namespace PartnerIntegration.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/mock/partners")]
public class MockPartnerVerificationController(
    IOptions<MockPartnerVerificationOptions> options) : ControllerBase
{
    private readonly MockPartnerVerificationOptions _options = options.Value;

    [HttpGet("{partnerId}/verification")]
    public ActionResult<PartnerVerificationResponse> Verify(string partnerId)
    {
        if (Random.Shared.NextDouble() < _options.TimeoutProbability)
        {
            throw new TimeoutException("Simulated timeout from the Partner Verification API.");
        }

        var isVerified = !_options.UnverifiedPartnerIds.Contains(
            partnerId,
            StringComparer.OrdinalIgnoreCase);

        return Ok(new PartnerVerificationResponse(partnerId, isVerified));
    }
}
