using Application.Common.Behaviors;
using Application.Common.Caching;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Application.Tests.Common.Behaviors;

public class CacheInvalidationBehaviorTests
{
    private sealed record FakeCommand(IReadOnlyCollection<string> CacheTagsToInvalidate)
        : IInvalidatesCache, IRequest<ErrorOr<int>>;

    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    private CacheInvalidationBehavior<FakeCommand, ErrorOr<int>> CreateBehavior() => new(_cache);

    [Fact]
    public async Task Handle_WhenResponseIsNotError_InvalidatesAllCacheTags()
    {
        var command = new FakeCommand(["tag-a", "tag-b"]);
        RequestHandlerDelegate<ErrorOr<int>> next = _ => Task.FromResult<ErrorOr<int>>(42);

        var response = await CreateBehavior().Handle(command, next, CancellationToken.None);

        response.IsError.Should().BeFalse();
        await _cache.Received(1).RemoveByTagAsync("tag-a", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync("tag-b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenResponseIsError_DoesNotInvalidateCache()
    {
        var command = new FakeCommand(["tag-a"]);
        RequestHandlerDelegate<ErrorOr<int>> next = _ => Task.FromResult<ErrorOr<int>>(Error.Failure("Fake.Failure"));

        var response = await CreateBehavior().Handle(command, next, CancellationToken.None);

        response.IsError.Should().BeTrue();
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
