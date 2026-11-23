---
type: verification
title: Verification — dashboard-holdings-sort-state — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-holdings-sort-state — frontend gate, aria-sort/icon/row order over six sort steps and accessible names.
tags: [sdd, verification, ftd, frontend, a11y]
timestamp: 2026-11-23T16:00:00Z
topic: sdd
slug: dashboard-holdings-sort-state
source_state: e8d7d39 (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-holdings-sort-state — 2026-11-23

Contrato: la ficha FTD [dashboard-holdings-sort-state](../../ftd/dashboard-holdings-sort-state.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/dashboard`, 1280 px, usuario de prueba)

| Paso | Activo | Exchange | Valor | Primera fila → última |
|---|---|---|---|---|
| por defecto | — ↕ | — ↕ | descending ↓ | BTC → BNB |
| clic Activo | ascending ↑ | — ↕ | — ↕ | BNB → ETH |
| clic Activo | descending ↓ | — ↕ | — ↕ | ETH → BNB |
| clic Exchange | — ↕ | ascending ↑ | — ↕ | (un solo exchange: orden estable) |
| clic Valor | — ↕ | — ↕ | descending ↓ | 1.957,54 € → 301,33 € |
| clic Valor | — ↕ | — ↕ | ascending ↑ | 301,33 € → 1.957,54 € |

- [x] Árbol de accesibilidad: `columnheader "Activo" > button "Activo"` (y Exchange, Valor).
- [x] 0 errores de página ni de consola.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — lector de pantalla real: se comprobó el árbol de accesibilidad de Chromium y `aria-sort`, no
  NVDA ni VoiceOver.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "Dos botones llamados «Activo» en la página" — descartado como defecto: están en regiones distintas con nombre
  (Participación y Holdings actuales).

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
