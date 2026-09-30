import { ChevronLeft, ChevronRight } from "lucide-react";
import { IconButton } from "./IconButton";

export const PAGE_SIZE_OPTIONS = [10, 20, 50] as const;

interface PaginationProps {
  page: number;
  totalPages: number;
  total: number;
  limit: number;
  onPageChange: (page: number) => void;
  onLimitChange: (limit: number) => void;
}

export function Pagination({
  page,
  totalPages,
  total,
  limit,
  onPageChange,
  onLimitChange,
}: PaginationProps) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-rule px-4 py-3 text-xs text-ink-muted">
      <label htmlFor="portfolio-entries-page-size" className="flex items-center gap-2">
        Filas por página
        <select
          id="portfolio-entries-page-size"
          value={limit}
          onChange={(e) => onLimitChange(Number(e.target.value))}
          className="rounded-lg border border-rule bg-paper px-2 py-1 text-sm text-ink focus-visible:outline-2 focus-visible:outline-brand"
        >
          {PAGE_SIZE_OPTIONS.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>
      </label>

      <div className="flex items-center gap-3">
        <span>
          {total === 0
            ? "0 resultados"
            : `Página ${page} de ${Math.max(totalPages, 1)} · ${total} resultados`}
        </span>
        <div className="flex items-center gap-1">
          <IconButton
            aria-label="Página anterior"
            onClick={() => onPageChange(page - 1)}
            disabled={page <= 1}
          >
            <ChevronLeft size={16} strokeWidth={1.75} aria-hidden />
          </IconButton>
          <IconButton
            aria-label="Página siguiente"
            onClick={() => onPageChange(page + 1)}
            disabled={page >= totalPages}
          >
            <ChevronRight size={16} strokeWidth={1.75} aria-hidden />
          </IconButton>
        </div>
      </div>
    </div>
  );
}
