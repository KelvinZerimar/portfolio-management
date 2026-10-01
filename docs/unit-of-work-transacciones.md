# Unit of Work y manejo de transacciones

## Contexto

Revisión arquitectónica de cómo la solución maneja transacciones de base de datos a través del
patrón Unit of Work (UoW), disparada por una pregunta sobre `AppDbContext.cs`. El objetivo era
entender el flujo endpoint → handler → UoW → `DbContext` → `SaveChanges`, y evaluar si el patrón
está correctamente aplicado.

## Estado actual

`IUnitOfWork` (`src/Application/Common/UnitOfWork/IUnitOfWork.cs`) expone un único miembro:

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

No existe una clase `UnitOfWork` separada: `AppDbContext` implementa `IUnitOfWork` directamente
(`src/Infrastructure/Common/Persistence/Contexts/AppDbContext.cs`), y el `SaveChangesAsync` heredado
de `DbContext` satisface la interfaz. No hay `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync`,
ni `IDbContextTransaction`, en ningún punto de la solución.

En `Infrastructure/DependencyInjection.cs`, `AppDbContext` se registra `Scoped` y `IUnitOfWork` se
resuelve de la misma instancia:

```csharp
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
```

Como los 5 repositorios también son `Scoped` y comparten el mismo `DbContext`/change tracker, el
patrón "repositorio hace staging (`Add`/`Update`/`RemoveRange`), un único `unitOfWork.SaveChangesAsync()`
al final del handler confirma todo en una sola transacción implícita de EF Core" sí funciona
correctamente. Se confirmó este flujo en los ~13 command handlers existentes (Portfolio,
PortfolioEntry, Exchange, CryptoCurrency, RegisterUser, RefreshPortfolioPrices): exactamente una
llamada a `SaveChangesAsync` por request, sin behavior de MediatR que la automatice.

```
Endpoint (MapPost/MapPut/MapDelete)
  -> [IdempotencyFilter]  (solo en POST de creación)
  -> ISender.Send(command)
  -> MediatR: ValidationBehavior -> LoggingBehavior
  -> Handler:
       repository.Add/Update/RemoveRange(entity)   (solo change tracker, sin I/O)
       await unitOfWork.SaveChangesAsync(ct)        (== AppDbContext.SaveChangesAsync, 1 transacción EF Core)
       await cache.RemoveByTagAsync(tag, ct)         (solo Exchange/CryptoCurrency, después del commit)
```

> **Actualizado 2026-10-01**: este diagrama describe el estado *antes* de implementar los puntos
> #1, #2 y #4 del plan de acción. El estado actual está documentado en cada sección de riesgo y en
> el plan de acción, al final de este documento.

## Riesgos identificados y solución propuesta

### 1. El UoW es solo una fachada de `SaveChangesAsync`, sin transacción explícita ni retry de Npgsql

Para el uso actual (una sola llamada a `SaveChangesAsync` por request) no es un problema: EF Core ya
envuelve cada `SaveChanges` en su propia transacción. El riesgo aparece si en el futuro un handler
necesita varias llamadas a `SaveChanges` dentro de una misma transacción lógica, y en que no hay
resiliencia configurada ante errores transitorios de Postgres.

**Solución propuesta:**
- Añadir `npgsqlOptions.EnableRetryOnFailure()` en `AddDbContexts` (`Infrastructure/DependencyInjection.cs`).
- Si surge la necesidad de multi-`SaveChanges` atómico, extender `IUnitOfWork` con
  `BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` respaldados por `IDbContextTransaction`,
  en vez de generalizar antes de necesitarlo.

**Implementado (2026-10-01):** `EnableRetryOnFailure()` añadido en
`Infrastructure/DependencyInjection.cs:61-63`. La extensión de `IUnitOfWork` con transacciones
explícitas queda pendiente, solo si surge un caso de uso real de multi-`SaveChanges` atómico.

### 2. `IRepository<TEntity>.UpdateRangeAsync` llama a `SaveChangesAsync` por su cuenta

En `src/Infrastructure/Common/Repository/Repository.cs`, `UpdateRangeAsync` hace
`dbContext.SaveChangesAsync(cancellationToken)` internamente, rompiendo la convención de que solo el
handler (vía `IUnitOfWork`) decide cuándo comitear. Sin llamadas actuales en el código, pero es una
trampa si alguien lo adopta.

**Solución propuesta:** quitar el `SaveChangesAsync` interno; que el método solo haga
`dbContext.Set<TEntity>().UpdateRange(entities)` (staging) y deje el commit al handler.

**Implementado (2026-10-01):** `UpdateRangeAsync` en `Repository.cs:33-37` ya no llama a
`SaveChangesAsync`.

### 3. Comentario obsoleto en `CreatePortfolioCommandHandler.cs`

Línea comentada `//await unitOfWork.CommitAsync(cancellationToken);`, referenciando un método que no
existe en `IUnitOfWork`. **Ya corregido** (eliminada el 2026-10-01).

### 4. No hay commit automático en el pipeline de MediatR

La disciplina de llamar a `unitOfWork.SaveChangesAsync()` es manual en cada handler. Hoy es
consistente, pero no está forzada estructuralmente: nada impide que un futuro handler olvide
comitear.

**Solución propuesta:** introducir un `UnitOfWorkBehavior<TRequest, TResponse>` en el pipeline de
MediatR (junto a `ValidationBehavior`/`LoggingBehavior` en `Application/DependencyInjection.cs`) que
llame a `SaveChangesAsync` tras ejecutar el handler, solo para requests marcados con una interfaz
`ICommand` y solo si el `ErrorOr<TResponse>` resultante no es un error (`!result.IsError`), ya que
los handlers no lanzan excepciones para fallos de negocio. Tras introducirlo, quitar las llamadas
manuales de los handlers.

**Implementado (2026-10-01):**
- `ICommand` (`src/Application/Common/Messaging/ICommand.cs`): marker interface vacía que
  implementan los 14 commands.
- `UnitOfWorkBehavior<TRequest, TResponse>` (`src/Application/Common/Behaviors/UnitOfWorkBehavior.cs`):
  llama a `unitOfWork.SaveChangesAsync()` tras `next()`, solo si `!response.IsError`. Restringido
  con `where TRequest : ICommand, IRequest<TResponse>` — al ser un generic abierto registrado con
  `AddOpenBehavior`, el contenedor de DI simplemente no resuelve ninguna implementación para los
  `IRequest` que no sean `ICommand` (las queries), así que el behavior no se ejecuta para ellas sin
  necesitar ningún `if`.
- Se detectó que mover el commit al behavior invertía el orden correcto de los 6 handlers
  (Exchange/CryptoCurrency) que hacían `cache.RemoveByTagAsync(...)` **después** de
  `SaveChangesAsync` dentro del propio handler: si el handler ya no comitea, esa invalidación
  pasaría a ejecutarse *antes* del commit real (al final del `Handle`, antes de que el behavior
  llame a `SaveChangesAsync`), lo cual es peor que el problema original del riesgo #7 (ver esa
  sección). Para evitarlo se añadió:
  - `IInvalidatesCache` (`src/Application/Common/Caching/IInvalidatesCache.cs`): expone
    `CacheTagsToInvalidate`, implementada por los 6 commands de Exchange/CryptoCurrency.
  - `CacheInvalidationBehavior<TRequest, TResponse>`
    (`src/Application/Common/Behaviors/CacheInvalidationBehavior.cs`): invalida esos tags tras
    `next()`, solo si `!response.IsError`.
  - Registrado **antes** que `UnitOfWorkBehavior` en `Application/DependencyInjection.cs`
    (`ValidationBehavior → LoggingBehavior → CacheInvalidationBehavior → UnitOfWorkBehavior`), de
    modo que `UnitOfWorkBehavior` queda más cerca del handler: su código posterior a `next()`
    (el commit) se ejecuta primero al desenrollar la pila, y la invalidación de caché ocurre
    después, preservando el orden commit → invalidación.
- Los 14 command handlers ya no reciben `IUnitOfWork` por constructor ni llaman a
  `SaveChangesAsync`; los 6 de Exchange/CryptoCurrency tampoco reciben `HybridCache` ni llaman a
  `RemoveByTagAsync` — esa responsabilidad vive ahora en el command record (`CacheTagsToInvalidate`)
  y en el pipeline.
- Se actualizó `RefreshPortfolioPricesCommandHandlerTests.cs` (único test que construía un handler
  con `IUnitOfWork`) para quitar el mock y las aserciones de `SaveChangesAsync`, ya que esa
  responsabilidad dejó de pertenecer al handler.

### 5. No hay domain events, outbox, ni `SaveChangesInterceptor`

No existe infraestructura para disparar efectos transaccionales ligados al commit (ej. notificar,
publicar eventos de integración) ni para garantizar su entrega.

**Solución propuesta (según necesidad):**
- Si solo se necesita reaccionar *después* del commit dentro del mismo proceso (ej. invalidar
  caché, logging): un `SaveChangesInterceptor` que recoja eventos de los agregados y los publique
  como `INotification` de MediatR tras `SavedChanges`.
- Si se necesita garantía de entrega hacia sistemas externos: outbox real — tabla
  `OutboxMessages` escrita en la misma transacción que la entidad de negocio, más un worker en
  background que la procese y marque como enviada.

### 6. La escritura de la clave de idempotencia no es atómica con el commit

`IdempotencyFilter` (`src/WebApi.MinimalAPI/Idempotency/IdempotencyFilter.cs`) envuelve todo el
pipeline en `HybridCache.GetOrCreateAsync`, pero la entrada de caché solo se persiste **después**
de que `SaveChangesAsync` tiene éxito. Si el proceso falla entre el commit y la escritura en caché,
un reintento no encontraría la clave y re-ejecutaría el handler, dependiendo únicamente de los
checks de unicidad de cada handler para evitar un duplicado real. Ver también
`docs/idempotency-post-endpoints.md` (limitación de caché in-process, no distribuida).

**Solución propuesta:** persistir la clave de idempotencia (y opcionalmente la respuesta
serializada) en una tabla de la propia base de datos, escrita dentro del mismo `SaveChangesAsync`
que la entidad de negocio, con constraint único sobre la clave. `HybridCache` puede mantenerse como
capa de lectura rápida por encima, pero la fuente de verdad pasa a ser transaccional.

### 7. La invalidación de `HybridCache` no es atómica con el commit

En los handlers de `Exchange`/`CryptoCurrency`, `cache.RemoveByTagAsync(...)` se llama después de
`SaveChangesAsync`, lo cual es el orden correcto para no repoblar la caché con datos pre-commit,
pero no es atómico: si el proceso falla o la llamada lanza excepción entre el commit y la
invalidación, la caché sirve datos obsoletos hasta que expire el TTL (10 minutos,
`DefaultEntryOptions.Expiration` en `AddCaching`).

**Solución propuesta:** dado que el TTL ya es corto y los datos son de referencia, es una
mitigación razonable tal cual. Si se quiere más robustez sin montar un outbox completo: publicar un
`INotification` post-commit (vía el interceptor del punto 5) que haga `RemoveByTagAsync` con
reintento, en vez de hacerlo inline en el handler.

## Plan de acción sugerido

Por esfuerzo/riesgo, de menor a mayor:

1. **#2** — ✅ implementado (2026-10-01): quitado el `SaveChangesAsync` interno de `UpdateRangeAsync`.
2. **#1** — ✅ implementado (2026-10-01): `EnableRetryOnFailure()` en Npgsql.
3. **#4** — ✅ implementado (2026-10-01): `UnitOfWorkBehavior` + `CacheInvalidationBehavior` en
   MediatR, marker interfaces `ICommand`/`IInvalidatesCache`, llamadas manuales quitadas de los 14
   handlers. Build y suite de tests (`dotnet test`, 19 + 9 casos) en verde tras el cambio.
4. **#6** y **#7** — pendientes, requieren diseño: tabla de idempotencia transaccional y/o
   interceptor + notificación post-commit.
5. **#5** — pendiente, outbox completo, solo si surge un caso de uso real que lo requiera (evitar
   sobre-diseñar antes de necesitarlo).
