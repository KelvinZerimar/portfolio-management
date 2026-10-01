using Application.Common.Caching;
using Application.Common.Messaging;
using Application.CryptoCurrencies.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CryptoCurrencies.Command;

public sealed record DeleteCryptoCurrencyCommand(long Id) : IRequest<ErrorOr<Deleted>>, ICommand, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => [CacheTags.CryptoCurrencies];
}

public sealed class DeleteCryptoCurrencyCommandHandler(
    ILogger<DeleteCryptoCurrencyCommandHandler> logger,
    ICryptoCurrencyRepository cryptoCurrencyRepository
    ) : IRequestHandler<DeleteCryptoCurrencyCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(DeleteCryptoCurrencyCommand command, CancellationToken cancellationToken)
    {
        var cryptoCurrency = await cryptoCurrencyRepository.GetByIdAsync(command.Id, cancellationToken);
        if (cryptoCurrency is null)
        {
            return Error.NotFound("CryptoCurrency.NotFound", $"Crypto currency with ID '{command.Id}' was not found.");
        }

        cryptoCurrencyRepository.RemoveRange([cryptoCurrency]);

        logger.LogInformation("Deleted crypto currency with ID {CryptoCurrencyId}", command.Id);

        return Result.Deleted;
    }
}
