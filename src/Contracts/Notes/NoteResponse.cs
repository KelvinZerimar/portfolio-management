
namespace Contracts.Notes;

public sealed record NoteResponse(
    string Id,
    string Category,
    string Title,
    string Content,
    DateTime CreatedAt,
    bool IsActive
    );
