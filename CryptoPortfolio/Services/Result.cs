namespace CryptoPortfolio.Services;

public record Result<T>(bool Success, T? Value, string? Error)
{
    public static Result<T> Ok(T value) => new(true, value, string.Empty);
    public static Result<T> Fail(string error) => new(false, default, error);
}

public record Result(bool Success, string? Error)
{
    public static Result Ok() => new(true, string.Empty);
    public static Result Fail(string error) => new(false, error);
}