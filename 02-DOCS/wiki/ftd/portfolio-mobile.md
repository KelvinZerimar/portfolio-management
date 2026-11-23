---
author: kramirez
branch: feat/portfolio-mobile
status: done
---
# Portfolio: vista móvil (lista y detalle)

## Intent

Punto 1 de la crítica de diseño de la página Portfolio (2026-11-23):

- **P0** — Lista de portfolios a 390 px: la tabla medía 394 px dentro de un panel de 358 px con `overflow-hidden`. La
  columna «Acciones» quedaba recortada: no se podía borrar un portfolio desde el móvil.
- **P1** — Detalle: la tabla de entradas (720 px) escondía Valor, Fecha y Acciones tras un scroll sin pista.

## Scope

**In:**
- Lista (`portfolio/page.tsx`): por debajo de `sm`, lista apilada (nombre enlazado, descripción, «Creado el …») con
  las acciones a la derecha. Tabla desde `sm` (a 640 px cabe con las acciones a la vista).
- Detalle (`portfolio/[id]/page.tsx`): por debajo de `lg`, lista apilada (activo + valor; exchange · cantidad ×
  precio · fecha; editar y borrar). Tabla desde `lg`. Medido: la tabla no cabe entre 640 y 1023 px (panel de 480 a
  668 px; la barra lateral aparece en `md`).
- Edición en la lista: tarjeta con campos etiquetados (Activo, Exchange, Cantidad, Precio, Fecha), valor calculado,
  Cancelar y Guardar. La lógica de edición se movió a un hook (`useEntryEditor`) compartido con la fila de la tabla,
  que no cambia.
- La fecha de cada entrada no se parte entre líneas.

**Out (resto de la crítica):** «Actualizar precios» y hora en la fecha (punto 2); validación, errores y nombres
accesibles en la fila de edición de escritorio (punto 3); nombre del activo en los selectores (punto 4); P2 y P3.

## Checklist

- [x] Lista a 390 px — prueba (DOM, 2026-11-23): lista visible, tabla oculta, página 390 = 390, botones dentro de la
      pantalla. Papelera → aparece «¿Eliminar?» → «No» (sin borrar).
- [x] Lista a 640 y 1440 px — prueba: tabla visible, acciones dentro de la pantalla, sin scroll de página.
- [x] Detalle a 390, 640, 768 y 1023 px — prueba: lista visible, sin scroll de página, botones dentro de la pantalla.
      Ejemplo: «BNB 301,33 € · Bitvavo · 0,4641 × 649,28 € · 20 nov 2026».
- [x] Detalle a 1024 y 1440 px — prueba: tabla visible y entera (736 / 896 px en su panel).
- [x] Edición en móvil — prueba (árbol de accesibilidad): combobox «Activo», «Exchange»; spinbutton «Cantidad»,
      «Precio»; textbox «Fecha»; botones «Cancelar», «Guardar». La tarjeta mide 358 px sin desbordar. Cancelar
      devuelve la fila sin cambios (no se guardó nada).
- [x] Edición en escritorio intacta — prueba: la fila de la tabla sigue mostrando 5 controles; Cancelar la cierra.
- [x] Revisión visual — capturas a 390 px de la lista, el detalle y la tarjeta de edición; la fecha ya no se parte.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página ni de consola.

Capturas revisadas y borradas.

## Next step

PR hacia `development`. Después, punto 2 de la crítica.
