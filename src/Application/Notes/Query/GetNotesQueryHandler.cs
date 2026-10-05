using Application.Notes.Interfaces;
using Contracts.Common;
using Contracts.Notes;
using ErrorOr;
using MediatR;

namespace Application.Notes.Query;

public sealed record GetNotesQuery(PaginatorRequest Paginator, string? Category, bool? IsActive)
    : IRequest<ErrorOr<PaginatorResponse<NoteResponse>>>;

public sealed class GetNotesQueryHandler(
    INoteRepository noteRepository
    ) : IRequestHandler<GetNotesQuery, ErrorOr<PaginatorResponse<NoteResponse>>>
{
    public async Task<ErrorOr<PaginatorResponse<NoteResponse>>> Handle(GetNotesQuery query, CancellationToken cancellationToken)
    {
        var result = await noteRepository.GetAllAsync(
            query.Paginator.Page, query.Paginator.Limit, query.Category, query.IsActive, cancellationToken);

        return new PaginatorResponse<NoteResponse>
        {
            Page = result.Page,
            PageSize = result.PageSize,
            Total = result.Total,
            TotalPages = result.TotalPages,
            Data = result.Data.Select(n => n.ToNoteResponse()).ToList()
        };
    }
}
