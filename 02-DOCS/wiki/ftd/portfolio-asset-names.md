---
author: kramirez
branch: feat/portfolio-asset-names
status: done
---
# Portfolio: nombre del activo en los selectores

## Intent

Punto 4 de la crítica de diseño de la página Portfolio (2026-11-23): los selectores de activo (filtro, alta, edición
en tabla y en móvil) listaban solo el símbolo, sin ordenar. 27 opciones, entre ellas 10 fondos cuyo «símbolo» es un
ISIN (p. ej. `LU0171310443`): imposibles de reconocer sin su nombre.

## Scope

**In:**
- `lib/assetLabels.ts`: `assetOptionLabel` — moneda: «BTC · Bitcoin»; fondo (símbolo con forma de ISIN): «MyInvestor
  Nasdaq 100 (ES0165265002)»; si el nombre falta o es igual al símbolo, solo el símbolo. `groupAssetsForSelect` —
  orden alfabético (es) por esa etiqueta, monedas y fondos separados.
- `AssetOptions` en `portfolio/[id]/page.tsx`: `optgroup` «Criptomonedas» y «Fondos» (sin grupos si no hay fondos).
  Los cuatro selectores usan la misma lista.
- Anchos: filtro 224 px; alta 160 px (más ancho empujaba «Añadir entrada» a otra línea a 1440 px). La lista
  desplegada muestra el nombre completo; en las monedas el símbolo va primero y sobrevive al recorte.

**Out:** la columna «Activo» de la tabla sigue mostrando el símbolo; punto 5 de la crítica.

## Checklist

- [x] Etiquetas con datos reales — prueba (Playwright, 2026-11-24): 27 opciones, p. ej. «BTC · Bitcoin», «EU · Euro»,
      «Vanguard Global Stock Index Fund EUR Acc (IE00B03HD191)»; sin nombre distinto: «NEXO», «XRP», «USTC».
- [x] Grupos y orden — prueba: `optgroup` Criptomonedas (17) y Fondos (10), ordenados; filtro y alta en el mismo orden.
- [x] Formulario de alta en una fila a 1440 px — prueba: captura tras bajar el selector a 160 px.
- [x] Edición: el activo seleccionado se lee «BNB · Binance Coin».
- [x] 390 y 768 px sin scroll horizontal. Ninguna escritura; 0 errores de página.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde en el worktree (2026-11-24, exit 0).

Capturas revisadas y borradas.

## Next step

PR hacia `development`. Después, punto 5 de la crítica (P2 y P3).
