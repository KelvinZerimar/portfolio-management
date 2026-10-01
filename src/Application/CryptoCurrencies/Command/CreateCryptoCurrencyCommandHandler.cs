using Application.Common.Caching;
using Application.Common.Messaging;
using Application.CryptoCurrencies.Interfaces;
using Contracts.CryptoCurrencies;
using Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.CryptoCurrencies.Command;

public sealed record CreateCryptoCurrencyCommand(CreateCryptoCurrencyRequest Request)
    : IRequest<ErrorOr<CreateCryptoCurrencyResponse>>, ICommand, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTagsToInvalidate => [CacheTags.CryptoCurrencies];
}

public sealed class CreateCryptoCurrencyCommandHandler(
    ILogger<CreateCryptoCurrencyCommandHandler> logger,
    ICryptoCurrencyRepository cryptoCurrencyRepository
    ) : IRequestHandler<CreateCryptoCurrencyCommand, ErrorOr<CreateCryptoCurrencyResponse>>
{
    public async Task<ErrorOr<CreateCryptoCurrencyResponse>> Handle(CreateCryptoCurrencyCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        logger.LogInformation("Handling CreateCryptoCurrencyCommand for CryptoCurrency: {Symbol}", request.Symbol);

        var existingCryptoCurrency = await cryptoCurrencyRepository.GetBySymbolAsync(request.Symbol, cancellationToken);
        if (existingCryptoCurrency is not null)
        {
            return Error.Conflict("CryptoCurrency.AlreadyExists", $"A crypto currency with the symbol '{request.Symbol}' already exists.");
        }

        var newCryptoCurrency = CryptoCurrency.Create(request.Symbol, request.Name, request.CoinGeckoId, request.Image);

        await cryptoCurrencyRepository.AddAsync(newCryptoCurrency, cancellationToken);

        logger.LogInformation("Created new crypto currency with ID {CryptoCurrencyId}", newCryptoCurrency.Id);

        return newCryptoCurrency.ToCreateCryptoCurrencyResponse();
    }
}
