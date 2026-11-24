---
type: verification
title: Verification — dashboard-history-total-in-tooltip — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-history-total-in-tooltip — frontend gate, drawn lines, legend and tooltip rows with several assets and with one.
tags: [sdd, verification, ftd, frontend, dataviz]
timestamp: 2026-11-23T18:00:00Z
topic: sdd
slug: dashboard-history-total-in-tooltip
source_state: 26b720a (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-history-total-in-tooltip — 2026-11-23

Contrato: la ficha FTD [dashboard-history-total-in-tooltip](../../ftd/dashboard-history-total-in-tooltip.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, 1440 px, usuario de prueba)

| Caso | Líneas | Leyenda | Tooltip |
|---|---|---|---|
| Varios activos (datos reales) | 3: #06b6d4, #eda100, #008300 | BNB, BTC, ETH | Total (negrita, sin muestra) + un activo por fila |
| Un activo (respuesta recortada) | 1: `var(--ink)` | ninguna | Total (negrita, muestra negra) |

- [x] Eje Y con varios activos: 0 – 2,6 mil € (antes hasta 4,5 mil €).
- [x] 0 errores de página ni de consola.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — portfolio real de un solo activo: se simuló recortando `byAsset` de `/api/v1/Portfolio/{id}/history`
  a una serie con `page.route`.
- **NO-APLICA** — backend.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
