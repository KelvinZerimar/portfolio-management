using Application.Notes.Interfaces;
using Contracts.Notes;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Notes.Command;

public sealed record UpdateNoteCommand(string Id, UpdateNoteRequest Request) : IRequest<ErrorOr<UpdateNoteResponse>>;

public sealed class UpdateNoteCommandHandler(
    ILogger<UpdateNoteCommandHandler> logger,
    INoteRepository noteRepository
    ) : IRequestHandler<UpdateNoteCommand, ErrorOr<UpdateNoteResponse>>
{
    public async Task<ErrorOr<UpdateNoteResponse>> Handle(UpdateNoteCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var note = await noteRepository.GetByIdAsync(command.Id, cancellationToken);
        if (note is null)
        {
            return Error.NotFound("Note.NotFound", $"Note with ID '{command.Id}' was not found.");
        }

        var originalCategory = note.Category;

        note.Category = request.Category;
        note.Title = request.Title;
        note.Content = request.Content;
        note.IsActive = request.IsActive;

        await noteRepository.UpdateAsync(note, originalCategory, cancellationToken);

        logger.LogInformation("Updated note with ID {NoteId}", note.Id);

        return note.ToUpdateNoteResponse();
    }
}
