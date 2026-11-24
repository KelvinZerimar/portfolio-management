---
author: kramirez
branch: feat/portfolio-refresh-and-entry-time
status: done
---
# Portfolio: sin entradas empatadas, «Registrar precios de hoy» y hora visible

## Intent

Punto 2 de la crítica de diseño de la página Portfolio (2026-11-23), corregido con datos reales:

- La crítica atribuía las dos entradas BNB · Bitvavo · 20 nov a «Actualizar precios». **No era así**: las dos tienen
  `RecordedAt = 2026-11-20T00:00:00Z` (entradas con fecha sin hora); el refresco sella la hora del momento.
- El problema real es del dominio: `Portfolio.GetHoldingsAsOf` toma la `RecordedAt` más reciente por activo +
  exchange **sin desempate**. Dos entradas en el mismo instante dejan el valor del portfolio a merced del orden de la
  base de datos.
- Decisión del usuario (2026-11-23): **impedir el duplicado** (no desempatar en el dominio).

## Scope

**In:**
- API: crear o editar una entrada con el mismo portfolio, activo, exchange y `RecordedAt` exacto que otra →
  **409** `PortfolioEntry.Duplicate`, `metadata.existingEntryId`. Al editar se excluye la propia entrada. Aplica
  también al agente (usa los mismos comandos). `IPortfolioEntryRepository.FindDuplicateAsync`.
- Formulario de alta: mensaje «Ya hay una entrada de BNB en Bitvavo el 20 nov 2026…» y botón **Editar la
  existente** (filtra por ese activo y fecha y abre la entrada en edición).
- Edición (tabla y tarjeta móvil): el error al guardar se muestra (antes fallaba en silencio); mensaje propio para
  el duplicado. Si no se cambia la fecha, se conserva la hora original (antes se reescribía a 00:00 UTC).
- «Actualizar precios» → **Registrar precios de hoy**: botón con borde e icono, texto de ayuda enlazado
  (`aria-describedby`) que dice que añade entradas y no cambia las existentes. Mensaje de éxito con nº de entradas y
  hora; activos sin precio por su símbolo (antes «sin id de CoinGecko»).
- Fecha con hora solo cuando la entrada la tiene (`formatRecordedAt`): las entradas a mano (00:00 UTC) siguen
  mostrando solo el día.
- `lib/errors.ts` lee `code/description/metadata` en camelCase y en PascalCase (ver Notas).

**Out:**
- Las dos BNB del 20 nov ya existentes siguen empatadas hasta que el usuario borre una. Editar cualquiera de ellas
  sin cambiar la fecha devuelve ahora 409.
- Restricción única en base de datos (protegería frente a carreras; fallaría con los duplicados existentes).
- Puntos 3–5 de la crítica.

## Checklist

- [x] Tests nuevos primero (no compilaban sin `FindDuplicateAsync`): crear duplicado → Conflict sin `AddAsync`;
      editar sobre otra → Conflict sin `Update` y sin tocar la entidad; editar se busca excluyendo su id. 3/3 en
      verde; suite Application 264/264, Domain 46/46.
- [x] API real (copia temporal en el puerto 5099 contra la base de desarrollo): POST de un duplicado de la 1305 →
      409 con `existingEntryId: 1305`, total sigue en 6; PUT de la 1302 sin cambios → 409 con `existingEntryId:
      1305`, la 1302 queda igual.
- [x] UI (Playwright, escrituras interceptadas y simuladas — ninguna llegó a la API): alerta de duplicado con
      activo, exchange y fecha; «Editar la existente» deja los filtros en BNB / 2026-11-20 / 2026-11-20 y abre la
      1305 (649,28 €); guardar con 409 muestra el error bajo la fila.
- [x] Hora: entrada con 14:32 UTC → «20 nov 2026, 15:32»; las de 00:00 UTC → «20 nov 2026».
- [x] Botón y ayuda: «Registrar precios de hoy» con `aria-describedby` → «Añade una entrada por activo con el precio
      actual. Las entradas existentes no cambian.» Éxito: «Añadidas 3 entrada(s) con el precio de 23 nov 2026,
      14:05. Sin precio disponible: CAKE.» (respuesta simulada).
- [x] 390 px sin scroll horizontal; capturas revisadas y borradas.
- [x] `dotnet build` (Infrastructure, 0 avisos), `dotnet format --verify-no-changes` (Application),
      `npx tsc --noEmit`, `npm run lint`, `npm run build` en verde (2026-11-23).

## Notas

- **Incidente durante la verificación:** un PUT de prueba contra la copia temporal cambió por error la fecha de la
  entrada 1302 a 2026-10-20 (conversión de fecha en el script). Se restauró en el acto a sus valores exactos
  (2026-11-20, 0,4641, 516,79 €) a través de la API del usuario y se comprobó el listado completo (6 entradas,
  iguales a las de antes).
- **Fallo previo encontrado:** las respuestas de los cuatro POST de creación pasan por `IdempotencyFilter`, que las
  guarda en `HybridCache` como `object` y las devuelve con claves PascalCase (`Code`, `Description`…). El frontend
  buscaba `description` y nunca mostraba los mensajes de error de creación. Aquí se resuelve en el cliente; el
  arreglo de fondo (serializar con las opciones JSON de la API) queda pendiente.

## Next step

PR hacia `development`. Después, decidir qué hacer con las dos BNB del 20 nov y el arreglo de `IdempotencyFilter`.
