---
author: kramirez
branch: feature/agent-page-maf
status: in-progress
---
# Chat del agente MAF en un panel lateral del dashboard

## Intent

El agente MAF se usa desde el **dashboard**, en un panel lateral superpuesto, y no desde `/agent`. Así el
agente sabe qué portafolio está viendo el usuario y no lo confunde con otro. La página `/agent` vuelve a
usar el agente SK (`POST /api/v1/Agent/chat`), como antes de esta rama.

Antecedente: un primer intento sustituyó SK por MAF dentro de `/agent` (tipos, servicio, tarjetas de
acción, textos en español, E2E con `qwen2.5:7b-instruct` el 2026-11-12). Ese código se **reutiliza** en el
panel; no se tira.

## Scope

**In** (decisiones del usuario, 2026-11-12):
- `/agent` restaurada a SK: `agentService`, `useAgentChat` y su store (`agent-chat-history`) tal como estaban.
- Backend: `SendMafAgentMessageRequest` recibe un `PortfolioId` opcional (portafolio activo).
  - Si no es del usuario o no existe: `404 MafAgent.Portfolio.NotFound`, sin llamar al modelo.
  - El contexto de cada turno dice al modelo cuál es el portafolio activo y que lo use salvo que el
    usuario nombre otro.
- Frontend: botón "Asistente" en el dashboard que abre un panel lateral derecho superpuesto (pantalla
  completa en móvil). Se cierra con Esc, clic en el fondo o la X.
  - Cabecera con el nombre del portafolio activo.
  - Una conversación por portafolio: `conversationId` e historial guardados por `portfolioId`
    (clave `maf-agent-chat`). Cambiar de portafolio cambia de conversación.
  - Tarjetas de acción con Confirmar/Rechazar; al confirmar se refrescan portafolios, entradas y notas.
- Lo ya hecho del primer intento: tipos MAF, `mafAgentService`, `lib/mafAgent.ts` (textos y errores),
  `PendingActionCard`.

**Out:** streaming; varias conversaciones por portafolio; cambios en confirm/reject; quitar SK.

**Anti-goal:** nunca confirmar una acción sin un clic explícito del usuario en su tarjeta.

## Checklist

- [x] `/agent` restaurada a SK — prueba: `git diff development -- frontend/app/(app)/agent` vacío
- [x] Backend `PortfolioId` opcional + validación de dueño + contexto del turno — prueba: tests de handler
      y runner nuevos en verde
- [x] Frontend: tipos, servicio, store por portafolio (`useMafAgentChatStore`), hooks
      (`useMafAgentChat`) separados de SK — prueba: `npm run build`
- [x] Panel lateral en el dashboard (`components/agent/MafAgentPanel.tsx`, `<dialog>` nativo modal) —
      prueba: `npm run lint` y `npm run build`
- [x] `dotnet test` completo en verde — 493/493 correctos, 0 errores (2026-11-12)
- [x] Fix (2026-11-12, rama `fix/agent-portfolio-value`): el valor del portafolio que da el agente coincide
      con el dashboard y va en euros (MAF y SK) — prueba: tests nuevos en rojo antes (880 en vez de 480; 450 en
      vez de 50) y en verde después; `dotnet test` completo 495/495, `dotnet format --verify-no-changes` limpio
  - Causa: `GetPortfolioValue` usaba `Portfolio.GetTotalValue`, que suma TODAS las entradas. Cada entrada
    es una foto de un (activo, exchange), no una compra: solo cuenta la última. El usuario vio 40.754,17
    con un dashboard de ~3.400 €. Ahora usa `GetPortfolioValueByDate(ahora)`, igual que el dashboard, y
    `GetTotalValue` se elimina para que nadie lo vuelva a usar.
  - Moneda: las instrucciones de MAF (regla 7) y SK dicen que todo está en euros (€). SK decía antes
    "never add a currency symbol".
- [ ] Revisión visual en navegador — pendiente del usuario (esta sesión no tiene navegador)

## Evidence

(2026-11-12, panel)
- `git diff development -- 'frontend/app/(app)/agent'`: 0 líneas.
- `dotnet test --filter MafAgent`: 309/309 correctos. Tests nuevos: handler (sin portafolio, propio,
  desconocido → 404 `MafAgent.Portfolio.NotFound`, ajeno → mismo 404, sin llamar al modelo) y runner
  (el contexto nombra el portafolio activo; sin él, no lo nombra).
- `npm run lint`: sin errores. `npm run build`: compila; `/agent` y `/dashboard` estáticas.
- Decisión: `<dialog>` nativo con `showModal()` (Esc, foco atrapado y fondo inerte sin dependencias).
  El panel sigue montado cerrado, así una respuesta tardía no se pierde. La respuesta se guarda en el hilo
  del portafolio al que se envió, aunque el usuario cambie de portafolio mientras tanto.

(Primer intento, 2026-11-12) E2E del agente MAF contra la API real: `CreateEntry` propuesto y confirmado
(`Succeeded`), rechazo 204, confirmar una rechazada → 409 `AlreadyDecided`, conversación desconocida → 404
`Conversation.NotFound`. Cada turno con `qwen2.5:7b-instruct` tardó 4–8 min; el primero llegó al timeout.

## Next

Revisión visual en el navegador (`npm run dev` + login →
dashboard → Asistente): abrir/cerrar (Esc, fondo, X), cambiar de portafolio con el panel, Confirmar y
Rechazar. Luego commit y PR hacia `development`.
