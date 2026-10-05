using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Common.Options;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    // Base64-encoded 32-byte (AES-256) key. Set via `dotnet user-secrets`, never in appsettings.json.
    [Required]
    public string Key { get; set; } = string.Empty;
}
