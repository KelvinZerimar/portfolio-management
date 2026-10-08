using FluentValidation;

namespace Application.Reports.Command;

public sealed class SendPortfolioStatusReportCommandValidator : AbstractValidator<SendPortfolioStatusReportCommand>
{
    public SendPortfolioStatusReportCommandValidator()
    {
        RuleFor(x => x.PortfolioId).GreaterThan(0);
        RuleFor(x => x.PeriodStart).LessThanOrEqualTo(x => x.PeriodEnd);
    }
}
