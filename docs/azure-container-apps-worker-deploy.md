# Despliegue del Worker (BackgroundService) a Azure Container Apps

## Contexto

`src/Worker` aloja el `PortfolioStatusReportBackgroundService` (ver
[`informe-mensual-por-email.md`](informe-mensual-por-email.md)) — un proceso sin HTTP que debe
correr de forma continua para que el envío mensual del informe se dispare solo. Se despliega como
un Container App más, en el mismo *environment* que backend y frontend, siguiendo exactamente el
mismo patrón que [`azure-container-apps-backend-deploy.md`](azure-container-apps-backend-deploy.md):
Docker Hub como registro + OIDC federado.

## Diferencias frente al backend

- **Imagen base `dotnet/runtime`, no `dotnet/aspnet`** (`src/Worker/Dockerfile`) — Worker no sirve
  HTTP, no necesita el runtime de ASP.NET Core. Tampoco expone ningún puerto (`EXPOSE`).
- **Sin ingress** en el Container App — no recibe tráfico externo.
- **Réplicas fijas, mín = máx = 1** — crítico, no opcional. El diseño actual (`LastStatusReportSentAt`
  por portafolio) no coordina múltiples instancias corriendo a la vez; dos réplicas podrían
  duplicar el envío del informe si ambas hacen *poll* casi al mismo tiempo. Nada de autoscaling.
- **Build context**: igual que el backend, debe ser `src/` (no la raíz ni `src/Worker/`), por las
  referencias `../` entre `Worker.csproj` y sus proyectos hermanos (`Application`, `Infrastructure`,
  etc.). Verificado localmente con `docker build -f src/Worker/Dockerfile src`.

## Pipeline

```
push a master (src/**)
  -> GitHub Actions: deploy-worker.yml
       -> docker build (context: src/, Dockerfile: src/Worker/Dockerfile)
       -> docker push a Docker Hub (tag :<sha> y :latest, imagen portfolio-worker)
       -> az login (OIDC) + az containerapp update --image <tag :sha>
  -> Azure Container App corre la nueva revisión
```

Archivos: `src/Worker/Dockerfile`, `.github/workflows/deploy-worker.yml` (copia de
`deploy-backend.yml` con la imagen/variable renombradas).

## Configuración pendiente (antes de que el workflow funcione)

Reutiliza los mismos secrets de identidad OIDC y Docker Hub que backend/frontend (mismo repo, mismo
Environment `production`, misma suscripción/resource group).

**Variable nueva a crear** (Settings → Secrets and variables → Actions → Variables):
- `WORKER_CONTAINER_APP_NAME` — nombre del Container App del Worker (distinto al del backend).

**Variables ya existentes que se reutilizan**: `DOCKERHUB_NAMESPACE`, `AZURE_RESOURCE_GROUP`.

## Creación del Container App (pendiente, manual la primera vez)

Aún no existe el recurso en Azure. Al crearlo (portal o `az containerapp create`):

- **Sin ingress** (`--ingress disabled` o simplemente no configurarlo).
- **`--min-replicas 1 --max-replicas 1`**.
- Variables de entorno con la convención de doble guion bajo de ASP.NET Core, usando los mismos
  valores que ya tienes en `dotnet user-secrets` para `WebApi.MinimalAPI` (comparten
  `UserSecretsId`, ver `informe-mensual-por-email.md`):
  `ConnectionStrings__Database`, `ConnectionStrings__CosmosDb`, `Jwt__Secret`, `Jwt__Issuer`,
  `Jwt__Audience`, `CoinGecko__ApiKey`, `Encryption__Key`, `Email__SmtpHost`, `Email__SenderEmail`,
  `Email__Username`, `Email__Password`.
- La primera imagen (`portfolio-worker`) debe existir en Docker Hub antes de que el Container App
  pueda apuntar a ella — lanzar el workflow con `workflow_dispatch` una vez para generarla.

## Pendiente / mejoras futuras

- Crear el recurso Container App en Azure y documentar aquí el primer despliegue real.
- Worker no tiene Serilog/Application Insights configurado — solo logging a consola. Si se quiere
  ver sus logs centralizados junto a los del backend, añadir `AddSerilogLogging()` en
  `src/Worker/Program.cs`.
