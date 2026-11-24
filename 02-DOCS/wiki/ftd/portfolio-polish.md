---
author: kramirez
branch: feat/portfolio-polish
status: done
---
# Portfolio: borrado con peso, alta arriba, valor visible, € y «hoy» local

## Intent

Punto 5 de la crítica de diseño de la página Portfolio (2026-11-23), hallazgos P2 y P3:

- 8: borrar un portfolio pedía la misma confirmación pequeña que borrar una fila, aunque se lleva todo su registro.
- 9: el formulario de alta estaba debajo de la paginación.
- 10: ni la lista ni la cabecera del detalle decían cuánto vale el portfolio.
- 11: el precio no indicaba la moneda.
- 12: «hoy» se calculaba en UTC; justo después de medianoche en Madrid daba el día anterior.
- 13: «Entrada añadida correctamente.» no desaparecía hasta el siguiente envío.

## Scope

**In:**
- `RowActions`: nueva prop `confirmMessage`, que solo se renderiza al confirmar. La confirmación ya puede ocupar
  varias líneas.
- Lista (`portfolio/page.tsx`):
  - columna «Valor», y también en la lista de móvil;
  - al borrar, «¿Eliminar «Nombre»? Se borrarán sus N entradas y no se puede deshacer.». El número viene de
    `total` de la lista de entradas, pedido solo al abrir la confirmación. El borrado del portfolio va en cascada
    (`PortfolioEntryConfiguration`).
- Detalle (`portfolio/[id]/page.tsx`):
  - «Valor actual X € · según la última entrada de cada activo» en la cabecera, desde `/value`;
  - el alta pasa a su propio panel «Añadir entrada», encima del registro; los avisos del alta van con ella;
  - «Precio (€)» en el alta, en la tarjeta y en la fila de edición, y en la cabecera de la tabla;
  - `todayInputValue` usa la fecha local; las entradas siguen guardándose a las 00:00Z de ese día;
  - el aviso de éxito se oculta a los 5 s y lleva `role="status"`.

**Out:**
- El caso «al editar, la hora vuelve a 00:00»: ya resuelto en
  [portfolio-refresh-and-entry-time](portfolio-refresh-and-entry-time.md), que conserva la hora si la fecha no cambia.
- `/value` sin fecha usa `UtcNow` en el backend: una entrada de hoy creada entre las 00:00 y la 01:00 de Madrid no
  cuenta en el valor hasta que en UTC es ese día. No se toca.

## Checklist

- [x] Lista: columna «Valor» (2.567,21 €), también en móvil — prueba (Playwright, 2026-11-24).
- [x] Confirmación de borrado: «… Se borrarán sus 6 entradas y no se puede deshacer.»; respondido «No». En móvil,
      sin scroll horizontal (390/390); captura revisada.
- [x] Lista sin scroll horizontal a 390, 640 y 768 px.
- [x] Cabecera del detalle: «Valor actual 2.567,21 € · según la última entrada de cada activo».
- [x] Panel de alta encima del registro (top 220 < 354); captura revisada.
- [x] «Precio (€)» en la etiqueta del alta y en la cabecera de la tabla.
- [x] «Hoy» local: con el reloj a 2026-11-24 23:30Z en Europe/Madrid, la fecha y su máximo son 2026-11-25.
- [x] Aviso de éxito (alta simulada con 201 falso): aparece y a los 5,6 s ya no se ve.
- [x] Detalle sin scroll horizontal a 390 y 768 px. Ninguna escritura real; 0 errores de página.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde (2026-11-24, exit 0).

Capturas revisadas y borradas.

## Next step

PR hacia `development`. Con esto termina la crítica de diseño de Portfolio.
