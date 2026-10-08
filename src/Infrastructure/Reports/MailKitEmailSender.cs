using Application.Reports.Interfaces;
using Infrastructure.Common.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Reports;

internal sealed class MailKitEmailSender(
    IOptions<EmailOptions> options,
    ILogger<MailKitEmailSender> logger
    ) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

        using var client = new SmtpClient();
        // Auto negotiates the right scheme for the port - STARTTLS on 587, implicit SSL on 465 -
        // instead of needing a separate UseSsl flag that's easy to misconfigure for 587.
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, SecureSocketOptions.Auto, cancellationToken);
        if (!string.IsNullOrEmpty(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Sent email to {Recipient} with subject {Subject}", to, subject);
    }
}
