---
type: verification
title: Verification — portfolio-edit-row-validation — 2026-11-24
description: Evidence-backed verdict for the FTD portfolio-edit-row-validation — frontend gate, validation messages, accessible names, focus and keyboard on the entry editor.
tags: [sdd, verification, ftd, frontend, a11y]
timestamp: 2026-11-24T10:00:00Z
topic: sdd
slug: portfolio-edit-row-validation
source_state: a787d2a (dirty — cambios sin commitear de esta FTD)
---

# Verification — portfolio-edit-row-validation — 2026-11-24

Contrato: la ficha FTD [portfolio-edit-row-validation](../../ftd/portfolio-edit-row-validation.md).

## Stack gate (frontend, worktree)
- `npx tsc --noEmit` → PASS, exit 0 (tras `next typegen` en el worktree nuevo)
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/portfolio/79`, usuario de prueba, escrituras bloqueadas)

| Acción | Resultado |
|---|---|
| Abrir edición | foco en SELECT «Activo» |
| Árbol de accesibilidad | Activo, Exchange, Cantidad, Precio, Fecha, Guardar, Cancelar |
| Cantidad vacía + Enter | «Completa todos los campos para guardar la entrada.» |
| Cantidad −1 + Enter | «La cantidad y el precio deben ser mayores que cero.» |
| Fecha de mañana + Guardar | «La fecha no puede ser futura.» (`max` 2026-11-24) |
| Escape | edición cerrada |
| Móvil: precio 0 + Guardar | «La cantidad y el precio deben ser mayores que cero.» |
| Escrituras intentadas | ninguna |

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — API del usuario (parada durante la prueba): copia temporal en el puerto 5099 compilada desde el
  worktree, con las llamadas reenviadas por Playwright.
- **NO-APLICA** — backend (sin cambios).

## Hallazgos descartados
- "El alta vacía no muestra el mensaje propio" — descartado: los campos tienen `required` y el navegador frena el
  envío antes; comportamiento previo, sin cambios.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
