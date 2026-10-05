# Notas (Cosmos DB)

## Contexto

Feature nueva: una lista de "notas" personales (`category`, `title`, `content`, fecha de creación,
activa/inactiva) con CRUD completo y un widget de las 10 más recientes en el dashboard. A
diferencia del resto de la app, las notas no viven en Postgres — ya existía un contenedor de Azure
Cosmos DB (`DashboardDB/Notes`) con 36 notas reales creadas por otra aplicación, con el `content`
cifrado con AES. El objetivo era que esta API pase a ser la que lee/escribe ese contenedor,
descifrando siempre en el backend: el frontend nunca ve la clave ni cifra/descifra nada.

## Arquitectura

Seguimos el mismo patrón vertical-por-feature que `Exchanges`/`CryptoCurrencies` (CQRS con
MediatR, `ErrorOr<T>`, endpoints Minimal API agrupados por feature), con dos diferencias
deliberadas frente al resto de la app:

### 1. Sin `ICommand` / `IInvalidatesCache`

Los commands de `Note` (`src/Application/Notes/Command/`) no implementan `ICommand` ni
`IInvalidatesCache`. Esas interfaces existen para que `UnitOfWorkBehavior`/`CacheInvalidationBehavior`
(ver `docs/unit-of-work-transacciones.md`) sepan cuándo llamar a `AppDbContext.SaveChangesAsync()` y
cuándo invalidar `HybridCache` — ambos atados al mundo EF Core/Postgres. Cosmos DB no comparte ese
change tracker: cada llamada al SDK (`CreateItemAsync`, `UpsertItemAsync`, `DeleteItemAsync`) ya es
su propio commit atómico. Por eso `NoteRepository` (`src/Infrastructure/Notes/NoteRepository.cs`)
escribe directamente dentro de `AddAsync`/`UpdateAsync`/`DeleteAsync`, sin esperar a que el pipeline
de MediatR haga nada después del handler.

### 2. Partition key `/category`, no `/id`

El contenedor `Notes` ya existía con datos reales, con partition key `/category` — no se pudo
elegir `/id` (que habría sido más simple) porque no era nuestro contenedor a diseñar desde cero.
Esto obliga a varias decisiones en `NoteRepository`:

- **`GetByIdAsync(string id)`**: un `ReadItemAsync` por punto necesita la partition key de
  antemano, y los endpoints solo reciben el `id`. Se resuelve con una query cross-partition
  (`WHERE c.id = @id`) en vez de un point-read — más RU, pero irrelevante a esta escala.
- **`UpdateAsync(Note note, string originalCategory, ...)`**: si la categoría cambia, el documento
  cambia de partición. Cosmos no permite "mover" un item con upsert si la partition key value
  difiere de la original: hay que crear el documento bajo la partición nueva y borrar el de la
  partición vieja. Se hace en ese orden (crear primero, borrar después) para que, si el borrado
  falla, el resultado sea un duplicado inofensivo y no una nota perdida. El handler
  (`UpdateNoteCommandHandler`) captura `originalCategory` **antes** de mutar `note.Category` con
  el valor del request.
- **`DeleteAsync(string id, string category, ...)`**: recibe la categoría explícitamente porque
  `DeleteItemAsync` sí exige la partition key — el handler ya tiene el `Note` completo (de
  `GetByIdAsync`, usado para el check de existencia) y se la pasa.

`EnsureCosmosDbInitializedAsync` (`src/Infrastructure/DependencyInjection.cs`) usa
`partitionKeyPath: "/category"` al crear el contenedor si no existe, para que un entorno nuevo
(sin el contenedor ya provisionado) quede consistente con este diseño.

## Cifrado

- `IEncryptionService` (`src/Application/Common/Security/IEncryptionService.cs`) /
  `AesEncryptionService` (`src/Infrastructure/Common/Security/AesEncryptionService.cs`):
  AES-256-CBC, IV aleatorio de 16 bytes por cada `Encrypt`, devuelto como
  `Convert.ToBase64String(iv ++ ciphertext)`. `Decrypt` separa los primeros 16 bytes como IV y
  descifra el resto.
- La clave (32 bytes, base64) sale de `EncryptionOptions.Key` (sección `Encryption`,
  `src/Infrastructure/Common/Options/EncryptionOptions.cs`), solo por `dotnet user-secrets`, nunca
  en `appsettings.json`.
- El cifrado/descifrado ocurre **únicamente en `NoteRepository`** (frontera de Infrastructure):
  `Domain.Entities.Note.Content` siempre es texto plano para el resto de capas (Application,
  endpoints, frontend). `NoteDocument.Content` (el DTO que mapea el JSON de Cosmos,
  `src/Infrastructure/Notes/NoteDocument.cs`) es el único sitio donde vive el ciphertext.

## Endpoints

`src/WebApi.MinimalAPI/Endpoints/Notes/NoteEndpoints.cs`, bajo `/api/v1/Note/`, todos con
`RequireAuthorization()`:

| Método | Ruta | Uso |
|---|---|---|
| POST | `/` | Crear (con `IdempotencyFilter`, igual que el resto de creaciones) |
| GET | `/` | Lista paginada, filtros opcionales `category`/`isActive` |
| GET | `/latest?count=10` | Las N más recientes — usado por el widget del dashboard |
| GET | `/{id}` | Detalle |
| PUT | `/{id}` | Actualizar |
| DELETE | `/{id}` | Borrar |

## Configuración

- `appsettings.json`: `CosmosDb:DatabaseName` / `CosmosDb:ContainerName` (no sensibles). **No**
  duplicar esta sección en `appsettings.Development.json` — ya pasó una vez que un valor
  desactualizado ahí (`PortfolioManagement` en vez de `DashboardDB`) hizo que, en entorno
  Development, la app intentara crear una base de datos nueva en vez de usar la existente.
- Secretos, solo vía `dotnet user-secrets` (proyecto `WebApi.MinimalAPI`):
  - `ConnectionStrings:CosmosDb` — cadena de conexión real de Cosmos DB
    (`AccountEndpoint=...;AccountKey=...;`).
  - `Encryption:Key` — clave AES de 32 bytes en base64.

## Resiliencia del arranque

`EnsureCosmosDbInitializedAsync` (`Program.cs`, llamada tras `builder.Build()`) intenta crear la
base de datos/contenedor si no existen, pero **nunca** tumba el arranque de la API: atrapa
`CosmosException` y solo deja un `LogWarning`. Un problema de aprovisionamiento de Cosmos DB (p.
ej. el límite de throughput de la cuenta — ver más abajo) no tiene por qué afectar a
Portfolio/Exchange/etc., que no dependen de Cosmos DB en absoluto.

### Límite de throughput de la cuenta

Cuentas de Cosmos DB con el throughput total limitado (p. ej. el *free tier*, 1000 RU/s) pueden
rechazar la creación de un contenedor nuevo con un `BadRequest` tipo "this operation would have
increased the total throughput to 1400 RU/s". Como en este caso el contenedor ya existía
(`DashboardDB/Notes`), la causa real nunca fue intentar crear un contenedor de verdad, sino que la
app apuntaba a un nombre de base de datos equivocado y trataba de crear una base de datos nueva.
Si esto volviera a pasar en un entorno realmente nuevo (sin contenedor provisionado), las opciones
son: crear el contenedor a mano en el portal con **"Share throughput across containers in
database"** en vez de throughput dedicado, liberar RU/s de otro recurso de la cuenta, o subir el
límite total de la cuenta.

## Migración del cifrado legado

Las 36 notas que ya existían en `DashboardDB/Notes` venían de otra aplicación, con un esquema de
cifrado distinto al de `AesEncryptionService`:

```csharp
// Esquema legado (otra app, no vive en este repo)
_key = SHA256.ComputeHash(Encoding.UTF8.GetBytes(configKey)); // 32 bytes
_iv = _key[..16]; // IV FIJO, derivado de la propia clave — no aleatorio, no va en el ciphertext
// Aes.Create() con Mode/Padding por defecto (CBC/PKCS7); content = Convert.ToBase64String(ciphertext)
```

Diferencias clave frente al esquema nuevo: la clave no es la configurada directamente sino su hash
SHA-256, y el IV es **fijo** (siempre el mismo, derivado de la clave) en vez de aleatorio por
registro — una debilidad real de CBC (texto plano igual ⇒ ciphertext igual), y una de las razones
para migrar.

Se migró con una herramienta de un solo uso (no forma parte del repo — vivió en el directorio de
scratchpad de la sesión y se borró al terminar), con estas garantías:

1. **Dry run por defecto**: descifra cada nota con el esquema legado y muestra el resultado antes
   de escribir nada.
2. **Detección de texto plano**: 35 de las 36 notas descifraban bien con el esquema legado; 1
   (`"AZURE"`, categoría `Service`) no era base64 válido en absoluto — resultó estar en texto plano
   sin cifrar (seguramente introducida directamente en Data Explorer). El script la detecta por el
   `FormatException` del `Convert.FromBase64String` y la trata como ya-en-texto-plano en vez de
   fallar.
3. **Re-ejecutable sin duplicar trabajo**: antes de intentar el esquema legado, prueba a descifrar
   con el esquema **nuevo**; si tiene éxito, la nota ya está migrada y se salta.
4. **Confirmación explícita para escribir**: solo con `--apply`, y pide escribir `YES` en el
   prompt antes de tocar Cosmos DB.

Resultado verificado con una segunda pasada en dry-run tras la migración: 36/36 notas descifran
correctamente con `AesEncryptionService`, 0 pendientes, 0 errores. La clave legada se borró de los
user-secrets una vez confirmada la migración (`Encryption:LegacyKey`, que ya no existe).
