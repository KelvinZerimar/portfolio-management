---
type: verification
title: Verification — dashboard-mobile — 2026-11-23
description: Evidence-backed verdict for the FTD dashboard-mobile — frontend gate and FTD checklist at 390 and 1440 px.
tags: [sdd, verification, ftd, frontend, mobile]
timestamp: 2026-11-23T12:00:00Z
topic: sdd
slug: dashboard-mobile
source_state: 30eb08c (dirty — cambios sin commitear de esta FTD)
---

# Verification — dashboard-mobile — 2026-11-23

Contrato: la ficha FTD [dashboard-mobile](../../ftd/dashboard-mobile.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, 390×844 y 1440×900, usuario de prueba)
- [x] Navegación móvil: 6/6 enlaces, sin scroll (390 = 390), alto mínimo 49 px.
- [x] Holdings móvil: lista con valor, exchange, cantidad × precio y fecha; tabla oculta.
- [x] Notas: 3/10 en móvil, 10/10 en escritorio.
- [x] `h1` en una línea en ambos tamaños.
- [x] Página sin scroll horizontal a 390 px. Escritorio sin cambios (captura). 0 errores de consola.

## Capas no ejecutadas
- **SUSTITUIDA** — revisión de otras páginas en móvil: `MobileNav` es del layout y cambia en todas; solo se midió en
  `/dashboard`. Puede ocultar un problema de navegación en otra página (no hay motivo para esperarlo: el componente
  no depende de la página).
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "El botón Asistente pierde su nombre accesible en móvil" — descartado: lleva `aria-label="Asistente"`
  (`app/(app)/dashboard/page.tsx`).

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
