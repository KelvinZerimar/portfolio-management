using Application.Notes.Interfaces;
using Contracts.Notes;
using Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Notes.Command;

// No ICommand/IInvalidatesCache: notes live in Cosmos DB, not behind AppDbContext's
// IUnitOfWork, and NoteRepository commits directly (see NoteRepository.AddAsync) —
// there's no shared change tracker for the MediatR pipeline to flush.
public sealed record CreateNoteCommand(CreateNoteRequest Request) : IRequest<ErrorOr<CreateNoteResponse>>;

public sealed class CreateNoteCommandHandler(
    ILogger<CreateNoteCommandHandler> logger,
    INoteRepository noteRepository
    ) : IRequestHandler<CreateNoteCommand, ErrorOr<CreateNoteResponse>>
{
    public async Task<ErrorOr<CreateNoteResponse>> Handle(CreateNoteCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var note = Note.Create(request.Category, request.Title, request.Content, request.IsActive, request.CreatedAt);

        await noteRepository.AddAsync(note, cancellationToken);

        logger.LogInformation("Created new note with ID {NoteId}", note.Id);

        return note.ToCreateNoteResponse();
    }
}
