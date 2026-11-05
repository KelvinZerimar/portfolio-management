# Despliegue del agente de IA con un modelo de Azure AI Foundry (Azure OpenAI)

## Contexto

En local, el agente (`src/Infrastructure/Agent/`, ver [`agent-chat-flow.md`](agent-chat-flow.md)) usa
Ollama con `qwen2.5:3b-instruct`. En producción no tiene sentido depender de un contenedor Ollama
dentro del Container App — se usa en su lugar un modelo Azure OpenAI desplegado vía Azure AI Foundry
(p. ej. `gpt-4o-mini`).

**No hace falta tocar código.** `KernelFactory.ConfigureModelConnector`
(`src/Infrastructure/Agent/KernelFactory.cs:75-84`) ya tiene una rama para
`AiProviderType.AzureOpenAI` que llama a `AddAzureOpenAIChatCompletion(deploymentName, endpoint,
apiKey, ...)`, nunca ejercitada hasta ahora. Todo lo que falta es aprovisionar el recurso en Azure y
configurar las variables de entorno del Container App — siguiendo el mismo patrón que el resto de la
configuración del backend (ver
[`azure-container-apps-backend-deploy.md`](azure-container-apps-backend-deploy.md): "la configuración
... se inyecta en runtime como variables de entorno del Container App, no en tiempo de build").

## 1. Aprovisionar el modelo en Azure AI Foundry

Paso manual en el portal/CLI de Azure (fuera de este repo):

1. Crear o reutilizar un proyecto de Azure AI Foundry.
2. Desplegar un modelo Azure OpenAI (p. ej. `gpt-4o-mini`) desde el catálogo de Foundry.
3. Anotar del deployment: **endpoint** (`https://<recurso>.openai.azure.com/` o el endpoint de
   Foundry equivalente), **nombre del deployment** y **API key**.

> Nota: el código solo soporta autenticación por API key (`ApiKey` en `ModelOptions`). Si se quiere
> usar Entra ID/managed identity en vez de key, hace falta un cambio de código adicional en
> `KernelFactory.cs` — fuera de alcance de este documento.

## 2. Variables de entorno en el Container App del backend

`AiOptions` se bindea con `GetRequiredSection("Ai")` + `ValidateOnStart()`
(`src/Infrastructure/DependencyInjection.cs`), así que **la sección `Ai` es obligatoria** — hoy no
existe en `appsettings.json` base ni hay `appsettings.Production.json`, por lo que estas variables
deben estar presentes en el Container App o el arranque falla.

Variables a crear (convención de doble guion bajo de ASP.NET Core, igual que `Jwt__*` /
`CoinGecko__*` en [`azure-container-apps-worker-deploy.md`](azure-container-apps-worker-deploy.md)):

```
Ai__Provider=AzureOpenAI
Ai__MaxAllowedToolLoops=5
Ai__RequestTimeoutSeconds=240

Ai__ReasoningModel__Endpoint=https://<recurso>.openai.azure.com/
Ai__ReasoningModel__DeploymentName=<nombre-del-deployment>
Ai__ReasoningModel__ApiKey=secretref:ai-apikey

Ai__FastModel__Endpoint=https://<recurso>.openai.azure.com/
Ai__FastModel__DeploymentName=<nombre-del-deployment>
Ai__FastModel__ApiKey=secretref:ai-apikey
```

`FastModel` puede reutilizar el mismo deployment/key que `ReasoningModel` si no se quiere desplegar
un segundo modelo — hoy el orquestador solo usa el servicio `ReasoningModel` (ver
[`agent-chat-flow.md`](agent-chat-flow.md)), pero ambas secciones son requeridas por `AiOptions`.

**La API key va como secret del Container App, no como variable plana:**

```bash
az containerapp secret set \
  --name <BACKEND_CONTAINER_APP_NAME> \
  --resource-group <AZURE_RESOURCE_GROUP> \
  --secrets ai-apikey=<api-key-de-foundry>
```

y luego se referencia con `secretref:ai-apikey` en las variables `Ai__*__ApiKey` de arriba, mismo
nivel de cuidado que el resto de secretos del proyecto (`dotnet user-secrets` en local, secrets de
Container App en producción — ver `CLAUDE.md`: "Secrets ... managed via `dotnet user-secrets`, never
`appsettings.json`").

## 3. Validar antes de desplegar

El code path de `AddAzureOpenAIChatCompletion` no se ha ejercitado todavía en este proyecto. Antes de
tocar el Container App, probar en local:

1. Poner temporalmente en `appsettings.Development.json` (o mejor, vía `dotnet user-secrets` para no
   committear la key real): `Ai:Provider = "AzureOpenAI"` y las credenciales reales del deployment de
   Foundry.
2. `dotnet run --project src/WebApi.MinimalAPI` y probar `POST /api/v1/Agent/chat` con un JWT válido.
3. Confirmar que responde igual que con Ollama. Esto también valida que
   `Microsoft.SemanticKernel` 1.81 resuelve `AddAzureOpenAIChatCompletion` sin necesitar añadir
   `Microsoft.SemanticKernel.Connectors.AzureOpenAI` como `PackageReference` explícito en
   `Infrastructure.csproj`.
4. Volver `appsettings.Development.json` a `Provider = "Ollama"` antes de commitear (el entorno de
   desarrollo sigue usando Ollama local; solo producción usa Azure AI Foundry).

## Pendiente / mejoras futuras

- Validar el primer despliegue real con las variables de entorno del Container App y documentar aquí
  cualquier problema encontrado (mismo formato que las guías de backend/frontend/worker).
- Evaluar autenticación con managed identity en vez de API key si se quiere evitar el secret.
