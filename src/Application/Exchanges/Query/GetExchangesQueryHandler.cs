using Application.Common.Caching;
using Application.Exchanges.Interfaces;
using Contracts.Common;
using Contracts.Exchanges;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Exchanges.Query;

public sealed record GetExchangesQuery(PaginatorRequest Paginator) : IRequest<ErrorOr<PaginatorResponse<ExchangeResponse>>>;

public sealed class GetExchangesQueryHandler(
    IExchangeRepository exchangeRepository,
    HybridCache cache
    ) : IRequestHandler<GetExchangesQuery, ErrorOr<PaginatorResponse<ExchangeResponse>>>
{
    public async Task<ErrorOr<PaginatorResponse<ExchangeResponse>>> Handle(GetExchangesQuery query, CancellationToken cancellationToken)
    {
        // Reference data (rarely mutated, read on almost every page) — cached by page/limit and
        // invalidated by tag from the Create/Update/Delete handlers.
        var cacheKey = $"{CacheTags.Exchanges}:{query.Paginator.Page}:{query.Paginator.Limit}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            (exchangeRepository, query.Paginator),
            static async (state, ct) =>
            {
                var result = await state.exchangeRepository.GetAllAsync(
                    state.Paginator.Page, state.Paginator.Limit, _ => true, ct);

                return new PaginatorResponse<ExchangeResponse>
                {
                    Page = result.Page,
                    PageSize = result.PageSize,
                    Total = result.Total,
                    TotalPages = result.TotalPages,
                    Data = result.Data.Select(e => e.ToExchangeResponse()).ToList()
                };
            },
            tags: [CacheTags.Exchanges],
            cancellationToken: cancellationToken);
    }
}
