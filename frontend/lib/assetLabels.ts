import type { CryptoCurrencyResponse } from "@/types";

// ISIN: 2-letter country code, 9 alphanumerics, 1 check digit (e.g. LU0171310443).
const ISIN = /^[A-Z]{2}[A-Z0-9]{9}[0-9]$/;

// The asset pickers listed bare symbols, unsorted: fine for "BTC", unreadable
// for the funds the ledger also holds, whose "symbol" is an ISIN. A fund leads
// with its name (the code still shows, in brackets); a coin keeps its ticker
// first, which is how it is recognised, followed by the name.
export function assetOptionLabel(asset: Pick<CryptoCurrencyResponse, "symbol" | "name">): string {
  const name = asset.name.trim();
  if (!name || name.toUpperCase() === asset.symbol.toUpperCase()) return asset.symbol;
  return ISIN.test(asset.symbol) ? `${name} (${asset.symbol})` : `${asset.symbol} · ${name}`;
}

// Alphabetical by the label the person reads, so a name can be found by eye.
export function sortAssetsForSelect<T extends Pick<CryptoCurrencyResponse, "symbol" | "name">>(assets: T[]): T[] {
  return [...assets].sort((a, b) =>
    assetOptionLabel(a).localeCompare(assetOptionLabel(b), "es", { sensitivity: "base" })
  );
}

export function isFund(asset: Pick<CryptoCurrencyResponse, "symbol">): boolean {
  return ISIN.test(asset.symbol);
}

// Coins and funds in separate groups: sorted together, a long fund name landed
// between AVAX and BNB.
export function groupAssetsForSelect<T extends Pick<CryptoCurrencyResponse, "symbol" | "name">>(
  assets: T[]
): { coins: T[]; funds: T[] } {
  const sorted = sortAssetsForSelect(assets);
  return { coins: sorted.filter((a) => !isFund(a)), funds: sorted.filter(isFund) };
}
