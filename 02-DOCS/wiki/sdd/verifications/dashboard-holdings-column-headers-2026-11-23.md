---
type: verification
title: Verification — dashboard-holdings-column-headers — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-holdings-column-headers — frontend gate, computed header styles and sorting at 1024 px.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-23T15:00:00Z
topic: sdd
slug: dashboard-holdings-column-headers
source_state: 4cb037a (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-holdings-column-headers — 2026-11-23

Contrato: la ficha FTD [dashboard-holdings-column-headers](../../ftd/dashboard-holdings-column-headers.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, 1024 px, usuario de prueba)

| Cabecera | text-transform | letter-spacing | Tamaño | Nombre accesible |
|---|---|---|---|---|
| ACTIVO | uppercase | 0,3 px | 12 px | Ordenar por Activo |
| EXCHANGE | uppercase | 0,3 px | 12 px | Ordenar por Exchange |
| VALOR | uppercase | 0,3 px | 12 px | Ordenar por Valor |
| CANTIDAD | uppercase | 0,3 px | 12 px | — |
| PRECIO | uppercase | 0,3 px | 12 px | — |
| REGISTRADO | uppercase | 0,3 px | 12 px | — |

- [x] Orden: BTC, ETH, BNB → (Activo) ETH, BTC, BNB → (Activo) BNB, BTC, ETH.
- [x] Tabla 736 px = contenedor 736 px; página 1024. 0 errores de página.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — otros navegadores: solo se midió en Chromium. El `uppercase` va puesto en el propio botón, así
  que no depende de lo que cada navegador aplique por defecto a `<button>`; no se comprobó en Firefox ni Safari.
- **NO-APLICA** — backend.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
