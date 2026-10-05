namespace Domain.Entities;

public sealed class Note
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    public static Note Create(string category, string title, string content, bool isActive, DateTime? createdAt = null)
    {
        return new Note
        {
            Id = Guid.NewGuid().ToString(),
            Category = category,
            Title = title,
            Content = content,
            IsActive = isActive,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}
