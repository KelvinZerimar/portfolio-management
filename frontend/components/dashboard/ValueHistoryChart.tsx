"use client";

import { useMemo } from "react";
import {
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { SegmentedControl } from "@/components/ui/SegmentedControl";
import { formatCompactCurrency, formatCurrency, formatDate, formatShortDate } from "@/lib/format";
import { assignSeriesColors } from "@/lib/chartColors";
import { isEuroAsset, MAX_RANKED_ASSETS } from "@/lib/assetFilters";
import { HISTORY_RANGE_OPTIONS, valueAsOf, type HistoryRange } from "@/lib/historyRange";
import type { PortfolioAssetHistorySeriesResponse, PortfolioHistoryPointResponse } from "@/types";

interface MergedPoint {
  date: string;
  value: number;
  byAsset: Record<string, number>;
}

interface ValueHistoryChartProps {
  // Already filtered to `range` by the page, which shares the same points with
  // the headline figure's change so both always describe the same period.
  points: PortfolioHistoryPointResponse[];
  byAsset: PortfolioAssetHistorySeriesResponse[];
  range: HistoryRange;
  onRangeChange: (range: HistoryRange) => void;
}

// The headline figure above owns "how much and how it changed"; this panel owns
// "how it got here". So no big duplicated value here — the tooltip gives each
// day's reading — and the Y axis is visible with no gradient fill, because an
// unlabeled auto-domain area made small moves look like crashes.
export function ValueHistoryChart({ points, byAsset, range, onRangeChange }: ValueHistoryChartProps) {
  // Same ranking as the allocation chart: top assets by value, Euro cash
  // excluded, capped at MAX_RANKED_ASSETS so the chart doesn't get overcrowded.
  const rankedAssets = useMemo(
    () =>
      byAsset
        .filter((series) => !isEuroAsset(series.symbol))
        .map((series) => ({
          series,
          latestValue: series.points.at(-1)?.value ?? 0,
        }))
        .sort((a, b) => b.latestValue - a.latestValue)
        .slice(0, MAX_RANKED_ASSETS)
        .map((entry) => entry.series),
    [byAsset]
  );

  // Multiple lines only earn their place once there's more than one asset to tell
  // apart — a single asset's line is identical to the total and would just double it.
  const showAssetLines = rankedAssets.length > 1;

  const merged = useMemo<MergedPoint[]>(() => {
    if (!showAssetLines) return points.map((p) => ({ date: p.date, value: p.value, byAsset: {} }));
    // valueAsOf, not an exact-date lookup: the range's anchor point sits at the
    // cutoff, a date no series has a point on, and must still show each asset's
    // last known value there (not 0).
    return points.map((p) => ({
      date: p.date,
      value: p.value,
      byAsset: Object.fromEntries(
        rankedAssets.map((series) => [series.symbol, valueAsOf(series.points, p.date)])
      ),
    }));
  }, [points, rankedAssets, showAssetLines]);

  // Same top-N set and same assignment as the allocation donut, so an asset
  // wears the same color in both charts.
  const legend = useMemo(() => {
    if (!showAssetLines) return [];
    const colors = assignSeriesColors(
      rankedAssets.map((series) => ({ id: series.cryptoCurrencyId, key: series.symbol }))
    );
    return rankedAssets
      .map((series) => ({
        symbol: series.symbol,
        color: colors.get(series.cryptoCurrencyId) ?? "var(--rule-strong)",
      }))
      .sort((a, b) => a.symbol.localeCompare(b.symbol));
  }, [rankedAssets, showAssetLines]);

  const first = merged[0];
  const last = merged[merged.length - 1];

  return (
    <section className="panel p-6" aria-label="Histórico de valor">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-bold">Histórico</h2>
          <p className="mt-0.5 text-xs text-ink-muted">
            {first && last && first !== last
              ? `${formatDate(first.date)} – ${formatDate(last.date)}`
              : "Sin datos suficientes en este rango"}
          </p>
        </div>
        <SegmentedControl
          aria-label="Rango del histórico"
          options={HISTORY_RANGE_OPTIONS}
          value={range}
          onChange={onRangeChange}
        />
      </div>

      <div className="mt-4 h-52">
        {merged.length > 1 ? (
          <ResponsiveContainer width="100%" height="100%">
            <ComposedChart data={merged} margin={{ top: 4, right: 4, left: 0, bottom: 0 }}>
              <CartesianGrid stroke="var(--rule)" vertical={false} />
              <XAxis
                dataKey="date"
                tickFormatter={(value: string) => formatShortDate(value)}
                stroke="var(--ink-muted)"
                fontSize={11}
                tickLine={false}
                axisLine={{ stroke: "var(--rule)" }}
                minTickGap={24}
                interval="preserveStartEnd"
              />
              <YAxis
                domain={["auto", "auto"]}
                tickFormatter={(value: number) => formatCompactCurrency(value)}
                tickCount={4}
                width={64}
                stroke="var(--ink-muted)"
                fontSize={11}
                tickLine={false}
                axisLine={false}
              />
              <Tooltip
                cursor={{ stroke: "var(--ink-muted)", strokeWidth: 1 }}
                content={({ active, payload }) => {
                  if (!active || !payload?.length) return null;
                  const row = payload[0].payload as MergedPoint;
                  const breakdown = legend
                    .map((entry) => ({ ...entry, value: row.byAsset[entry.symbol] ?? 0 }))
                    .filter((entry) => entry.value > 0)
                    .sort((a, b) => b.value - a.value);
                  return (
                    <div className="panel px-3 py-2.5 text-xs">
                      <p className="mb-1 text-ink-muted">{formatDate(row.date)}</p>
                      <ul className="space-y-0.5">
                        <li className="flex items-center gap-2 font-semibold">
                          <span aria-hidden className="h-0.5 w-2.5 shrink-0 bg-ink" />
                          <span className="flex-1">Total</span>
                          <span className="tabular">{formatCurrency(row.value)}</span>
                        </li>
                        {breakdown.map((entry) => (
                          <li key={entry.symbol} className="flex items-center gap-2">
                            <span
                              aria-hidden
                              className="h-0.5 w-2.5 shrink-0"
                              style={{ background: entry.color }}
                            />
                            <span className="flex-1">{entry.symbol}</span>
                            <span className="tabular">{formatCurrency(entry.value)}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  );
                }}
              />
              <Line
                type="monotone"
                dataKey="value"
                stroke="var(--ink)"
                strokeWidth={1.5}
                dot={false}
                activeDot={{ r: 3, fill: "var(--ink)", stroke: "none" }}
                isAnimationActive={false}
              />
              {legend.map((entry) => (
                <Line
                  key={entry.symbol}
                  type="monotone"
                  name={entry.symbol}
                  dataKey={(row: MergedPoint) => row.byAsset[entry.symbol]}
                  stroke={entry.color}
                  strokeWidth={1}
                  dot={false}
                  activeDot={{ r: 2.5, fill: entry.color, stroke: "none" }}
                  isAnimationActive={false}
                />
              ))}
            </ComposedChart>
          </ResponsiveContainer>
        ) : (
          <p className="flex h-full items-center justify-center text-center text-sm text-ink-muted">
            Registra entradas en más de una fecha para ver el histórico.
          </p>
        )}
      </div>

      {legend.length > 0 && (
        <ul className="mt-3 flex flex-wrap gap-x-3 gap-y-1.5 border-t border-rule pt-2.5 text-xs text-ink-muted">
          <li className="flex items-center gap-1.5">
            <span aria-hidden className="h-0.5 w-2.5 shrink-0 bg-ink" />
            Total
          </li>
          {legend.map((entry) => (
            <li key={entry.symbol} className="flex items-center gap-1.5">
              <span aria-hidden className="h-0.5 w-2.5 shrink-0" style={{ background: entry.color }} />
              {entry.symbol}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
