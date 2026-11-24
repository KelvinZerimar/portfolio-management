// Shared formatting so every ledger figure (table, charts, alerts) renders identically.
// Currency is assumed EUR: the backend stores a bare decimal PricePerUnit with no
// currency unit, manually-logged prices are entered in EUR, and the CoinGecko price
// refresh also quotes in EUR. Revisit if the product ever supports another quote currency.

const currencyFormatter = new Intl.NumberFormat("es-ES", {
  style: "currency",
  currency: "EUR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  // es-ES skips the thousands separator for 4-digit numbers by default
  // (minimumGroupingDigits: 2), so "1957,54 €" sat next to "55.913,70 €" in the
  // same column. "always" groups every figure the same way.
  useGrouping: "always",
});

const quantityFormatter = new Intl.NumberFormat("es-ES", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 8,
  useGrouping: "always",
});

// Same locale as currency, so percentages use a decimal comma too ("74,47 %"),
// not the "74.47%" that toFixed produced.
const percentageFormatter = new Intl.NumberFormat("es-ES", {
  style: "percent",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

// Changes (deltas) always carry an explicit sign: "+12,30 €" / "-418,98 €".
const signedCurrencyFormatter = new Intl.NumberFormat("es-ES", {
  style: "currency",
  currency: "EUR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  useGrouping: "always",
  signDisplay: "exceptZero",
});

const signedPercentageFormatter = new Intl.NumberFormat("es-ES", {
  style: "percent",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
  signDisplay: "exceptZero",
});

// Axis ticks only: short enough to fit a narrow axis ("2,6 mil €").
const compactCurrencyFormatter = new Intl.NumberFormat("es-ES", {
  style: "currency",
  currency: "EUR",
  notation: "compact",
  maximumFractionDigits: 1,
});

// Timestamps are stored and transmitted in UTC (e.g. price-refresh snapshots use
// DateTime.UtcNow). Pinning the display timezone to Europe/Madrid — instead of
// relying on the ambient runtime timezone, which differs between server-side
// rendering and the browser — keeps the shown calendar date correct and consistent
// regardless of where the formatting code executes.
const dateFormatter = new Intl.DateTimeFormat("es-ES", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  timeZone: "Europe/Madrid",
});

const shortDateFormatter = new Intl.DateTimeFormat("es-ES", {
  day: "2-digit",
  month: "short",
  timeZone: "Europe/Madrid",
});

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value);
}

export function formatQuantity(value: number): string {
  return quantityFormatter.format(value);
}

// `value` is on a 0–100 scale (what the API and callers use), not 0–1.
export function formatPercentage(value: number): string {
  return percentageFormatter.format(value / 100);
}

export function formatSignedCurrency(value: number): string {
  return signedCurrencyFormatter.format(value);
}

// `value` is on a 0–100 scale, like formatPercentage.
export function formatSignedPercentage(value: number): string {
  return signedPercentageFormatter.format(value / 100);
}

export function formatCompactCurrency(value: number): string {
  return compactCurrencyFormatter.format(value);
}

export function formatDate(value: string | Date): string {
  return dateFormatter.format(new Date(value));
}

const dateTimeFormatter = new Intl.DateTimeFormat("es-ES", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: "Europe/Madrid",
});

// Ledger timestamps: entries typed in by hand carry a date only (stored as 00:00
// UTC), so showing "01:00" for them would be noise. Entries written with a real
// time (e.g. "Actualizar precios", which stamps the moment it ran) show it.
export function formatRecordedAt(value: string): string {
  const isDateOnly = /T00:00:00(\.0+)?(Z|\+00:00)?$/.test(value);
  return isDateOnly ? dateFormatter.format(new Date(value)) : dateTimeFormatter.format(new Date(value));
}

export function formatShortDate(value: string | Date): string {
  return shortDateFormatter.format(new Date(value));
}

export function daysSince(value: string | Date): number {
  const ms = Date.now() - new Date(value).getTime();
  return Math.floor(ms / (1000 * 60 * 60 * 24));
}
