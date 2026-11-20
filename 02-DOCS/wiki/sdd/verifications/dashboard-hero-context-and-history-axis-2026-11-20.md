---
type: verification
title: Verification — dashboard-hero-context-and-history-axis — 2026-11-20
description: Evidence-backed verdict for the FTD dashboard-hero-context-and-history-axis — frontend gate and FTD checklist.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-20T17:30:00Z
topic: sdd
slug: dashboard-hero-context-and-history-axis
source_state: 8e274d3 (dirty — cambios sin commitear de esta FTD; se commitean justo después)
---

# Verification — dashboard-hero-context-and-history-axis — 2026-11-20

Contrato: la ficha FTD [dashboard-hero-context-and-history-axis](../../ftd/dashboard-hero-context-and-history-axis.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` (eslint) → PASS, exit 0
- `npm run build` (next 16.3.6) → PASS, exit 0, `/dashboard` generada

## FTD checklist
- [x] Cifra principal con fecha, variación y periodo — captura (escritorio y móvil).
- [x] El rango actualiza la cifra principal — Playwright: 30d «en 30 días», 90d «en 90 días», Todo «desde el
      primer registro».
- [x] Histórico sin número duplicado, con eje Y y sin área — captura: eje 0 € … 4,5 mil €, fechas inicial y final.
- [x] Base del rango con el último valor conocido — Playwright: 30d cabecera «21 oct 2026 – 20 nov 2026»,
      variación «-418,98 € (-13,75 %)», 4 líneas; 0 errores de consola en los tres rangos.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests unitarios del frontend: no hay runner (`package.json` sin script `test`).
  `filterHistoryByRange`, `valueAsOf` e `historyChange` no tienen test. En particular el caso «sin registros dentro
  del rango» (cierre plano en `now`) no se observó: los datos del usuario de prueba no lo producen.
- **NO-APLICA** — backend: ningún `.cs` tocado.

## Hallazgos descartados
- "Las líneas por activo cambian también en 90d/Todo al pasar de búsqueda exacta a `valueAsOf`" — revisado: con
  búsqueda exacta un activo sin punto en esa fecha valía 0; ahora vale su último valor conocido, que es la regla del
  ledger. Con los datos de prueba 90d y Todo se ven igual que antes (captura previa y posterior).

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada. Limitación: sin tests unitarios; caso de rango sin registros no observado.
