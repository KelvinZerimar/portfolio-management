"use client";

import { type FormEvent, useState } from "react";
import { Check, X } from "lucide-react";
import {
  useCreateNote,
  useDeleteNote,
  useNotes,
  useUpdateNote,
} from "@/hooks/useNotes";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { IconButton } from "@/components/ui/IconButton";
import { RowActions } from "@/components/ui/RowActions";
import { TextField } from "@/components/ui/TextField";
import { formatDate } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";
import type { NoteResponse } from "@/types";

function truncate(text: string, max = 80) {
  if (text.length <= max) return text;
  return `${text.slice(0, max)}…`;
}

export default function NotesPage() {
  const { data, isPending, isError, error } = useNotes({ limit: 100 });
  const createMutation = useCreateNote();
  const deleteMutation = useDeleteNote();

  const items = data?.data ?? [];
  const [editingId, setEditingId] = useState<string | null>(null);
  const [category, setCategory] = useState("");
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isActive, setIsActive] = useState(true);

  function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!category.trim() || !title.trim() || !content.trim()) return;
    createMutation.mutate(
      { category: category.trim(), title: title.trim(), content: content.trim(), isActive },
      {
        onSuccess: () => {
          setCategory("");
          setTitle("");
          setContent("");
          setIsActive(true);
        },
      }
    );
  }

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Notas</h1>
        <p className="text-sm text-ink-muted">
          Notas personales guardadas de forma cifrada. El contenido viaja siempre en texto
          plano entre esta pantalla y la API; el cifrado solo vive en el servidor.
        </p>
      </div>

      <section className="panel overflow-hidden">
        {isPending ? (
          <div className="h-32 animate-pulse bg-paper-raised" />
        ) : isError ? (
          <ErrorNotice>{getApiErrorMessage(error) ?? "No se pudieron cargar las notas."}</ErrorNotice>
        ) : (
          <table className="w-full border-collapse text-sm">
            <thead>
              <tr className="border-b border-rule-strong text-left text-xs tracking-wide text-ink-muted uppercase">
                <th className="px-4 py-2 font-medium">Categoría</th>
                <th className="px-4 py-2 font-medium">Título</th>
                <th className="px-4 py-2 font-medium">Contenido</th>
                <th className="px-4 py-2 font-medium">Creada</th>
                <th className="px-4 py-2 font-medium">Activa</th>
                <th className="px-4 py-2 text-right font-medium">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-ink-muted">
                    Todavía no hay notas guardadas.
                  </td>
                </tr>
              )}
              {items.map((item) =>
                editingId === item.id ? (
                  <EditRow key={item.id} item={item} onDone={() => setEditingId(null)} />
                ) : (
                  <tr
                    key={item.id}
                    className="border-b border-rule last:border-0 hover:bg-paper-raised"
                  >
                    <td className="px-4 py-2.5 font-medium">{item.category}</td>
                    <td className="px-4 py-2.5">{item.title}</td>
                    <td className="px-4 py-2.5 text-ink-muted">{truncate(item.content)}</td>
                    <td className="px-4 py-2.5 text-ink-muted">{formatDate(item.createdAt)}</td>
                    <td className="px-4 py-2.5 text-ink-muted">{item.isActive ? "Sí" : "No"}</td>
                    <td className="px-4 py-2.5 text-right">
                      <RowActions
                        onEdit={() => setEditingId(item.id)}
                        onDelete={() => deleteMutation.mutate(item.id)}
                        deleting={
                          deleteMutation.isPending && deleteMutation.variables === item.id
                        }
                      />
                    </td>
                  </tr>
                )
              )}
            </tbody>
          </table>
        )}

        <form
          onSubmit={handleCreate}
          className="flex flex-wrap items-end gap-3 border-t border-rule px-4 py-3"
        >
          <TextField
            label="Categoría"
            name="category"
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            placeholder="Service"
            required
            className="w-32"
          />
          <TextField
            label="Título"
            name="title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Sanitas"
            required
            className="w-40"
          />
          <label className="flex flex-1 basis-full flex-col gap-1 text-xs text-ink-muted">
            Contenido
            <textarea
              name="content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Contenido de la nota"
              required
              rows={3}
              className="rounded-lg border border-rule bg-paper px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus-visible:outline-2 focus-visible:outline-brand"
            />
          </label>
          <label className="flex items-center gap-2 text-xs text-ink-muted">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
            />
            Activa
          </label>
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending ? "Añadiendo…" : "Añadir"}
          </Button>
        </form>
        {createMutation.isError && (
          <p role="alert" className="border-t border-rule bg-accent-soft px-4 py-2 text-sm text-accent">
            {getApiErrorMessage(createMutation.error) ?? "No se pudo añadir la nota."}
          </p>
        )}
      </section>
    </div>
  );
}

function EditRow({ item, onDone }: { item: NoteResponse; onDone: () => void }) {
  const [category, setCategory] = useState(item.category);
  const [title, setTitle] = useState(item.title);
  const [content, setContent] = useState(item.content);
  const [isActive, setIsActive] = useState(item.isActive);
  const updateMutation = useUpdateNote(item.id);

  function handleSave() {
    if (!category.trim() || !title.trim() || !content.trim()) return;
    updateMutation.mutate(
      { category: category.trim(), title: title.trim(), content: content.trim(), isActive },
      { onSuccess: onDone }
    );
  }

  return (
    <tr className="border-b border-rule bg-paper-raised last:border-0">
      <td className="px-4 py-2">
        <input
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          className="w-full rounded-lg border border-rule bg-paper px-2 py-1 text-sm focus-visible:outline-2 focus-visible:outline-brand"
        />
      </td>
      <td className="px-4 py-2">
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="w-full rounded-lg border border-rule bg-paper px-2 py-1 text-sm focus-visible:outline-2 focus-visible:outline-brand"
        />
      </td>
      <td className="px-4 py-2">
        <textarea
          value={content}
          onChange={(e) => setContent(e.target.value)}
          rows={2}
          className="w-full rounded-lg border border-rule bg-paper px-2 py-1 text-sm focus-visible:outline-2 focus-visible:outline-brand"
        />
      </td>
      <td className="px-4 py-2 text-ink-muted">{formatDate(item.createdAt)}</td>
      <td className="px-4 py-2">
        <input
          type="checkbox"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
      </td>
      <td className="px-4 py-2 text-right">
        <span className="inline-flex items-center gap-3">
          <IconButton aria-label="Guardar" onClick={handleSave} disabled={updateMutation.isPending}>
            <Check size={14} strokeWidth={1.75} aria-hidden />
          </IconButton>
          <IconButton aria-label="Cancelar" onClick={onDone}>
            <X size={14} strokeWidth={1.75} aria-hidden />
          </IconButton>
        </span>
      </td>
    </tr>
  );
}
