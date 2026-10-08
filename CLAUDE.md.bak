# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A personal cryptocurrency portfolio tracker: a .NET 10 Clean Architecture Minimal API (`src/`) backed by
PostgreSQL, with a separate Next.js frontend (`frontend/`, has its own `frontend/CLAUDE.md`). A single
user manually logs holdings (asset, exchange, quantity, price, timestamp) per portfolio; valuation and
history are computed from this ledger rather than a live price feed. See `PRODUCT.md` for product intent
and `ARCHITECTURE.md` for a diagram-level overview (Spanish).

## Commands

All `dotnet` commands below run from the repo root against `portfolio-management.slnx`, unless noted.

```bash
dotnet build                                   # build whole solution
dotnet test                                    # run all tests (Domain.Tests, Application.Tests)
dotnet test tests/Domain.Tests                 # run one test project
dotnet test --filter "FullyQualifiedName~PortfolioTests"   # run a single test class/method
dotnet format                                  # formats per .editorconfig; run before committing C# changes
```

Note: `tests/Architecture.Tests` exists as a scaffold but is not wired into the `.slnx` and has no tests
yet — don't rely on it running via `dotnet test` at the solution level.

Frontend (`cd frontend`):
```bash
npm run dev      # Next.js dev server
npm run build
npm run lint
```

EF Core migrations (run from `src/`, against the `Infrastructure` project, `WebApi.MinimalAPI` as startup):
```bash
dotnet ef migrations add <Name> --context AppDbContext --project Infrastructure --startup-project WebApi.MinimalAPI
dotnet ef database update --project Infrastructure --startup-project WebApi.MinimalAPI
```

A PostToolUse hook (`.claude/hooks/after-cs-edit.sh`) already runs `dotnet format` + `dotnet build` on the
owning project after every `.cs` file edit — no need to do this manually after each change.

## Conventions

- Target C# 12 / .NET 10.
- Primary constructors for DI; prefer `record` for immutable data.
- Favor explicit typing — only use `var` when the type is evident from the right-hand side.
- Types are `internal sealed` by default unless there's a reason otherwise.
- Prefer `Guid` for identifiers unless otherwise specified (note: current entities mostly use `long`).
- Use `is null` / `is not null` instead of `== null` / `!= null`.
- Prefer endpoint classes (`IEndpoint`-style static registration, see below) over controllers for new
  endpoints.
- Folder structure is per-feature (`Portfolios`, `Exchanges`, `CryptoCurrencies`, `PortfolioEntries`,
  `Users`), not per-type, and is mirrored across `Application`, `Contracts`, `Infrastructure`, and the
  `WebApi.MinimalAPI/Endpoints` layer — when adding a feature, touch the same subfolder name in each
  project.

## Architecture

Clean Architecture, five projects under `src/`, plus `Worker` for future background jobs (currently
unused — `Program.cs` is a stub):

- **`Domain`** — entities (`Portfolio`, `PortfolioEntry`, `CryptoCurrency`, `Exchange`, `User`) and
  business logic. Portfolio valuation (`GetPortfolioValueByDate`) and per-asset history
  (`GetCryptoCurrencyHistory`) live as methods on the `Portfolio` entity itself, not in handlers — this is
  the domain's key invariant: valuation logic is computed from the `PortfolioEntry` ledger, not recomputed
  by callers.
- **`Contracts`** — request/response DTOs per feature, referenced by both `Application` (commands/queries
  wrap a request) and `WebApi.MinimalAPI`.
- **`Application`** — CQRS commands/queries handled via MediatR, one feature folder per aggregate, each
  with `Command/`, `Query/`, `Interfaces/` (repository abstractions), and a `*Mapper.cs`. Handlers return
  `ErrorOr<TResponse>` (never throw for business failures). FluentValidation validators sit alongside each
  command/query.
- **`Infrastructure`** — EF Core (`AppDbContext`, Npgsql, snake_case naming convention), repository
  implementations, JWT/password security, the CoinGecko HTTP client, caching, health checks. All wired in
  `Infrastructure/DependencyInjection.cs`.
- **`WebApi.MinimalAPI`** — Minimal API endpoints, one static `*Endpoints.cs` class per feature under
  `Endpoints/<Feature>/`, registered from `Endpoints/Common/EndpointRegistrar.cs`. Endpoints map a request
  straight to `ISender.Send(command/query)` and translate `ErrorOr` results via
  `EndpointResultExtensions.ToProblemResult()`. API versioning via `Asp.Versioning` (`/api/v{version}/...`).

### Request pipeline

Endpoint → `IdempotencyFilter` (POST-create only) → MediatR pipeline → handler:

```
ValidationBehavior → LoggingBehavior → CacheInvalidationBehavior → UnitOfWorkBehavior → Handler
```

- `UnitOfWorkBehavior` calls `IUnitOfWork.SaveChangesAsync()` after the handler returns, but **only**
  for requests implementing the `ICommand` marker interface, and only if the result isn't an error.
  Handlers never call `SaveChangesAsync` themselves — they just stage changes via the repository
  (`Add`/`Update`/`RemoveRange`).
- `CacheInvalidationBehavior` runs for commands implementing `IInvalidatesCache` (exposes
  `CacheTagsToInvalidate`), invalidating `HybridCache` tags after commit. It's registered *before*
  `UnitOfWorkBehavior` so that, unwinding the pipeline, the commit happens first and cache invalidation
  happens after (see `docs/unit-of-work-transacciones.md` for the full reasoning).
- There is no explicit multi-statement transaction support (`IUnitOfWork` is just
  `SaveChangesAsync`) — don't add one speculatively; extend only when a handler genuinely needs several
  `SaveChanges` calls in one transaction.
- `IdempotencyFilter` (`WebApi.MinimalAPI/Idempotency/`) is an `IEndpointFilter`, not a MediatR behavior
  (deliberately, since `ErrorOr<T>` isn't generically serializable for caching). It requires an
  `Idempotency-Key` header on the four creation `POST`s and replays the cached response on retry with the
  same key+body hash; `422` on same key with a different body. Caching is in-process `HybridCache` only —
  not safe across multiple API instances yet (see `docs/idempotency-post-endpoints.md`).
- `Notes` is the one feature that doesn't go through this pipeline the same way: it's backed by Azure
  Cosmos DB, not Postgres/EF Core, so its commands don't implement `ICommand`/`IInvalidatesCache` —
  `NoteRepository` commits directly on every call since Cosmos has no shared `IUnitOfWork` to flush. See
  `docs/notes-cosmos-db.md` for the full design (partition key, encryption, data migration).

### Error handling

Handlers return `ErrorOr<TResponse>`. Endpoints map errors to HTTP via
`EndpointResultExtensions.ToProblemResult()` (`NotFound`→404, `Conflict`→409, `Unauthorized`→401,
`Forbidden`→403, else 400). Unhandled exceptions go through `GlobalExceptionHandler`
(`Endpoints/Common/`), registered via `app.UseExceptionHandler()`.

### Auth

JWT bearer, configured in `WebApi.MinimalAPI/DependencyInjection.cs`. `ICurrentUserProvider`
(`Security/CurrentUserProvider.cs`) exposes the authenticated user id to handlers for scoping queries
(every `Portfolio` belongs to exactly one `User`). Secrets (JWT signing key, connection strings, CoinGecko
API key) are managed via `dotnet user-secrets`, never `appsettings.json`.

### Tests

- `tests/Domain.Tests` — pure domain entity logic (valuation, history calculations).
- `tests/Application.Tests` — handler-level tests (currently covers `RefreshPortfolioPricesCommandHandler`).
- xUnit. Mirror the `Domain`/`Application` feature folder structure when adding new test files.
