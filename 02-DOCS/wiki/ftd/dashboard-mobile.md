---
author: kramirez
branch: feat/dashboard-mobile
status: done
---
# Dashboard: vista móvil

## Intent

Último punto de la crítica de diseño del dashboard (2026-11-20). Antecedente:
[dashboard-distinct-asset-colors](dashboard-distinct-asset-colors.md) (PR #41). En 390 px de ancho:

- La navegación es una tira de 6 pestañas con scroll horizontal que se corta («Exc…») sin pista de que hay más.
- La tabla de Holdings tiene `min-w-[640px]`: Cantidad, Precio y Registrado quedan fuera de pantalla, sin pista.
- Las notas recientes (10) ocupan cerca de la mitad del alto de la página.
- El título del portfolio se parte en dos líneas junto a «Asistente»; la fecha del histórico, también.

## Scope

**In** (solo por debajo de `md`/`sm`; escritorio sin cambios):
- `MobileNav`: 6 columnas fijas, icono sobre etiqueta, alto ≥ 48 px, sin scroll. Etiqueta corta opcional en
  `navItems` (`shortLabel`: «Cripto»).
- `HoldingsTable`: lista apilada por debajo de `sm` (activo + valor; exchange · cantidad × precio; fecha). Misma
  lista filtrada y ordenada que la tabla. La tabla sigue desde `sm`.
- `LatestNotesWidget`: 3 notas por debajo de `sm`.
- «Asistente»: solo icono por debajo de `sm` (con `aria-label`). Cabecera del histórico con `flex-wrap`.

**Out:** controles de orden en la lista móvil (sigue el orden de la tabla, por defecto valor descendente); otras
páginas.

## Checklist

- [x] Navegación: 6 enlaces visibles a 390 px, sin scroll — prueba (DOM, 2026-11-23): `scrollWidth` 390 =
      `clientWidth` 390; etiquetas Dashboard, Portfolio, Cripto, Exchanges, Notas, Agente; alto mínimo 49 px.
- [x] Holdings: lista a 390 px con todos los datos — prueba (DOM): tabla oculta; 3 filas, p. ej. «BTC / Bitcoin /
      1.957,54 € / Bitvavo · 0,03501 × 55.913,70 € / 20 nov 2026».
- [x] Notas: 3/10 visibles a 390 px, 10/10 a 1440 px — prueba (DOM; las capturas se cortaron antes del widget).
- [x] Título en una línea a 390 px y a 1440 px — prueba (DOM: `h1` ocupa 1 línea).
- [x] Escritorio sin cambios — prueba (captura 1440 px): navegación móvil y lista ocultas, tabla visible.
- [x] Página sin scroll horizontal a 390 px — prueba: `scrollWidth` 390. 0 errores de consola.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).

Capturas revisadas y borradas.

## Notas

- `MobileNav` es del layout: el cambio de navegación aplica a todas las páginas de la app en móvil, no solo al
  dashboard. Las demás páginas no se revisaron en móvil.

## Next step

PR hacia `development`. Con esto se cierran todos los puntos de la crítica del dashboard. Pendiente menor visto en
la captura de escritorio: la cabecera de Holdings (CSV, Excel y buscador en dos filas) sigue siendo alta.
