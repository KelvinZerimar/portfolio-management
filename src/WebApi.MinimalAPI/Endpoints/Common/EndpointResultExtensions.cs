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
            ErrorType.Unauthorized => Results.Unauthorized(),
            ErrorType.Forbidden => Results.Forbid(),
            _ => Results.BadRequest(errors),
        };
    }
}
