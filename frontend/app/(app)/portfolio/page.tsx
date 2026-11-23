"use client";

import { type FormEvent, useState } from "react";
import Link from "next/link";
import { useCreatePortfolio, useDeletePortfolio, usePortfolios } from "@/hooks/usePortfolios";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { RowActions } from "@/components/ui/RowActions";
import { TextField } from "@/components/ui/TextField";
import { formatDate } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";

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
                    <span className="text-xs text-ink-muted">Creado el {formatDate(item.createdAt)}</span>
                  </div>
                  <span className="shrink-0 pt-0.5">
                    <RowActions
                      onDelete={() => deleteMutation.mutate(item.id)}
                      deleting={deleteMutation.isPending && deleteMutation.variables === item.id}
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
                <th className="px-4 py-2 font-medium">Creado</th>
                <th className="px-4 py-2 text-right font-medium">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-ink-muted">
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
                  <td className="px-4 py-2.5 text-ink-muted">{formatDate(item.createdAt)}</td>
                  <td className="px-4 py-2.5 text-right">
                    <RowActions
                      onDelete={() => deleteMutation.mutate(item.id)}
                      deleting={deleteMutation.isPending && deleteMutation.variables === item.id}
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
