"use client";

import { type ReactNode, useEffect } from "react";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Placeholder from "@tiptap/extension-placeholder";
import { TextStyle } from "@tiptap/extension-text-style";
import { Color } from "@tiptap/extension-color";
import {
  Baseline,
  Bold,
  Italic,
  List,
  ListOrdered,
  Quote,
  Redo,
  Strikethrough,
  Undo,
} from "lucide-react";

// Fixed palette instead of a free color picker: keeps the toolbar inline (no
// floating popover that could get clipped by an ancestor's overflow-hidden,
// as happened with the table panel) and matches the app's existing palette.
const TEXT_COLORS = [
  { label: "Rojo", value: "#dc2626" },
  { label: "Ámbar", value: "#d97706" },
  { label: "Verde", value: "#16a34a" },
  { label: "Azul", value: "#2563eb" },
  { label: "Morado", value: "#4f46e5" },
];

interface RichTextEditorProps {
  label?: string;
  value: string;
  onChange: (html: string) => void;
  placeholder?: string;
  className?: string;
}

function ToolbarButton({
  active,
  disabled,
  onClick,
  label,
  children,
}: {
  active?: boolean;
  disabled?: boolean;
  onClick: () => void;
  label: string;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      aria-pressed={active}
      disabled={disabled}
      onMouseDown={(e) => e.preventDefault()}
      onClick={onClick}
      className={
        "rounded-md p-1.5 transition-colors disabled:pointer-events-none disabled:opacity-40 " +
        (active ? "bg-brand-soft text-brand" : "text-ink-muted hover:bg-paper-raised hover:text-ink")
      }
    >
      {children}
    </button>
  );
}

export function RichTextEditor({ label, value, onChange, placeholder, className }: RichTextEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit,
      TextStyle,
      Color,
      Placeholder.configure({
        placeholder: placeholder ?? "",
      }),
    ],
    content: value,
    immediatelyRender: false,
    editorProps: {
      attributes: {
        class: "rich-text-input min-h-24 px-3 py-2 text-sm focus-visible:outline-none",
      },
    },
    onUpdate: ({ editor: updatedEditor }) => onChange(updatedEditor.getHTML()),
  });

  useEffect(() => {
    if (!editor) return;
    if (value !== editor.getHTML() && !editor.isFocused) {
      editor.commands.setContent(value, { emitUpdate: false });
    }
  }, [editor, value]);

  if (!editor) return null;

  return (
    <div className={"flex flex-col gap-1 text-xs text-ink-muted " + (className ?? "")}>
      {label}
      <div className="rounded-lg border border-rule bg-paper focus-within:outline-2 focus-within:outline-brand">
        <div className="flex flex-wrap items-center gap-0.5 border-b border-rule px-1.5 py-1">
          <ToolbarButton
            label="Negrita"
            active={editor.isActive("bold")}
            onClick={() => editor.chain().focus().toggleBold().run()}
          >
            <Bold size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <ToolbarButton
            label="Cursiva"
            active={editor.isActive("italic")}
            onClick={() => editor.chain().focus().toggleItalic().run()}
          >
            <Italic size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <ToolbarButton
            label="Tachado"
            active={editor.isActive("strike")}
            onClick={() => editor.chain().focus().toggleStrike().run()}
          >
            <Strikethrough size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <span className="mx-1 h-4 w-px bg-rule" aria-hidden />
          <ToolbarButton
            label="Lista con viñetas"
            active={editor.isActive("bulletList")}
            onClick={() => editor.chain().focus().toggleBulletList().run()}
          >
            <List size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <ToolbarButton
            label="Lista numerada"
            active={editor.isActive("orderedList")}
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
          >
            <ListOrdered size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <ToolbarButton
            label="Cita"
            active={editor.isActive("blockquote")}
            onClick={() => editor.chain().focus().toggleBlockquote().run()}
          >
            <Quote size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <span className="mx-1 h-4 w-px bg-rule" aria-hidden />
          <span className="flex items-center gap-1 px-0.5" role="group" aria-label="Color de texto">
            {TEXT_COLORS.map((color) => (
              <button
                key={color.value}
                type="button"
                aria-label={`Color ${color.label}`}
                aria-pressed={editor.isActive("textStyle", { color: color.value })}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => editor.chain().focus().setColor(color.value).run()}
                style={{ backgroundColor: color.value }}
                className={
                  "h-4 w-4 rounded-full transition-shadow " +
                  (editor.isActive("textStyle", { color: color.value })
                    ? "ring-2 ring-offset-1 ring-brand"
                    : "ring-1 ring-inset ring-black/10 hover:ring-black/20")
                }
              />
            ))}
            <ToolbarButton
              label="Quitar color"
              onClick={() => editor.chain().focus().unsetColor().run()}
            >
              <Baseline size={14} strokeWidth={2} aria-hidden />
            </ToolbarButton>
          </span>
          <span className="mx-1 h-4 w-px bg-rule" aria-hidden />
          <ToolbarButton
            label="Deshacer"
            disabled={!editor.can().undo()}
            onClick={() => editor.chain().focus().undo().run()}
          >
            <Undo size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
          <ToolbarButton
            label="Rehacer"
            disabled={!editor.can().redo()}
            onClick={() => editor.chain().focus().redo().run()}
          >
            <Redo size={14} strokeWidth={2} aria-hidden />
          </ToolbarButton>
        </div>
        <EditorContent editor={editor} />
      </div>
    </div>
  );
}

export function isRichTextEmpty(html: string | undefined | null): boolean {
  if (!html) return true;
  return html.replace(/<[^>]*>/g, "").replace(/&nbsp;/g, " ").trim().length === 0;
}
