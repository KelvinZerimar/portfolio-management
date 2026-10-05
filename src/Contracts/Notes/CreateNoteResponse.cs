
namespace Contracts.Notes;

public sealed record CreateNoteResponse(
    string Id,
    string Category,
    string Title,
    string Content,
    DateTime CreatedAt,
    bool IsActive
    );
