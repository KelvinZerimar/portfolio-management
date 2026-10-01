using Application.Common.Caching;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Common.Behaviors;

// Registered to wrap UnitOfWorkBehavior (not the other way around), so invalidation runs
// after SaveChangesAsync has committed, not before — otherwise a reader could repopulate the
// cache with pre-commit data in the window between invalidation and commit.
public class CacheInvalidationBehavior<TRequest, TResponse>(HybridCache cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IInvalidatesCache, IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (!response.IsError)
        {
            foreach (var tag in request.CacheTagsToInvalidate)
            {
                await cache.RemoveByTagAsync(tag, cancellationToken);
            }
        }

        return response;
    }
}
