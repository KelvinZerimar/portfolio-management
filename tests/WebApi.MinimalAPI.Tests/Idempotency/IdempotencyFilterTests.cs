using System.Text.Json;
using Application.Common.Security;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WebApi.MinimalAPI.Idempotency;

namespace WebApi.MinimalAPI.Tests.Idempotency;

public class IdempotencyFilterTests
{
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private sealed record FakeRequest(string Value);

    public IdempotencyFilterTests()
    {
        _currentUserProvider.UserId.Returns(1L);
    }

    // HybridCache always round-trips its stored value through serialization, even for a pure
    // in-process cache (no distributed backend) - so comparing JSON content, not CLR type/reference,
    // is what actually matches production behavior for a replayed response.
    private static string ToJson(object? value) => JsonSerializer.Serialize(value);

    private (IdempotencyFilter Filter, IServiceProvider Services) CreateFilter()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();

        return (new IdempotencyFilter(provider.GetRequiredService<HybridCache>(), _currentUserProvider), provider);
    }

    private EndpointFilterInvocationContext CreateContext(IServiceProvider services, string path, string? idempotencyKey, FakeRequest request)
    {
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Path = path;
        if (idempotencyKey is not null)
        {
            httpContext.Request.Headers[IdempotencyFilter.HeaderName] = idempotencyKey;
        }

        return EndpointFilterInvocationContext.Create(httpContext, _sender, request);
    }

    [Fact]
    public async Task InvokeAsync_WithoutIdempotencyKeyHeader_ReturnsBadRequestWithoutCallingNext()
    {
        var (filter, services) = CreateFilter();
        var context = CreateContext(services, "/api/v1/Portfolio", idempotencyKey: null, new FakeRequest("a"));
        var nextCalls = 0;
        EndpointFilterDelegate next = _ => { nextCalls++; return ValueTask.FromResult<object?>(TypedResults.Ok("unreachable")); };

        var result = await filter.InvokeAsync(context, next);

        nextCalls.Should().Be(0);
        (result as IStatusCodeHttpResult)?.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_FirstCallWithKey_InvokesNextAndReturnsItsResult()
    {
        var (filter, services) = CreateFilter();
        var context = CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("a"));
        var nextCalls = 0;
        EndpointFilterDelegate next = _ => { nextCalls++; return ValueTask.FromResult<object?>(TypedResults.Ok("created-response")); };

        var result = await filter.InvokeAsync(context, next);

        nextCalls.Should().Be(1);
        (result as IStatusCodeHttpResult)?.StatusCode.Should().Be(StatusCodes.Status200OK);
        ToJson((result as IValueHttpResult)?.Value).Should().Be(ToJson("created-response"));
    }

    [Fact]
    public async Task InvokeAsync_SecondCallWithSameKeyAndSameBody_ReplaysCachedResponseWithoutCallingNextAgain()
    {
        var (filter, services) = CreateFilter();
        var nextCalls = 0;
        EndpointFilterDelegate next = _ => { nextCalls++; return ValueTask.FromResult<object?>(TypedResults.Ok($"response-{nextCalls}")); };

        var first = await filter.InvokeAsync(CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("a")), next);
        var second = await filter.InvokeAsync(CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("a")), next);

        nextCalls.Should().Be(1);
        (second as IStatusCodeHttpResult)?.StatusCode.Should().Be(StatusCodes.Status200OK);
        ToJson((second as IValueHttpResult)?.Value).Should().Be(ToJson((first as IValueHttpResult)?.Value));
    }

    [Fact]
    public async Task InvokeAsync_SecondCallWithSameKeyButDifferentBody_ReturnsUnprocessableEntityWithoutCallingNextAgain()
    {
        var (filter, services) = CreateFilter();
        var nextCalls = 0;
        EndpointFilterDelegate next = _ => { nextCalls++; return ValueTask.FromResult<object?>(TypedResults.Ok("response")); };

        var first = await filter.InvokeAsync(CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("a")), next);
        var second = await filter.InvokeAsync(CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("different")), next);

        nextCalls.Should().Be(1);
        (first as IStatusCodeHttpResult)?.StatusCode.Should().Be(StatusCodes.Status200OK);
        (second as IStatusCodeHttpResult)?.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task InvokeAsync_WithSameKeyOnDifferentPaths_AreTreatedAsIndependentRequests()
    {
        var (filter, services) = CreateFilter();
        var nextCalls = 0;
        EndpointFilterDelegate next = _ => { nextCalls++; return ValueTask.FromResult<object?>(TypedResults.Ok($"response-{nextCalls}")); };

        await filter.InvokeAsync(CreateContext(services, "/api/v1/Portfolio", "key-1", new FakeRequest("a")), next);
        await filter.InvokeAsync(CreateContext(services, "/api/v1/Exchange", "key-1", new FakeRequest("a")), next);

        nextCalls.Should().Be(2);
    }
}
