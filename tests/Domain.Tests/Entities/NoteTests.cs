using Domain.Entities;
using FluentAssertions;

namespace Domain.Tests.Entities;

public class NoteTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsNoteWithExpectedProperties()
    {
        var note = Note.Create("Service", "Sanitas", "some plaintext content", isActive: true);

        note.Id.Should().NotBeNullOrWhiteSpace();
        note.Category.Should().Be("Service");
        note.Title.Should().Be("Sanitas");
        note.Content.Should().Be("some plaintext content");
        note.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutCreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow;
        var note = Note.Create("Service", "Sanitas", "content", isActive: true);
        var after = DateTime.UtcNow;

        note.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithExplicitCreatedAt_UsesThatValue()
    {
        var createdAt = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

        var note = Note.Create("Service", "Sanitas", "content", isActive: true, createdAt);

        note.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Create_CalledTwice_ReturnsDistinctIds()
    {
        var first = Note.Create("Service", "A", "content", isActive: true);
        var second = Note.Create("Service", "B", "content", isActive: true);

        first.Id.Should().NotBe(second.Id);
    }
}
