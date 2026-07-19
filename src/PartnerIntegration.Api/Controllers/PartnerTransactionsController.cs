using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Api.Contracts;
using PartnerIntegration.Api.Extensions;
using PartnerIntegration.Api.Security;
using PartnerIntegration.Application.Common.Results;
using PartnerIntegration.Application.Transactions.CreateTransaction;

namespace PartnerIntegration.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = PartnerApiKeyAuthenticationHandler.SchemeName)]
[Route("api/v1/partner/transactions")]
public class PartnerTransactionsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreatePartnerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        CreateTransactionCommand command = request.ToCommand(
            User.GetRequiredPartnerId(),
            HttpContext.TraceIdentifier);

        Result<CreateTransactionResult> transactionResult =
            await sender.Send(command, cancellationToken);

        return transactionResult.ToAcceptedResult(transaction => transaction.ToResponse());
    }
}
