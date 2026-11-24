---
author: kramirez
branch: fix/dashboard-chart-focus-ring
status: done
---
# Dashboard: sin recuadro negro al pulsar los gráficos

## Intent

Al pulsar el gráfico Histórico o el de Participación aparecía un recuadro de líneas negras alrededor.

Causa (medida en el DOM, 2026-11-23): Recharts 3.10.1 activa por defecto su capa de accesibilidad, que hace
enfocable el `<svg>` del gráfico (`tabindex="0"`, `role="application"`) y la capa de sectores del donut, para recorrer
el tooltip con las flechas. Un clic enfoca el `<svg>` y Chrome pinta su anillo por defecto (`outline: auto`, negro).
El anillo de la marca de `globals.css` solo aplica a `:focus-visible` (foco por teclado), que un clic no activa.

## Scope

**In:** en `frontend/app/globals.css`, `.recharts-wrapper :focus:not(:focus-visible) { outline: none; }`. Oculta el
anillo solo para el foco con puntero; el foco por teclado conserva el anillo de la marca y la navegación con flechas.

**Out (descartado):** `accessibilityLayer={false}` (quita la navegación por teclado del tooltip); `outline: none`
para todo foco (deja sin indicador a quien navega con teclado).

## Checklist

- [x] Clic sin recuadro — prueba (DOM, 1440 px): clic en el histórico, en el donut y en un sector →
      `svg.recharts-surface` enfocado con `outline: none`, `:focus-visible` falso. Antes: `outline: auto` negro.
- [x] Teclado conserva el anillo — prueba: con Tab hasta el gráfico → `outline: solid 2px rgb(79, 70, 229)`
      (marca), `:focus-visible` verdadero. Captura con margen: anillo visible alrededor del gráfico.
- [x] Navegación con flechas intacta — prueba: flecha derecha con el gráfico enfocado → tooltip `visible`.
- [x] El anillo no se recorta — prueba: ningún ancestro del `<svg>` tiene `overflow` distinto de `visible`.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página.

Capturas revisadas y borradas; solo incluían el área del histórico (ningún widget de notas).

## Next step

PR hacia `development`.
