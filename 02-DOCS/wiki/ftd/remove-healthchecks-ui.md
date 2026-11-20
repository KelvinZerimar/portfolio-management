---
author: kramirez
branch: fix/remove-healthchecks-ui
status: in-progress
---
# Quitar el dashboard de HealthChecks UI

## Intent

En cada arranque, el log registra `HealthCheck collector HostedService threw an error: Could not load
file or assembly 'IdentityModel, Version=5.2.0.0'`.

La causa: `AspNetCore.HealthChecks.UI` 9.0.0 se compiló contra `KubernetesClient` 15.0.1, que
dependía de `IdentityModel` 5.2.0. El commit `7190d30` fijó `KubernetesClient` 21.0.8, que ya no
depende de `IdentityModel`. Por eso la DLL no llega a `bin`, y el collector del dashboard falla.

El dashboard (`/healthchecks-ui`) no se usa. Quitarlo elimina el error de raíz. También elimina la
única razón para tener `KubernetesClient`.

## Scope

Dentro:
- Quitar `AddHealthChecksUI(...)` y `MapHealthChecksUI()`.
- Quitar los paquetes `AspNetCore.HealthChecks.UI` y `AspNetCore.HealthChecks.UI.InMemory.Storage`.
- Quitar `KubernetesClient` de los 4 `.csproj` (Infrastructure, WebApi.MinimalAPI, Worker,
  WebApi.MinimalAPI.Tests).
- Quitar los overrides de log `HealthChecks.UI.Core.Notifications` y
  `System.Net.Http.HttpClient.health-checks` (el `HttpClient` del collector) en los `appsettings`.
- Quitar de 3 factories de test el filtro de servicios `HealthChecks.UI`, que queda muerto.

Fuera:
- `/health` y `/health/json` se mantienen. `/health/json` sigue usando `UIResponseWriter` del paquete
  `AspNetCore.HealthChecks.UI.Client`, que no depende de Kubernetes.
- Los health checks de Npgsql y Cosmos DB no cambian.

## Checklist

- [x] Quitar el registro y el mapeo del dashboard. Prueba: `dotnet build` en verde.
- [x] Quitar los paquetes UI, UI.InMemory.Storage y KubernetesClient. Prueba: `dotnet list package
      --include-transitive` no muestra `KubernetesClient` ni `HealthChecks.UI` (salvo `.Client` y `.Core`).
- [x] Limpiar appsettings y factories de test. Prueba: `grep` sin coincidencias de `HealthChecks.UI`
      fuera de `UIResponseWriter`.
- [x] Suite completa. Prueba: `dotnet test` en verde.
- [x] La API arranca sin el error de `IdentityModel` y `/health/json` responde. Prueba: log de arranque
      y respuesta HTTP.

## Evidence

Fecha: 2026-11-20, worktree `.worktrees/fix-remove-healthchecks-ui`.

- **Build:** `dotnet build portfolio-management.slnx` → `Compilación correcta.` `dotnet format` sobre
  los `.cs` tocados no produjo más cambios.
- **Paquetes:** `dotnet list package --include-transitive` solo muestra `AspNetCore.HealthChecks.CosmosDb`,
  `.NpgSql`, `.UI.Client` y `.UI.Core`. Ni `KubernetesClient` ni `IdentityModel`.
- **Tests:** `dotnet test --filter "Category!=Manual"` → 502 / 502 correctos. Sin filtro: 503 / 506. Los
  3 fallos son `MafSpikeTests` con `Category=Manual`, que necesitan Ollama en `localhost:11434`. Ollama
  estaba apagado (errores de socket). No tienen relación con este cambio.
- **Arranque:** API del worktree en `http://localhost:5299` (entorno Development).
  - `GET /health` → `Healthy`.
  - `GET /health/json` → `{"status":"Healthy",...,"postgres":{...Healthy},"cosmosdb":{...Healthy}}`.
  - `GET /healthchecks-ui` → `404` (el dashboard ya no existe).
  - Log de arranque: 0 coincidencias de `IdentityModel` o `HealthCheck collector`, ningún evento Error/Fatal.

### Verify (gate de `02-DOCS/wiki/sdd/config.yaml`, ejecución fresca antes del commit)

Medido sobre `bba3f0a` + cambios sin commit (árbol dirty). Desde la última edición de código solo
cambiaron documentos.

- `dotnet format --verify-no-changes` → exit 0.
- `dotnet build` → exit 0, 0 advertencias.
- Tests unitarios (`--filter-not-trait Integration/Manual`) → 490 / 490.
- Tests de integración (`--filter-trait Integration`) → 12 / 12.
- **NO-APLICA** — frontend: no cambió `frontend/`.
- **NO EJECUTADA** — tests `Category=Manual`: necesitan Ollama, que estaba apagado. No tocan este cambio.

## Next

Commit en `fix/remove-healthchecks-ui` y pull request a `development`.
