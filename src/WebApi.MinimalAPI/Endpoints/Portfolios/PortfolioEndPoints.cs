using Application.Portfolios.Command;
using Application.Portfolios.Query;
using Application.Reports.Command;
using Asp.Versioning.Conventions;
using Contracts.Common;
using Contracts.Portfolios;
using ErrorOr;
using MediatR;
using WebApi.MinimalAPI.Endpoints.Common;
using WebApi.MinimalAPI.Idempotency;

namespace WebApi.MinimalAPI.Endpoints.Portfolios;

public static class PortfolioEndPoints
{
    public static WebApplication RegisterPortfolioEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
           .HasApiVersion(1)
           .Build();
        var bases = app.MapGroup("/api/v{version:apiVersion}/Portfolio/")
            .WithApiVersionSet(versionSet)
            .MapToApiVersion(1)
            .WithTags(EndpointTags.Portfolio)
            .RequireAuthorization();

        bases.MapPost("/", async (ISender mediatr, CreatePortfolioRequest request) =>
        {
            var result = await mediatr.Send(new CreatePortfolioCommand(request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .AddEndpointFilter<IdempotencyFilter>()
           .Produces<CreatePortfolioResponse>()
           .Produces(409)
           .Produces(422)
           .Produces<List<Error>>(400);

        bases.MapGet("/", async (ISender mediatr, [AsParameters] PaginatorRequest paginator) =>
        {
            var result = await mediatr.Send(new GetPortfoliosQuery(paginator));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PaginatorResponse<PortfolioResponse>>()
           .Produces<List<Error>>(400);

        bases.MapGet("{id:long}", async (ISender mediatr, long id) =>
        {
            var result = await mediatr.Send(new GetPortfolioByIdQuery(id));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PortfolioResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapPut("{id:long}", async (ISender mediatr, long id, UpdatePortfolioRequest request) =>
        {
            var result = await mediatr.Send(new UpdatePortfolioCommand(id, request));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<UpdatePortfolioResponse>()
           .Produces(404)
           .Produces(409)
           .Produces<List<Error>>(400);

        bases.MapDelete("{id:long}", async (ISender mediatr, long id) =>
        {
            var result = await mediatr.Send(new DeletePortfolioCommand(id));
            return result.Match(_ => Results.NoContent(), errors => errors.ToProblemResult());
        })
           .Produces(204)
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapGet("{id:long}/value", async (ISender mediatr, long id, DateTime? date) =>
        {
            var result = await mediatr.Send(new GetPortfolioValueQuery(id, date));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PortfolioValueResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapGet("{id:long}/history", async (ISender mediatr, long id) =>
        {
            var result = await mediatr.Send(new GetPortfolioHistoryQuery(id));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PortfolioHistoryResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapGet("{id:long}/holdings", async (ISender mediatr, long id, DateTime? date) =>
        {
            var result = await mediatr.Send(new GetPortfolioHoldingsQuery(id, date));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PortfolioHoldingsResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapGet("{id:long}/allocation", async (ISender mediatr, long id, DateTime? date, PortfolioAllocationGroupBy groupBy = PortfolioAllocationGroupBy.Asset) =>
        {
            var result = await mediatr.Send(new GetPortfolioAllocationQuery(id, groupBy, date));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<PortfolioAllocationResponse>()
           .Produces(404)
           .Produces<List<Error>>(400);

        bases.MapPost("{id:long}/refresh-prices", async (ISender mediatr, long id) =>
        {
            var result = await mediatr.Send(new RefreshPortfolioPricesCommand(id));
            return result.Match(Results.Ok, errors => errors.ToProblemResult());
        })
           .Produces<RefreshPortfolioPricesResponse>()
           .Produces(404)
           .Produces(409)
           .Produces<List<Error>>(400);

        bases.MapPost("{id:long}/send-status-report", async (ISender mediatr, long id, DateTime? periodStart, DateTime? periodEnd) =>
        {
            var result = await mediatr.Send(new TriggerPortfolioStatusReportCommand(id, periodStart, periodEnd));
            return result.Match(_ => Results.NoContent(), errors => errors.ToProblemResult());
        })
           .Produces(204)
           .Produces(404)
           .Produces<List<Error>>(400);

        return app;
    }
}
