using FluentValidation;

namespace Application.Reports.Command;

public sealed class TriggerPortfolioStatusReportCommandValidator : AbstractValidator<TriggerPortfolioStatusReportCommand>
{
    public TriggerPortfolioStatusReportCommandValidator()
    {
        RuleFor(x => x.PortfolioId).GreaterThan(0);

        When(x => x.PeriodStart.HasValue && x.PeriodEnd.HasValue, () =>
        {
            RuleFor(x => x).Must(x => x.PeriodStart!.Value <= x.PeriodEnd!.Value)
                .WithMessage("PeriodStart must be on or before PeriodEnd.");
        });
    }
}
