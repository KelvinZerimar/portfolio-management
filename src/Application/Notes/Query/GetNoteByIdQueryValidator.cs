using FluentValidation;

namespace Application.Notes.Query;

public sealed class GetNoteByIdQueryValidator : AbstractValidator<GetNoteByIdQuery>
{
    public GetNoteByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
