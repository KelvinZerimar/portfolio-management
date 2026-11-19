---
author: kramirez
branch: development
status: in-progress
---
# Herramienta MAF `GetPortfolioAssetValuesAt`: valor de cada activo en fechas concretas

## Intent

Pregunta del usuario (2026-11-19): "Obtén los datos históricos de cada activo del portafolio en estas fechas
específicas 2026-10-02 y 2026-11-17". El agente MAF respondió con un ranking de caídas en porcentaje, en inglés,
con datos inventados.

Causa: ninguna herramienta devolvía el valor por activo en una fecha pasada. La más cercana,
`GetPortfolioAssetValueComparison`, es una **comparación** (inicio, fin, diferencia). El modelo
`qwen2.5:3b-instruct` la convirtió en un ranking.

## Scope

**In:**
- Nueva herramienta de lectura `GetPortfolioAssetValuesAt(portfolioId, dates[])`:
  - 1 a 10 fechas `yyyy-MM-dd`; repetidas cuentan una vez. Sin fechas, más de 10 o formato malo → `InvalidDates`.
  - Por fecha: símbolo, cantidad y valor en €, sumando exchanges (`Portfolio.GetHoldingsAsOf`, igual que el
    dashboard y la comparación).
  - Un activo que no se tiene en una fecha no aparece en ella.
  - Orden: por fecha y, dentro de la fecha, de mayor a menor valor.
  - Portafolio de otro usuario → `NotFound`, sin leer sus entradas.
- Descripciones de `GetPortfolioHoldings` y `GetPortfolioAssetValueComparison` apuntan a la nueva herramienta
  cuando la pregunta es "valores en fechas", no "cambio".

**Out:** el agente SK (`*/Agent/`), cambiar de modelo, el tamaño de contexto de Ollama.

## Checklist

- [x] Tests en rojo por la razón esperada — prueba: CS1061 "no contiene una definición para
      GetPortfolioAssetValuesAt" y CS0246 `MafAssetValueAtDate`
- [x] Herramienta implementada y tests en verde — prueba: `MafAgentReadToolsTests` + `MafAgentRunnerTests`
      65/65; todos los tests `~MafAgent` (incluida integración) 175/175, 0 errores (2026-11-19)
- [x] `MafAgentRunnerTests`: el modelo recibe 21 herramientas (antes 20), incluida `GetPortfolioAssetValuesAt`
- [x] Causa de que siga fallando tras la herramienta (2026-11-19) — prueba: log de Ollama
      `truncating input prompt limit=2050 prompt=6102`, `n_ctx = 4096`, y un `500` a los `10m0s`
- [x] `docker-compose.yml`: `OLLAMA_CONTEXT_LENGTH: "16384"` en el servicio `ollama` — prueba: `docker compose config`
- [ ] Recrear el contenedor de Ollama desde este compose y comprobar `context_length: 16384` en `/api/ps` y que
      desaparece `truncating input prompt` — pendiente del usuario
- [ ] Prueba manual con el agente: la pregunta original devuelve la lista por fecha — pendiente del usuario

## Evidence

- Compilación y tests con `--artifacts-path` en el scratchpad: la API estaba abierta en el depurador y
  bloqueaba las DLL de `bin/`.
- El verde se obtuvo en un worktree temporal con `ListNotes`/`GetNote` activas. En la copia de trabajo de
  `development` están comentadas (cambio local del usuario, sin commitear) y eso rompe 5 tests ajenos a esta
  herramienta: `GetTools_ExposesExactly…`, `TheModelGetsTheReadAndProposeTools…`, `SixToolCalls…`,
  `AFailingTool_IsLogged…`, `TheToolLog_Has…` (todos usan `ListNotes`/`GetNote`).
- Comparativa con SK (misma pregunta acierta en SK): SK envía 5 herramientas y ningún historial; MAF envía 21 y
  toda la conversación. Con `qwen2.5:3b-instruct` y el contexto por defecto de Ollama, probablemente se
  recortan las instrucciones de MAF. Recomendado: `OLLAMA_CONTEXT_LENGTH=16384` y conversación nueva.

## Next step

Prueba manual en el panel del agente con una conversación nueva.
