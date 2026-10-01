using FluentValidation;

namespace Application.PortfolioEntries.Query;

public sealed class GetPortfolioEntriesQueryValidator : AbstractValidator<GetPortfolioEntriesQuery>
{
    public GetPortfolioEntriesQueryValidator()
    {
        RuleFor(x => x.PortfolioId)
            .GreaterThan(0);

        RuleFor(x => x.Paginator.Page)
            .GreaterThan(0);

        RuleFor(x => x.Paginator.Limit)
            .InclusiveBetween(1, 100);

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}
