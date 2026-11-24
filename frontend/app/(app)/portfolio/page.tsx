"use client";

import { type FormEvent, useState } from "react";
import Link from "next/link";
import { useCreatePortfolio, useDeletePortfolio, usePortfolios } from "@/hooks/usePortfolios";
import { usePortfolioValue } from "@/hooks/usePortfolioAnalytics";
import { usePortfolioEntries } from "@/hooks/usePortfolioEntries";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { RowActions } from "@/components/ui/RowActions";
import { TextField } from "@/components/ui/TextField";
import { formatCurrency, formatDate } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";
import type { PortfolioResponse } from "@/types";

// Current value (latest entry per asset), so the list answers "how much" without
// a trip to the dashboard.
function PortfolioValue({ id }: { id: number }) {
  const { data, isPending, isError } = usePortfolioValue(id);
  if (isPending) return <span className="text-ink-muted">…</span>;
  if (isError) return <span className="text-ink-muted">—</span>;
  return <span className="tabular">{formatCurrency(data.value)}</span>;
}

// Deleting a portfolio deletes its whole ledger (cascade). The question says so,
// with the number of entries at stake; it mounts only while confirming, so the
// count is fetched only then.
function DeletePortfolioQuestion({ portfolio }: { portfolio: PortfolioResponse }) {
  const { data } = usePortfolioEntries({ portfolioId: portfolio.id, limit: 1 });
  const total = data?.total;
  const lost =
    total === undefined
      ? ""
      : total === 0
        ? " Está vacío."
        : ` Se borrarán ${total === 1 ? "su entrada" : `sus ${total} entradas`} y no se puede deshacer.`;
  return (
    <span className="max-w-64 text-right font-medium text-accent">
      ¿Eliminar «{portfolio.name}»?{lost}
    </span>
  );
}

export default function PortfolioListPage() {
  const { data, isPending, isError, error } = usePortfolios({ limit: 100 });
  const createMutation = useCreatePortfolio();
  const deleteMutation = useDeletePortfolio();

  const items = data?.data ?? [];
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;
    createMutation.mutate(
      { name: name.trim(), description: description.trim() },
      { onSuccess: () => { setName(""); setDescription(""); } }
    );
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Portfolios</h1>
        <p className="text-sm text-ink-muted">
          Cada portfolio lleva su propio registro de entradas por activo, exchange y fecha.
        </p>
      </div>

      <section className="panel overflow-hidden">
        {isPending ? (
          <div className="h-32 animate-pulse bg-paper-raised" />
        ) : isError ? (
          <ErrorNotice>{getApiErrorMessage(error) ?? "No se pudieron cargar los portfolios."}</ErrorNotice>
        ) : (
          <>
          {/* Below sm the four-column table measured 394px inside a 358px
              overflow-hidden panel: the Acciones column was clipped and a
              portfolio could not be deleted on a phone. A stacked list keeps
              every field and the actions in view. */}
          <ul aria-label="Portfolios" className="divide-y divide-rule text-sm sm:hidden">
            {items.length === 0 ? (
              <li className="px-4 py-8 text-center text-ink-muted">Todavía no tienes portfolios.</li>
            ) : (
              items.map((item) => (
                <li key={item.id} className="flex items-start justify-between gap-3 px-4 py-3">
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <Link
                      href={`/portfolio/${item.id}`}
                      className="font-medium underline-offset-2 hover:underline"
                    >
                      {item.name}
                    </Link>
                    {item.description && <span className="text-ink-muted">{item.description}</span>}
                    <span className="font-semibold">
                      <PortfolioValue id={item.id} />
                    </span>
                    <span className="text-xs text-ink-muted">Creado el {formatDate(item.createdAt)}</span>
                  </div>
                  <span className="max-w-[55%] shrink-0 pt-0.5 text-right">
                    <RowActions
                      onDelete={() => deleteMutation.mutate(item.id)}
                      deleting={deleteMutation.isPending && deleteMutation.variables === item.id}
                      confirmMessage={<DeletePortfolioQuestion portfolio={item} />}
                    />
                  </span>
                </li>
              ))
            )}
          </ul>
          <table className="hidden w-full border-collapse text-sm sm:table">
            <thead>
              <tr className="border-b border-rule-strong text-left text-xs tracking-wide text-ink-muted uppercase">
                <th className="px-4 py-2 font-medium">Nombre</th>
                <th className="px-4 py-2 font-medium">Descripción</th>
                <th className="px-4 py-2 text-right font-medium">Valor</th>
                <th className="px-4 py-2 font-medium">Creado</th>
                <th className="px-4 py-2 text-right font-medium">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-ink-muted">
                    Todavía no tienes portfolios.
                  </td>
                </tr>
              )}
              {items.map((item) => (
                <tr
                  key={item.id}
                  className="border-b border-rule last:border-0 hover:bg-paper-raised"
                >
                  <td className="px-4 py-2.5">
                    <Link
                      href={`/portfolio/${item.id}`}
                      className="font-medium underline-offset-2 hover:underline"
                    >
                      {item.name}
                    </Link>
                  </td>
                  <td className="px-4 py-2.5 text-ink-muted">{item.description || "—"}</td>
                  <td className="px-4 py-2.5 text-right whitespace-nowrap">
                    <PortfolioValue id={item.id} />
                  </td>
                  <td className="px-4 py-2.5 text-ink-muted">{formatDate(item.createdAt)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <RowActions
                      onDelete={() => deleteMutation.mutate(item.id)}
                      deleting={deleteMutation.isPending && deleteMutation.variables === item.id}
                      confirmMessage={<DeletePortfolioQuestion portfolio={item} />}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </>
        )}

        <form
          onSubmit={handleCreate}
          className="flex flex-wrap items-end gap-3 border-t border-rule px-4 py-3"
        >
          <TextField
            label="Nombre"
            name="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Mi portfolio principal"
            required
            className="w-56"
          />
          <TextField
            label="Descripción"
            name="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Opcional"
            className="w-64"
          />
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? "Creando…" : "Crear portfolio"}
          </Button>
        </form>
        {createMutation.isError && (
          <p role="alert" className="border-t border-rule bg-accent-soft px-4 py-2 text-sm text-accent">
            {getApiErrorMessage(createMutation.error) ?? "No se pudo crear el portfolio."}
          </p>
        )}
      </section>
    </div>
  );
}
