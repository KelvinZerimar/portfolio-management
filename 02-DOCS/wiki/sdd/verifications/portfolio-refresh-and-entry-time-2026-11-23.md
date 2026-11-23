---
type: verification
title: Verification — portfolio-refresh-and-entry-time — 2026-11-23
description: Evidence-backed verdict for the FTD portfolio-refresh-and-entry-time — backend tests, real-API 409 checks, intercepted UI flow and frontend gate.
tags: [sdd, verification, ftd, backend, frontend]
timestamp: 2026-11-23T21:00:00Z
topic: sdd
slug: portfolio-refresh-and-entry-time
source_state: 0417fcc (dirty — cambios sin commitear de esta FTD)
---

# Verification — portfolio-refresh-and-entry-time — 2026-11-23

Contrato: la ficha FTD [portfolio-refresh-and-entry-time](../../ftd/portfolio-refresh-and-entry-time.md).

## Stack gate
- `dotnet test tests/Application.Tests` → PASS, 264/264
- `dotnet test tests/Domain.Tests` → PASS, 46/46
- Tests nuevos (`--filter-method`) → 3/3
- `dotnet build src/Infrastructure` → PASS, 0 avisos, 0 errores
- `dotnet format src/Application --verify-no-changes` → PASS
- `npx tsc --noEmit`, `npm run lint`, `npm run build` → PASS, exit 0

## Checklist

| Prueba | Resultado |
|---|---|
| POST duplicado de la 1305 (API real, puerto 5099) | 409 `PortfolioEntry.Duplicate`, `existingEntryId: 1305`; 6 entradas antes y después |
| PUT de la 1302 sin cambios (empata con la 1305) | 409, `existingEntryId: 1305`; la 1302 intacta |
| Alta duplicada en la UI (409 simulado, PascalCase) | «Ya hay una entrada de BNB en Bitvavo el 20 nov 2026…» + «Editar la existente» |
| «Editar la existente» | filtros BNB / 2026-11-20 / 2026-11-20; fila 1305 en edición |
| Guardar con 409 (simulado, camelCase) | error visible bajo la fila |
| Hora | 14:32Z → «20 nov 2026, 15:32»; 00:00Z → «20 nov 2026» |
| Registrar precios de hoy (simulado) | «Añadidas 3 entrada(s) con el precio de 23 nov 2026, 14:05. Sin precio disponible: CAKE.» |
| 390 px | 390/390, sin scroll horizontal |

Escrituras de la prueba de UI: POST entrada, PUT 1305, POST refresh — las tres simuladas con `page.route`.

## Capas no ejecutadas
- **SUSTITUIDA** — prueba de extremo a extremo con la API del usuario: corre `dotnet run` con el código anterior y
  no se reinició. Sustituida por una copia temporal (puerto 5099) para el backend y respuestas simuladas para la UI.
- **NO-APLICA** — migración de base de datos (no hay cambio de esquema).

## Hallazgos descartados
- "La 1302 cambió de fecha" — real, causado por el script de prueba (no por el código); restaurada y comprobada.
  Ver Notas de la ficha.

## Verdict: PASS — tests, puertas en verde y comportamiento observado en API y UI.
