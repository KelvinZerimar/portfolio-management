using Newtonsoft.Json;

namespace Infrastructure.Notes;

// Maps 1:1 to the existing Cosmos DB item shape (note the irregular "create" property name —
// not renamed to keep compatibility with documents already stored). Content is the AES-encrypted
// ciphertext here; NoteRepository is the only place that encrypts/decrypts it.
internal sealed class NoteDocument
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("create")]
    public DateTime Create { get; set; }

    [JsonProperty("isActive")]
    public bool IsActive { get; set; }
}
