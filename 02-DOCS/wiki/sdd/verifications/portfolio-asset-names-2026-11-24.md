---
type: verification
title: Verification — portfolio-asset-names — 2026-11-24
description: Evidence-backed verdict for the FTD portfolio-asset-names — frontend gate and asset picker labels, groups and layout with real data.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-24T12:00:00Z
topic: sdd
slug: portfolio-asset-names
source_state: 7afe295 (dirty — cambios sin commitear de esta FTD)
---

# Verification — portfolio-asset-names — 2026-11-24

Contrato: la ficha FTD [portfolio-asset-names](../../ftd/portfolio-asset-names.md).

## Stack gate (frontend, worktree)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, `/portfolio/79`, usuario de prueba, escrituras bloqueadas)

| Comprobación | Resultado |
|---|---|
| Opciones del alta | 27 + «—»; monedas «SÍMBOLO · Nombre», fondos «Nombre (ISIN)» |
| Grupos | Criptomonedas 17, Fondos 10 |
| Filtro vs alta | mismo orden |
| Anchos | filtro 224 px; alta 160 px; formulario en una fila a 1440 px (captura) |
| Edición | «BNB · Binance Coin» |
| 390 / 768 px | 390/390, 768/768 |
| Escrituras / errores | ninguna / 0 |

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — API del usuario (parada): copia temporal en el puerto 5099, llamadas reenviadas por Playwright.
- **NO-APLICA** — backend.

## Hallazgos descartados
- "El formulario de alta ocupa 3 filas" (métrica por `top` de cada hijo) — descartado: el botón está alineado abajo
  (`items-end`) y cambia el `top`; la captura muestra una sola fila.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
