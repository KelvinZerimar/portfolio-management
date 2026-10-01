using Microsoft.AspNetCore.Http.HttpResults;

namespace WebApi.MinimalAPI.Idempotency;

public sealed record CachedIdempotentResult(int StatusCode, object? Value, string RequestBodyHash)
{
    public static CachedIdempotentResult From(object? result, string requestBodyHash) => new(
        (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK,
        (result as IValueHttpResult)?.Value,
        requestBodyHash);

    public IResult ToResult() => Value is null
        ? Results.StatusCode(StatusCode)
        : Results.Json(Value, statusCode: StatusCode);
}
