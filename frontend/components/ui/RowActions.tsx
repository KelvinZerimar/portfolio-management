"use client";

import { type ReactNode, useState } from "react";
import { Pencil, Trash2 } from "lucide-react";
import { IconButton } from "./IconButton";

interface RowActionsProps {
  onEdit?: () => void;
  onDelete: () => void;
  deleting?: boolean;
  /** Question shown while confirming. Rendered only then, so a component
   *  passed here can fetch what it needs (e.g. how much would be lost) lazily. */
  confirmMessage?: ReactNode;
}

// Inline delete confirmation instead of a modal — the task needs a moment of
// protection, not an interruption. "Exhaust inline alternatives first."
export function RowActions({ onEdit, onDelete, deleting, confirmMessage }: RowActionsProps) {
  const [confirming, setConfirming] = useState(false);

  if (confirming) {
    return (
      <span className="inline-flex flex-wrap items-center justify-end gap-x-2 gap-y-1 text-xs">
        {confirmMessage ?? <span className="whitespace-nowrap">¿Eliminar?</span>}
        <button
          type="button"
          onClick={onDelete}
          disabled={deleting}
          className="font-medium text-accent hover:underline disabled:opacity-40"
        >
          Sí
        </button>
        <button
          type="button"
          onClick={() => setConfirming(false)}
          className="text-ink-muted hover:underline"
        >
          No
        </button>
      </span>
    );
  }

  return (
    <span className="inline-flex items-center gap-3">
      {onEdit && (
        <IconButton aria-label="Editar" onClick={onEdit}>
          <Pencil size={14} strokeWidth={1.75} aria-hidden />
        </IconButton>
      )}
      <IconButton aria-label="Eliminar" tone="danger" onClick={() => setConfirming(true)}>
        <Trash2 size={14} strokeWidth={1.75} aria-hidden />
      </IconButton>
    </span>
  );
}
