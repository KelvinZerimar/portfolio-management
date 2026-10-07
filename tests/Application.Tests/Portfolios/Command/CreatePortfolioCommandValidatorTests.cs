using Application.Portfolios.Command;
using Contracts.Portfolios;
using FluentValidation.TestHelper;

namespace Application.Tests.Portfolios.Command;

public class CreatePortfolioCommandValidatorTests
{
    private readonly CreatePortfolioCommandValidator _validator = new();

    private static CreatePortfolioRequest CreateRequest(string name = "My Portfolio", string description = "A description") => new()
    {
        Name = name,
        Description = description
    };

    [Fact]
    public void Validate_WithValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreatePortfolioCommand(CreateRequest()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyName_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreatePortfolioCommand(CreateRequest(name: string.Empty)));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }

    [Fact]
    public void Validate_WithNameLongerThan100Characters_HasErrorForName()
    {
        var result = _validator.TestValidate(new CreatePortfolioCommand(CreateRequest(name: new string('a', 101))));

        result.ShouldHaveValidationErrorFor(x => x.Request.Name);
    }

    [Fact]
    public void Validate_WithDescriptionLongerThan500Characters_HasErrorForDescription()
    {
        var result = _validator.TestValidate(new CreatePortfolioCommand(CreateRequest(description: new string('a', 501))));

        result.ShouldHaveValidationErrorFor(x => x.Request.Description);
    }

    [Fact]
    public void Validate_WithEmptyDescription_HasNoErrorForDescription()
    {
        var result = _validator.TestValidate(new CreatePortfolioCommand(CreateRequest(description: string.Empty)));

        result.ShouldNotHaveValidationErrorFor(x => x.Request.Description);
    }
}
