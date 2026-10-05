using Contracts.Common;
using Domain.Entities;

namespace Application.Notes.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<PaginatorResponse<Note>> GetAllAsync(
        int page, int limit, string? category, bool? isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<Note>> GetLatestAsync(int count, CancellationToken cancellationToken);

    Task AddAsync(Note note, CancellationToken cancellationToken);

    // originalCategory is the Category the note had before this update (as loaded by
    // GetByIdAsync) — needed because the container's partition key is /category: if Category
    // changed, the document has to move partitions (create under the new one, delete the old).
    Task UpdateAsync(Note note, string originalCategory, CancellationToken cancellationToken);

    Task DeleteAsync(string id, string category, CancellationToken cancellationToken);
}
