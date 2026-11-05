using Application.Agent.Command;
using Asp.Versioning.Conventions;
using Contracts.Agent;
using ErrorOr;
using MediatR;
using WebApi.MinimalAPI.Endpoints.Common;

namespace WebApi.MinimalAPI.Endpoints.Agent;

public static class AgentEndpoints
{
    public static WebApplication RegisterAgentEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
           .HasApiVersion(1)
           .Build();

        var bases = app.MapGroup("/api/v{version:apiVersion}/Agent/")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1)
            .WithTags(EndpointTags.Agent)
            .RequireAuthorization();

        bases.MapPost("chat", async (ISender mediatr, SendAgentMessageRequest request) =>
        {
            var result = await mediatr.Send(new SendAgentMessageCommand(request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<SendAgentMessageResponse>()
           .Produces<List<Error>>(400);

        return app;
    }
}
