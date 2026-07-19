using PartnerIntegration.Application.Common.Results;

namespace PartnerIntegration.Application.Transactions.CreateTransaction;

public static class CreateTransactionErrors
{
    public static readonly Error PartnerMismatch = new(
        "partner.authorization_mismatch",
        "The authenticated caller cannot submit transactions for the requested partner.",
        ErrorCategory.AccessDenied);

    public static readonly Error PartnerNotVerified = new(
        "partner.not_verified",
        "The partner could not be verified.",
        ErrorCategory.BusinessRule);
}
