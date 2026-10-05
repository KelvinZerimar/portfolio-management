using Application.Notes.Interfaces;
using Contracts.Notes;
using ErrorOr;
using MediatR;

namespace Application.Notes.Query;

public sealed record GetLatestNotesQuery(int Count = 10) : IRequest<ErrorOr<List<NoteResponse>>>;

public sealed class GetLatestNotesQueryHandler(
    INoteRepository noteRepository
    ) : IRequestHandler<GetLatestNotesQuery, ErrorOr<List<NoteResponse>>>
{
    public async Task<ErrorOr<List<NoteResponse>>> Handle(GetLatestNotesQuery query, CancellationToken cancellationToken)
    {
        var notes = await noteRepository.GetLatestAsync(query.Count, cancellationToken);

        return notes.Select(n => n.ToNoteResponse()).ToList();
    }
}
