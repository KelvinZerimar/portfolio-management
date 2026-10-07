using Application.Common.Behaviors;
using Application.Common.Messaging;
using Application.Common.UnitOfWork;
using ErrorOr;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Application.Tests.Common.Behaviors;

public class UnitOfWorkBehaviorTests
{
    private sealed record FakeCommand : ICommand, IRequest<ErrorOr<int>>;

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UnitOfWorkBehavior<FakeCommand, ErrorOr<int>> CreateBehavior() => new(_unitOfWork);

    [Fact]
    public async Task Handle_WhenResponseIsNotError_CommitsViaUnitOfWork()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = _ => Task.FromResult<ErrorOr<int>>(42);

        var response = await CreateBehavior().Handle(new FakeCommand(), next, CancellationToken.None);

        response.IsError.Should().BeFalse();
        response.Value.Should().Be(42);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenResponseIsError_DoesNotCommit()
    {
        RequestHandlerDelegate<ErrorOr<int>> next = _ => Task.FromResult<ErrorOr<int>>(Error.Failure("Fake.Failure"));

        var response = await CreateBehavior().Handle(new FakeCommand(), next, CancellationToken.None);

        response.IsError.Should().BeTrue();
        response.FirstError.Code.Should().Be("Fake.Failure");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
