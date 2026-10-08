using Application.Common.Messaging;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Reports.Interfaces;
using Application.Users.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Reports.Command;

public sealed record SendPortfolioStatusReportCommand(long PortfolioId, DateTime PeriodStart, DateTime PeriodEnd)
    : IRequest<ErrorOr<Success>>, ICommand;

public sealed class SendPortfolioStatusReportCommandHandler(
    ILogger<SendPortfolioStatusReportCommandHandler> logger,
    IPortfolioRepository portfolioRepository,
    IPortfolioEntryRepository portfolioEntryRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    TimeProvider timeProvider
    ) : IRequestHandler<SendPortfolioStatusReportCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SendPortfolioStatusReportCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.PortfolioId, cancellationToken);
        if (portfolio is null)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.PortfolioId}' was not found.");
        }

        var user = await userRepository.GetByIdAsync(portfolio.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User with ID '{portfolio.UserId}' was not found.");
        }

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(command.PortfolioId, cancellationToken);
        var report = portfolio.GetStatusReport(entries, command.PeriodStart, command.PeriodEnd);

        var subject = PortfolioStatusReportEmailTemplate.RenderSubject(portfolio.Name, report);
        var body = PortfolioStatusReportEmailTemplate.RenderBody(portfolio.Name, report);
        await emailSender.SendAsync(user.Email, subject, body, cancellationToken);

        portfolio.LastStatusReportSentAt = timeProvider.GetUtcNow().UtcDateTime;
        portfolioRepository.Update(portfolio);

        logger.LogInformation(
            "Sent status report email for portfolio {PortfolioId} to {Email}", command.PortfolioId, user.Email);

        return Result.Success;
    }
}
