using System.Linq.Expressions;
using Application.Common.Security;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Contracts.Common;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfoliosQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;

    private GetPortfoliosQueryHandler CreateHandler() => new(_portfolioRepository, _currentUserProvider);

    public GetPortfoliosQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_ReturnsMappedPaginatedPortfolios()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", "A description");
        _portfolioRepository.GetAllAsync(1, 10, Arg.Any<Expression<Func<Portfolio, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatorResponse<Portfolio>
            {
                Page = 1,
                PageSize = 10,
                Total = 1,
                TotalPages = 1,
                Data = [portfolio]
            });

        var result = await CreateHandler().Handle(new GetPortfoliosQuery(new PaginatorRequest(1, 10)), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Total.Should().Be(1);
        result.Value.Data.Should().ContainSingle().Which.Name.Should().Be("My Portfolio");
    }

    [Fact]
    public async Task Handle_PassesRequestedPageAndLimitToRepository()
    {
        _portfolioRepository.GetAllAsync(2, 5, Arg.Any<Expression<Func<Portfolio, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatorResponse<Portfolio> { Page = 2, PageSize = 5, Total = 0, TotalPages = 0, Data = [] });

        await CreateHandler().Handle(new GetPortfoliosQuery(new PaginatorRequest(2, 5)), CancellationToken.None);

        await _portfolioRepository.Received(1).GetAllAsync(2, 5, Arg.Any<Expression<Func<Portfolio, bool>>>(), Arg.Any<CancellationToken>());
    }
}
