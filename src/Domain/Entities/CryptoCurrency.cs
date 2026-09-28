using Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public sealed class CryptoCurrency : Entity
{
    public string Symbol { get; set; } = string.Empty; // e.g., "BTC", "ETH"
    public string Name { get; set; } = string.Empty; // e.g., "Bitcoin", "Ethereum"
    public string? CoinGeckoId { get; set; } // e.g., "bitcoin", "ethereum" - CoinGecko's own coin id, used to fetch live prices
    public DateTime CreatedAt { get; set; }

    public static CryptoCurrency Create(string symbol, string name, string? coinGeckoId = null)
    {
        return new CryptoCurrency
        {
            Symbol = symbol,
            Name = name,
            CoinGeckoId = coinGeckoId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
