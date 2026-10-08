using ErrorOr;

namespace WebApi.MinimalAPI.Endpoints.Common;

public static class EndpointResultExtensions
{
    public static IResult ToProblemResult(this List<Error> errors)
    {
        if (errors.Count == 0)
        {
            return Results.BadRequest(errors);
        }

        return errors[0].Type switch
        {
            ErrorType.NotFound => Results.NotFound(errors),
            ErrorType.Conflict => Results.Conflict(errors),
            ErrorType.Unauthorized => Results.Json(errors, statusCode: StatusCodes.Status401Unauthorized),
            ErrorType.Forbidden => Results.Json(errors, statusCode: StatusCodes.Status403Forbidden),
            _ => Results.BadRequest(errors),
        };
    }
}
