using Application.Common.Caching;
using Application.Common.Messaging;
using Application.Exchanges.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Exchanges.Command;

public sealed record DeleteExchangeCommand(long Id) : IRequest<ErrorOr<Deleted>>, ICommand, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => [CacheTags.Exchanges];
}

public sealed class DeleteExchangeCommandHandler(
    ILogger<DeleteExchangeCommandHandler> logger,
    IExchangeRepository exchangeRepository
    ) : IRequestHandler<DeleteExchangeCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteExchangeCommand command, CancellationToken cancellationToken)
    {
        var exchange = await exchangeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (exchange is null)
        {
            return Error.NotFound("Exchange.NotFound", $"Exchange with ID '{command.Id}' was not found.");
        }

        exchangeRepository.RemoveRange([exchange]);

        logger.LogInformation("Deleted exchange with ID {ExchangeId}", command.Id);

        return Result.Deleted;
    }
}
