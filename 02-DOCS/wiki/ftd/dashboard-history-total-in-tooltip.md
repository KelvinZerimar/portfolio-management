---
author: kramirez
branch: feat/dashboard-history-total-in-tooltip
status: done
---
# Dashboard: el Total del histórico, solo en el tooltip

## Intent

Petición (2026-11-23): en el gráfico Histórico, quitar la línea del Total y mostrar el total solo dentro del tooltip al
pasar el ratón sobre una fecha.

Efecto añadido: la línea del Total marcaba la escala del eje Y (hasta ~4,5 mil €) y aplastaba las líneas por activo.
Sin ella, el eje se ajusta a los activos (hasta ~2,6 mil €). La cifra principal de la página ya da el total actual.

## Scope

**In** (`ValueHistoryChart`):
- Con líneas por activo (2 o más activos, sin contar EUR): no se dibuja la línea del Total; la leyenda pierde «Total».
- Tooltip: «Total» sigue como primera fila, en negrita, sin muestra de color (hueco en blanco para alinear cifras).
- **Caso límite:** con un solo activo solo existe la línea del Total (la del activo sería idéntica). Se mantiene; si
  no, el gráfico quedaría vacío. Ahí el tooltip conserva la muestra negra.

**Out:** fila «Otros» en el tooltip. Con más de 4 activos, el Total incluye activos que no tienen línea; ya pasaba
antes.

## Checklist

- [x] Sin línea del Total con varios activos — prueba (DOM, 2026-11-23, 1440 px): 3 líneas (#06b6d4, #eda100,
      #008300), ninguna `var(--ink)`. Leyenda: BNB | BTC | ETH.
- [x] Total en el tooltip — prueba: al pasar el ratón el 20 oct → «Total» (negrita, sin muestra) ; BTC ; ETH; el 20
      nov → Total ; BTC ; ETH ; BNB. Captura: «Total 3.047,68 €» en el tooltip.
- [x] Eje ajustado a los activos — prueba (captura): eje Y 0 – 2,6 mil € (antes hasta 4,5 mil €).
- [x] Un solo activo — prueba (respuesta del histórico recortada a 1 serie con Playwright): 1 línea `var(--ink)`,
      sin leyenda, tooltip «Total» con muestra negra.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página ni de consola.

Capturas revisadas y borradas; solo incluían el panel del histórico.

## Next step

PR hacia `development`. El PR #47 (recuadro de foco) toca también `wiki/index.md`; las entradas se insertaron en
otra posición para no chocar.
