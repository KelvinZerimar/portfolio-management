import { isEuroAsset } from "@/lib/assetFilters";
import { colorForKey } from "@/lib/chartColors";
import type { PortfolioAllocationItemResponse } from "@/types";

export type AllocationSliceKind = "item" | "other" | "cash";

export interface AllocationSlice {
  key: string;
  label: string;
  value: number;
  // 0–100 share of the WHOLE portfolio, as the API reports it.
  percentage: number;
  kind: AllocationSliceKind;
  color: string;
}

// Aggregates are grey: they are not assets with an identity of their own, so
// they must not compete with (or borrow) a categorical asset color.
const OTHER_COLOR = "var(--rule-strong)";
const CASH_COLOR = "var(--ink-muted)";

interface BuildOptions {
  // Asset grouping only: Euro cash rows become one "Efectivo" slice instead of
  // competing in the ranking. Exchange grouping has no cash concept.
  separateCash: boolean;
  max: number;
}

// Slices for a part-of-a-whole chart. The ring must add up to the real total:
// ranking used to drop Euro cash and everything past the top N, then draw the
// ring from what was left — while the labels kept the % of the full total, so a
// slice could fill 40 % of the ring and read "28 %". Now the top N keep their
// color, the tail folds into "Otros (n)" and cash into "Efectivo", so every
// slice's size matches its label.
export function buildAllocationSlices(
  items: PortfolioAllocationItemResponse[],
  { separateCash, max }: BuildOptions
): AllocationSlice[] {
  const positive = items.filter((item) => item.value > 0);
  const cash = separateCash ? positive.filter((item) => isEuroAsset(item.label)) : [];
  const ranked = positive
    .filter((item) => !cash.includes(item))
    .sort((a, b) => b.value - a.value);

  const slices: AllocationSlice[] = ranked.slice(0, max).map((item) => ({
    key: `item-${item.groupId}`,
    label: item.label,
    value: item.value,
    percentage: item.percentage,
    kind: "item",
    color: colorForKey(item.label),
  }));

  const tail = ranked.slice(max);
  if (tail.length > 0) {
    slices.push({
      key: "other",
      label: `Otros (${tail.length})`,
      ...sum(tail),
      kind: "other",
      color: OTHER_COLOR,
    });
  }

  if (cash.length > 0) {
    slices.push({ key: "cash", label: "Efectivo", ...sum(cash), kind: "cash", color: CASH_COLOR });
  }

  return slices;
}

function sum(items: PortfolioAllocationItemResponse[]): { value: number; percentage: number } {
  return items.reduce(
    (acc, item) => ({ value: acc.value + item.value, percentage: acc.percentage + item.percentage }),
    { value: 0, percentage: 0 }
  );
}
