---
type: verification
title: Verification — portfolio-mobile — 2026-11-23
description: Evidence-backed verdict for the FTD portfolio-mobile — frontend gate, list/table switch and action reachability at six widths, labelled mobile edit card.
tags: [sdd, verification, ftd, frontend, mobile]
timestamp: 2026-11-23T19:00:00Z
topic: sdd
slug: portfolio-mobile
source_state: c1cbeab (dirty — cambios sin commitear de esta FTD)
---

# Verification — portfolio-mobile — 2026-11-23

Contrato: la ficha FTD [portfolio-mobile](../../ftd/portfolio-mobile.md).

## Stack gate (frontend, tras la última edición)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, usuario de prueba, sin acciones destructivas)

| Página | Ancho | Vista | Página (scroll/ancho) | Acciones a la vista |
|---|---|---|---|---|
| Lista | 390 | lista | 390/390 | sí (antes: recortadas) |
| Lista | 640 | tabla | 640/640 | sí |
| Lista | 1440 | tabla | 1440/1440 | sí |
| Detalle | 390 | lista | 390/390 | sí (antes: fuera, tras scroll) |
| Detalle | 640 | lista | 640/640 | sí (con tabla: no) |
| Detalle | 768 | lista | 768/768 | sí |
| Detalle | 1023 | lista | 1023/1023 | sí |
| Detalle | 1024 | tabla | 1024/1024 | sí |
| Detalle | 1440 | tabla | 1440/1440 | sí |

- [x] Borrar en móvil: «¿Eliminar?» aparece; se respondió «No».
- [x] Tarjeta de edición: 5 controles con nombre (Activo, Exchange, Cantidad, Precio, Fecha); Cancelar sin cambios.
- [x] Fila de edición de escritorio: 5 controles; Cancelar la cierra.
- [x] 0 errores de página ni de consola.

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — guardar desde la tarjeta móvil: no se ejecutó para no modificar datos; usa el mismo hook
  (`useEntryEditor`) y la misma mutación que la fila de escritorio.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "A 640 px el detalle oculta las acciones" — era real con el corte en `sm`; corregido moviendo el corte a `lg`
  (medido: la tabla de 720 px no cabe hasta 1024 px).

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
