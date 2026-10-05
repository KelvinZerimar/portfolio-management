using System.Net;
using Application.Common.Security;
using Application.Notes.Interfaces;
using Contracts.Common;
using Domain.Entities;
using Infrastructure.Common.Options;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notes;

internal sealed class NoteRepository(
    CosmosClient cosmosClient,
    IOptions<CosmosDbOptions> options,
    IEncryptionService encryptionService) : INoteRepository
{
    private Container Container => cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);

    // Partition key is /category, not /id, so a point ReadItemAsync would need the category
    // up front — callers only have the id. A cross-partition query is the simple, robust
    // alternative; fine at this dataset's scale.
    public async Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var query = Container.GetItemLinqQueryable<NoteDocument>()
            .Where(d => d.Id == id)
            .Take(1);

        using var iterator = query.ToFeedIterator();
        if (!iterator.HasMoreResults)
        {
            return null;
        }

        var response = await iterator.ReadNextAsync(cancellationToken);
        var document = response.FirstOrDefault();
        return document is null ? null : ToNote(document);
    }

    public async Task<PaginatorResponse<Note>> GetAllAsync(
        int page, int limit, string? category, bool? isActive, CancellationToken cancellationToken)
    {
        IQueryable<NoteDocument> query = Container.GetItemLinqQueryable<NoteDocument>();

        if (category is not null)
        {
            query = query.Where(d => d.Category == category);
        }

        if (isActive is not null)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var ordered = query.OrderByDescending(d => d.Create).Skip((page - 1) * limit).Take(limit);

        var items = new List<NoteDocument>();
        using (var iterator = ordered.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                items.AddRange(response);
            }
        }

        return new PaginatorResponse<Note>
        {
            Page = page,
            PageSize = limit,
            Total = total,
            TotalPages = limit == 0 ? 0 : (int)Math.Ceiling(total / (double)limit),
            Data = items.Select(ToNote).ToList()
        };
    }

    public async Task<IReadOnlyList<Note>> GetLatestAsync(int count, CancellationToken cancellationToken)
    {
        var query = Container.GetItemLinqQueryable<NoteDocument>()
            .OrderByDescending(d => d.Create)
            .Take(count);

        var items = new List<NoteDocument>();
        using (var iterator = query.ToFeedIterator())
        {
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                items.AddRange(response);
            }
        }

        return items.Select(ToNote).ToList();
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken)
    {
        var document = ToDocument(note);
        await Container.CreateItemAsync(document, new PartitionKey(document.Category), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Note note, string originalCategory, CancellationToken cancellationToken)
    {
        var document = ToDocument(note);

        if (note.Category == originalCategory)
        {
            await Container.UpsertItemAsync(document, new PartitionKey(document.Category), cancellationToken: cancellationToken);
            return;
        }

        // Category changed: the document belongs to a different partition now. Create under the
        // new partition first, then remove the old one — if the delete fails, we're left with a
        // harmless duplicate instead of having lost the note.
        await Container.CreateItemAsync(document, new PartitionKey(document.Category), cancellationToken: cancellationToken);
        await DeleteAsync(note.Id, originalCategory, cancellationToken);
    }

    public async Task DeleteAsync(string id, string category, CancellationToken cancellationToken)
    {
        try
        {
            await Container.DeleteItemAsync<NoteDocument>(id, new PartitionKey(category), cancellationToken: cancellationToken);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // DeleteNoteCommandHandler already checks existence before calling this — defensive only.
        }
    }

    private Note ToNote(NoteDocument document) => new()
    {
        Id = document.Id,
        Category = document.Category,
        Title = document.Title,
        Content = encryptionService.Decrypt(document.Content),
        CreatedAt = document.Create,
        IsActive = document.IsActive
    };

    private NoteDocument ToDocument(Note note) => new()
    {
        Id = note.Id,
        Category = note.Category,
        Title = note.Title,
        Content = encryptionService.Encrypt(note.Content),
        Create = note.CreatedAt,
        IsActive = note.IsActive
    };
}
