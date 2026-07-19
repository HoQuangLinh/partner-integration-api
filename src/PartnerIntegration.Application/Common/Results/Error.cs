namespace PartnerIntegration.Application.Common.Results;

public sealed record Error(string Code, string Description, ErrorCategory Category)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorCategory.None);
}

public enum ErrorCategory
{
    None,
    AccessDenied,
    BusinessRule
}
