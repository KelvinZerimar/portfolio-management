---
type: verification
title: Verification — dashboard-notes-privacy-and-palette — 2026-11-20
description: Evidence-backed verdict for the FTD dashboard-notes-privacy-and-palette — frontend gate and FTD checklist.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-20T15:48:30Z
topic: sdd
slug: dashboard-notes-privacy-and-palette
source_state: bba3f0a (dirty — cambios sin commitear de esta FTD; se commitean justo después)
---

# Verification — dashboard-notes-privacy-and-palette — 2026-11-20

Contrato: la ficha FTD [dashboard-notes-privacy-and-palette](../../ftd/dashboard-notes-privacy-and-palette.md)
(no hay spec SDD: es un cambio del carril FTD).

## Stack gate (frontend, una sola ejecución tras la última edición)
- `npm run lint` (eslint) → PASS, exit 0
- `npx tsc --noEmit` → PASS, exit 0
- `npm run build` (next 16.3.6) → PASS, exit 0, 13/13 páginas generadas, `/dashboard` incluida

## FTD checklist
- [x] Widget de notas sin contenido — Playwright sobre `/dashboard` con el usuario de prueba: 10 notas, 0 bloques
      `.rich-text` dentro de `section[aria-label="Notas recientes"]`.
- [x] Paleta sin rosa/rojo — captura: ETH en fucsia (`#d946ef`) en donut, histórico y leyenda; el único rojo es el
      delta negativo.
- [x] Números `es-ES` consistentes — captura: `2.628,70 €`, `1.957,54 €`, `1.573,07 €`, `74,47 %`.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests unitarios del frontend: `frontend/package.json` no tiene script `test` ni
  runner. `formatPercentage`/`formatCurrency` no tienen test; la prueba es la captura, que cubre solo los valores
  de este usuario.
- **NO-APLICA** — backend (.NET): no se tocó ningún `.cs` de esta FTD. El `MafAgentReadTools.cs` modificado en el
  árbol es ajeno y queda fuera del commit.

## Hallazgos descartados
- "Cambiar la paleta reasigna colores a otros activos" — descartado: `colorForKey` hace `hash % palette.length` y la
  longitud sigue en 6; solo cambia el valor del índice 3 (`frontend/lib/chartColors.ts`).
- "`formatPercentage` recibe valores 0–1" — descartado: sus dos llamadas (`AllocationChart.tsx`,
  `AssetSummaryCards.tsx`) pasan escala 0–100; la UI mostraba `74.47` antes del cambio.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada. Limitación: sin tests unitarios del frontend.
