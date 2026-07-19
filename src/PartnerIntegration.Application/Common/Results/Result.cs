namespace PartnerIntegration.Application.Common.Results;

public class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T Value => IsSuccess && _value is not null
        ? _value
        : throw new InvalidOperationException("A failed result does not contain a value.");

    public Error Error { get; }

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(true, value, Error.None);
    }

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.Category == ErrorCategory.None)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        return new Result<T>(false, default, error);
    }
}
