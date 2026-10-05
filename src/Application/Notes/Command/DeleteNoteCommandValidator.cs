using FluentValidation;

namespace Application.Notes.Command;

public sealed class DeleteNoteCommandValidator : AbstractValidator<DeleteNoteCommand>
{
    public DeleteNoteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
