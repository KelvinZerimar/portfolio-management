using FluentValidation;

namespace Application.Notes.Query;

public sealed class GetLatestNotesQueryValidator : AbstractValidator<GetLatestNotesQuery>
{
    public GetLatestNotesQueryValidator()
    {
        RuleFor(x => x.Count)
            .InclusiveBetween(1, 50);
    }
}
