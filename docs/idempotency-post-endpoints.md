# Idempotencia en endpoints POST de creación

## Contexto

Los clientes que crean recursos (`Exchange`, `Portfolio`, `PortfolioEntry`, `CryptoCurrency`) vía
`POST` pueden reintentar la solicitud ante timeouts o cortes de red y crear duplicados por
accidente, ya que `POST` no tiene garantía de idempotencia (a diferencia de `PUT`/`DELETE` en esta
API, que ya actúan sobre un id concreto y son naturalmente idempotentes). Se agregó soporte para el
header `Idempotency-Key`, de modo que un reintento con la misma clave devuelve la respuesta
original en vez de crear un segundo recurso.

## Diseño

Se implementó como un `IEndpointFilter` (el primero en el proyecto), no como un `IPipelineBehavior`
de MediatR, porque los comandos devuelven `ErrorOr<TResponse>` sin una forma de serialización
estable para cachear genéricamente. En cambio, todo resultado HTTP producido por estos endpoints
(`Ok<T>`, `Conflict`, `NotFound`, `BadRequest`, ...) implementa `IStatusCodeHttpResult`, y los que
tienen cuerpo también implementan `IValueHttpResult`, lo que permite capturar "status code + valor"
de forma genérica sin hijackear el stream de respuesta.

**Archivos nuevos** (`src/WebApi.MinimalAPI/Idempotency/`):
- `IdempotencyFilter.cs`: exige el header, calcula el hash del body, y usa `HybridCache` (ya
  registrado en `Infrastructure/DependencyInjection.cs`) para cachear/reproducir la respuesta.
- `CachedIdempotentResult.cs`: forma serializable de "status code + valor + hash del body" que se
  guarda en caché.

**Aplicado únicamente a los 4 `POST` de creación** (`ExchangeEndpoints.cs`, `PortfolioEndPoints.cs`,
`PortfolioEntryEndpoints.cs`, `CryptoCurrencyEndpoints.cs`) vía `.AddEndpointFilter<IdempotencyFilter>()`.
Quedan fuera a propósito `POST register` (un reintento ya no puede duplicar un usuario por el
constraint único de email) y `PUT`/`DELETE`/`refresh-prices` (ya son idempotentes por diseño).

### Semántica
- Sin header `Idempotency-Key` → `400` inmediato, el handler nunca se ejecuta.
- Primera solicitud con una clave → el handler corre normalmente; la respuesta (éxito **o**
  error, ej. `409 Conflict`) se cachea 24h y se reproduce igual ante un reintento con la misma
  clave.
- Reintento con la misma clave pero **body distinto** → `422 Unprocessable Entity`, sin reproducir
  la respuesta cacheada ni ejecutar el handler. Se detecta comparando un hash SHA-256 del body
  (serializado a JSON) contra el hash guardado junto con la respuesta original.
- Solicitudes concurrentes con la misma clave → `HybridCache.GetOrCreateAsync` colapsa la ejecución
  en una sola (protección de "stampede"), el resto espera y recibe el mismo resultado.

Validado manualmente end-to-end (registro de usuario, login, `POST /Exchange` repetido con y sin
header, con la misma clave y body distinto, y con clave distinta) contra la base de Supabase de
desarrollo, limpiando los datos de prueba al finalizar.

## Limitación conocida: solo funciona en una sola instancia

> Pendiente si el API llega a desplegarse en más de una instancia/pod: `HybridCache` está
> registrado **solo in-process** (sin `Redis`/`IDistributedCache` como L2), ver el comentario en
> `AddCaching()` en `src/Infrastructure/DependencyInjection.cs`. Esto afecta a la idempotencia de
> dos formas:
>
> 1. **La caché no se comparte entre instancias**: una clave usada en la instancia A no es visible
>    para la instancia B, así que dos reintentos que caen en instancias distintas podrían ambos
>    ejecutar el handler y crear un duplicado.
> 2. **La protección de concurrencia (`GetOrCreateAsync`) tampoco es distribuida**: solo colapsa
>    solicitudes simultáneas dentro de la misma instancia, no entre instancias.
>
> La solución, cuando aplique, es agregar Redis como backing store de `HybridCache` (es un cambio
> de configuración: `HybridCache` lo recoge automáticamente al registrar un
> `IDistributedCache`/`IHybridCacheSerializer` respaldado por Redis, sin tocar `IdempotencyFilter`).
> Redis también permitiría, si hiciera falta, un lock distribuido explícito para el caso límite de
> alta concurrencia entre instancias.
