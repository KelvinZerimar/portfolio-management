---
author: kramirez
branch: feat/portfolio-edit-row-validation
status: done
---
# Portfolio: validación, nombres accesibles y teclado en la edición de entradas

## Intent

Punto 3 de la crítica de diseño de la página Portfolio (2026-11-23). En la fila de edición de entradas:

- No había validación: con un campo vacío «Guardar» no hacía nada y no decía por qué; cantidad o precio cero o
  negativos y fechas futuras se enviaban a la API. El formulario de alta sí validaba.
- Los cinco controles no tenían nombre accesible (en la tabla la cabecera de columna es la única etiqueta visible):
  un lector de pantalla los anunciaba como «cuadro combinado» / «botón de número» sin más.
- (El error del servidor al guardar ya se mostraba desde [portfolio-refresh-and-entry-time](portfolio-refresh-and-entry-time.md).)

## Scope

**In** (`app/(app)/portfolio/[id]/page.tsx`):
- `entryFieldsError`: una sola función de reglas para alta y edición (todos los campos, cantidad y precio > 0, fecha
  no futura). La edición la usa desde `useEntryEditor`, así que aplica a la fila de escritorio y a la tarjeta móvil.
  El texto del alta no cambia.
- Fila de escritorio: `aria-label` en Activo, Exchange, Cantidad, Precio y Fecha; `min="0"` e `inputMode="decimal"`
  en los números; `max` = hoy en la fecha (también en la tarjeta móvil).
- Teclado: al abrir la edición el foco va a «Activo»; Enter en un campo guarda; Escape cancela.

**Out:** el desplegable de exchange se corta en la fila de edición («Bitvavc») por el ancho de la columna — ya
pasaba; el formulario de alta vacío lo sigue frenando la validación nativa del navegador (`required`), sin cambios.
Puntos 4 y 5 de la crítica.

## Checklist

- [x] Foco al abrir — prueba (Playwright, 2026-11-24, 1440 px): `document.activeElement` = SELECT «Activo».
- [x] Nombres accesibles — prueba (árbol de accesibilidad): combobox «Activo», «Exchange»; spinbutton «Cantidad»,
      «Precio»; textbox «Fecha»; botones «Guardar», «Cancelar».
- [x] Validación en la fila — prueba: cantidad vacía + Enter → «Completa todos los campos para guardar la
      entrada.»; −1 + Enter → «La cantidad y el precio deben ser mayores que cero.»; fecha de mañana + Guardar →
      «La fecha no puede ser futura.» (`max` = 2026-11-24). Ninguna petición de escritura salió.
- [x] Escape cierra la edición — prueba: 0 controles de edición tras Escape.
- [x] Tarjeta móvil (390 px) — prueba: precio 0 + Guardar → «La cantidad y el precio deben ser mayores que cero.»
- [x] Revisión visual — captura de la tabla con el error bajo la fila; borrada.
- [x] `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde en el worktree (2026-11-24, exit 0). 0 errores de
      página.

## Notas

- Trabajo hecho en el worktree `.worktrees/feat-portfolio-edit-row-validation` (otra sesión en la carpeta
  principal). Prueba con el servidor de desarrollo del worktree (puerto 3001) y una copia temporal de la API
  (puerto 5099), ambos parados al terminar; las llamadas a la API pasaron por Playwright y toda escritura se
  bloqueó.

## Next step

PR hacia `development`. Después, punto 4 (nombre del activo en los selectores).
