# Flujo del chat del agente (`POST /api/v1/Agent/chat`)

## Contexto

El agente de IA (Semantic Kernel + tool-calling) permite al usuario preguntar en lenguaje natural
sobre sus propios portafolios. Corre contra un modelo servido por Ollama a través de su endpoint
OpenAI-compatible. Este documento resume el recorrido completo endpoint → respuesta y las
decisiones de diseño que no son obvias leyendo un solo archivo.

## Levantar Ollama en local

El repo incluye un `docker-compose.yml` en la raíz con un servicio `ollama` (puerto fijo `11434`) y
un servicio corto `ollama-pull` que descarga `qwen2.5:3b-instruct` automáticamente la primera vez:

```bash
docker compose up -d
```

`appsettings.Development.json` ya apunta `Ai:FastModel`/`Ai:ReasoningModel` a
`http://localhost:11434/v1`. La primera vez, el pull tarda unos minutos (~2 GB); se puede seguir con
`docker compose logs -f ollama-pull`. Los modelos quedan cacheados en el volumen `ollama_data`, así
que reinicios posteriores (`docker compose up -d` o un simple reboot del contenedor) no vuelven a
descargar nada.

Si ya tienes un Ollama propio corriendo (p. ej. un AppHost de .NET Aspire con
`builder.AddOllama("ollama")`, como se usó originalmente para este proyecto), no hace falta el
compose: basta con ajustar el `Endpoint` en `appsettings.Development.json` al puerto que exponga esa
otra instancia.

## Flujo paso a paso

**1. Endpoint** (`src/WebApi.MinimalAPI/Endpoints/Agent/AgentEndpoints.cs`)
Requiere JWT (`RequireAuthorization()`). Recibe `SendAgentMessageRequest { Message }` y lo envía vía
`ISender.Send(new SendAgentMessageCommand(request))`.

**2. Pipeline de MediatR**
`ValidationBehavior` (`Message` no vacío, máx. 2000 caracteres) → `LoggingBehavior` →
`CacheInvalidationBehavior` (no-op, el comando no implementa `IInvalidatesCache`) →
`UnitOfWorkBehavior` (no-op, no implementa `ICommand` — no hay nada que commitear en Postgres) →
Handler.

**3. `SendAgentMessageCommandHandler`** (`src/Application/Agent/Command/SendAgentMessageCommand.cs`)
Delega en `IAgentOrchestrator.SendMessageAsync(...)`. Atrapa `AgentLoopExhaustedException` →
`ErrorOr.Failure("Agent.LoopExhausted")`, y `AgentTimeoutException` →
`ErrorOr.Failure("Agent.Timeout")` (ambos se mapean a 400 vía `ToProblemResult()`).

**4. `AgentOrchestrator.SendMessageAsync`** (`src/Infrastructure/Agent/AgentOrchestrator.cs`)

a. `KernelFactory.CreateKernel()` (`src/Infrastructure/Agent/KernelFactory.cs`) arma un `Kernel`
   **nuevo por request** (nunca singleton — los plugins/filters llevan identidad del usuario
   autenticado, no se puede compartir entre requests):
   - Registra dos conectores de chat por `serviceId` (`FastUtilityModel`, `ReasoningModel`) contra
     el endpoint Ollama configurado en `Ai:FastModel`/`Ai:ReasoningModel`. **Hoy solo se usa
     `ReasoningModel`** — `FastUtilityModel` está plomeado para un futuro split de tareas
     (ej. decidir qué tool llamar con un modelo barato, redactar la respuesta final con uno más
     capaz) pero ningún código lo consume todavía.
   - Agrega `PortfolioAgentPlugin` (`src/Infrastructure/Agent/Plugins/PortfolioAgentPlugin.cs`)
     como plugin `"PortfolioOperations"`, resuelto del contenedor *scoped* (`ListMyPortfoliosAsync`,
     `GetPortfolioValueAsync`, `GetPortfolioHoldingsAsync`, `GetAssetPeakOrTroughDateAsync` como
     `[KernelFunction]`).
   - Engancha `LoopCappingFilter` (`IAutoFunctionInvocationFilter`, corta en
     `Ai:MaxAllowedToolLoops`) y `ToolInvocationAuditFilter` (`IFunctionInvocationFilter`, loguea
     usuario + argumentos + resultado serializado en JSON + duración de cada tool-call).

b. Arma el `ChatHistory`: system message (persona + guardrails — solo portafolios propios, nunca
   inventar ids/valores, las listas de holdings ya vienen ordenadas de mayor a menor valor, nunca
   agregar un símbolo de moneda porque las tools no devuelven ninguno, **más reglas explícitas de
   qué tool usar según si la pregunta menciona un activo específico y/o una fecha** — ver más abajo)
   + mensaje del usuario.

c. Llama a `chatService.GetChatMessageContentAsync(...)` con `OpenAIPromptExecutionSettings
   { Temperature = 0.1, FunctionChoiceBehavior = Auto() }` sobre el `HttpClient` nombrado
   `"InferenceClient"` (resiliencia Polly en
   `src/Infrastructure/Resilience/InferenceResilienceExtensions.cs`: timeout por intento =
   `Ai:RequestTimeoutSeconds`, timeout total = 2×, retry + circuit breaker para 429/5xx).

d. Bucle de auto-invocación de Semantic Kernel:
   - El modelo decide llamar una tool (ej. `GetPortfolioHoldingsAsync(portfolioId)`).
   - `PortfolioAgentPlugin` resuelve la identidad **solo** desde `ICurrentUserProvider` — las
     funciones nunca aceptan un `userId` como parámetro, ese es justo el vector de inyección que
     el patrón evita. Valida ownership del portafolio (`GetOwnedPortfolioOrNullAsync`), trae las
     entries vía `IPortfolioEntryRepository.GetByPortfolioIdAsync`, y calcula holdings con los
     métodos de dominio de `Portfolio` (`GetHoldingsAsOf`/`GetTotalValue`) — **desde el ledger de
     `PortfolioEntry`, sin consultar CoinGecko en vivo**. El precio reflejado es el último guardado,
     ya sea cargado a mano o por el último `POST /api/v1/Portfolios/{id}/refresh-prices` (máximo
     una vez por día calendario en `Europe/Madrid`).
   - El resultado vuelve al modelo, que genera la respuesta final en lenguaje natural.

e. Si se excede `Ai:MaxAllowedToolLoops` → `LoopCappingFilter` lanza `AgentLoopExhaustedException`.
   Si la llamada al modelo excede el timeout de Polly → se atrapa `TimeoutRejectedException` y se
   relanza como `AgentTimeoutException`.

f. Devuelve `AgentChatResult(Reply, ChatHistoryLength)`.

**5. Vuelta al endpoint**
`result.Match(Results.Ok, errors => errors.ToProblemResult())` → `200 { "reply": "..." }`, o error
mapeado a 400.

## Decisiones de diseño no obvias

- **Identidad nunca viene del LLM.** Todas las `[KernelFunction]` de `PortfolioAgentPlugin` derivan
  el usuario de `ICurrentUserProvider` (JWT), nunca de un parámetro — evita que el modelo (o un
  prompt-injection) pida datos de otro usuario pasando un id distinto.
- **El agente no calcula precios en vivo.** Responde con lo último guardado en el ledger, igual que
  el resto de la app (ver `PRODUCT.md`/`CLAUDE.md`: "valuation... computed from this ledger rather
  than a live price feed"). Si se necesita un valor actualizado, hay que llamar primero al refresh
  manual de precios.
- **Modelo local pequeño (3B) y fiabilidad.** Con `llama3.2:3b` se observó que el modelo podía
  alucinar datos (incluso inventando una unidad de moneda que ninguna tool devuelve) al tener que
  comparar varios valores dentro de un JSON para responder "cuál es el activo con más valor" —
  aunque la tool ya devuelve la lista pre-ordenada. Mitigado con `Temperature = 0.1`, un system
  prompt más explícito, y cambiando el modelo a `qwen2.5:3b-instruct` (mismo tamaño, mejor
  instruction-following). Si una pregunta puntual sigue fallando, la alternativa más robusta es
  agregar una tool server-side más específica (ej. `GetTopHoldingByValueAsync`) que no requiera que
  el modelo compare nada.
- **Timeouts de inferencia local.** `RequestTimeoutSeconds` (240s en desarrollo) existe porque la
  inferencia en CPU sin GPU puede tardar bastante más que el default de 10s de la librería —
  especialmente el segundo turno (sintetizar la respuesta final a partir del resultado de la tool).
- **Nombres de tool sin solapamiento léxico.** Con 4 tools en el menú, un modelo chico puede elegir
  la incorrecta por "parecido de palabras" en vez de por intención real — pasó con
  `GetPortfolioValueAsync` (valor total actual) vs. la tool de histórico por activo, que originalmente
  se llamaba `FindAssetExtremeValueAsync` y compartía la palabra "Value". Se renombró a
  `GetAssetPeakOrTroughDateAsync` y se reforzaron las descripciones de las tres tools de
  valor/holdings/histórico para que se excluyan explícitamente entre sí (ver reglas de ruteo en el
  system prompt de `AgentOrchestrator.cs`). Lección general: con modelos pequeños, cuantas más tools
  y parámetros existan, más superficie de error hay — tanto al elegir la tool como al rellenar sus
  argumentos.
- **Parámetros opcionales: usar `string?`, no `DateTime?`/otros value types nullable.** Agregar
  `DateTime? from = null, DateTime? to = null` a una `[KernelFunction]` rompía toda petición con un
  `HTTP 400` desde Ollama (`json: cannot unmarshal array into Go struct field ...properties.type of
  type string`): Semantic Kernel genera el JSON Schema de un parámetro `Nullable<T>` como
  `"type": ["string", "null"]` (sintaxis válida de JSON Schema 2020-12), pero el parser Go del
  backend de Ollama solo acepta `"type"` como string simple. Un `string?` no tiene este problema
  (los reference types no generan ese array de tipos) — se resuelve recibiendo la fecha como texto
  (`yyyy-MM-dd`) y parseándola a mano con `DateTime.TryParse` dentro del método.
