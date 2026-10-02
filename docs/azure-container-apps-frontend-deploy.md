# Despliegue del frontend (Next.js) a Azure Container Apps

## Contexto

El frontend (`frontend/`) es una app Next.js 16 con render SSR (`next build` / `next start`), sin
export estático. El backend (`src/WebApi.MinimalAPI`) ya corría como contenedor en un Azure
Container App propio. Se decidió desplegar el frontend de la misma forma para mantener ambos
servicios en el mismo modelo de despliegue.

Decisiones tomadas:
- **Azure Container Apps** en vez de Azure Static Web Apps, porque el soporte SSR de Static Web
  Apps para Next.js es un adaptador híbrido más limitado, y aquí se usan rutas dinámicas
  (`/portfolio/[id]`) y la app depende de un backend propio vía Axios, no de rutas estáticas.
- **Docker Hub** (repositorio público) como registro de imágenes en vez de Azure Container
  Registry, por preferencia del equipo.
- **OIDC federado** (federated credentials de Microsoft Entra ID) para que GitHub Actions
  autentique contra Azure sin guardar un client secret de larga duración.

## Arquitectura del pipeline

```
push a master (frontend/**)
  -> GitHub Actions: deploy-frontend.yml
       -> docker build (multi-stage, output standalone) + build-arg NEXT_PUBLIC_API_URL
       -> docker push a Docker Hub (tag :<sha> y :latest)
       -> az login (OIDC) + az containerapp update --image <tag :sha>
  -> Azure Container App sirve la nueva revisión
```

Archivos relevantes:
- `frontend/Dockerfile` — build multi-stage, imagen final `node:22-alpine` no-root, sirve
  `.next/standalone/server.js` en el puerto `3000`.
- `frontend/.dockerignore`
- `frontend/next.config.ts` — `output: "standalone"` (necesario para que el Dockerfile solo
  copie lo imprescindible).
- `.github/workflows/deploy-frontend.yml`

## Pasos seguidos

1. Añadir `output: "standalone"` a `next.config.ts` y crear `Dockerfile` + `.dockerignore` en
   `frontend/`, siguiendo el patrón oficial de Next.js para Docker (etapas `deps` → `builder` →
   `runner`).
2. Crear el workflow `.github/workflows/deploy-frontend.yml`: build con `docker/build-push-action`
   (cache vía `type=gha`), login a Docker Hub con `docker/login-action`, y despliegue final con
   `az containerapp update` tras un `azure/login` por OIDC.
3. Generar manualmente la primera imagen (`docker build` + `docker push`) desde la máquina local,
   porque Azure exige una imagen existente al crear el Container App por primera vez.
4. Crear el Container App en Azure Portal apuntando a `docker.io/<namespace>/portfolio-frontend:latest`.
5. Configurar la identidad OIDC en Azure AD:
   - `az ad app create` (App Registration) + `az ad sp create` (Service Principal)
   - `az role assignment create` con rol `Container Apps Contributor` sobre el resource group
   - `az ad app federated-credential create`, con subject ligado al **Environment** de GitHub
     (`repo:<owner>/<repo>:environment:production`) en vez de a una rama, para que funcione tanto
     con el push a `master` como con `workflow_dispatch` manual.
6. Configurar en GitHub (repo → Settings → Secrets and variables → Actions):
   - Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `DOCKERHUB_USERNAME`,
     `DOCKERHUB_TOKEN`
   - Variables: `DOCKERHUB_NAMESPACE`, `CONTAINER_APP_NAME`, `AZURE_RESOURCE_GROUP`,
     `NEXT_PUBLIC_API_URL`
   - Environment `production` creado en Settings → Environments
7. Commit + push del workflow y Dockerfile a `development`, PR a `master`, merge. El push a
   `master` disparó el primer run real del workflow.
8. Iterar sobre los fallos del primer run (ver sección siguiente) hasta obtener un deploy exitoso.
9. Ajustar el **target port** del Container App a `3000` (coincidiendo con el puerto del Dockerfile).
10. Ajustar `NEXT_PUBLIC_API_URL` para incluir el prefijo `/api/v1` que usa el backend.
11. Añadir el origin del frontend a la política CORS del backend (`Program.cs`).

## Problemas encontrados y solución

| # | Síntoma | Causa | Solución |
|---|---------|-------|----------|
| 1 | `buildx failed: "/app/public": not found` | `frontend/public` existía localmente pero estaba **vacía**; Git no versiona directorios vacíos, así que no existía tras el checkout en el runner de CI. | Añadir `frontend/public/.gitkeep` para que el directorio quede trackeado. |
| 2 | Paso `Azure logout` fallaba con `ERROR: There are no active accounts` | Síntoma secundario: el paso real que fallaba era `Azure login (OIDC)` (más abajo, causas #4); `Azure logout` tiene `if: always()` y se ejecutaba igualmente sin sesión que cerrar. | Resuelto al arreglar la causa raíz del login. |
| 3 | `401 Unauthorized: access token has insufficient scopes` al hacer `docker push` | El Access Token de Docker Hub (`DOCKERHUB_TOKEN`) se generó con permisos **Read-only**. | Generar un nuevo Access Token en Docker Hub con permisos **Read & Write** y actualizar el secret. |
| 4 | `AADSTS700213: No matching federated identity record found for presented assertion subject 'repo:<owner>@<ownerId>/<repo>@<repoId>:environment:production'` | GitHub cambió el formato del claim `sub` de los tokens OIDC para incluir **IDs inmutables** de owner/repo (mitigación de seguridad ante renombrados de repo/org). La federated credential se había creado con el formato antiguo (`repo:<owner>/<repo>:environment:production`, sin los IDs). | `az ad app federated-credential update` con el subject exacto que reporta el error (incluyendo los `@<id>`). |
| 5 | La URL del Container App cargaba pero mostraba **"stream timeout"** | El **target port** del Ingress del Container App estaba en `80` (valor por defecto del portal), pero el servidor Next.js dentro del contenedor escucha en `3000` (`EXPOSE 3000` / `ENV PORT=3000` del Dockerfile). | `az containerapp ingress update --target-port 3000` (o editarlo desde Portal → Ingress). |
| 6 | Las llamadas a la API devolvían `404` | `NEXT_PUBLIC_API_URL` no incluía el prefijo `/api/v1` que usan todos los endpoints del backend (`MapGroup("/api/v{version:apiVersion}/...")`). Al ser una variable **build-time** de Next.js, no basta con cambiarla en el Container App: hay que reconstruir la imagen. | Corregir la variable en GitHub y volver a disparar el workflow. |
| 7 | Peticiones bloqueadas por CORS desde la URL real del frontend | El backend solo permitía orígenes `localhost` en `AddCors` (`Program.cs`). | Añadir la URL pública del Container App del frontend a `WithOrigins`, y volver a publicar el backend. |

## Comandos de Docker utilizados (primera imagen manual)

Antes de crear el Container App en Azure, este hacía falta una imagen ya existente en Docker Hub
(Azure pide "Image and tag" al crearlo). Se generó manualmente desde la máquina local, con Docker
Desktop corriendo. Sustituir `<tu-usuario>` y la URL del backend.

**1. Login en Docker Hub:**
```powershell
docker login -u <tu-usuario>
```

**2. Build de la imagen** (el build-arg queda incrustado en el bundle del cliente en tiempo de
build, ver problema #6 de la tabla si se omite o queda incompleto):
```powershell
docker build --build-arg NEXT_PUBLIC_API_URL=https://portfolio-mgmt.redstone-57b2779e.spaincentral.azurecontainerapps.io/api/v1 -t <tu-usuario>/portfolio-frontend:latest ./frontend
```

**3. Push a Docker Hub:**
```powershell
docker push <tu-usuario>/portfolio-frontend:latest
```

Tras este push, el Container App se creó en Azure Portal apuntando a
`docker.io/<tu-usuario>/portfolio-frontend:latest` (Image source: Docker Hub or other registries,
registro público, sin credenciales). Las imágenes posteriores (una por cada deploy) ya las genera
y publica automáticamente el workflow `deploy-frontend.yml`, con tag `:<git-sha>` en vez de
`:latest`.

## Comandos de Azure CLI utilizados

Ejecutados desde Azure Cloud Shell (modo PowerShell). Sustituir los valores entre `<...>`.

**1. Obtener subscription y tenant:**
```powershell
az account show --query "{subscriptionId:id, tenantId:tenantId}" -o jsonc
```

**2. Crear el App Registration + Service Principal (identidad OIDC):**
```powershell
$APP_ID = az ad app create --display-name "github-actions-portfolio-frontend" --query appId -o tsv
az ad sp create --id $APP_ID
Write-Host "AZURE_CLIENT_ID=$APP_ID"
```

**3. Asignar el rol sobre el resource group del Container App:**
```powershell
az role assignment create `
  --assignee $APP_ID `
  --role "Container Apps Contributor" `
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>"
```

**4. Crear la federated credential (subject ligado al Environment `production`):**
```powershell
$params = @'
{
  "name": "github-actions-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}
'@

az ad app federated-credential create --id $APP_ID --parameters $params
```

**5. Corregir el subject tras el cambio de GitHub a IDs inmutables** (ver problema #4 de la tabla
anterior; el subject exacto se copia del mensaje de error `AADSTS700213`):
```powershell
az ad app federated-credential list --id $APP_ID -o table

$params = @'
{
  "name": "github-actions-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>@<ownerId>/<repo>@<repoId>:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}
'@

az ad app federated-credential update --id $APP_ID --federated-credential-id "github-actions-production" --parameters $params
```

**6. Corregir el target port del Ingress** (ver problema #5 de la tabla anterior):
```powershell
az containerapp ingress update `
  --name <CONTAINER_APP_NAME> `
  --resource-group <AZURE_RESOURCE_GROUP> `
  --target-port 3000
```

**7. Despliegue de la imagen nueva** (este comando lo ejecuta el propio workflow de GitHub
Actions en cada run, no manualmente — se incluye aquí como referencia de lo que hace
`deploy-frontend.yml` tras el login OIDC):
```bash
az containerapp update \
  --name <CONTAINER_APP_NAME> \
  --resource-group <AZURE_RESOURCE_GROUP> \
  --image <DOCKERHUB_NAMESPACE>/portfolio-frontend:<git-sha>
```

## Configuración de referencia (GitHub Actions)

**Secrets** (Settings → Secrets and variables → Actions → Secrets):
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` — identidad OIDC del App Registration
- `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` — token con permisos **Read & Write**

**Variables** (misma sección, pestaña Variables):
- `DOCKERHUB_NAMESPACE` — usuario/namespace de Docker Hub
- `CONTAINER_APP_NAME`, `AZURE_RESOURCE_GROUP` — Container App del frontend
- `NEXT_PUBLIC_API_URL` — URL pública del backend **incluyendo** `/api/v1`

**Environment**: `production` (requerido para que el subject de la federated credential
`repo:<owner>/<repo>:environment:production` coincida).

## Pendiente / mejoras futuras

- Dominio personalizado y certificado TLS para las URLs de `*.azurecontainerapps.io`.
- Revisar reglas de autoscaling (min/max réplicas) de ambos Container Apps para producción.
