---
author: kramirez
branch: fix/dashboard-holdings-sort-state
status: done
---
# Dashboard: estado de orden en Holdings (aria-sort y flecha)

## Intent

Pendiente de [dashboard-holdings-column-headers](dashboard-holdings-column-headers.md) (PR #45). En la tabla de
Holdings:

- El orden activo no se anunciaba a lectores de pantalla (sin `aria-sort`).
- La flecha era la misma ↕ en todas las columnas; solo cambiaba de opacidad, sin indicar la dirección.
- Al cambiar de columna el orden empezaba siempre en descendente: Activo y Exchange abrían de la Z a la A.
- Hallazgo durante la verificación: el `aria-label` «Ordenar por Activo» del botón se convertía también en el nombre
  de la cabecera de columna, que el lector de pantalla repite al recorrer cada celda.

## Scope

**In** (`HoldingsTable`, tabla de escritorio):
- `aria-sort="ascending|descending"` en la cabecera activa; nada en las demás.
- Icono ↑ / ↓ en la columna activa y etiqueta en tinta plena; ↕ tenue en las inactivas.
- Primera dirección por columna: Activo y Exchange A→Z, Valor mayor primero.
- Botón nombrado solo con el título de la columna (patrón de tabla ordenable de WAI-ARIA), sin `aria-label`.

**Out:** controles de orden en la lista móvil; ordenar por Cantidad, Precio o Registrado.

## Checklist

- [x] `aria-sort` y flecha siguen a la columna activa — prueba (DOM, 2026-11-23, 1280 px): por defecto
      VALOR[descending|↓]; Activo → ACTIVO[ascending|↑]; Activo → [descending|↓]; Exchange → EXCHANGE[ascending|↑];
      Valor → VALOR[descending|↓]; Valor → [ascending|↑]. Las inactivas sin `aria-sort` y con ↕.
- [x] Orden real de filas coincide — prueba: Activo ↑ BNB, BTC, ETH; Activo ↓ ETH, BTC, BNB; Valor ↓ 1.957,54 €,
      369,83 €, 301,33 €; Valor ↑ al revés.
- [x] Nombres accesibles — prueba (árbol de accesibilidad): columnheader «Activo» > button «Activo»; igual para
      Exchange y Valor.
- [x] Revisión visual — captura solo de la cabecera: VALOR ↑ en tinta plena, el resto con ↕ tenue.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página ni de consola.

Capturas revisadas y borradas.

## Notas

- Hay otro botón «Activo» en la página (selector de Participación). No choca para un lector de pantalla: cada uno está
  en su región con nombre («Participación…», «Holdings actuales»).

## Next step

PR hacia `development`.
