---
author: kramirez
branch: feat/dashboard-hero-context-and-history-axis
status: done
---
# Dashboard: contexto en la cifra principal y eje en el histórico

## Intent

Punto 3 de la crítica de diseño del dashboard (2026-11-20). Antecedente:
[dashboard-notes-privacy-and-palette](dashboard-notes-privacy-and-palette.md) (puntos 1 y 2, PR #37).

- La cifra principal (`2.628,70 €`) no decía a qué fecha correspondía ni cuánto había cambiado. Con datos
  introducidos a mano, la fecha es lo que más importa (principio 3 de `PRODUCT.md`: el tiempo es vista principal).
- La variación vivía pequeña dentro del panel del histórico y sin periodo («-418,98 €» junto a una fecha).
- El panel del histórico repetía el valor actual en grande.
- El histórico ocultaba el eje Y y rellenaba un área desde una base que no es cero: una variación pequeña parecía
  grande. El eje X solo mostraba la fecha final.

## Scope

**In:**
- Cifra principal: fecha del último punto («a 20 nov 2026») y variación en importe y % con su periodo
  («en 90 días»). Rojo solo si es negativa (regla de color del proyecto).
- El rango (30d/90d/1a/Todo) sube a la página: cifra principal y gráfico usan el mismo periodo.
- Histórico: sin número grande duplicado; cabecera con el intervalo de fechas del rango. Eje Y visible con
  importes compactos; sin área degradada; eje X con fecha inicial y final.

- Base del rango (añadido 2026-11-20 a petición del usuario, ver *Hallazgo*): el rango empieza en el corte con el
  último valor conocido antes de él; las líneas por activo usan el mismo criterio (`valueAsOf`).

**Out:** quitar las tarjetas duplicadas, móvil.

## Checklist

- [x] Cifra principal con fecha, variación y periodo — prueba (captura 2026-11-20, escritorio y móvil):
      «a 20 nov 2026 · -418,98 € (-13,75 %) en 90 días», variación en rojo.
- [x] Cambiar el rango actualiza la cifra principal — prueba (DOM con Playwright): 90d → «en 90 días»;
      Todo → «desde el primer registro»; 30d → solo «a 20 nov 2026» (un único punto en el rango, sin variación).
- [x] Histórico sin número duplicado, con eje Y y sin área — prueba (captura): cabecera «20 oct 2026 – 20 nov
      2026», eje Y 0 € / 1,5 mil € / 3 mil € / 4,5 mil €, eje X con «20 oct» y «20 nov». Sin errores en consola.
- [x] Rango con base en el último valor conocido — prueba (DOM + captura, 2026-11-20): 30d → cabecera
      «21 oct 2026 – 20 nov 2026», cifra principal «-418,98 € (-13,75 %) en 30 días», 4 líneas dibujadas; 90d y
      Todo sin cambios («20 oct 2026 – 20 nov 2026»). Sin errores en consola.
- [x] `npm run lint`, `npx tsc --noEmit`, `npm run build` en verde tras la última edición (2026-11-20, exit 0).

Capturas borradas tras revisarlas; ninguna incluía el widget de notas.

## Hallazgo (resuelto en esta ficha)

El usuario de prueba tiene solo dos puntos de histórico (20 oct y 20 nov). Con 30d el rango se quedaba con un
punto: sin variación y con el gráfico pidiendo más datos, aunque el valor en la fecha de corte se conocía. Ahora
`filterHistoryByRange` abre el rango con un punto en el corte con el último valor anterior (en un ledger manual,
el valor en esa fecha) y, si dentro del rango no hay registros, lo cierra en `now` con el mismo valor (línea
plana, variación 0). Este último caso no se pudo observar con los datos del usuario de prueba.

## Next step

PR hacia `development`. Pendiente de la crítica: tarjetas duplicadas y móvil.
