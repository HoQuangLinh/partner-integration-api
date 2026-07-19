using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PartnerIntegration.Api.ErrorHandling;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string title,
        string detail) =>
        AddRequestContext(
            new ProblemDetails
            {
                Title = title,
                Detail = detail
            },
            context,
            status);

    public static HttpValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors) =>
        AddRequestContext(
            new HttpValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Detail = "One or more validation errors occurred."
            },
            context,
            StatusCodes.Status400BadRequest);

    private static TProblem AddRequestContext<TProblem>(
        TProblem problem,
        HttpContext context,
        int status)
        where TProblem : ProblemDetails
    {
        problem.Status = status;
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] =
            Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        problem.Extensions["correlationId"] = context.TraceIdentifier;

        return problem;
    }
}
