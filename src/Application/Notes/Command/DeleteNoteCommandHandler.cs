using Application.Notes.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Notes.Command;

public sealed record DeleteNoteCommand(string Id) : IRequest<ErrorOr<Deleted>>;

public sealed class DeleteNoteCommandHandler(
    ILogger<DeleteNoteCommandHandler> logger,
    INoteRepository noteRepository
    ) : IRequestHandler<DeleteNoteCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteNoteCommand command, CancellationToken cancellationToken)
    {
        var note = await noteRepository.GetByIdAsync(command.Id, cancellationToken);
        if (note is null)
        {
            return Error.NotFound("Note.NotFound", $"Note with ID '{command.Id}' was not found.");
        }

        await noteRepository.DeleteAsync(command.Id, note.Category, cancellationToken);

        logger.LogInformation("Deleted note with ID {NoteId}", command.Id);

        return Result.Deleted;
    }
}
