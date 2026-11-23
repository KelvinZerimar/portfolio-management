---
author: kramirez
branch: fix/dashboard-holdings-header
status: done
---
# Dashboard: cabecera de Holdings en una fila (y rejilla sin desborde)

## Intent

Pendiente menor de [dashboard-mobile](dashboard-mobile.md) (PR #42): en escritorio la cabecera de Holdings ocupaba
dos filas (CSV y Excel, y debajo el buscador), unos 112 px de alto para una tabla de pocas filas. Causa: el buscador
llevaba `w-full` dentro de una fila `flex-wrap`, así que siempre pedía una línea propia.

## Scope

**In:**
- Cabecera de Holdings: buscador (256 px) primero, CSV y Excel después, en una fila desde `sm`. En móvil, buscador a
  todo el ancho y botones debajo (igual que antes). `aria-label` en el buscador (solo tenía placeholder).
- **Hallazgo durante la verificación, arreglado aquí:** a 768 px la página tenía 28 px de scroll horizontal
  (`scrollWidth` 796). Causa: la rejilla `md:grid-cols-[7fr_3fr]` daba ~144 px a Participación (y ~210 px a
  1024) con la barra lateral visible; el donut y la leyenda con valores necesitan ~290 px. Las dos columnas pasan
  a `xl` (1280 px); por debajo se apilan. Venía de antes de esta rama (ya pasaba con #39–#42).
- Leyenda del donut con `max-w-sm`: apilado a todo el ancho, el nombre y el valor quedaban a ~700 px.

**Out:** fechas de la tabla que se parten en dos líneas a 1024 px (columna «Registrado»); controles de orden en
móvil.

## Checklist

- [x] Cabecera en una fila desde 1024 px — prueba (DOM, 2026-11-23): alto 71 px a 1024/1280/1440 (antes ~112);
      103 px a 768 (título arriba, controles en una fila); 145 px a 390 (sin cambios).
- [x] Filtro sigue funcionando — prueba: buscar por su nombre accesible y escribir «eth» → 1 fila (ETH).
- [x] Sin scroll horizontal — prueba: `scrollWidth` = ancho a 390, 768, 1024, 1280 y 1440 px.
- [x] Columnas lado a lado solo desde 1280 — prueba: apiladas a 390/768/1024, lado a lado a 1280 (Participación
      290 px) y 1440 (338 px).
- [x] Leyenda legible apilada — prueba: captura a 1024 px, leyenda de 384 px centrada.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde tras la última edición (2026-11-23, exit 0).
      0 errores de página.

Capturas revisadas y borradas; ninguna incluía el widget de notas.

## Next step

PR hacia `development`.
