"use client";

import { useMemo, useState } from "react";
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { SegmentedControl } from "@/components/ui/SegmentedControl";
import { usePortfolioAllocation } from "@/hooks/usePortfolioAnalytics";
import { PortfolioAllocationGroupBy } from "@/types";
import { formatCurrency, formatPercentage } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";
import { MAX_RANKED_ASSETS } from "@/lib/assetFilters";
import { buildAllocationSlices } from "@/lib/allocationSlices";

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

  // Top MAX_RANKED_ASSETS by value (same ranking as the history chart), plus
  // "Otros" and — by asset only — "Efectivo", so the ring adds up to the total
  // its percentage labels are measured against.
  const items = useMemo(
    () =>
      buildAllocationSlices(data?.items ?? [], {
        separateCash: groupBy === "asset",
        max: MAX_RANKED_ASSETS,
      }),
    [data, groupBy]
  );

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
                    <Cell key={item.key} fill={item.color} />
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
          <ul className="w-full max-w-sm divide-y divide-rule text-sm">
            {items.map((item) => (
              <li
                key={item.key}
                className="grid grid-cols-[minmax(0,1fr)_auto_4.5rem] items-center gap-x-3 py-1.5"
              >
                <span className="flex min-w-0 items-center gap-1.5">
                  <span
                    aria-hidden
                    className="size-2 shrink-0 rounded-full"
                    style={{ background: item.color }}
                  />
                  <span
                    className={`truncate ${item.kind === "item" ? "font-medium" : "text-ink-muted"}`}
                  >
                    {item.label}
                  </span>
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
