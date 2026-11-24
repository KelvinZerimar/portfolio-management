---
type: verification
title: Verification — dashboard-distinct-asset-colors — 2026-11-20
description: Evidence-backed verdict for the FTD dashboard-distinct-asset-colors — palette validator, frontend gate and FTD checklist.
tags: [sdd, verification, ftd, frontend, dataviz]
timestamp: 2026-11-20T20:30:00Z
topic: sdd
slug: dashboard-distinct-asset-colors
source_state: bb91484 (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-distinct-asset-colors — 2026-11-20

Contrato: la ficha FTD [dashboard-distinct-asset-colors](../../ftd/dashboard-distinct-asset-colors.md).

## Palette gate (`dataviz` `validate_palette.js`, surface `#ffffff`, light)
- Adyacentes → ALL CHECKS PASS (CVD 16,2; normal 26,4)
- `--pairs all` → ALL CHECKS PASS (CVD 16,2; normal 17,5)
- Contraste → WARN (amarillo 2,17; cian 2,43): exige etiquetas visibles; las hay (leyenda-tabla del donut, leyenda y
  tooltip del histórico).
- Paleta anterior, `--pairs all` → FAILED (violeta↔fucsia CVD 1,3; violeta↔índigo normal 11,4). Prueba de que el
  validador falla con una entrada mala, no solo pasa.

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist
- [x] Paleta validada.
- [x] Datos reales: mismo color por símbolo en donut e histórico (color calculado en el DOM).
- [x] Datos ficticios: 4 colores distintos + «Otros (8)» + «Efectivo», suma 10.000 € / 100 %.

## Capas no ejecutadas
- **SUSTITUIDA** — tests unitarios de `assignSeriesColors`: no hay runner; en su lugar, DOM con datos reales y con
  una respuesta interceptada. No cubre colisiones de hash concretas ni más entidades que colores.
- **NO-APLICA** — modo oscuro: el proyecto no lo tiene. Backend: solo se leyó
  `GetPortfolioAllocationQueryHandler.cs` para confirmar que `GroupId` = `CryptoCurrencyId` agrupando por activo.

## Hallazgos descartados
- "Donut e histórico pueden pintar distinto el mismo activo" — descartado mientras reciban el mismo conjunto: ambos
  usan los N primeros por valor sin Euro, y `assignSeriesColors` ordena por id antes de asignar (no depende del
  orden de entrada). Si los dos rankings divergieran (fechas distintas), podría pasar; no ocurre con la fecha actual.

## Verdict: PASS — paleta validada, puerta del frontend en verde y checklist FTD con prueba observada.
