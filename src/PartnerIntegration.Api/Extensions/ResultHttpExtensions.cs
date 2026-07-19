using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Api.ErrorHandling;
using PartnerIntegration.Application.Common.Results;

namespace PartnerIntegration.Api.Extensions;

public static class ResultHttpExtensions
{
    public static IActionResult ToAcceptedResult<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> responseFactory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(responseFactory);

        return result.IsSuccess
            ? new AcceptedResult(location: null, responseFactory(result.Value))
            : new ErrorActionResult(result.Error);
    }

    private static IActionResult CreateErrorResult(Error error, HttpContext httpContext)
    {
        var (statusCode, title) = error.Category switch
        {
            ErrorCategory.AccessDenied => (StatusCodes.Status403Forbidden, "Forbidden"),
            ErrorCategory.BusinessRule => (StatusCodes.Status422UnprocessableEntity, "Request rejected"),
            ErrorCategory.None => throw new InvalidOperationException(
                "A failed result cannot contain Error.None."),
            _ => throw new InvalidOperationException(
                $"Error category '{error.Category}' does not have an HTTP mapping.")
        };
        var problemDetails = ApiProblemDetailsFactory.Create(
            httpContext,
            statusCode,
            title,
            error.Description);
        problemDetails.Extensions["errorCode"] = error.Code;

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }

    private class ErrorActionResult(Error error) : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context) =>
            CreateErrorResult(error, context.HttpContext)
                .ExecuteResultAsync(context);
    }
}
