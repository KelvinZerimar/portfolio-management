"use client";

import { type FormEvent, use, useMemo, useState } from "react";
import Link from "next/link";
import { Check, RefreshCw, X } from "lucide-react";
import { useCryptoCurrencies } from "@/hooks/useCryptoCurrencies";
import { useExchanges } from "@/hooks/useExchanges";
import { usePortfolio } from "@/hooks/usePortfolios";
import { useRefreshPortfolioPrices } from "@/hooks/usePortfolioAnalytics";
import {
  useCreatePortfolioEntry,
  useDeletePortfolioEntry,
  usePortfolioEntries,
  useUpdatePortfolioEntry,
} from "@/hooks/usePortfolioEntries";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { IconButton } from "@/components/ui/IconButton";
import { Pagination } from "@/components/ui/Pagination";
import { RowActions } from "@/components/ui/RowActions";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { formatCurrency, formatDate, formatQuantity, formatRecordedAt } from "@/lib/format";
import { getApiError, getApiErrorMessage } from "@/lib/errors";
import type { CryptoCurrencyResponse, ExchangeResponse, PortfolioEntryResponse } from "@/types";

function todayInputValue() {
  return new Date().toISOString().slice(0, 10);
}

// The API refuses a second entry for the same asset + exchange at the same
// moment (409 PortfolioEntry.Duplicate): the valuation keeps the latest one and
// a tie would make the portfolio value arbitrary. Returns the existing entry's
// id when the error is that one.
function duplicateEntryId(error: unknown): number | null {
  const apiError = getApiError(error);
  if (apiError?.code !== "PortfolioEntry.Duplicate") return null;
  const id = Number(apiError.metadata.existingEntryId);
  return Number.isFinite(id) ? id : null;
}

export default function PortfolioDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const portfolioId = Number(id);

  const [page, setPage] = useState(1);
  const [limit, setLimit] = useState(20);
  const [filterCryptoCurrencyId, setFilterCryptoCurrencyId] = useState("");
  const [filterFromDate, setFilterFromDate] = useState("");
  const [filterToDate, setFilterToDate] = useState("");

  const { data: portfolio, isError: portfolioError, error: portfolioErrorDetail } =
    usePortfolio(portfolioId);
  const {
    data: entriesPage,
    isPending: entriesPending,
    isError: entriesError,
    error: entriesErrorDetail,
  } = usePortfolioEntries({
    portfolioId,
    page,
    limit,
    cryptoCurrencyId: filterCryptoCurrencyId ? Number(filterCryptoCurrencyId) : undefined,
    fromDate: filterFromDate || undefined,
    toDate: filterToDate || undefined,
  });
  const { data: cryptoPage, isError: cryptoError } = useCryptoCurrencies({ limit: 100 });
  const { data: exchangePage, isError: exchangeError } = useExchanges({ limit: 100 });

  const cryptoCurrencies = useMemo(() => cryptoPage?.data ?? [], [cryptoPage]);
  const exchanges = useMemo(() => exchangePage?.data ?? [], [exchangePage]);
  const entries = entriesPage?.data ?? [];

  function updateFilters(update: Partial<{ cryptoCurrencyId: string; fromDate: string; toDate: string }>) {
    if (update.cryptoCurrencyId !== undefined) setFilterCryptoCurrencyId(update.cryptoCurrencyId);
    if (update.fromDate !== undefined) setFilterFromDate(update.fromDate);
    if (update.toDate !== undefined) setFilterToDate(update.toDate);
    setPage(1);
  }

  function handleLimitChange(nextLimit: number) {
    setLimit(nextLimit);
    setPage(1);
  }

  const hasActiveFilters = Boolean(filterCryptoCurrencyId || filterFromDate || filterToDate);

  const cryptoById = useMemo(
    () => new Map(cryptoCurrencies.map((c) => [c.id, c])),
    [cryptoCurrencies]
  );
  const exchangeById = useMemo(() => new Map(exchanges.map((e) => [e.id, e])), [exchanges]);

  const createMutation = useCreatePortfolioEntry();
  const deleteMutation = useDeletePortfolioEntry(portfolioId);
  const refreshMutation = useRefreshPortfolioPrices(portfolioId);

  const [editingId, setEditingId] = useState<number | null>(null);
  const [cryptoCurrencyId, setCryptoCurrencyId] = useState("");
  const [exchangeId, setExchangeId] = useState("");
  const [quantity, setQuantity] = useState("");
  const [pricePerUnit, setPricePerUnit] = useState("");
  const [recordedAt, setRecordedAt] = useState(todayInputValue());
  const [formError, setFormError] = useState<string | null>(null);
  const [justAdded, setJustAdded] = useState(false);

  const createDuplicateId = createMutation.isError ? duplicateEntryId(createMutation.error) : null;

  // Narrows the ledger to the clashing entry's asset and date (it may sit on
  // another page or behind other filters) and opens it for editing.
  function openExistingEntry(entryId: number) {
    updateFilters({ cryptoCurrencyId, fromDate: recordedAt, toDate: recordedAt });
    setEditingId(entryId);
    createMutation.reset();
  }

  const referenceDataFailed = cryptoError || exchangeError;
  const canAddEntry = !referenceDataFailed && cryptoCurrencies.length > 0 && exchanges.length > 0;

  function handleCreate(e: FormEvent) {
    e.preventDefault();
    setJustAdded(false);

    if (!cryptoCurrencyId || !exchangeId || !quantity || !pricePerUnit || !recordedAt) {
      setFormError("Completa todos los campos para añadir la entrada.");
      return;
    }
    if (Number(quantity) <= 0 || Number(pricePerUnit) <= 0) {
      setFormError("La cantidad y el precio deben ser mayores que cero.");
      return;
    }
    if (recordedAt > todayInputValue()) {
      setFormError("La fecha no puede ser futura.");
      return;
    }

    setFormError(null);
    createMutation.mutate(
      {
        portfolioId,
        cryptoCurrencyId: Number(cryptoCurrencyId),
        exchangeId: Number(exchangeId),
        quantity: Number(quantity),
        pricePerUnit: Number(pricePerUnit),
        recordedAt: new Date(recordedAt).toISOString(),
      },
      {
        onSuccess: () => {
          setCryptoCurrencyId("");
          setExchangeId("");
          setQuantity("");
          setPricePerUnit("");
          setRecordedAt(todayInputValue());
          setJustAdded(true);
        },
      }
    );
  }

  if (portfolioError) {
    return (
      <div className="mx-auto flex max-w-4xl flex-col gap-3">
        <Link href="/portfolio" className="text-xs text-ink-muted hover:underline">
          ← Portfolios
        </Link>
        <ErrorNotice>
          {getApiErrorMessage(portfolioErrorDetail) ?? "No se pudo cargar este portfolio."}
        </ErrorNotice>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-6">
      <div>
        <Link href="/portfolio" className="text-xs text-ink-muted hover:underline">
          ← Portfolios
        </Link>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold tracking-tight">{portfolio?.name ?? "Portfolio"}</h1>
            {portfolio?.description && (
              <p className="text-sm text-ink-muted">{portfolio.description}</p>
            )}
          </div>
          {/* This writes to the ledger: one new entry per held asset at the
              current CoinGecko price, stamped with the time it ran. It used to
              be a bare text button labelled "Actualizar precios", which read as
              a harmless refresh of the view. */}
          <div className="flex max-w-xs flex-col items-start gap-1 sm:items-end sm:text-right">
            <Button
              variant="ghost"
              onClick={() => refreshMutation.mutate()}
              disabled={refreshMutation.isPending}
              aria-describedby="refresh-prices-help"
              className="border border-rule"
            >
              <span className="inline-flex items-center gap-1.5">
                <RefreshCw size={14} strokeWidth={1.75} aria-hidden />
                {refreshMutation.isPending ? "Registrando precios…" : "Registrar precios de hoy"}
              </span>
            </Button>
            <p id="refresh-prices-help" className="text-xs text-ink-muted">
              Añade una entrada por activo con el precio actual. Las entradas existentes no cambian.
            </p>
          </div>
        </div>
        {refreshMutation.isError && (
          <p role="alert" className="mt-2 text-sm text-accent">
            {getApiErrorMessage(refreshMutation.error) ?? "No se pudieron registrar los precios."}
          </p>
        )}
        {refreshMutation.isSuccess && (
          <p role="status" className="mt-2 text-sm text-brand">
            {refreshMutation.data.updated.length === 0 && refreshMutation.data.skipped.length === 0
              ? "Este portfolio no tiene activos que registrar."
              : `Añadidas ${refreshMutation.data.updated.length} entrada(s) con el precio de ${formatRecordedAt(refreshMutation.data.refreshedAt)}.`}
            {refreshMutation.data.skipped.length > 0 &&
              ` Sin precio disponible: ${refreshMutation.data.skipped.map((s) => s.symbol).join(", ")}.`}
          </p>
        )}
      </div>

      <section className="panel overflow-hidden">
        <div className="flex flex-wrap items-end gap-3 border-b border-rule px-4 py-3">
          <SelectField
            label="Activo"
            name="filterCryptoCurrencyId"
            value={filterCryptoCurrencyId}
            onChange={(e) => updateFilters({ cryptoCurrencyId: e.target.value })}
            className="w-28"
          >
            <option value="">Todos</option>
            {cryptoCurrencies.map((c) => (
              <option key={c.id} value={c.id}>
                {c.symbol}
              </option>
            ))}
          </SelectField>
          <TextField
            label="Desde"
            name="filterFromDate"
            type="date"
            value={filterFromDate}
            onChange={(e) => updateFilters({ fromDate: e.target.value })}
            max={filterToDate || undefined}
            className="w-36"
          />
          <TextField
            label="Hasta"
            name="filterToDate"
            type="date"
            value={filterToDate}
            onChange={(e) => updateFilters({ toDate: e.target.value })}
            min={filterFromDate || undefined}
            max={todayInputValue()}
            className="w-36"
          />
          {hasActiveFilters && (
            <Button
              variant="ghost"
              onClick={() =>
                updateFilters({ cryptoCurrencyId: "", fromDate: "", toDate: "" })
              }
            >
              Limpiar filtros
            </Button>
          )}
        </div>
        {entriesPending ? (
          <div className="h-32 animate-pulse bg-paper-raised" />
        ) : entriesError ? (
          <ErrorNotice>
            {getApiErrorMessage(entriesErrorDetail) ?? "No se pudieron cargar las entradas."}
          </ErrorNotice>
        ) : (
          <>
          {/* Below lg the seven-column table (720px) hid Valor, Fecha and the
              actions behind a scroll nobody could see: the panel is 358px on a
              phone and 480–612px between md and lg once the sidebar appears.
              Same entries, stacked: asset + value; exchange · quantity × price
              · date; actions. The table takes over at lg, where it fits. */}
          <ul aria-label="Entradas" className="divide-y divide-rule text-sm lg:hidden">
            {entries.length === 0 ? (
              <li className="px-4 py-8 text-center text-ink-muted">
                {hasActiveFilters
                  ? "Ninguna entrada coincide con los filtros seleccionados."
                  : "Todavía no hay entradas en este portfolio."}
              </li>
            ) : (
              entries.map((entry) =>
                editingId === entry.id ? (
                  <EditEntryCard
                    key={entry.id}
                    entry={entry}
                    portfolioId={portfolioId}
                    cryptoCurrencies={cryptoCurrencies}
                    exchanges={exchanges}
                    onDone={() => setEditingId(null)}
                  />
                ) : (
                  <li key={entry.id} className="flex flex-col gap-1 px-4 py-3">
                    <div className="flex items-center justify-between gap-3">
                      <span className="font-medium">
                        {cryptoById.get(entry.cryptoCurrencyId)?.symbol ?? `#${entry.cryptoCurrencyId}`}
                      </span>
                      <span className="tabular font-semibold">
                        {formatCurrency(entry.quantity * entry.pricePerUnit)}
                      </span>
                    </div>
                    <div className="flex items-center justify-between gap-3 text-xs text-ink-muted">
                      <span className="min-w-0">
                        {exchangeById.get(entry.exchangeId)?.name ?? `#${entry.exchangeId}`} ·{" "}
                        <span className="tabular">
                          {formatQuantity(entry.quantity)} × {formatCurrency(entry.pricePerUnit)}
                        </span>{" "}
                        <span className="whitespace-nowrap">· {formatRecordedAt(entry.recordedAt)}</span>
                      </span>
                      <span className="shrink-0">
                        <RowActions
                          onEdit={() => setEditingId(entry.id)}
                          onDelete={() => deleteMutation.mutate(entry.id)}
                          deleting={deleteMutation.isPending && deleteMutation.variables === entry.id}
                        />
                      </span>
                    </div>
                  </li>
                )
              )
            )}
          </ul>
          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr className="border-b border-rule-strong text-left text-xs tracking-wide text-ink-muted uppercase">
                  <th className="px-4 py-2 font-medium">Activo</th>
                  <th className="px-4 py-2 font-medium">Exchange</th>
                  <th className="px-4 py-2 text-right font-medium">Cantidad</th>
                  <th className="px-4 py-2 text-right font-medium">Precio</th>
                  <th className="px-4 py-2 text-right font-medium">Valor</th>
                  <th className="px-4 py-2 font-medium">Fecha</th>
                  <th className="px-4 py-2 text-right font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {entries.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-ink-muted">
                      {hasActiveFilters
                        ? "Ninguna entrada coincide con los filtros seleccionados."
                        : "Todavía no hay entradas en este portfolio."}
                    </td>
                  </tr>
                )}
                {entries.map((entry) =>
                  editingId === entry.id ? (
                    <EditEntryRow
                      key={entry.id}
                      entry={entry}
                      portfolioId={portfolioId}
                      cryptoCurrencies={cryptoCurrencies}
                      exchanges={exchanges}
                      onDone={() => setEditingId(null)}
                    />
                  ) : (
                    <tr
                      key={entry.id}
                      className="border-b border-rule last:border-0 hover:bg-paper-raised"
                    >
                      <td className="px-4 py-2.5 font-medium">
                        {cryptoById.get(entry.cryptoCurrencyId)?.symbol ??
                          `#${entry.cryptoCurrencyId}`}
                      </td>
                      <td className="px-4 py-2.5 text-ink-muted">
                        {exchangeById.get(entry.exchangeId)?.name ?? `#${entry.exchangeId}`}
                      </td>
                      <td className="tabular px-4 py-2.5 text-right text-ink-muted">
                        {formatQuantity(entry.quantity)}
                      </td>
                      <td className="tabular px-4 py-2.5 text-right text-ink-muted">
                        {formatCurrency(entry.pricePerUnit)}
                      </td>
                      <td className="tabular px-4 py-2.5 text-right">
                        {formatCurrency(entry.quantity * entry.pricePerUnit)}
                      </td>
                      <td className="px-4 py-2.5 text-ink-muted">
                        {formatRecordedAt(entry.recordedAt)}
                      </td>
                      <td className="px-4 py-2.5 text-right">
                        <RowActions
                          onEdit={() => setEditingId(entry.id)}
                          onDelete={() => deleteMutation.mutate(entry.id)}
                          deleting={
                            deleteMutation.isPending && deleteMutation.variables === entry.id
                          }
                        />
                      </td>
                    </tr>
                  )
                )}
              </tbody>
            </table>
          </div>
          </>
        )}

        {entriesPage && (
          <Pagination
            page={entriesPage.page}
            totalPages={entriesPage.totalPages}
            total={entriesPage.total}
            limit={limit}
            onPageChange={setPage}
            onLimitChange={handleLimitChange}
          />
        )}

        {canAddEntry ? (
          <form
            onSubmit={handleCreate}
            className="flex flex-wrap items-end gap-3 border-t border-rule px-4 py-3"
          >
            <SelectField
              label="Activo"
              name="cryptoCurrencyId"
              value={cryptoCurrencyId}
              onChange={(e) => setCryptoCurrencyId(e.target.value)}
              required
              className="w-28"
            >
              <option value="" disabled>
                —
              </option>
              {cryptoCurrencies.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.symbol}
                </option>
              ))}
            </SelectField>
            <SelectField
              label="Exchange"
              name="exchangeId"
              value={exchangeId}
              onChange={(e) => setExchangeId(e.target.value)}
              required
              className="w-32"
            >
              <option value="" disabled>
                —
              </option>
              {exchanges.map((ex) => (
                <option key={ex.id} value={ex.id}>
                  {ex.name}
                </option>
              ))}
            </SelectField>
            <TextField
              label="Cantidad"
              name="quantity"
              type="number"
              step="any"
              min="0"
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              required
              className="w-28"
            />
            <TextField
              label="Precio"
              name="pricePerUnit"
              type="number"
              step="any"
              min="0"
              value={pricePerUnit}
              onChange={(e) => setPricePerUnit(e.target.value)}
              required
              className="w-28"
            />
            <TextField
              label="Fecha"
              name="recordedAt"
              type="date"
              value={recordedAt}
              onChange={(e) => setRecordedAt(e.target.value)}
              max={todayInputValue()}
              required
              className="w-36"
            />
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? "Añadiendo…" : "Añadir entrada"}
            </Button>
          </form>
        ) : null}
        {formError && (
          <p role="alert" className="border-t border-rule bg-accent-soft px-4 py-2 text-sm text-accent">
            {formError}
          </p>
        )}
        {createMutation.isError &&
          (createDuplicateId !== null ? (
            <div
              role="alert"
              className="flex flex-wrap items-center justify-between gap-2 border-t border-rule bg-accent-soft px-4 py-2 text-sm text-accent"
            >
              <span>
                Ya hay una entrada de {cryptoById.get(Number(cryptoCurrencyId))?.symbol ?? "este activo"} en{" "}
                {exchangeById.get(Number(exchangeId))?.name ?? "este exchange"} el{" "}
                {formatDate(`${recordedAt}T00:00:00Z`)}. Edítala en lugar de añadir otra.
              </span>
              <Button variant="ghost" onClick={() => openExistingEntry(createDuplicateId)}>
                Editar la existente
              </Button>
            </div>
          ) : (
            <p role="alert" className="border-t border-rule bg-accent-soft px-4 py-2 text-sm text-accent">
              {getApiErrorMessage(createMutation.error) ?? "No se pudo añadir la entrada."}
            </p>
          ))}
        {justAdded && !createMutation.isError && (
          <p className="border-t border-rule bg-brand-soft px-4 py-2 text-sm text-brand">
            Entrada añadida correctamente.
          </p>
        )}
        {referenceDataFailed ? (
          <ErrorNotice>
            No se pudieron cargar las criptomonedas o exchanges disponibles.
          </ErrorNotice>
        ) : (
          !canAddEntry && (
            <p className="border-t border-rule px-4 py-3 text-sm text-ink-muted">
              Necesitas al menos una{" "}
              <Link href="/cryptocurrencies" className="underline">
                criptomoneda
              </Link>{" "}
              y un{" "}
              <Link href="/exchanges" className="underline">
                exchange
              </Link>{" "}
              registrados antes de poder añadir una entrada.
            </p>
          )
        )}
      </section>
    </div>
  );
}

interface EditEntryProps {
  entry: PortfolioEntryResponse;
  portfolioId: number;
  cryptoCurrencies: CryptoCurrencyResponse[];
  exchanges: ExchangeResponse[];
  onDone: () => void;
}

// Edit state and save, shared by the desktop table row and the phone card so
// both submit exactly the same thing.
function useEntryEditor(entry: PortfolioEntryResponse, portfolioId: number, onDone: () => void) {
  const [cryptoCurrencyId, setCryptoCurrencyId] = useState(String(entry.cryptoCurrencyId));
  const [exchangeId, setExchangeId] = useState(String(entry.exchangeId));
  const [quantity, setQuantity] = useState(String(entry.quantity));
  const [pricePerUnit, setPricePerUnit] = useState(String(entry.pricePerUnit));
  const [recordedAt, setRecordedAt] = useState(entry.recordedAt.slice(0, 10));
  const updateMutation = useUpdatePortfolioEntry(entry.id, portfolioId);

  function handleSave() {
    if (!cryptoCurrencyId || !exchangeId || !quantity || !pricePerUnit) return;
    updateMutation.mutate(
      {
        cryptoCurrencyId: Number(cryptoCurrencyId),
        exchangeId: Number(exchangeId),
        quantity: Number(quantity),
        pricePerUnit: Number(pricePerUnit),
        // The date input only holds a day. If it wasn't changed, keep the
        // original timestamp: rewriting a timed entry (e.g. one from "Registrar
        // precios de hoy") to 00:00 would reorder it, or clash with a hand-typed
        // entry for the same day.
        recordedAt:
          recordedAt === entry.recordedAt.slice(0, 10)
            ? entry.recordedAt
            : new Date(recordedAt).toISOString(),
      },
      { onSuccess: onDone }
    );
  }

  const errorMessage = !updateMutation.isError
    ? null
    : duplicateEntryId(updateMutation.error) !== null
      ? "Ya hay otra entrada de ese activo en ese exchange con esa fecha. Cambia la fecha o edita la otra."
      : (getApiErrorMessage(updateMutation.error) ?? "No se pudo guardar la entrada.");

  return {
    cryptoCurrencyId, setCryptoCurrencyId,
    exchangeId, setExchangeId,
    quantity, setQuantity,
    pricePerUnit, setPricePerUnit,
    recordedAt, setRecordedAt,
    saving: updateMutation.isPending,
    errorMessage,
    handleSave,
  };
}

function EditEntryCard({ entry, portfolioId, cryptoCurrencies, exchanges, onDone }: EditEntryProps) {
  const editor = useEntryEditor(entry, portfolioId, onDone);
  const idPrefix = `edit-entry-${entry.id}`;

  return (
    <li className="flex flex-col gap-3 bg-paper-raised px-4 py-3">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
        <SelectField
          label="Activo"
          id={`${idPrefix}-asset`}
          value={editor.cryptoCurrencyId}
          onChange={(e) => editor.setCryptoCurrencyId(e.target.value)}
        >
          {cryptoCurrencies.map((c) => (
            <option key={c.id} value={c.id}>
              {c.symbol}
            </option>
          ))}
        </SelectField>
        <SelectField
          label="Exchange"
          id={`${idPrefix}-exchange`}
          value={editor.exchangeId}
          onChange={(e) => editor.setExchangeId(e.target.value)}
        >
          {exchanges.map((ex) => (
            <option key={ex.id} value={ex.id}>
              {ex.name}
            </option>
          ))}
        </SelectField>
        <TextField
          label="Cantidad"
          id={`${idPrefix}-quantity`}
          type="number"
          step="any"
          inputMode="decimal"
          value={editor.quantity}
          onChange={(e) => editor.setQuantity(e.target.value)}
          className="tabular w-full text-right"
        />
        <TextField
          label="Precio"
          id={`${idPrefix}-price`}
          type="number"
          step="any"
          inputMode="decimal"
          value={editor.pricePerUnit}
          onChange={(e) => editor.setPricePerUnit(e.target.value)}
          className="tabular w-full text-right"
        />
        <TextField
          label="Fecha"
          id={`${idPrefix}-date`}
          type="date"
          value={editor.recordedAt}
          onChange={(e) => editor.setRecordedAt(e.target.value)}
          className="w-full"
        />
        <div className="flex flex-col justify-end gap-1 text-xs text-ink-muted">
          Valor
          <span className="tabular py-2 text-sm text-ink">
            {formatCurrency(Number(editor.quantity || 0) * Number(editor.pricePerUnit || 0))}
          </span>
        </div>
      </div>
      {editor.errorMessage && (
        <p role="alert" className="rounded-lg bg-accent-soft px-3 py-2 text-sm text-accent">
          {editor.errorMessage}
        </p>
      )}
      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onDone}>
          Cancelar
        </Button>
        <Button onClick={editor.handleSave} disabled={editor.saving}>
          {editor.saving ? "Guardando…" : "Guardar"}
        </Button>
      </div>
    </li>
  );
}

function EditEntryRow({ entry, portfolioId, cryptoCurrencies, exchanges, onDone }: EditEntryProps) {
  const {
    cryptoCurrencyId, setCryptoCurrencyId,
    exchangeId, setExchangeId,
    quantity, setQuantity,
    pricePerUnit, setPricePerUnit,
    recordedAt, setRecordedAt,
    saving,
    errorMessage,
    handleSave,
  } = useEntryEditor(entry, portfolioId, onDone);

  const inputClassName =
    "w-full rounded-lg border border-rule bg-paper px-2 py-1 text-sm focus-visible:outline-2 focus-visible:outline-brand";

  return (
    <>
    <tr className={`bg-paper-raised ${errorMessage ? "" : "border-b border-rule last:border-0"}`}>
      <td className="px-4 py-2">
        <select
          value={cryptoCurrencyId}
          onChange={(e) => setCryptoCurrencyId(e.target.value)}
          className={inputClassName}
        >
          {cryptoCurrencies.map((c) => (
            <option key={c.id} value={c.id}>
              {c.symbol}
            </option>
          ))}
        </select>
      </td>
      <td className="px-4 py-2">
        <select
          value={exchangeId}
          onChange={(e) => setExchangeId(e.target.value)}
          className={inputClassName}
        >
          {exchanges.map((ex) => (
            <option key={ex.id} value={ex.id}>
              {ex.name}
            </option>
          ))}
        </select>
      </td>
      <td className="px-4 py-2">
        <input
          type="number"
          step="any"
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
          className={`tabular text-right ${inputClassName}`}
        />
      </td>
      <td className="px-4 py-2">
        <input
          type="number"
          step="any"
          value={pricePerUnit}
          onChange={(e) => setPricePerUnit(e.target.value)}
          className={`tabular text-right ${inputClassName}`}
        />
      </td>
      <td className="tabular px-4 py-2 text-right text-ink-muted">
        {formatCurrency(Number(quantity || 0) * Number(pricePerUnit || 0))}
      </td>
      <td className="px-4 py-2">
        <input
          type="date"
          value={recordedAt}
          onChange={(e) => setRecordedAt(e.target.value)}
          className={inputClassName}
        />
      </td>
      <td className="px-4 py-2 text-right">
        <span className="inline-flex items-center gap-3">
          <IconButton aria-label="Guardar" onClick={handleSave} disabled={saving}>
            <Check size={14} strokeWidth={1.75} aria-hidden />
          </IconButton>
          <IconButton aria-label="Cancelar" onClick={onDone}>
            <X size={14} strokeWidth={1.75} aria-hidden />
          </IconButton>
        </span>
      </td>
    </tr>
    {errorMessage && (
      <tr className="border-b border-rule bg-paper-raised last:border-0">
        <td colSpan={7} className="px-4 pb-2">
          <p role="alert" className="rounded-lg bg-accent-soft px-3 py-2 text-sm text-accent">
            {errorMessage}
          </p>
        </td>
      </tr>
    )}
    </>
  );
}
