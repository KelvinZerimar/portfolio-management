---
author: kramirez
branch: fix/dashboard-holdings-column-headers
status: done
---
# Dashboard: cabeceras de Holdings en mayúsculas

## Intent

Pendiente de [dashboard-holdings-date-column](dashboard-holdings-date-column.md) (PR #44): en la tabla de Holdings,
«Activo», «Exchange» y «Valor» salían en minúsculas junto a «CANTIDAD», «PRECIO» y «REGISTRADO».

Causa: la fila de cabecera lleva `uppercase`, pero las tres primeras columnas son botones de orden, y la hoja de estilos
por defecto de Chrome pone `text-transform: none` en `<button>`. Tailwind v4 no reinicia esa propiedad (su preflight
sí hereda `letter-spacing`, por eso el espaciado ya coincidía).

Dirección: mayúsculas. Las seis tablas de la app (`portfolio`, `portfolio/[id]`, `cryptocurrencies`, `exchanges`,
`notes` y Holdings) usan la misma fila `text-xs tracking-wide uppercase`; Holdings es la única con botones en la
cabecera.

## Scope

**In:** `uppercase` en los botones de orden de `HoldingsTable`.

**Out:** el estado de orden no se anuncia a lectores de pantalla (no hay `aria-sort`) y la flecha no indica la
dirección (solo cambia de opacidad).

## Checklist

- [x] Seis cabeceras en mayúsculas — prueba (estilos calculados, 2026-11-23): `text-transform: uppercase`,
      `letter-spacing` 0,3 px y 12 px en las seis; texto pintado ACTIVO, EXCHANGE, VALOR, CANTIDAD, PRECIO,
      REGISTRADO.
- [x] Nombre accesible sin cambios — prueba: «Ordenar por Activo/Exchange/Valor».
- [x] El orden sigue funcionando — prueba: por defecto BTC, ETH, BNB; un clic en Activo → ETH, BTC, BNB; otro →
      BNB, BTC, ETH.
- [x] La tabla sigue cabiendo a 1024 px — prueba: tabla 736 px = contenedor 736; página 1024.
- [x] Revisión visual — captura solo de la cabecera de la tabla a 1024 px.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página.

Capturas revisadas y borradas. Un primer recorte salió desplazado (mostraba parte del histórico, sin cifras); se
borró y se repitió sobre `thead`.

## Next step

PR hacia `development`.
