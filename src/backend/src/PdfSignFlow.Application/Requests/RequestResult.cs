namespace PdfSignFlow.Application.Requests;

public enum RequestResultStatus
{
    Ok = 0,
    NotFound = 1,
    Invalid = 2,
}

/// <param name="Errors">Messages by request field, in camelCase, when the status is <see cref="RequestResultStatus.Invalid"/>.</param>
public sealed record RequestResult<T>(RequestResultStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors)
{
    private static readonly Dictionary<string, string[]> NoErrors = [];

    public static RequestResult<T> NotFound { get; } = new(RequestResultStatus.NotFound, default, NoErrors);

    public static RequestResult<T> Ok(T value) => new(RequestResultStatus.Ok, value, NoErrors);

    public static RequestResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(RequestResultStatus.Invalid, default, errors);
}
