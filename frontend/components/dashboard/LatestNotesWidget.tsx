"use client";

import Link from "next/link";
import { useLatestNotes } from "@/hooks/useNotes";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { formatDate } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";

function truncate(text: string, max = 100) {
  if (text.length <= max) return text;
  return `${text.slice(0, max)}…`;
}

// Mirrors HoldingsTable's panel shell, but as a simple list — ten items don't
// need sorting/filtering/export the way a full holdings table does.
export function LatestNotesWidget() {
  const { data, isPending, isError, error } = useLatestNotes(10);
  const notes = data ?? [];

  return (
    <section aria-label="Notas recientes" className="panel overflow-hidden">
      <div className="flex items-center justify-between gap-3 border-b border-rule px-6 py-4">
        <h2 className="text-sm font-bold">Notas recientes</h2>
        <Link href="/notes" className="text-xs font-medium text-ink-muted hover:text-ink">
          Ver todas
        </Link>
      </div>

      {isPending ? (
        <div className="h-32 animate-pulse bg-paper-raised" />
      ) : isError ? (
        <div className="p-1">
          <ErrorNotice>
            {getApiErrorMessage(error) ?? "No se pudieron cargar las notas."}
          </ErrorNotice>
        </div>
      ) : notes.length === 0 ? (
        <p className="px-4 py-8 text-center text-sm text-ink-muted">
          Todavía no hay notas guardadas.
        </p>
      ) : (
        <ul className="divide-y divide-rule">
          {notes.map((note) => (
            <li key={note.id} className="flex flex-col gap-1 px-6 py-3">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-medium">{note.title}</span>
                <span className="rounded-full bg-paper-raised px-2 py-0.5 text-[11px] font-medium text-ink-muted">
                  {note.category}
                </span>
              </div>
              <p className="text-sm text-ink-muted">{truncate(note.content)}</p>
              <span className="text-xs text-ink-muted">{formatDate(note.createdAt)}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
