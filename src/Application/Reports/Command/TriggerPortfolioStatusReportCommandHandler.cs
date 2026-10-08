using Application.Common.Security;
using Application.Portfolios.Interfaces;
using ErrorOr;
using MediatR;

namespace Application.Reports.Command;

// HTTP-facing entry point for sending a portfolio's status report on demand, instead of waiting
// for the scheduled day of the month. Unlike SendPortfolioStatusReportCommand (trusted, used by
// the Worker's background sweep with no current user), this one checks ownership before
// delegating - the same NotFound-on-mismatch pattern GetPortfolioValueQueryHandler etc. use.
public sealed record TriggerPortfolioStatusReportCommand(long PortfolioId, DateTime? PeriodStart, DateTime? PeriodEnd)
    : IRequest<ErrorOr<Success>>;

public sealed class TriggerPortfolioStatusReportCommandHandler(
    IPortfolioRepository portfolioRepository,
    ICurrentUserProvider currentUserProvider,
    ISender sender,
    TimeProvider timeProvider
    ) : IRequestHandler<TriggerPortfolioStatusReportCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(TriggerPortfolioStatusReportCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.PortfolioId, cancellationToken);
        if (portfolio is null || portfolio.UserId != currentUserProvider.UserId)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.PortfolioId}' was not found.");
        }

        var (defaultStart, defaultEnd) = PortfolioStatusReportPeriod.PreviousCalendarMonth(timeProvider.GetUtcNow().UtcDateTime);
        var periodStart = command.PeriodStart ?? defaultStart;
        var periodEnd = command.PeriodEnd ?? defaultEnd;

        return await sender.Send(new SendPortfolioStatusReportCommand(portfolio.Id, periodStart, periodEnd), cancellationToken);
    }
}
