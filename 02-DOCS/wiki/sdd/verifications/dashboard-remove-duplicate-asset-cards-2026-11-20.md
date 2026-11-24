---
type: verification
title: Verification — dashboard-remove-duplicate-asset-cards — 2026-11-20
description: Evidence-backed verdict for the FTD dashboard-remove-duplicate-asset-cards — frontend gate and FTD checklist.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-20T18:30:00Z
topic: sdd
slug: dashboard-remove-duplicate-asset-cards
source_state: f2c9057 (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-remove-duplicate-asset-cards — 2026-11-20

Contrato: la ficha FTD [dashboard-remove-duplicate-asset-cards](../../ftd/dashboard-remove-duplicate-asset-cards.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` (eslint) → PASS, exit 0
- `npm run build` (next 16.3.6) → PASS, exit 0

## FTD checklist
- [x] Tarjetas fuera — componente borrado, `grep` sin referencias, captura.
- [x] Leyenda con valor y % — Playwright, agrupación Activo y Exchange.
- [x] Panel sin hueco — Participación 366 px = Histórico 366 px.
- [x] Donut de un segmento sin muesca — captura antes/después.
- [x] Móvil — captura a 390 px. 0 errores de consola.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests unitarios del frontend: no hay runner. Cambio de maquetación, cubierto por captura.
- **NO-APLICA** — backend: ningún `.cs` tocado.

## Hallazgos descartados
- "Quitar las tarjetas pierde los logos de los activos" — descartado: la tabla de Holdings sigue mostrando el logo de
  cada activo (`HoldingsTable.tsx`).

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
