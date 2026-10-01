using Application.Common.Messaging;
using Application.Common.Security;
using Application.Portfolios.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Command;

public sealed record DeletePortfolioCommand(long Id) : IRequest<ErrorOr<Deleted>>, ICommand;

public sealed class DeletePortfolioCommandHandler(
    ILogger<DeletePortfolioCommandHandler> logger,
    IPortfolioRepository portfolioRepository,
    ICurrentUserProvider currentUserProvider
    ) : IRequestHandler<DeletePortfolioCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeletePortfolioCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.Id, cancellationToken);
        if (portfolio is null || portfolio.UserId != currentUserProvider.UserId)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.Id}' was not found.");
        }

        portfolioRepository.RemoveRange([portfolio]);

        logger.LogInformation("Deleted portfolio with ID {PortfolioId}", command.Id);

        return Result.Deleted;
    }
}
