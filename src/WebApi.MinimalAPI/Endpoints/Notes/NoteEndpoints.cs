using Application.Notes.Command;
using Application.Notes.Query;
using Asp.Versioning.Conventions;
using Contracts.Common;
using Contracts.Notes;
using ErrorOr;
using MediatR;
using WebApi.MinimalAPI.Endpoints.Common;
using WebApi.MinimalAPI.Idempotency;

namespace WebApi.MinimalAPI.Endpoints.Notes;

public static class NoteEndpoints
{
    public static WebApplication RegisterNoteEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
           .HasApiVersion(1)
           .Build();
        var bases = app.MapGroup("/api/v{version:apiVersion}/Note/")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1)
            .WithTags(EndpointTags.Note)
            .RequireAuthorization();

        bases.MapPost("/", async (ISender mediatr, CreateNoteRequest request) =>
        {
            var result = await mediatr.Send(new CreateNoteCommand(request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .AddEndpointFilter<IdempotencyFilter>()
           .Produces<CreateNoteResponse>()
           .Produces(422)
           .Produces<List<Error>>(400);

        bases.MapGet("/", async (ISender mediatr, [AsParameters] PaginatorRequest paginator, string? category, bool? isActive) =>
        {
            var result = await mediatr.Send(new GetNotesQuery(paginator, category, isActive));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PaginatorResponse<NoteResponse>>()
           .Produces<List<Error>>(400);

        bases.MapGet("latest", async (ISender mediatr, int count = 10) =>
        {
            var result = await mediatr.Send(new GetLatestNotesQuery(count));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<List<NoteResponse>>()
           .Produces<List<Error>>(400);

        bases.MapGet("{id:guid}", async (ISender mediatr, string id) =>
        {
            var result = await mediatr.Send(new GetNoteByIdQuery(id));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<NoteResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapPut("{id:guid}", async (ISender mediatr, string id, UpdateNoteRequest request) =>
        {
            var result = await mediatr.Send(new UpdateNoteCommand(id, request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<UpdateNoteResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapDelete("{id:guid}", async (ISender mediatr, string id) =>
        {
            var result = await mediatr.Send(new DeleteNoteCommand(id));
            return result.Match(_ => Results.NoContent(), errors => errors.ToProblemResult());
        })
           .Produces(204)
           .Produces(404)
           .Produces<List<Error>>(400);

        return app;
    }
}
