using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Security;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using WebApi.MinimalAPI.Endpoints.Common;

namespace WebApi.MinimalAPI.Idempotency;

public sealed class IdempotencyFilter(HybridCache cache, ICurrentUserProvider currentUser) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private static readonly HybridCacheEntryOptions Options = new() { Expiration = TimeSpan.FromHours(24) };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key) || string.IsNullOrWhiteSpace(key))
        {
            return new List<Error>
            {
                Error.Validation("Idempotency.MissingHeader", $"The '{HeaderName}' header is required."),
            }.ToProblemResult();
        }

        var cacheKey = $"idempotency:{currentUser.UserId}:{context.HttpContext.Request.Path}:{key}";
        var requestBodyHash = ComputeRequestBodyHash(context);

        // HybridCache's factory can run on an execution context detached from this request
        // (its stampede de-duplication path), which drops the AsyncLocal-backed HttpContext
        // that IHttpContextAccessor (and therefore ICurrentUserProvider) depend on. Re-anchor
        // it explicitly from the context captured here by closure so downstream handlers can
        // still resolve the authenticated user.
        var httpContext = context.HttpContext;
        var httpContextAccessor = httpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                httpContextAccessor.HttpContext = httpContext;
                return CachedIdempotentResult.From(await next(context), requestBodyHash);
            },
            Options,
            tags: ["idempotency"],
            cancellationToken: context.HttpContext.RequestAborted);

        if (cached.RequestBodyHash != requestBodyHash)
        {
            // Not expressible via ErrorOr.ToProblemResult() (no ErrorType maps to 422), so the
            // body is built by hand here — same List<Error> shape as the rest of the API, just
            // with the 422 status code this case is documented to return (see
            // docs/idempotency-post-endpoints.md).
            var errors = new List<Error>
            {
                Error.Validation(
                    "Idempotency.KeyReused",
                    $"The '{HeaderName}' header was already used with a different request body."),
            };
            return Results.Json(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return cached.ToResult();
    }

    private static string ComputeRequestBodyHash(EndpointFilterInvocationContext context)
    {
        var body = context.Arguments.FirstOrDefault(argument => argument is not ISender);
        var json = JsonSerializer.Serialize(body);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }
}
