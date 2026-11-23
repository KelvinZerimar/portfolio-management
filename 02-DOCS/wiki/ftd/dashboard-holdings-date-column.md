---
author: kramirez
branch: fix/dashboard-holdings-date-column
status: done
---
# Dashboard: filas de Holdings en una línea (fecha y nombre)

## Intent

Pendiente de [dashboard-holdings-header](dashboard-holdings-header.md) (PR #43): a 1024 px la fecha de la columna
«Registrado» («20 nov 2026») se partía en dos líneas. Al medirlo, el nombre del activo («Binance Coin») también se
partía. Cada fila pasaba de 41 a 61 px de alto.

## Scope

**In:**
- `whitespace-nowrap` en la celda de fecha y en el bloque avatar + símbolo + nombre de la tabla.
- Margen lateral de las celdas de la tabla de `px-4` a `px-3` (cabecera y cuerpo). Sin ese cambio, la fila sin
  cortes medía 742 px y el panel tiene 736 a 1024 px: 6 px de scroll interno. Con `px-3` mide 736.

**Out:** cabeceras de columna con mayúsculas mezcladas («Activo», «Exchange», «Valor» frente a «CANTIDAD», «PRECIO»,
«REGISTRADO»: los botones de orden no heredan `uppercase`); lista móvil (no cambia).

## Checklist

- [x] Fecha en una línea — prueba (DOM, 2026-11-23): «Registrado» ocupa 1 línea a 768, 1024, 1280 y 1440 px (antes 2
      a 768 y 1024).
- [x] Filas a 41 px en todos los anchos — prueba: alto de fila 41/41/41 a los cuatro anchos (antes 61 a 768 y 1024).
- [x] Sin scroll interno a 1024 px — prueba: tabla 736 px = contenedor 736 px. A 768 px la tabla (695 px) usa su
      scroll propio dentro de un panel de 480 px, como antes; la página no desborda (`scrollWidth` 768).
- [x] Revisión visual — captura de la sección Holdings a 1024 px: «20 nov 2026» y «Binance Coin» en una línea.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página.

Captura revisada y borrada; solo incluía la sección Holdings.

## Next step

PR hacia `development`.
