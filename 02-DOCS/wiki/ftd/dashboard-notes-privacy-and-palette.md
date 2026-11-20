---
author: kramirez
branch: fix/dashboard-notes-privacy-and-palette
status: done
---
# Dashboard: notas sin contenido, paleta sin rojo y números consistentes

## Intent

La crítica de diseño del dashboard (2026-11-20, `/impeccable critique`, con capturas reales) encontró dos
problemas prioritarios:

1. **Privacidad.** El widget «Notas recientes» pinta el contenido de cada nota en la página principal. Las notas
   guardan contraseñas, cuentas bancarias y emails, que quedan visibles para cualquiera que mire la pantalla o
   una captura.
2. **Consistencia.** La paleta por activo incluye un rosa (`#f43f5e`) que en pantalla se lee como el rojo
   reservado para movimientos negativos (ETH quedaba junto al delta negativo, ambos en rojo). Además los
   porcentajes salían con punto decimal (`74.47%`) y los importes con coma, y los importes de 4 cifras no
   llevaban separador de miles (`1957,54 €` frente a `55.913,70 €`).

## Scope

**In:**
- `LatestNotesWidget`: mostrar solo título, categoría y fecha. Sin contenido.
- `chartColors.ts`: sustituir el rosa por un color que no se confunda con el rojo. Mismo tamaño de paleta, así
  que el resto de activos conserva su color.
- `format.ts`: porcentajes con `Intl` `es-ES` (coma decimal) y separador de miles siempre en importes y
  cantidades.

**Out:** el resto de la crítica (eje del histórico, cifra principal con fecha/delta, tarjetas duplicadas, móvil);
cifrar u ocultar contenido en la página `/notes`.

## Checklist

- [x] Widget de notas sin contenido — prueba (2026-11-20, DOM con Playwright): 10 notas, 0 bloques
      `.rich-text` dentro del widget. Verificado por DOM, no por captura, para no fotografiar datos sensibles.
- [x] Paleta sin rosa/rojo — prueba (captura 2026-11-20): ETH en fucsia en donut, histórico y leyenda; el
      único rojo es el delta negativo `-418,98 €`.
- [x] Números `es-ES` consistentes — prueba (captura 2026-11-20): `2.628,70 €`, `1.957,54 €`, `1.573,07 €`,
      `74,47 %`.
- [x] `npm run lint`, `npx tsc --noEmit` y `npm run build` en verde (2026-11-20, exit 0 los tres) — registro:
      [verificación](../sdd/verifications/dashboard-notes-privacy-and-palette-2026-11-20.md).

## Next step

PR de `fix/dashboard-notes-privacy-and-palette` hacia `development`. Siguientes puntos de la crítica (fuera de
esta ficha): fecha y delta junto a la cifra principal, eje Y del histórico, quitar tarjetas duplicadas, móvil.
Las capturas de verificación se borraron.
