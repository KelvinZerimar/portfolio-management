using Application.Exchanges;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.Exchanges;

public class ExchangesMapperTests
{
    private static Exchange CreateExchange() => Exchange.Create("Binance", "api-key");

    [Fact]
    public void ToCreateExchangeResponse_MapsAllFields()
    {
        var exchange = CreateExchange();

        var response = exchange.ToCreateExchangeResponse();

        response.Id.Should().Be(exchange.Id);
        response.Name.Should().Be(exchange.Name);
        response.ApiKey.Should().Be(exchange.ApiKey);
        response.CreatedAt.Should().Be(exchange.CreatedAt);
    }

    [Fact]
    public void ToUpdateExchangeResponse_MapsAllFields()
    {
        var exchange = CreateExchange();

        var response = exchange.ToUpdateExchangeResponse();

        response.Id.Should().Be(exchange.Id);
        response.Name.Should().Be(exchange.Name);
        response.ApiKey.Should().Be(exchange.ApiKey);
        response.CreatedAt.Should().Be(exchange.CreatedAt);
    }

    [Fact]
    public void ToExchangeResponse_MapsAllFields()
    {
        var exchange = CreateExchange();

        var response = exchange.ToExchangeResponse();

        response.Id.Should().Be(exchange.Id);
        response.Name.Should().Be(exchange.Name);
        response.ApiKey.Should().Be(exchange.ApiKey);
        response.CreatedAt.Should().Be(exchange.CreatedAt);
    }
}
