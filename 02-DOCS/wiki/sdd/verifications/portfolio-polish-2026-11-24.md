---
type: verification
title: Verification — portfolio-polish — 2026-11-24
description: Evidence-backed verdict for the FTD portfolio-polish — frontend gate, delete weight, add panel, value, currency, local today and success auto-hide with real data.
tags: [sdd, verification, ftd, frontend]
timestamp: 2026-11-24T18:00:00Z
topic: sdd
slug: portfolio-polish
source_state: 0167b18 (dirty — cambios sin commitear de esta FTD)
---

# Verification — portfolio-polish — 2026-11-24

Contrato: la ficha FTD [portfolio-polish](../../ftd/portfolio-polish.md).

## Stack gate (frontend, worktree)
- `npx tsc --noEmit` → PASS, exit 0
- `npm run lint` → PASS, exit 0
- `npm run build` → PASS, exit 0

## FTD checklist (Playwright, usuario de prueba, zona Europe/Madrid, escrituras bloqueadas)

| Comprobación | Resultado |
|---|---|
| Lista: columna Valor | 2.567,21 € (tabla y móvil) |
| Confirmación de borrado | «¿Eliminar «…»? Se borrarán sus 6 entradas y no se puede deshacer.» → «No» |
| Lista 390 / 640 / 768 px | sin scroll horizontal; confirmación en móvil 390/390 |
| Cabecera del detalle | «Valor actual 2.567,21 € · según la última entrada de cada activo» |
| Alta encima del registro | top 220 < 354 (captura) |
| Moneda | «Precio (€)» en el alta y en la tabla |
| «Hoy» local a las 00:30 de Madrid | valor y máximo 2026-11-25 |
| Aviso de éxito | visible tras el alta; oculto a los 5,6 s |
| Detalle 390 / 768 px | sin scroll horizontal |
| Escrituras / errores | 1 alta respondida con un 201 falso, ninguna llega a la API / 0 |

## Capas no ejecutadas
- **HERRAMIENTA-AUSENTE** — tests del frontend: no hay runner.
- **SUSTITUIDA** — API del usuario (parada): copia temporal en el puerto 5099, llamadas reenviadas por Playwright.
- **NO-APLICA** — backend (sin cambios).

## Hallazgos descartados
- Primer intento: el alta no mostró el aviso — el script falseaba `/PortfolioEntries` y la ruta real es
  `/PortfolioEntry`; la petición se bloqueó (no se escribió nada). Corregido el script, repetido.

## Verdict: PASS — puerta del frontend en verde y checklist FTD con prueba observada.
