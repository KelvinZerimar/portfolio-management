---
author: kramirez
branch: feat/dashboard-allocation-donut-totals
status: done
---
# Dashboard: el donut de Participación suma el total real

## Intent

Punto P0 de la crítica de diseño del dashboard (2026-11-20). Antecedente:
[dashboard-remove-duplicate-asset-cards](dashboard-remove-duplicate-asset-cards.md) (PR #39).

`AllocationChart` quitaba el Euro (agrupación por activo) y recortaba a los `MAX_RANKED_ASSETS` primeros, y después
dibujaba el anillo en proporción a lo que quedaba. Las etiquetas, en cambio, muestran el % sobre el **total**. Con
efectivo o con más de 10 activos, un segmento podía ocupar el 40 % del anillo y decir «28 %».

## Scope

**In:**
- El anillo representa el total: los N primeros con su color; el resto, agrupado en «Otros (n)»; el Euro, en
  «Efectivo». Los dos agregados en gris (no son activos con identidad propia) y al final de la leyenda.
- Agrupación por Exchange: mismo límite de N con «Otros»; sin «Efectivo» (no aplica).
- Lógica pura en `lib/allocationSlices.ts`.

**Out:** el ranking del histórico (sigue sin Euro y con N líneas; es un gráfico de líneas, no de partes de un todo);
móvil.

## Checklist

- [x] Datos reales sin cambios visibles — prueba (DOM, 2026-11-20): Activo «BTC 1.957,54 € 74,47 % | ETH 369,83 €
      14,07 % | BNB 301,33 € 11,46 %»; Exchange «Bitvavo 2.628,70 € 100,00 %». Igual que antes del cambio.
- [x] Con Euro y más de N activos, el anillo suma el total — prueba (Playwright interceptando
      `/Portfolio/{id}/allocation?groupBy=Asset` con 12 activos ficticios T01–T12 + EUR, total 10.000 €): 12 filas y
      12 sectores = 10 activos + «Otros (2)» 250,00 € 2,50 % (T10 + T12) + «Efectivo» 2.000,00 € 20,00 %. Suma de
      la leyenda: 10.000 € y 100 %. Captura del panel revisada y borrada. 0 errores de consola.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-20, exit 0).

## Hallazgo (fuera de esta ficha)

La paleta tiene 6 colores (`CHART_COLORS`) y `MAX_RANKED_ASSETS` es 10: a partir del 7.º activo los colores se
repiten (en la prueba, T01 y T07 cian; T02, T08 y T11 ámbar). Con los datos reales (3 activos) no se nota.
Opciones: ampliar la paleta a 10 colores distinguibles (sin rojos) o bajar N a 6 y dejar el resto en «Otros».

## Next step

PR hacia `development`. Pendiente de la crítica: colores repetidos (hallazgo) y móvil.
