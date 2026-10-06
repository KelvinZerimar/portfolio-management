using Application.Notes;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.Notes;

public class NotesMapperTests
{
    private static Note CreateNote() => Note.Create("Personal", "My title", "My content", isActive: true);

    [Fact]
    public void ToCreateNoteResponse_MapsAllFields()
    {
        var note = CreateNote();

        var response = note.ToCreateNoteResponse();

        response.Id.Should().Be(note.Id);
        response.Category.Should().Be(note.Category);
        response.Title.Should().Be(note.Title);
        response.Content.Should().Be(note.Content);
        response.CreatedAt.Should().Be(note.CreatedAt);
        response.IsActive.Should().Be(note.IsActive);
    }

    [Fact]
    public void ToUpdateNoteResponse_MapsAllFields()
    {
        var note = CreateNote();

        var response = note.ToUpdateNoteResponse();

        response.Id.Should().Be(note.Id);
        response.Category.Should().Be(note.Category);
        response.Title.Should().Be(note.Title);
        response.Content.Should().Be(note.Content);
        response.CreatedAt.Should().Be(note.CreatedAt);
        response.IsActive.Should().Be(note.IsActive);
    }

    [Fact]
    public void ToNoteResponse_MapsAllFields()
    {
        var note = CreateNote();

        var response = note.ToNoteResponse();

        response.Id.Should().Be(note.Id);
        response.Category.Should().Be(note.Category);
        response.Title.Should().Be(note.Title);
        response.Content.Should().Be(note.Content);
        response.CreatedAt.Should().Be(note.CreatedAt);
        response.IsActive.Should().Be(note.IsActive);
    }
}
