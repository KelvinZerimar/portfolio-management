---
author: kramirez
branch: fix/dashboard-distinct-asset-colors
status: done
---
# Dashboard: colores de activo distinguibles

## Intent

Hallazgo de [dashboard-allocation-donut-totals](dashboard-allocation-donut-totals.md) (PR #40): la paleta tenía 6
colores y el límite era de 10 activos, así que los colores se repetían. Al validarla con la skill `dataviz`
(`validate_palette.js`, 2026-11-20) apareció algo más grave: **la paleta actual ya fallaba con 6**:

- violeta `#8b5cf6` ↔ fucsia `#d946ef`: ΔE 1,3 con protanopia (el mismo color para un daltónico);
- violeta ↔ índigo `#4f46e5`: ΔE 11,4 con visión normal (el mínimo es 15).

Además, `colorForKey` asigna por hash: dos activos podían recibir el mismo color aunque hubiera huecos libres.

## Decisión

Las líneas del histórico se cruzan y los segmentos del donut se ordenan por valor, así que cualquier color puede
quedar junto a cualquier otro: se valida con `--pairs all`. Sin rojos ni rosas (el rojo es solo para pérdidas;
los grises, para «Otros» y «Efectivo»; el casi negro, para la línea Total), la búsqueda sobre 12 tonos (paleta de
referencia de `dataviz` + la actual) dio:

| Colores | Resultado con todos los pares |
|---|---|
| 6 | ninguna combinación pasa |
| 5 | 12 pasan, todas en la franja mínima de CVD (6–8): solo legal con etiquetas directas, que las líneas no tienen |
| 4 | 30 pasan con CVD ≥ 8 |

Elegida: azul `#2a78d6`, amarillo `#eda100`, verde `#008300`, cian `#06b6d4` (CVD peor par 16,2; visión normal
peor par 17,5). Contraste: amarillo 2,17 y cian 2,43 frente a blanco, por debajo de 3:1. Eso obliga a mostrar
etiquetas visibles, y ya existen (leyenda-tabla del donut con nombre, valor y %; leyenda y tooltip del histórico).

Regla de `dataviz`: nunca se inventa un color más; lo que no cabe va a «Otros». Por tanto, N = 4.

## Scope

**In:**
- `CHART_COLORS` = las 4 de arriba. `MAX_RANKED_ASSETS` = `CHART_COLORS.length`.
- `assignSeriesColors`: colores distintos garantizados dentro de un gráfico. Cada entidad prefiere su color por
  hash del nombre (estable) y, si ya está cogido, toma el siguiente libre, procesando por id ascendente
  (determinista). Donut e histórico reciben el mismo conjunto de los N primeros, así que coinciden.
- Donut (activo y exchange) e histórico usan `assignSeriesColors`.

**Out:** el avatar de respaldo de la tabla de Holdings (decorativo, sigue con `colorForKey`); modo oscuro (no
existe en el proyecto); móvil.

## Checklist

- [x] Paleta validada sobre `#ffffff` — prueba (`validate_palette.js`, 2026-11-20): adyacentes y `--pairs all` →
      ALL CHECKS PASS; peor CVD 16,2, peor visión normal 17,5; aviso de contraste (amarillo, cian) cubierto por
      etiquetas visibles.
- [x] Datos reales: colores distintos e iguales en los dos gráficos — prueba (color calculado del punto de cada
      leyenda): donut BTC `rgb(237,161,0)`, ETH `rgb(0,131,0)`, BNB `rgb(6,182,212)`; histórico, los mismos.
- [x] 12 activos ficticios + EUR — prueba (respuesta de `allocation?groupBy=Asset` interceptada): 6 filas y 6
      sectores = T01–T04 con 4 colores distintos + «Otros (8)» 2.200,00 € 22,00 % + «Efectivo» 2.000,00 € 20,00 %;
      suma 10.000 € y 100 %. 0 errores de consola.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-20, exit 0).

Capturas revisadas y borradas; ninguna incluía el widget de notas.

## Next step

PR hacia `development`. Pendiente de la crítica: móvil.
