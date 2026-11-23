---
type: verification
title: Verification — dashboard-chart-focus-ring — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-chart-focus-ring — frontend gate, pointer vs keyboard focus on both dashboard charts.
tags: [sdd, verification, ftd, frontend, a11y]
timestamp: 2026-11-23T17:00:00Z
topic: sdd
slug: dashboard-chart-focus-ring
source_state: 26b720a (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-chart-focus-ring — 2026-11-23

Contrato: la ficha FTD [dashboard-chart-focus-ring](../../ftd/dashboard-chart-focus-ring.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, 1440 px, usuario de prueba)

| Acción | Elemento enfocado | outline | :focus-visible |
|---|---|---|---|
| Clic en el histórico (antes) | `svg.recharts-surface` | auto 5px negro | no |
| Clic en el histórico | `svg.recharts-surface` | none | no |
| Clic en el donut | `svg.recharts-surface` | none | no |
| Clic en un sector | `svg.recharts-surface` | none | no |
| Tab hasta el gráfico | `svg.recharts-surface` | solid 2px rgb(79, 70, 229) | sí |

- [x] Flecha derecha con foco por teclado → tooltip `visible`.
- [x] Sin ancestros que recorten (`overflow` visible en toda la cadena) en ambos gráficos.
- [x] 0 errores de página.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — otros navegadores: solo Chromium. `:focus-visible` es estándar en Firefox y Safari; no se
  comprobó allí.
- **NO-APLICA** — backend.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
