using Application.Exchanges.Command;
using Contracts.Exchanges;
using FluentValidation.TestHelper;

namespace Application.Tests.Exchanges.Command;

public class CreateExchangeCommandValidatorTests
{
    private readonly CreateExchangeCommandValidator _validator = new();

    private static CreateExchangeRequest CreateRequest(string name = "Binance", string apiKey = "api-key") => new()
    {
        Name = name,
        ApiKey = apiKey
    };

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateExchangeCommand(CreateRequest()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreateExchangeCommand(CreateRequest(name: string.Empty)));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }

    [Fact]
    public void Validate_WithNameLongerThan100Characters_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreateExchangeCommand(CreateRequest(name: new string('a', 101))));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }

    [Fact]
    public void Validate_WithEmptyApiKey_HasErrorForApiKey()
    {
        var result = _validator.TestValidate(new CreateExchangeCommand(CreateRequest(apiKey: string.Empty)));

        result.ShouldHaveValidationErrorFor(x => x.Request.ApiKey);
    }

    [Fact]
    public void Validate_WithApiKeyLongerThan500Characters_HasErrorForApiKey()
    {
        var result = _validator.TestValidate(new CreateExchangeCommand(CreateRequest(apiKey: new string('a', 501))));

        result.ShouldHaveValidationErrorFor(x => x.Request.ApiKey);
    }
}
