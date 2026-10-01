using FluentValidation;

namespace Application.Portfolios.Command;

public sealed class RefreshPortfolioPricesCommandValidator : AbstractValidator<RefreshPortfolioPricesCommand>
{
    public RefreshPortfolioPricesCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);
    }
}
