---
type: verification
title: Verification — dashboard-holdings-header — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-holdings-header — frontend gate and checklist at five widths.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-23T13:00:00Z
topic: sdd
slug: dashboard-holdings-header
source_state: 6d818c6 (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-holdings-header — 2026-11-23

Contrato: la ficha FTD [dashboard-holdings-header](../../ftd/dashboard-holdings-header.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, usuario de prueba)

| Ancho | Scroll página | Lado a lado | Participación | Cabecera Holdings |
|---|---|---|---|---|
| 390 | 390 | no | 358 px | 145 px |
| 768 | 768 (antes 796) | no | 480 px | 103 px |
| 1024 | 1024 | no | 736 px | 71 px |
| 1280 | 1280 | sí | 290 px | 71 px |
| 1440 | 1440 | sí | 338 px | 71 px (antes ~112) |

- [x] Filtro «eth» → 1 fila. 0 errores de página.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "La tabla también desborda a 768" — descartado: `table` aparece fuera del viewport en el barrido, pero está dentro
  de `overflow-x-auto` (scroll propio); el `scrollWidth` de la página es 768.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
