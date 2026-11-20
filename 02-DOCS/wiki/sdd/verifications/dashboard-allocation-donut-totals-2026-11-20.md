---
type: verification
title: Verification — dashboard-allocation-donut-totals — 2026-11-20
description: Evidence-backed verdict for the FTD dashboard-allocation-donut-totals — frontend gate and FTD checklist.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-20T19:30:00Z
topic: sdd
slug: dashboard-allocation-donut-totals
source_state: ad33f0a (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-allocation-donut-totals — 2026-11-20

Contrato: la ficha FTD [dashboard-allocation-donut-totals](../../ftd/dashboard-allocation-donut-totals.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` (eslint) → PASS, exit 0
- `npm run build` (next 16.3.6) → PASS, exit 0

## FTD checklist
- [x] Datos reales sin cambios visibles — DOM en agrupación Activo y Exchange.
- [x] Anillo = total con Euro y más de N activos — respuesta interceptada (datos ficticios T01–T12 + EUR):
      12 sectores; leyenda suma 10.000 € y 100 %; «Otros (2)» y «Efectivo» correctos.

## Capas no ejecutadas
- **SUSTITUIDA** — tests unitarios de `buildAllocationSlices`: no hay runner en el frontend; en su lugar, una
  respuesta interceptada en el navegador. Cubre un caso (empates incluidos: T09 y T11 a 200 €), no los bordes
  (lista vacía, solo efectivo, valores 0 o negativos).
- **NO-APLICA** — backend: ningún `.cs` tocado.

## Hallazgos descartados
- "Los % de los agregados pueden no sumar 100 por redondeo" — descartado para este cambio: se suman los `percentage`
  que devuelve la API, sin redondear en cliente; el formateo a 2 decimales es solo de presentación.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada. Limitación: sin tests unitarios de los bordes.
