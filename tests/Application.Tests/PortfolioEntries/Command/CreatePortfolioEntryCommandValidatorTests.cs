using Application.PortfolioEntries.Command;
using Contracts.PortfolioEntries;
using FluentValidation.TestHelper;

namespace Application.Tests.PortfolioEntries.Command;

public class CreatePortfolioEntryCommandValidatorTests
{
    private readonly CreatePortfolioEntryCommandValidator _validator = new();

    private static CreatePortfolioEntryRequest CreateRequest(
        long portfolioId = 1,
        long cryptoCurrencyId = 1,
        long exchangeId = 1,
        decimal quantity = 2m,
        decimal pricePerUnit = 100m,
        DateTime? recordedAt = null) => new()
        {
            PortfolioId = portfolioId,
            CryptoCurrencyId = cryptoCurrencyId,
            ExchangeId = exchangeId,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            RecordedAt = recordedAt ?? DateTime.UtcNow
        };

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePortfolioId_HasErrorForPortfolioId(long portfolioId)
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(portfolioId: portfolioId)));

        result.ShouldHaveValidationErrorFor(x => x.Request.PortfolioId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveCryptoCurrencyId_HasErrorForCryptoCurrencyId(long cryptoCurrencyId)
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(cryptoCurrencyId: cryptoCurrencyId)));

        result.ShouldHaveValidationErrorFor(x => x.Request.CryptoCurrencyId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveExchangeId_HasErrorForExchangeId(long exchangeId)
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(exchangeId: exchangeId)));

        result.ShouldHaveValidationErrorFor(x => x.Request.ExchangeId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveQuantity_HasErrorForQuantity(decimal quantity)
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(quantity: quantity)));

        result.ShouldHaveValidationErrorFor(x => x.Request.Quantity);
    }

    [Fact]
    public void Validate_WithNegativePricePerUnit_HasErrorForPricePerUnit()
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(pricePerUnit: -0.01m)));

        result.ShouldHaveValidationErrorFor(x => x.Request.PricePerUnit);
    }

    [Fact]
    public void Validate_WithZeroPricePerUnit_HasNoErrorForPricePerUnit()
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(pricePerUnit: 0m)));

        result.ShouldNotHaveValidationErrorFor(x => x.Request.PricePerUnit);
    }

    [Fact]
    public void Validate_WithDefaultRecordedAt_HasErrorForRecordedAt()
    {
        var result = _validator.TestValidate(new CreatePortfolioEntryCommand(CreateRequest(recordedAt: default(DateTime))));

        result.ShouldHaveValidationErrorFor(x => x.Request.RecordedAt);
    }
}
