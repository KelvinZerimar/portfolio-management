// Categorical palette for per-asset (or per-exchange) chart series. Validated
// with the dataviz skill's validate_palette.js on the white panel, ALL pairs
// (history lines cross and donut slices sit in value order, so any two colors
// can touch): worst CVD ΔE 16.2 (protan, target ≥ 8), worst normal-vision ΔE
// 17.5 (floor 15). See 02-DOCS/wiki/ftd/dashboard-distinct-asset-colors.md.
//
// Only four on purpose: with red/pink excluded (red = negative change only),
// greys taken by "Otros"/"Efectivo" and near-black by the Total line, no set of
// five or more passes all-pairs. Charts never invent a fifth color — anything
// past the top CHART_COLORS.length folds into "Otros".
//
// Yellow (2.17:1) and cyan (2.43:1) sit below 3:1 against white, which the
// method allows only because every series is also named in text (legend table
// with values, tooltip). Keep those labels if the charts change.
export const CHART_COLORS = [
  "#2a78d6", // blue
  "#eda100", // yellow
  "#008300", // green
  "#06b6d4", // cyan
];

function hashKey(key: string): number {
  let hash = 0;
  for (let i = 0; i < key.length; i++) {
    hash = (hash * 31 + key.charCodeAt(i)) >>> 0;
  }
  return hash;
}

// Decorative use only (e.g. an avatar fallback): a stable color per key, with
// no guarantee two keys differ. Chart series use assignSeriesColors instead.
export function colorForKey(key: string, palette: string[] = CHART_COLORS): string {
  return palette[hashKey(key) % palette.length];
}

export interface SeriesEntity {
  id: number;
  key: string;
}

// Distinct colors for the series of ONE chart. Hashing alone collided (two
// assets could share a color while others sat unused). Each entity prefers its
// hash slot — so it keeps its color while the set is stable — and on a clash
// takes the next free slot. Entities are processed in ascending id order, so
// the result depends only on the set, not on how the caller sorted it: the
// donut and the history chart, given the same top-N set, color it identically.
// Beyond palette length colors repeat; callers fold the rest into "Otros".
export function assignSeriesColors(
  entities: SeriesEntity[],
  palette: string[] = CHART_COLORS
): Map<number, string> {
  const colors = new Map<number, string>();
  const taken = new Set<number>();
  for (const entity of [...entities].sort((a, b) => a.id - b.id)) {
    let slot = hashKey(entity.key) % palette.length;
    for (let tries = 0; taken.has(slot) && tries < palette.length; tries++) {
      slot = (slot + 1) % palette.length;
    }
    if (taken.size < palette.length) taken.add(slot);
    colors.set(entity.id, palette[slot]);
  }
  return colors;
}
