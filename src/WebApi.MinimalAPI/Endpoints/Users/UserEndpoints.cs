using Application.Users.Command;
using Application.Users.Query;
using Asp.Versioning.Conventions;
using Contracts.Users;
using ErrorOr;
using MediatR;
using WebApi.MinimalAPI.Endpoints.Common;

namespace WebApi.MinimalAPI.Endpoints.Users;

public static class UserEndpoints
{
    public static WebApplication RegisterUserEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
           .HasApiVersion(1)
           .Build();
        var bases = app.MapGroup("/api/v{version:apiVersion}/Users/")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1)
            .WithTags(EndpointTags.Users)
            .AllowAnonymous();

        bases.MapPost("register", async (ISender mediatr, RegisterUserRequest request) =>
        {
            var result = await mediatr.Send(new RegisterUserCommand(request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<RegisterUserResponse>()
           .Produces(409)
           .Produces<List<Error>>(400);

        bases.MapPost("login", async (ISender mediatr, LoginUserRequest request) =>
        {
            var result = await mediatr.Send(new LoginUserQuery(request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<LoginUserResponse>()
           .Produces(401);

        return app;
    }
}
