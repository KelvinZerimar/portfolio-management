# Despliegue del backend (ASP.NET Core) a Azure Container Apps

## Contexto

El backend (`src/WebApi.MinimalAPI`) ya corre como contenedor en un Azure Container App propio,
publicado manualmente desde Visual Studio. Este workflow automatiza ese despliegue siguiendo el
mismo patrón que [`deploy-frontend.yml`](azure-container-apps-frontend-deploy.md): Docker Hub como
registro + OIDC federado para autenticar contra Azure.

## Arquitectura del pipeline

```
push a master (src/**)
  -> GitHub Actions: deploy-backend.yml
       -> docker build (context: src/, Dockerfile: src/WebApi.MinimalAPI/Dockerfile)
       -> docker push a Docker Hub (tag :<sha> y :latest)
       -> az login (OIDC) + az containerapp update --image <tag :sha>
  -> Azure Container App sirve la nueva revisión
```

Archivos relevantes:
- `src/WebApi.MinimalAPI/Dockerfile` — build multi-stage (`sdk:10.0` → `aspnet:10.0`). El
  **build context debe ser `src/`**, no la raíz del repo ni `src/WebApi.MinimalAPI/`, porque el
  Dockerfile copia `WebApi.MinimalAPI/WebApi.MinimalAPI.csproj` de forma relativa y el proyecto
  referencia a sus hermanos (`Application`, `Infrastructure`, `Domain`, `Contracts`) con rutas
  `../`. Verificado localmente con `docker build -f src/WebApi.MinimalAPI/Dockerfile src`.
- `.github/workflows/deploy-backend.yml`

A diferencia del frontend, no hace falta ningún `build-arg`: la configuración (connection
strings, JWT, CoinGecko, etc.) se inyecta en runtime como variables de entorno del Container App,
no en tiempo de build.

## Configuración pendiente en GitHub (antes de que el workflow funcione)

El workflow reutiliza los secrets de identidad OIDC y de Docker Hub ya configurados para el
frontend (mismo repo, mismo Environment `production`), bajo el supuesto de que **ambos Container
Apps están en el mismo resource group / subscription**. Si no es así, hay que crear un App
Registration y un role assignment propios para el backend (ver pasos 2–5 de
[`azure-container-apps-frontend-deploy.md`](azure-container-apps-frontend-deploy.md)).

**Secrets** (Settings → Secrets and variables → Actions → Secrets) — ya existen si el frontend
está desplegado:
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`

**Variables nuevas a crear** (Settings → Secrets and variables → Actions → Variables):
- `BACKEND_CONTAINER_APP_NAME` — nombre del Container App del backend. Se usa un nombre distinto
  a `CONTAINER_APP_NAME` (que ya usa el frontend) para no pisar esa variable.

**Variables ya existentes que se reutilizan:**
- `DOCKERHUB_NAMESPACE`, `AZURE_RESOURCE_GROUP`

Si la primera imagen `portfolio-backend` no existe aún en el namespace de Docker Hub configurado,
generarla manualmente una vez (igual que se hizo con el frontend, ver paso 3 de su guía) antes de
que el Container App pueda apuntar a ella, o simplemente lanzar el workflow con
`workflow_dispatch` desde la rama con el Dockerfile ya corregido.

## Pendiente / mejoras futuras

- Validar el primer run real del workflow y documentar aquí cualquier problema encontrado,
  siguiendo el formato de la tabla de problemas de la guía del frontend.
