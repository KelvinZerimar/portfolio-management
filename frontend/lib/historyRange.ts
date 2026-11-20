import type { PortfolioHistoryPointResponse } from "@/types";

// The history range is page-level state: the headline figure's change and the
// value history chart must always describe the same period.
export type HistoryRange = "30d" | "90d" | "1y" | "all";

export const HISTORY_RANGE_OPTIONS: { value: HistoryRange; label: string }[] = [
  { value: "30d", label: "30d" },
  { value: "90d", label: "90d" },
  { value: "1y", label: "1a" },
  { value: "all", label: "Todo" },
];

const RANGE_DAYS: Record<HistoryRange, number | null> = {
  "30d": 30,
  "90d": 90,
  "1y": 365,
  all: null,
};

// Spoken form for the headline change ("-418,98 € en 90 días").
export const HISTORY_RANGE_PHRASE: Record<HistoryRange, string> = {
  "30d": "en 30 días",
  "90d": "en 90 días",
  "1y": "en 1 año",
  all: "desde el primer registro",
};

// Points inside the range, anchored at the cutoff. The ledger is manual and
// point-in-time, so the value on any date is the last value recorded on or
// before it: when earlier points exist, the range opens with a point AT the
// cutoff carrying that last known value. Without it, a sparse ledger (entries a
// month apart) left a 30-day range with one point — no change, no chart —
// although the value at the cutoff was known all along. If nothing was recorded
// inside the range, it closes at `now` with the same value: a flat, truthful line.
// `points` must be in ascending date order (as the API returns them).
export function filterHistoryByRange(
  points: PortfolioHistoryPointResponse[],
  range: HistoryRange,
  now: number
): PortfolioHistoryPointResponse[] {
  const days = RANGE_DAYS[range];
  if (days === null) return points;
  const cutoff = now - days * 24 * 60 * 60 * 1000;

  const inside = points.filter((p) => new Date(p.date).getTime() >= cutoff);
  const before = points.filter((p) => new Date(p.date).getTime() < cutoff);
  const lastKnown = before.at(-1);
  if (!lastKnown) return inside;

  const anchored = [{ date: new Date(cutoff).toISOString(), value: lastKnown.value }, ...inside];
  if (anchored.length === 1) {
    anchored.push({ date: new Date(now).toISOString(), value: lastKnown.value });
  }
  return anchored;
}

// Value of a series on `date`: the last point recorded on or before it (same
// point-in-time rule as above), or 0 if the series had not started yet.
export function valueAsOf(points: PortfolioHistoryPointResponse[], date: string): number {
  const target = new Date(date).getTime();
  let value = 0;
  for (const p of points) {
    if (new Date(p.date).getTime() > target) break;
    value = p.value;
  }
  return value;
}

export interface HistoryChange {
  amount: number;
  // 0–100 scale; null when the period starts at 0 (no meaningful percentage).
  percentage: number | null;
}

// Change across the points of one range: last value minus first. Null with
// fewer than two points — a single point has no change to report.
export function historyChange(points: PortfolioHistoryPointResponse[]): HistoryChange | null {
  if (points.length < 2) return null;
  const start = points[0].value;
  const amount = points[points.length - 1].value - start;
  return { amount, percentage: start === 0 ? null : (amount / start) * 100 };
}
