using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using PartnerIntegration.Application.Abstractions.ExternalServices;
using PartnerIntegration.Application.Abstractions.Messaging;

namespace PartnerIntegration.Api.ErrorHandling;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblem(httpContext, validationException),
            PartnerVerificationUnavailableException => ApiProblemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "External dependency unavailable",
                "Partner verification is temporarily unavailable. Please retry later."),
            TransactionMessagePublishingException => ApiProblemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Message broker unavailable",
                "The transaction could not be queued. Please retry later."),
            TimeoutException => ApiProblemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status503ServiceUnavailable,
                "Simulated partner verification timeout",
                "The mock partner verification API timed out."),
            _ => ApiProblemDetailsFactory.Create(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred.")
        };

        if (exception is not (
                PartnerVerificationUnavailableException or
                TransactionMessagePublishingException or
                TimeoutException or
                ValidationException))
        {
            logger.LogError(
                exception,
                "Unhandled request failure for {RequestMethod} {RequestPath} with category {ErrorCategory} " +
                "and correlation {CorrelationId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                "Unexpected",
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static HttpValidationProblemDetails CreateValidationProblem(
        HttpContext context,
        ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => char.ToLowerInvariant(failure.PropertyName[0]) + failure.PropertyName[1..])
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
        return ApiProblemDetailsFactory.CreateValidation(context, errors);
    }
}
