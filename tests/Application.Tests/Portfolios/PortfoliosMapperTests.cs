using Application.Portfolios;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.Portfolios;

public class PortfoliosMapperTests
{
    private static Portfolio CreatePortfolio() => Portfolio.Create(userId: 1, name: "My Portfolio", description: "A description");

    [Fact]
    public void ToCreatePortfolioResponse_MapsIdAndName()
    {
        var portfolio = CreatePortfolio();

        var response = portfolio.ToCreatePortfolioResponse();

        response.Id.Should().Be(portfolio.Id);
        response.Name.Should().Be(portfolio.Name);
    }

    [Fact]
    public void ToUpdatePortfolioResponse_MapsAllFields()
    {
        var portfolio = CreatePortfolio();
        portfolio.UpdatedAt = DateTime.UtcNow;

        var response = portfolio.ToUpdatePortfolioResponse();

        response.Id.Should().Be(portfolio.Id);
        response.UserId.Should().Be(portfolio.UserId);
        response.Name.Should().Be(portfolio.Name);
        response.Description.Should().Be(portfolio.Description);
        response.CreatedAt.Should().Be(portfolio.CreatedAt);
        response.UpdatedAt.Should().Be(portfolio.UpdatedAt);
    }

    [Fact]
    public void ToPortfolioResponse_MapsAllFields()
    {
        var portfolio = CreatePortfolio();

        var response = portfolio.ToPortfolioResponse();

        response.Id.Should().Be(portfolio.Id);
        response.UserId.Should().Be(portfolio.UserId);
        response.Name.Should().Be(portfolio.Name);
        response.Description.Should().Be(portfolio.Description);
        response.CreatedAt.Should().Be(portfolio.CreatedAt);
        response.UpdatedAt.Should().Be(portfolio.UpdatedAt);
    }
}
