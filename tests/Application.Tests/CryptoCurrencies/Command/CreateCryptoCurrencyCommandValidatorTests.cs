using Application.CryptoCurrencies.Command;
using Contracts.CryptoCurrencies;
using FluentValidation.TestHelper;

namespace Application.Tests.CryptoCurrencies.Command;

public class CreateCryptoCurrencyCommandValidatorTests
{
    private readonly CreateCryptoCurrencyCommandValidator _validator = new();

    private static CreateCryptoCurrencyRequest CreateRequest(string symbol = "BTC", string name = "Bitcoin") => new()
    {
        Symbol = symbol,
        Name = name
    };

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateCryptoCurrencyCommand(CreateRequest()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptySymbol_HasErrorForSymbol()
    {
        var result = _validator.TestValidate(new CreateCryptoCurrencyCommand(CreateRequest(symbol: string.Empty)));

        result.ShouldHaveValidationErrorFor(x => x.Request.Symbol);
    }

    [Fact]
    public void Validate_WithSymbolLongerThan20Characters_HasErrorForSymbol()
    {
        var result = _validator.TestValidate(new CreateCryptoCurrencyCommand(CreateRequest(symbol: new string('a', 21))));

        result.ShouldHaveValidationErrorFor(x => x.Request.Symbol);
    }

    [Fact]
    public void Validate_WithEmptyName_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreateCryptoCurrencyCommand(CreateRequest(name: string.Empty)));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }

    [Fact]
    public void Validate_WithNameLongerThan100Characters_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreateCryptoCurrencyCommand(CreateRequest(name: new string('a', 101))));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }
}
