using FluentValidation;

namespace Application.Notes.Query;

public sealed class GetNotesQueryValidator : AbstractValidator<GetNotesQuery>
{
    public GetNotesQueryValidator()
    {
        RuleFor(x => x.Paginator.Page)
            .GreaterThan(0);

        RuleFor(x => x.Paginator.Limit)
            .InclusiveBetween(1, 100);
    }
}
