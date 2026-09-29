import { formatCurrency, formatPercentage } from "@/lib/format";
import { colorForKey } from "@/lib/chartColors";
import type { PortfolioHoldingItemResponse } from "@/types";

interface AssetSummary {
  cryptoCurrencyId: number;
  symbol: string;
  name: string;
  image: string | null;
  value: number;
  percentage: number;
}

// Aggregates holdings across exchanges into one figure per asset (a holding
// exists per (crypto, exchange) pair, but this row is a per-asset summary),
// sorted the same way the allocation-by-asset endpoint orders its items.
function summarizeByAsset(holdings: PortfolioHoldingItemResponse[]): AssetSummary[] {
  const totals = new Map<
    number,
    { symbol: string; name: string; image: string | null; value: number }
  >();
  for (const h of holdings) {
    const existing = totals.get(h.cryptoCurrencyId);
    if (existing) {
      existing.value += h.value;
    } else {
      totals.set(h.cryptoCurrencyId, {
        symbol: h.cryptoCurrencySymbol,
        name: h.cryptoCurrencyName,
        image: h.cryptoCurrencyImage,
        value: h.value,
      });
    }
  }

  const totalValue = [...totals.values()].reduce((sum, t) => sum + t.value, 0);

  return [...totals.entries()]
    .map(([cryptoCurrencyId, t]) => ({
      cryptoCurrencyId,
      ...t,
      percentage: totalValue === 0 ? 0 : (t.value / totalValue) * 100,
    }))
    .sort((a, b) => b.value - a.value);
}

interface AssetSummaryCardsProps {
  holdings: PortfolioHoldingItemResponse[];
}

export function AssetSummaryCards({ holdings }: AssetSummaryCardsProps) {
  const assets = summarizeByAsset(holdings);

  if (assets.length === 0) {
    return null;
  }

  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
      {assets.map((asset) => (
        <div key={asset.cryptoCurrencyId} className="panel flex items-center gap-3 p-3">
          {asset.image ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={asset.image}
              alt=""
              aria-hidden
              className="size-9 shrink-0 rounded-full object-cover"
            />
          ) : (
            <span
              aria-hidden
              className="flex size-9 shrink-0 items-center justify-center rounded-full text-xs font-bold text-white"
              style={{ background: colorForKey(asset.symbol) }}
            >
              {asset.symbol.slice(0, 2).toUpperCase()}
            </span>
          )}
          <div className="min-w-0">
            <p className="truncate text-xs text-ink-muted">
              <span className="font-semibold text-ink">{asset.symbol}</span> | {asset.name}
            </p>
            <p className="tabular text-base font-bold">{formatCurrency(asset.value)}</p>
            <p className="tabular text-xs text-ink-muted">
              {formatPercentage(asset.percentage)} participación
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}
