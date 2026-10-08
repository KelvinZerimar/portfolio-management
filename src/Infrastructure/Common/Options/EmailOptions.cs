using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Common.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public string? Username { get; set; }

    // Set via `dotnet user-secrets`, never in appsettings.json.
    public string? Password { get; set; }

    [Required]
    public string SenderEmail { get; set; } = string.Empty;

    public string SenderName { get; set; } = "Portfolio Tracker";
}
