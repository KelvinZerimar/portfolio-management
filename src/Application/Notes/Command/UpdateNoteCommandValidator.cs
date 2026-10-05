using FluentValidation;

namespace Application.Notes.Command;

public sealed class UpdateNoteCommandValidator : AbstractValidator<UpdateNoteCommand>
{
    public UpdateNoteCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request cannot be null.");

        RuleFor(x => x.Request.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Request.Content)
            .NotEmpty();
    }
}
