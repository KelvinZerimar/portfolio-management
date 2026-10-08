# Informe mensual de estado del portafolio por email

## Contexto

Requerimiento: enviar por email un resumen periódico del estado de cada portafolio. Se evaluó
también incluir un análisis del mercado del mes anterior con predicciones de subida/bajada citando
fuentes de internet, pero esa parte quedó fuera de alcance a propósito (Fase 1 = solo el informe de
status): una predicción no tiene una "fuente de internet" verificable que la respalde como dato, y
presentarla con el mismo nivel de confianza que el valor real del portafolio sería engañoso para un
único usuario que toma decisiones con esta información. Puede retomarse como Fase 2 si se decide
asumir ese riesgo explícitamente (con disclaimer, y separada del dato de valuación real).

## Arquitectura

### Domain (`src/Domain/Entities/`)

- `Portfolio.LastStatusReportSentAt` (`DateTime?`): mismo rol que `LastPriceRefreshAt` — gate
  anti-duplicados, no debe enviarse dos veces el mismo informe el mismo día local.
- `Portfolio.GetStatusReport(entries, periodStart, periodEnd)`: reutiliza
  `GetPortfolioValueByDate`/`GetHoldingsAsOf` (no recalcula valuación por su cuenta, respetando la
  invariante del dominio) y devuelve un `PortfolioStatusReport` (`PortfolioStatusReport.cs`): valor
  al inicio/fin del período, variación absoluta y porcentual (`null` si el valor de inicio era 0,
  para no dividir entre cero), y breakdown de holdings de cierre por activo.

### Application (`src/Application/Reports/`)

- `Interfaces/IEmailSender.cs`: abstracción mínima (`SendAsync(to, subject, htmlBody, ct)`), para
  que Infrastructure sea la única capa que conoce MailKit.
- `PortfolioStatusReportEmailTemplate.cs`: arma asunto y cuerpo HTML (es-ES, formato EUR), con un
  disclaimer explícito de que el informe es automático y no constituye asesoramiento financiero.
- `PortfolioStatusReportPeriod.cs`: helper compartido — "el mes natural anterior completo" relativo
  a una fecha de referencia. Lo usan tanto el job programado como el endpoint manual, para que
  ambos reporten el mismo período por defecto.
- `Command/SendPortfolioStatusReportCommandHandler.cs`: comando **de confianza**, sin chequeo de
  propietario — implementa `ICommand` (pasa por `UnitOfWorkBehavior`, que persiste
  `LastStatusReportSentAt` tras el envío). Lo invoca únicamente el `BackgroundService` del Worker,
  que no tiene un usuario autenticado de quien colgar la verificación.
- `Command/TriggerPortfolioStatusReportCommandHandler.cs`: comando **de cara a HTTP**, con el mismo
  patrón de chequeo de propiedad que `GetPortfolioValueQueryHandler` (portafolio inexistente o de
  otro usuario → `NotFound`). Si pasa el chequeo, delega en `SendPortfolioStatusReportCommand` vía
  `ISender.Send` anidado (patrón normal en MediatR: cada `Send` corre su propio pipeline completo,
  comparten el mismo `AppDbContext` scoped de la petición HTTP). No implementa `ICommand` él mismo —
  no toca repositorios directamente, solo delega.

### Infrastructure (`src/Infrastructure/Reports/`)

- `MailKitEmailSender.cs`: implementación SMTP de `IEmailSender`.
- `PortfolioStatusReportBackgroundService.cs`: `BackgroundService` que sondea cada
  `ReportSchedulingOptions.PollIntervalMinutes` (por defecto 60) y, si el día local
  (Europe/Madrid) coincide con `ReportSchedulingOptions.DayOfMonth` (por defecto 1), recorre
  **todos** los portafolios (`IPortfolioRepository.GetAllAsync`, sin filtrar por usuario — no hay
  "usuario actual" en un proceso en background) y envía el informe del mes anterior a cada uno que
  no lo haya recibido ya ese mismo día local.
- `ReportsDependencyInjection.cs` (`AddReportScheduling`): registra el hosted service y hace el
  *bind* estricto (`GetRequiredSection` + `ValidateDataAnnotations().ValidateOnStart()`) de
  `EmailOptions` — exclusivo del proceso Worker.

### Por qué `IEmailSender`/`EmailOptions` también viven en el `AddInfrastructure` compartido

MediatR registra **todos** los handlers del ensamblado `Application` en cualquier proceso que llame
a `AddApplication` — incluida la API, aunque esta nunca despacha `SendPortfolioStatusReportCommand`.
Dejar `IEmailSender` registrado *solo* en `AddReportScheduling` (exclusivo del Worker) rompía la
validación estructural de ASP.NET Core al construir el host (`services.BuildServiceProvider`
valida que *todo* lo registrado sea resoluble, no solo lo que se usa) — esto se detectó porque
`dotnet ef migrations add` falló con *"Unable to resolve service for type
'Application.Reports.Interfaces.IEmailSender'"* al intentar construir `AppDbContext` a través del
host completo de `WebApi.MinimalAPI`.

Solución: `IEmailSender → MailKitEmailSender` se registra en el `AddAdapters()` compartido (ambos
procesos lo resuelven estructuralmente), y `EmailOptions` se enlaza con valores reales también en
el `AddConfigurationOptions()` compartido (sin `GetRequiredSection`/`ValidateOnStart`, para no
obligar a la API a tener credenciales SMTP solo para arrancar). Solo `AddReportScheduling` (Worker)
exige y valida que `Email:*` esté realmente configurado antes de arrancar el sweep.

Este mismo enlace compartido fue necesario para que el endpoint manual (que corre en el proceso de
la API) funcionara: sin él, `IOptions<EmailOptions>` resolvía siempre a una instancia vacía por
defecto (`SmtpHost` vacío) independientemente de lo que hubiera en `user-secrets`.

### Worker (`src/Worker/`)

Pasó de ser un stub (`Console.WriteLine("Hello, World!")`) a un host real:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddApplication(builder.Configuration)
    .AddReportScheduling(builder.Configuration);
var host = builder.Build();
await host.RunAsync();
```

Comparte el mismo `UserSecretsId` que `WebApi.MinimalAPI` (en vez de uno propio), para no duplicar
en dos sitios los secretos que ya necesita vía `AddInfrastructure` (cadena de conexión, `Jwt`,
`CoinGecko`, `Encryption`) más los nuevos de `Email`.

## Endpoint manual

`POST /api/v1/Portfolio/{id}/send-status-report` (autenticado, mismo grupo que el resto de
`Portfolio`), para probar sin esperar al día programado del mes:

- Query params opcionales `periodStart`/`periodEnd` — si se omiten, usa el mes natural anterior
  completo (igual que el job programado); útiles para forzar un rango con datos si el portafolio es
  demasiado nuevo para tener "mes anterior".
- `204` si se envió, `404` si el portafolio no existe o no es tuyo.
- No lleva `IdempotencyFilter` (no es un POST de creación — mismo criterio que `refresh-prices`).

## Envío SMTP (MailKit)

`MailKitEmailSender` conecta con `SecureSocketOptions.Auto` (no un booleano `UseSsl` propio) para
que MailKit negocie automáticamente STARTTLS en el puerto 587 o SSL implícito en el 465 — un
`UseSsl=true` fijo con puerto 587 produce `SslHandshakeException` (587 es texto plano hasta que se
negocia STARTTLS; SSL directo ahí falla).

## Configuración

Vía `dotnet user-secrets` (proyecto `WebApi.MinimalAPI`, compartido con `Worker`):

```
Email:SmtpHost
Email:SmtpPort       (opcional, default 587)
Email:Username
Email:Password
Email:SenderEmail
Email:SenderName     (opcional, default "Portfolio Tracker")
```

En `appsettings.json` del Worker (no sensible):

```json
"ReportScheduling": {
  "DayOfMonth": 1,
  "PollIntervalMinutes": 60
}
```

**Destinatario**: no es un valor de configuración — es el `Email` del `User` dueño del portafolio
(la cuenta con la que se inicia sesión en la app), resuelto vía `IUserRepository.GetByIdAsync`.

## Migración

`AddLastStatusReportSentAtToPortfolio` — añade `last_status_report_sent_at` (`timestamp with time
zone`, *nullable*) a `Portfolios`, mismo patrón que `AddLastPriceRefreshAtToPortfolio`.

## Limitaciones conocidas

> **El envío automático depende de que el proceso Worker corra de forma continua.** No hay cron
> externo ni trigger disparado por la API — es un `BackgroundService` con `PeriodicTimer` dentro de
> `dotnet run --project src/Worker` (o su despliegue como servicio/contenedor). Si el Worker no
> está corriendo, el único envío posible es el endpoint manual. El despliegue continuo del Worker
> quedó pendiente de decidir (servicio de Windows, contenedor, Azure Container App, etc.).

> **Reintentos**: si `SendPortfolioStatusReportCommand` falla (ej. SMTP caído), `LastStatusReportSentAt`
> no se actualiza — el siguiente *poll* del mismo día local lo volverá a intentar automáticamente.
> No hay backoff ni límite de reintentos dentro del mismo día.

> **Una sola instancia**: igual que `IdempotencyFilter` (ver `docs/idempotency-post-endpoints.md`),
> nada en este diseño coordina múltiples instancias del Worker corriendo a la vez — no debería
> haber más de una en producción, o cada portafolio podría recibir el informe duplicado si dos
> instancias hacen *poll* casi al mismo tiempo antes de que la primera persista
> `LastStatusReportSentAt`.

## Tests

- `tests/Domain.Tests/Entities/PortfolioTests.cs`: `GetStatusReport` (variación normal y caso de
  valor de inicio en cero → `ChangePercentage` nulo).
- `tests/Application.Tests/Reports/Command/`: `SendPortfolioStatusReportCommandHandlerTests` y
  `TriggerPortfolioStatusReportCommandHandlerTests` (NotFound por portafolio inexistente/ajeno,
  envío exitoso con sello de fecha, período por defecto vs. explícito).
