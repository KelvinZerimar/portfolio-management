"use client";

import { type FormEvent, useMemo, useState } from "react";
import { Check, Search, X } from "lucide-react";
import {
  useCreateNote,
  useDeleteNote,
  useNotes,
  useUpdateNote,
} from "@/hooks/useNotes";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { IconButton } from "@/components/ui/IconButton";
import { Pagination } from "@/components/ui/Pagination";
import { RichTextContent } from "@/components/ui/RichTextContent";
import { isRichTextEmpty, RichTextEditor } from "@/components/ui/RichTextEditor";
import { RowActions } from "@/components/ui/RowActions";
import { TextField } from "@/components/ui/TextField";
import { formatDate } from "@/lib/format";
import { getApiErrorMessage } from "@/lib/errors";
import type { NoteResponse } from "@/types";

function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, " ");
}

const TABLE_COLUMN_COUNT = 5;

export default function NotesPage() {
  // Notes has no server-side free-text search (its content is encrypted at
  // rest, so the API can't filter on it cheaply) — fetch the API's max page
  // size once and search/paginate client-side over that batch.
  const { data, isPending, isError, error } = useNotes({ limit: 100 });
  const createMutation = useCreateNote();
  const deleteMutation = useDeleteNote();

  const allItems = useMemo(() => data?.data ?? [], [data]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [category, setCategory] = useState("");
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [limit, setLimit] = useState(10);

  const filteredItems = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return allItems;
    return allItems.filter(
      (item) =>
        item.category.toLowerCase().includes(query) ||
        item.title.toLowerCase().includes(query) ||
        stripHtml(item.content).toLowerCase().includes(query)
    );
  }, [allItems, search]);

  const total = filteredItems.length;
  const totalPages = Math.max(Math.ceil(total / limit), 1);
  const items = filteredItems.slice((page - 1) * limit, page * limit);

  function handleSearchChange(value: string) {
    setSearch(value);
    setPage(1);
  }

  function handleLimitChange(value: number) {
    setLimit(value);
    setPage(1);
  }

  function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!category.trim() || !title.trim() || isRichTextEmpty(content)) return;
    createMutation.mutate(
      { category: category.trim(), title: title.trim(), content, isActive },
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
    <div className="mx-auto flex max-w-4xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Notas</h1>
        <p className="text-sm text-ink-muted">
          Notas personales guardadas de forma cifrada. El contenido viaja siempre en texto
          plano entre esta pantalla y la API; el cifrado solo vive en el servidor.
        </p>
      </div>

      <section className="panel overflow-hidden">
        <div className="border-b border-rule px-4 py-3">
          <label className="relative flex max-w-sm items-center text-xs text-ink-muted">
            <Search
              size={14}
              strokeWidth={1.75}
              className="pointer-events-none absolute left-3 text-ink-muted"
              aria-hidden
            />
            <input
              type="search"
              value={search}
              onChange={(e) => handleSearchChange(e.target.value)}
              placeholder="Buscar por categoría, título o contenido"
              className="w-full rounded-lg border border-rule bg-paper py-2 pr-3 pl-9 text-sm text-ink placeholder:text-ink-muted focus-visible:outline-2 focus-visible:outline-brand"
            />
          </label>
        </div>

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
                <th className="w-24 px-4 py-2 text-right font-medium">Acciones</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr>
                  <td colSpan={TABLE_COLUMN_COUNT} className="px-4 py-8 text-center text-ink-muted">
                    {allItems.length === 0
                      ? "Todavía no hay notas guardadas."
                      : "Ninguna nota coincide con la búsqueda."}
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
                    <td className="px-4 py-2.5 text-ink-muted">
                      <RichTextContent html={item.content} className="line-clamp-2 max-w-sm" />
                    </td>
                    <td className="px-4 py-2.5 text-ink-muted">{formatDate(item.createdAt)}</td>
                    <td className="px-4 py-2.5 text-right whitespace-nowrap">
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

        {!isPending && !isError && (
          <Pagination
            page={page}
            totalPages={totalPages}
            total={total}
            limit={limit}
            onPageChange={setPage}
            onLimitChange={handleLimitChange}
          />
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
          <RichTextEditor
            label="Contenido"
            value={content}
            onChange={setContent}
            placeholder="Contenido de la nota"
            className="flex-1 basis-full"
          />
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
  const updateMutation = useUpdateNote(item.id);

  function handleSave() {
    if (!category.trim() || !title.trim() || isRichTextEmpty(content)) return;
    updateMutation.mutate(
      { category: category.trim(), title: title.trim(), content, isActive: item.isActive },
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
        <RichTextEditor value={content} onChange={setContent} />
      </td>
      <td className="px-4 py-2 text-ink-muted">{formatDate(item.createdAt)}</td>
      <td className="px-4 py-2 text-right whitespace-nowrap">
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
