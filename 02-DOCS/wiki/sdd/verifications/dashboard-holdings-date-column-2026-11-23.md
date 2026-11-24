---
type: verification
title: Verification — dashboard-holdings-date-column — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-holdings-date-column — frontend gate and line counts per column at four widths.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-23T14:00:00Z
topic: sdd
slug: dashboard-holdings-date-column
source_state: 596bedc (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-holdings-date-column — 2026-11-23

Contrato: la ficha FTD [dashboard-holdings-date-column](../../ftd/dashboard-holdings-date-column.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, usuario de prueba)

| Ancho | Página | Tabla / contenedor | Alto de fila | Registrado |
|---|---|---|---|---|
| 768 | 768 | 695 / 480 (scroll propio) | 41 (antes 61) | 1 línea (antes 2) |
| 1024 | 1024 | 736 / 736 | 41 (antes 61) | 1 línea (antes 2) |
| 1280 | 1280 | 992 / 992 | 41 | 1 línea |
| 1440 | 1440 | 1152 / 1152 | 41 | 1 línea |

- [x] Captura a 1024 px (solo la sección Holdings): fecha y «Binance Coin» en una línea. 0 errores de página.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "La columna Activo ocupa 2 líneas" — descartado: el script cuenta alturas distintas de avatar, símbolo y nombre
  como líneas. La fila mide 41 px (una línea) y la captura lo confirma.
- Primer intento (solo `whitespace-nowrap`): tabla 742 px en un contenedor de 736 a 1024 px → 6 px de scroll
  interno. Corregido con `px-3` en las celdas.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
