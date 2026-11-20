---
author: kramirez
branch: feat/dashboard-remove-duplicate-asset-cards
status: done
---
# Dashboard: quitar las tarjetas por activo duplicadas

## Intent

Punto 4 de la crítica de diseño del dashboard (2026-11-20). Antecedentes:
[dashboard-notes-privacy-and-palette](dashboard-notes-privacy-and-palette.md) (PR #37) y
[dashboard-hero-context-and-history-axis](dashboard-hero-context-and-history-axis.md) (PR #38).

Las tarjetas por activo (`AssetSummaryCards`) repetían por tercera vez los mismos N primeros activos que ya
mostraban la leyenda del donut y la tabla de Holdings. Con 3 activos ocupaban media fila de una rejilla de 6 y
dejaban la otra media vacía. El panel de Participación, a su vez, dejaba vacía su mitad inferior.

## Scope

**In:**
- Quitar `AssetSummaryCards` (componente y uso). Su único dato propio, el valor en € por activo, pasa a la leyenda.
- Participación: donut arriba y leyenda debajo como tabla (grupo · valor · %), que rellena el panel.
- Donut con un solo segmento: sin hueco ni trazo blanco (cortaban una muesca en el anillo).

**Out:** que el donut represente el total (segmento «Otros / Efectivo» cuando se filtra el Euro o se recorta a N);
móvil.

## Checklist

- [x] Tarjetas fuera — prueba (2026-11-20): `AssetSummaryCards.tsx` borrado; `grep` sin referencias; captura sin la
      fila de tarjetas.
- [x] Leyenda con valor y % — prueba (DOM con Playwright): Activo «BTC 1.957,54 € 74,47 % | ETH 369,83 € 14,07 % |
      BNB 301,33 € 11,46 %»; Exchange «Bitvavo 2.628,70 € 100,00 %».
- [x] Panel sin hueco — prueba: Participación e Histórico miden ambos 366 px de alto (1440 px de ancho).
- [x] Donut de un segmento sin muesca — prueba: captura con Exchange antes (muesca) y después (anillo continuo).
- [x] Móvil — captura a 390 px: panel apilado, leyenda legible.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-20, exit 0);
      0 errores de consola.

Capturas borradas tras revisarlas; ninguna incluía el widget de notas.

## Next step

PR hacia `development`. Pendiente de la crítica: donut que sume el total real («Otros / Efectivo») y móvil
(navegación cortada, tabla con scroll sin pista).
