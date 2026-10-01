import type { PortfolioHoldingItemResponse } from "@/types";

interface ExportColumn {
  header: string;
  value: (h: PortfolioHoldingItemResponse) => string | number;
}

// es-ES uses ',' as the decimal separator (and the CSV field separator is ';',
// so the comma never collides with it). Rounded to 2 decimals as requested.
const numberFormatter = new Intl.NumberFormat("es-ES", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function formatNumber(value: number): string {
  return numberFormatter.format(value);
}

const COLUMNS: ExportColumn[] = [
  { header: "Activo", value: (h) => h.cryptoCurrencySymbol },
  { header: "Nombre", value: (h) => h.cryptoCurrencyName },
  { header: "Exchange", value: (h) => h.exchangeName },
  { header: "Cantidad", value: (h) => formatNumber(h.quantity) },
  { header: "Precio (EUR)", value: (h) => formatNumber(h.pricePerUnit) },
  { header: "Valor (EUR)", value: (h) => formatNumber(h.value) },
  { header: "Registrado", value: (h) => new Date(h.recordedAt).toISOString().slice(0, 10) },
];

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function csvEscape(value: string | number): string {
  const text = String(value);
  return /[",\n;]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

export function exportHoldingsToCsv(holdings: PortfolioHoldingItemResponse[], filename: string) {
  // Excel (es-ES locale) expects ';' as the field separator for CSV auto-detection.
  const lines = [
    COLUMNS.map((c) => csvEscape(c.header)).join(";"),
    ...holdings.map((h) => COLUMNS.map((c) => csvEscape(c.value(h))).join(";")),
  ];
  const blob = new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv;charset=utf-8;" });
  downloadBlob(blob, `${filename}.csv`);
}

function htmlEscape(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
}

// Excel opens an HTML table saved with an .xls extension and a
// "vnd.ms-excel" MIME type as a real worksheet — no XLSX/OOXML library needed.
export function exportHoldingsToExcel(holdings: PortfolioHoldingItemResponse[], filename: string) {
  const headerRow = `<tr>${COLUMNS.map((c) => `<th>${htmlEscape(c.header)}</th>`).join("")}</tr>`;
  const bodyRows = holdings
    .map(
      (h) =>
        `<tr>${COLUMNS.map((c) => `<td>${htmlEscape(String(c.value(h)))}</td>`).join("")}</tr>`
    )
    .join("");
  const html = `<!DOCTYPE html><html><head><meta charset="UTF-8"></head><body><table>${headerRow}${bodyRows}</table></body></html>`;
  const blob = new Blob([html], { type: "application/vnd.ms-excel;charset=utf-8;" });
  downloadBlob(blob, `${filename}.xls`);
}
