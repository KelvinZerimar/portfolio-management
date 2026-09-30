using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Common.Security;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace WebApi.MinimalAPI.Idempotency;

public sealed class IdempotencyFilter(HybridCache cache, ICurrentUserProvider currentUser) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private static readonly HybridCacheEntryOptions Options = new() { Expiration = TimeSpan.FromHours(24) };

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key) || string.IsNullOrWhiteSpace(key))
        {
            return Results.BadRequest($"The '{HeaderName}' header is required.");
        }

        var cacheKey = $"idempotency:{currentUser.UserId}:{context.HttpContext.Request.Path}:{key}";
        var requestBodyHash = ComputeRequestBodyHash(context);

        var cached = await cache.GetOrCreateAsync(
            cacheKey,
            async _ => CachedIdempotentResult.From(await next(context), requestBodyHash),
            Options,
            tags: ["idempotency"],
            cancellationToken: context.HttpContext.RequestAborted);

        if (cached.RequestBodyHash != requestBodyHash)
        {
            return Results.UnprocessableEntity(
                $"The '{HeaderName}' header was already used with a different request body.");
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
