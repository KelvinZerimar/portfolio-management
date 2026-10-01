// Some users track their EUR cash balance as a regular holding (a crypto-currency
// row with symbol/name "EUR"/"Euro") so it shows up in totals. It isn't an
// investment though, so dashboard rankings exclude it before taking the top N.
const EURO_TOKENS = new Set(["eur", "euro", "euros"]);

export function isEuroAsset(...values: Array<string | null | undefined>): boolean {
  return values.some((value) => value != null && EURO_TOKENS.has(value.trim().toLowerCase()));
}

export const MAX_RANKED_ASSETS = 10;
