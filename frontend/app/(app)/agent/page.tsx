"use client";

import { type FormEvent, useEffect, useRef, useState } from "react";
import { Bot, Send, User } from "lucide-react";
import { useAgentChat } from "@/hooks/useAgentChat";
import { type ChatTurn, useAgentChatStore } from "@/store/useAgentChatStore";
import { Button } from "@/components/ui/Button";
import { ErrorNotice } from "@/components/ui/ErrorNotice";
import { getApiErrorMessage } from "@/lib/errors";

// The backend caps the outgoing `message` at 2000 chars and keeps no memory between
// calls, so this is the only place conversation continuity exists. The margin below
// 2000 leaves room for the "previous turns" framing text itself.
const MAX_OUTGOING_CHARS = 1800;

// Recreates enough of the visible thread for the agent to feel like it remembers,
// without ever exceeding the server's per-message limit. Keeps the most recent turns
// and silently drops older ones first — the user still sees their full history on
// screen, only what gets sent to the model is trimmed.
function buildOutgoingMessage(history: ChatTurn[], question: string): string {
  const header = "Conversación previa:\n";
  const footer = `\n\nPregunta actual del usuario:\n${question}`;
  const budget = MAX_OUTGOING_CHARS - header.length - footer.length;
  if (budget <= 0) return question;

  const lines: string[] = [];
  let used = 0;
  for (let i = history.length - 1; i >= 0; i--) {
    const turn = history[i];
    const line = `${turn.role === "user" ? "Usuario" : "Agente"}: ${turn.content}`;
    if (used + line.length + 1 > budget) break;
    lines.unshift(line);
    used += line.length + 1;
  }

  if (lines.length === 0) return question;
  return header + lines.join("\n") + footer;
}

export default function AgentPage() {
  const hasHydrated = useAgentChatStore((state) => state.hasHydrated);
  const history = useAgentChatStore((state) => state.history);
  const addTurn = useAgentChatStore((state) => state.addTurn);
  const clear = useAgentChatStore((state) => state.clear);

  const [draft, setDraft] = useState("");
  const { sendMessage, isPending, error, reset } = useAgentChat();
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [history]);

  async function handleSend(e: FormEvent) {
    e.preventDefault();
    const question = draft.trim();
    if (!question || isPending) return;

    const outgoing = buildOutgoingMessage(history, question);
    addTurn({ role: "user", content: question });
    setDraft("");

    try {
      const response = await sendMessage(outgoing);
      addTurn({ role: "agent", content: response.reply });
    } catch {
      // Error surfaced below via `error`; the user's own message stays in the thread
      // so they can see what was asked when it failed.
    }
  }

  function handleNewConversation() {
    clear();
    reset();
  }

  if (!hasHydrated) {
    return null;
  }

  return (
    <div className="mx-auto flex h-[calc(100vh-10rem)] max-w-3xl flex-col gap-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Agente</h1>
          <p className="text-sm text-ink-muted">
            Pregúntale en lenguaje natural por tus portafolios. No recuerda nada fuera de
            esta conversación, y los valores reflejan el último precio guardado, no uno en
            vivo.
          </p>
        </div>
        <Button variant="ghost" onClick={handleNewConversation} disabled={history.length === 0}>
          Nueva conversación
        </Button>
      </div>

      <section className="panel flex min-h-0 flex-1 flex-col overflow-hidden">
        <div className="flex-1 overflow-y-auto px-4 py-4">
          {history.length === 0 ? (
            <p className="py-8 text-center text-sm text-ink-muted">
              Todavía no hay mensajes. Escribe tu primera pregunta abajo.
            </p>
          ) : (
            <ul className="flex flex-col gap-3">
              {history.map((turn, index) => (
                <li
                  key={index}
                  className={"flex gap-2 " + (turn.role === "user" ? "justify-end" : "justify-start")}
                >
                  {turn.role === "agent" && (
                    <Bot size={18} strokeWidth={1.75} className="mt-1.5 shrink-0 text-ink-muted" aria-hidden />
                  )}
                  <div
                    className={
                      "max-w-[75%] rounded-lg px-3.5 py-2 text-sm whitespace-pre-wrap " +
                      (turn.role === "user"
                        ? "bg-brand text-white"
                        : "bg-paper-raised text-ink")
                    }
                  >
                    {turn.content}
                  </div>
                  {turn.role === "user" && (
                    <User size={18} strokeWidth={1.75} className="mt-1.5 shrink-0 text-ink-muted" aria-hidden />
                  )}
                </li>
              ))}
              {isPending && (
                <li className="flex justify-start gap-2">
                  <Bot size={18} strokeWidth={1.75} className="mt-1.5 shrink-0 text-ink-muted" aria-hidden />
                  <div className="max-w-[75%] animate-pulse rounded-lg bg-paper-raised px-3.5 py-2 text-sm text-ink-muted">
                    Pensando… puede tardar, el modelo corre en local.
                  </div>
                </li>
              )}
              <div ref={scrollRef} />
            </ul>
          )}
        </div>

        {error && (
          <div className="border-t border-rule px-4 py-2">
            <ErrorNotice>
              {getApiErrorMessage(error) ?? "No se pudo contactar al agente. Intenta de nuevo."}
            </ErrorNotice>
          </div>
        )}

        <form onSubmit={handleSend} className="flex items-end gap-2 border-t border-rule px-4 py-3">
          <textarea
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter" && !e.shiftKey) {
                e.preventDefault();
                handleSend(e);
              }
            }}
            placeholder="Pregúntale algo sobre tus portafolios…"
            rows={2}
            maxLength={2000}
            className="flex-1 resize-none rounded-lg border border-rule bg-paper px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus-visible:outline-2 focus-visible:outline-brand"
          />
          <Button type="submit" disabled={isPending || !draft.trim()}>
            <Send size={14} strokeWidth={1.75} aria-hidden />
          </Button>
        </form>
      </section>
    </div>
  );
}
