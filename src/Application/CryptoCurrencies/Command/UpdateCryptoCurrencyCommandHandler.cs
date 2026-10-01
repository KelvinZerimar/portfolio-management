using Application.Common.Caching;
using Application.Common.Messaging;
using Application.CryptoCurrencies.Interfaces;
using Contracts.CryptoCurrencies;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CryptoCurrencies.Command;

public sealed record UpdateCryptoCurrencyCommand(long Id, UpdateCryptoCurrencyRequest Request)
    : IRequest<ErrorOr<UpdateCryptoCurrencyResponse>>, ICommand, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => [CacheTags.CryptoCurrencies];
}

public sealed class UpdateCryptoCurrencyCommandHandler(
    ILogger<UpdateCryptoCurrencyCommandHandler> logger,
    ICryptoCurrencyRepository cryptoCurrencyRepository
    ) : IRequestHandler<UpdateCryptoCurrencyCommand, ErrorOr<UpdateCryptoCurrencyResponse>>
{
    public async Task<ErrorOr<UpdateCryptoCurrencyResponse>> Handle(UpdateCryptoCurrencyCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var cryptoCurrency = await cryptoCurrencyRepository.GetByIdAsync(command.Id, cancellationToken);
        if (cryptoCurrency is null)
        {
            return Error.NotFound("CryptoCurrency.NotFound", $"Crypto currency with ID '{command.Id}' was not found.");
        }

        var duplicate = await cryptoCurrencyRepository.GetBySymbolAsync(request.Symbol, cancellationToken);
        if (duplicate is not null && duplicate.Id != cryptoCurrency.Id)
        {
            return Error.Conflict("CryptoCurrency.AlreadyExists", $"A crypto currency with the symbol '{request.Symbol}' already exists.");
        }

        cryptoCurrency.Symbol = request.Symbol;
        cryptoCurrency.Name = request.Name;
        cryptoCurrency.CoinGeckoId = request.CoinGeckoId;
        cryptoCurrency.Image = request.Image;

        cryptoCurrencyRepository.Update(cryptoCurrency);

        logger.LogInformation("Updated crypto currency with ID {CryptoCurrencyId}", cryptoCurrency.Id);

        return cryptoCurrency.ToUpdateCryptoCurrencyResponse();
    }
}
