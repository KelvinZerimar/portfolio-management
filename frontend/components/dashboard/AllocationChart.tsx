"use client";

import { useMemo, useState } from "react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { SegmentedControl } from "@/components/ui/SegmentedControl";
import { usePortfolioAllocation } from "@/hooks/usePortfolioAnalytics";
import { PortfolioAllocationGroupBy } from "@/types";
import { formatCurrency, formatPercentage } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";
import { colorForKey } from "@/lib/chartColors";
import { isEuroAsset, MAX_RANKED_ASSETS } from "@/lib/assetFilters";

type GroupBy = "asset" | "exchange";

const GROUP_OPTIONS: { value: GroupBy; label: string }[] = [
  { value: "asset", label: "Activo" },
  { value: "exchange", label: "Exchange" },
];

interface AllocationChartProps {
  portfolioId: number;
}

export function AllocationChart({ portfolioId }: AllocationChartProps) {
  const [groupBy, setGroupBy] = useState<GroupBy>("asset");
  const { data, isPending, isError, error } = usePortfolioAllocation(
    portfolioId,
    groupBy === "asset" ? PortfolioAllocationGroupBy.Asset : PortfolioAllocationGroupBy.Exchange
  );

  // Top assets by value, Euro cash excluded (exchange grouping has no "Euro"
  // concept, so it's only capped, not filtered), capped at MAX_RANKED_ASSETS —
  // matching the value history chart.
  const items = useMemo(() => {
    const raw = data?.items ?? [];
    const filtered = groupBy === "asset" ? raw.filter((item) => !isEuroAsset(item.label)) : raw;
    return [...filtered].sort((a, b) => b.value - a.value).slice(0, MAX_RANKED_ASSETS);
  }, [data, groupBy]);

  return (
    <section className="panel p-6" aria-label="Participación por activo o exchange">
      <div className="flex items-start justify-between gap-3">
        <h2 className="text-sm font-bold">Participación</h2>
        <SegmentedControl
          aria-label="Agrupar participación por"
          options={GROUP_OPTIONS}
          value={groupBy}
          onChange={setGroupBy}
        />
      </div>

      {isPending ? (
        <div className="mt-4 h-48 animate-pulse rounded-xl bg-paper-raised" />
      ) : isError ? (
        <div className="mt-4">
          <ErrorNotice>{getApiErrorMessage(error) ?? "No se pudo cargar la participación."}</ErrorNotice>
        </div>
      ) : items.length === 0 ? (
        <p className="mt-4 flex h-48 items-center justify-center text-center text-sm text-ink-muted">
          Sin holdings todavía.
        </p>
      ) : (
        // Donut on top, legend below as a value table: this panel replaced the
        // per-asset summary cards (same top-N data shown a third time), so the
        // legend now carries each group's value as well as its share — and
        // stacking it fills the panel to the history chart's height instead of
        // leaving the lower half empty.
        <div className="mt-4 flex flex-col items-center gap-4">
          <div className="h-40 w-40 shrink-0">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={items}
                  dataKey="value"
                  nameKey="label"
                  innerRadius={48}
                  outerRadius={70}
                  // Gap and white stroke separate slices; with a single slice
                  // there is nothing to separate and they cut a notch in the ring.
                  paddingAngle={items.length > 1 ? 1 : 0}
                  stroke={items.length > 1 ? "var(--paper)" : "none"}
                  strokeWidth={2}
                  isAnimationActive={false}
                >
                  {items.map((item) => (
                    <Cell key={item.groupId} fill={colorForKey(item.label)} />
                  ))}
                </Pie>
                <Tooltip
                  formatter={(value) => formatCurrency(Number(value))}
                  contentStyle={{
                    background: "var(--paper)",
                    border: "1px solid var(--rule)",
                    borderRadius: 10,
                    boxShadow: "var(--shadow-card)",
                    fontSize: 12,
                  }}
                />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <ul className="w-full divide-y divide-rule text-sm">
            {items.map((item) => (
              <li
                key={item.groupId}
                className="grid grid-cols-[minmax(0,1fr)_auto_4.5rem] items-center gap-x-3 py-1.5"
              >
                <span className="flex min-w-0 items-center gap-1.5">
                  <span
                    aria-hidden
                    className="size-2 shrink-0 rounded-full"
                    style={{ background: colorForKey(item.label) }}
                  />
                  <span className="truncate font-medium">{item.label}</span>
                </span>
                <span className="tabular text-right">{formatCurrency(item.value)}</span>
                <span className="tabular text-right text-ink-muted">
                  {formatPercentage(item.percentage)}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
