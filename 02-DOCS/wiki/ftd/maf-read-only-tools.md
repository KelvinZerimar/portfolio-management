---
author: kramirez
branch: feature/maf-read-only-tools
status: done
---
# Agente MAF en modo solo lectura (configurable)

## Intent

Por ahora el usuario solo consulta datos con el agente MAF: no crea ni modifica nada. Enviar las 8
herramientas `Propose*` y sus reglas engorda el prompt (unos 3000 tokens solo de esquemas con las 21
herramientas), lo que en local hacía que Ollama recortara el prompt y que cada respuesta tardara minutos. El
siguiente paso es usar `gpt-4.1-nano` en Azure AI Foundry, donde un prompt menor también es más barato.

Antecedente: [maf-asset-values-at-dates](maf-asset-values-at-dates.md) (herramienta
`GetPortfolioAssetValuesAt` y contexto de Ollama a 16384), ya commiteado en `development` (`b6e13bf`). Esta rama
parte de ese commit.

## Scope

**In** (decisiones del usuario, 2026-11-19):
- Opción `Ai:MafAgent:EnableWriteProposals` (por defecto `true`, comportamiento actual).
- Con `false`:
  - el modelo recibe solo las herramientas de lectura activas (ninguna `Propose*`). Hoy son 10: en `development`
    (`b6e13bf`) están comentadas `ListExchanges`, `ListNotes` y `GetNote`;
  - las instrucciones son una versión de solo lectura: sin reglas de propuestas ni `MissingOrAmbiguous`, y
    con la regla "READ-ONLY: crear, editar o borrar no está disponible; dilo en una frase".
- `appsettings.Development.json`: `false`. En Azure: variable `Ai__MafAgent__EnableWriteProposals=false`.

**Out:** quitar el código de las `Propose*`; los endpoints confirm/reject; el agente SK.

## Checklist

- [x] Tests en rojo por la razón esperada — prueba: CS1061 "AiOptions no contiene una definición para MafAgent"
- [x] Opción + runner + instrucciones de solo lectura — prueba: `MafAgentRunnerTests`, `MafAgentReadToolsTests`,
      `MafAgentRunnerDiTests` 71/71 (2026-11-19)
- [x] `dotnet format --verify-no-changes` limpio y `dotnet test` completo — prueba (2026-11-19): 503/506; Domain y
      Application en verde. Los 3 fallos son `MafSpikeTests` en vivo (`Category=Manual`), que piden
      `qwen2.5:3b-instruct` a Ollama: el contenedor nuevo solo tiene `qwen2.5:7b-instruct` → HTTP 404. Ajenos al
      cambio; pasan con `docker compose exec ollama ollama pull qwen2.5:3b-instruct`.
- [x] Prueba manual con `gpt-4.1-nano` (Azure AI Foundry, `Ai:Provider = AzureOpenAI`, endpoint raíz del recurso
      sin `/openai/v1`): la pregunta de valores en 2026-10-02 y 2026-11-17 responde correctamente y la mejora de
      rendimiento frente a Ollama en CPU (~9 min por respuesta) es enorme — prueba: comprobación del usuario,
      2026-11-19

## Evidence

- Verificación previa al commit (2026-11-19, rama sobre `b6e13bf`): `dotnet build` correcta; tests MAF (runner,
  tools, DI) 66/71. Los 3 tests nuevos pasan. Los 5 que fallan ya fallaban en `development` (`b6e13bf`) por las
  herramientas comentadas: `GetTools_ExposesExactly…`, `TheModelGetsTheReadAndProposeTools…`, `SixToolCalls…`,
  `AFailingTool_IsLogged…`, `TheToolLog_Has…` (cuentan 13/21 herramientas o usan `ListNotes`). No se tocan aquí:
  decisión pendiente del usuario (reactivar las herramientas o adaptar esos tests).
- El test de solo lectura compara con `MafAgentReadTools.GetTools()`, sin número fijo: sigue valiendo si se
  reactivan o se comentan herramientas de lectura.

## Next step

Commit y PR de `feature/maf-read-only-tools` hacia `development`. En Azure (Container App), configurar
`Ai__MafAgent__EnableWriteProposals=false` mientras el agente sea de solo lectura.
