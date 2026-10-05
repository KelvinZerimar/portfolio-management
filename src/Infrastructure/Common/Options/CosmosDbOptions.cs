using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Common.Options;

public sealed class CosmosDbOptions
{
    public const string SectionName = "CosmosDb";

    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = string.Empty;
}
