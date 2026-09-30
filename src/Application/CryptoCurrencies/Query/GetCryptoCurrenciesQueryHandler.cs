using Application.Common.Caching;
using Application.CryptoCurrencies.Interfaces;
using Contracts.Common;
using Contracts.CryptoCurrencies;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.CryptoCurrencies.Query;

public sealed record GetCryptoCurrenciesQuery(PaginatorRequest Paginator) : IRequest<ErrorOr<PaginatorResponse<CryptoCurrencyResponse>>>;

public sealed class GetCryptoCurrenciesQueryHandler(
    ICryptoCurrencyRepository cryptoCurrencyRepository,
    HybridCache cache
    ) : IRequestHandler<GetCryptoCurrenciesQuery, ErrorOr<PaginatorResponse<CryptoCurrencyResponse>>>
{
    public async Task<ErrorOr<PaginatorResponse<CryptoCurrencyResponse>>> Handle(GetCryptoCurrenciesQuery query, CancellationToken cancellationToken)
    {
        // Reference data (rarely mutated, read on almost every page) — cached by page/limit and
        // invalidated by tag from the Create/Update/Delete handlers.
        var cacheKey = $"{CacheTags.CryptoCurrencies}:{query.Paginator.Page}:{query.Paginator.Limit}";

        return await cache.GetOrCreateAsync(
            cacheKey,
            (cryptoCurrencyRepository, query.Paginator),
            static async (state, ct) =>
            {
                var result = await state.cryptoCurrencyRepository.GetAllAsync(
                    state.Paginator.Page, state.Paginator.Limit, _ => true, ct);

                return new PaginatorResponse<CryptoCurrencyResponse>
                {
                    Page = result.Page,
                    PageSize = result.PageSize,
                    Total = result.Total,
                    TotalPages = result.TotalPages,
                    Data = result.Data.Select(c => c.ToCryptoCurrencyResponse()).ToList()
                };
            },
            tags: [CacheTags.CryptoCurrencies],
            cancellationToken: cancellationToken);
    }
}
