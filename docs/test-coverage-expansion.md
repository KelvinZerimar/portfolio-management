# Ampliación de la cobertura de tests (Application + WebApi.MinimalAPI)

## Contexto

Un análisis de cobertura (`test-engineer`, apoyado en `dotnet test` + lectura directa del código)
encontró que `Application.Tests` solo cubría un handler (`RefreshPortfolioPricesCommandHandler`, 9
tests) de los 33 que existen, además de 0 tests para validadores de FluentValidation, mappers,
pipeline behaviors (`UnitOfWorkBehavior`, `CacheInvalidationBehavior`) e `IdempotencyFilter`. Este
documento resume el trabajo hecho para cerrar esa brecha.

**Antes**: `Domain.Tests` 23 tests, `Application.Tests` 9 tests. **Después**: `Domain.Tests` 23,
`Application.Tests` 106, `WebApi.MinimalAPI.Tests` 5 (proyecto nuevo) — 134 tests en total, `dotnet
test` en verde en los tres proyectos.

## Qué se añadió, por capa

- **Handlers de `Users`**: `RegisterUserCommandHandler` (email nuevo vs. duplicado → `409`) y
  `LoginUserQueryHandler` (credenciales válidas, email inexistente, password incorrecta → `401`),
  sin cobertura previa pese a ser la superficie de autenticación.
- **Handlers de `PortfolioEntries`**: `Create`/`Update`/`Delete`, incluyendo las validaciones
  `NotFound` encadenadas (`Portfolio` → `CryptoCurrency` → `Exchange`) y que cada fallo corta la
  cadena antes de tocar el siguiente repositorio.
- **Resto de handlers de `Portfolios`**: `Create`/`Update`/`Delete` y los 6 query handlers
  (`GetById`, `GetPortfolios` paginado, `GetValue`, `GetHoldings`, `GetHistory`, `GetAllocation`),
  incluyendo casos borde como división por cero con 0 holdings y agrupación por activo/exchange.
- **Pipeline behaviors** (`src/Application/Common/Behaviors/`): `UnitOfWorkBehavior` (solo hace
  commit si la respuesta no es error) y `CacheInvalidationBehavior` (solo invalida tags si la
  respuesta no es error) — protegen la invariante "commit antes que invalidación de caché"
  documentada en `docs/unit-of-work-transacciones.md`.
- **Validadores de FluentValidation** de los 4 comandos de creación (`CreatePortfolio`,
  `CreatePortfolioEntry`, `CreateExchange`, `CreateCryptoCurrency`): límites de longitud, campos
  requeridos, `GreaterThan`/`GreaterThanOrEqualTo` en cantidades y precios.
- **Mappers** (`*Mapper.cs` de las 6 features): un test por método de mapeo que verifica que todos
  los campos de la entidad llegan al DTO de respuesta, para detectar un campo olvidado en un
  refactor futuro.
- **`IdempotencyFilter`** (`src/WebApi.MinimalAPI/Idempotency/`): proyecto de test nuevo,
  `tests/WebApi.MinimalAPI.Tests/`, por ser el primer componente que vive en la capa de endpoints.
  Usa una instancia real de `HybridCache` (in-memory, vía `AddHybridCache()`) en vez de un mock,
  porque simular su semántica de "colapsar llamadas concurrentes + serializar el valor cacheado"
  con un substitute habría sido más frágil que usar la implementación real. Cubre: header ausente
  → `400` sin invocar el handler; primera llamada con clave → invoca el handler; misma clave +
  mismo body → responde con el resultado cacheado sin reinvocar; misma clave + body distinto →
  `422`; misma clave en paths distintos → tratadas como solicitudes independientes.

## Decisiones técnicas no obvias

- **`Portfolio.Id` es de solo lectura** (`private init` en `Entity`), así que dos entidades creadas
  en memoria con `Portfolio.Create(...)` siempre tienen `Id = 0`. Para el test de
  "`UpdatePortfolioCommandHandler` con nombre que choca con **otro** portfolio" hacía falta
  distinguirlas por id; se añadió un helper `SetId` por reflexión en
  `UpdatePortfolioCommandHandlerTests.cs` (mismo mecanismo que usa EF Core al materializar claves
  `private init`), en vez de añadir un setter público solo para tests.
- **Borde horario de `RefreshPortfolioPricesCommandHandler`**: el handler decide "ya se refrescó
  hoy" convirtiendo a hora de Madrid, pero usaba `DateTime.UtcNow` directamente, lo que hacía ese
  test potencialmente *flaky* si corría justo en el instante de medianoche española. Se refactorizó
  el handler para recibir `TimeProvider` por inyección de dependencias (registrado como singleton
  en `Application/DependencyInjection.cs`), y la suite de tests pasó a usar
  `Microsoft.Extensions.TimeProvider.Testing.FakeTimeProvider`. Se añadieron dos tests de borde que
  fijan instantes a milisegundos de la medianoche en Madrid (uno en invierno, cruzando el año) para
  demostrar que la lógica depende de verdad de la conversión a hora local y no de la fecha UTC.
- **`IdempotencyFilter` se probó con HTTP simulado, no mockeado**: se construye un
  `EndpointFilterInvocationContext` real vía `EndpointFilterInvocationContext.Create(...)` y un
  `DefaultHttpContext`, en vez de extraer la lógica a algo más fácil de testear — mantiene el test
  fiel al contrato real de `IEndpointFilter`. Las aserciones comparan el **JSON serializado** del
  valor de respuesta, no su tipo CLR ni su referencia, porque `HybridCache` serializa/deserializa
  internamente incluso en modo puramente in-process (confirmado empíricamente): el valor que sale
  de la caché es siempre un `JsonElement`, nunca el objeto original.

## Pendiente

- **Primer test de integración real** (`WebApplicationFactory` + `Testcontainers.PostgreSql`): no
  se implementó porque Docker no estaba disponible en el entorno de desarrollo donde se hizo este
  trabajo, y no habría sido posible verificar que realmente pasa. Sigue siendo el hueco estructural
  más grande: hoy ningún test ejercita el stack HTTP completo (routing, versionado de API, EF Core
  contra Postgres real, `GlobalExceptionHandler`).
