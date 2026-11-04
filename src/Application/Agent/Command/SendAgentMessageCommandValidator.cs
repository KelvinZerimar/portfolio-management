using FluentValidation;

namespace Application.Agent.Command;

public sealed class SendAgentMessageCommandValidator : AbstractValidator<SendAgentMessageCommand>
{
    public SendAgentMessageCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request cannot be null.");

        RuleFor(x => x.Request.Message)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
