
namespace Contracts.Notes;

public sealed record UpdateNoteResponse(
    string Id,
    string Category,
    string Title,
    string Content,
    DateTime CreatedAt,
    bool IsActive
    );
