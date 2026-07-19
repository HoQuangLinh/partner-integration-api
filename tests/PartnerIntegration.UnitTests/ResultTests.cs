using PartnerIntegration.Application.Common.Results;

namespace PartnerIntegration.UnitTests;

public class ResultTests
{
    private static readonly Error BusinessError = new(
        "transaction.rejected",
        "The transaction was rejected.",
        ErrorCategory.BusinessRule);

    [Test]
    public void Success_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Action createSuccessfulResult = () => Result<string>.Success(null!);

        Assert.Throws<ArgumentNullException>(createSuccessfulResult);
    }

    [Test]
    public void Failure_WhenErrorIsNone_ShouldThrowArgumentException()
    {
        Action createFailedResult = () => Result<string>.Failure(Error.None);

        Assert.Throws<ArgumentException>(createFailedResult);
    }

    [Test]
    public void Value_WhenResultIsFailure_ShouldThrowInvalidOperationException()
    {
        var result = Result<string>.Failure(BusinessError);
        Action readValue = () => _ = result.Value;

        Assert.Throws<InvalidOperationException>(readValue);
    }
}
