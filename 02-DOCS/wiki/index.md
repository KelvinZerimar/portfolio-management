---
type: index
title: Knowledge map
description: Entry point to the project wiki under 02-DOCS/wiki.
tags: [index]
timestamp: 2026-11-11T12:00:00Z
---

# Knowledge map

## harness
- [User profile](harness/user-profile.md)
- [Harness decisions](harness/decisions.md)
- [Installation plan](harness/installation-plan.md)

## ftd
- [Agent chat page](ftd/agent-chat-page.md)
- [MAF: valores por activo en fechas](ftd/maf-asset-values-at-dates.md)
- [MAF: modo solo lectura](ftd/maf-read-only-tools.md)
- [Quitar el dashboard de HealthChecks UI](ftd/remove-healthchecks-ui.md)
- [Dashboard: privacidad de notas y paleta](ftd/dashboard-notes-privacy-and-palette.md)
- [Dashboard: contexto en la cifra principal y eje del histórico](ftd/dashboard-hero-context-and-history-axis.md)
- [Dashboard: quitar tarjetas por activo duplicadas](ftd/dashboard-remove-duplicate-asset-cards.md)
- [Dashboard: el donut suma el total real](ftd/dashboard-allocation-donut-totals.md)
- [Dashboard: colores de activo distinguibles](ftd/dashboard-distinct-asset-colors.md)
- [Dashboard: vista móvil](ftd/dashboard-mobile.md)
- [Dashboard: cabecera de Holdings y rejilla sin desborde](ftd/dashboard-holdings-header.md)
- [Dashboard: filas de Holdings en una línea (fecha y nombre)](ftd/dashboard-holdings-date-column.md)
- [Dashboard: cabeceras de Holdings en mayúsculas](ftd/dashboard-holdings-column-headers.md)
- [Dashboard: el Total del histórico, solo en el tooltip](ftd/dashboard-history-total-in-tooltip.md)
- [Dashboard: estado de orden en Holdings (aria-sort y flecha)](ftd/dashboard-holdings-sort-state.md)
- [Dashboard: sin recuadro negro al pulsar los gráficos](ftd/dashboard-chart-focus-ring.md)
- [Portfolio: vista móvil (lista y detalle)](ftd/portfolio-mobile.md)
- [Portfolio: sin entradas empatadas, «Registrar precios de hoy» y hora visible](ftd/portfolio-refresh-and-entry-time.md)
- [Portfolio: validación, nombres accesibles y teclado en la edición de entradas](ftd/portfolio-edit-row-validation.md)
- [Portfolio: nombre del activo en los selectores](ftd/portfolio-asset-names.md)
- [Portfolio: borrado con peso, alta arriba, valor visible, € y «hoy» local](ftd/portfolio-polish.md)

## sdd
- [Constitution (draft)](sdd/constitution.md)
- [SDD decisions log](sdd/decisions.md)
- [SDD runtime config](sdd/config.yaml) — comandos de test, TDD estricto, presupuesto de revisión

### sdd/summaries
- [Resumen — agente MAF con tool-calling](sdd/summaries/maf-tool-calling-agent.md) — qué se construyó, decisiones y pendientes, 2026-11-12

### sdd/specs
- [Agente MAF con tool-calling](sdd/specs/maf-tool-calling-agent.md) — clarified, 2026-11-11

### sdd/plans
- [Agente MAF con tool-calling](sdd/plans/maf-tool-calling-agent.md) — approved + tasks T001–T041 (revisado tras analyze ×3), 2026-11-11

### sdd/analysis
- [Agente MAF con tool-calling](sdd/analysis/maf-tool-calling-agent.md) — GATE: PASS (ejecución 4: 0 HIGH; 9 MEDIUM/LOW corregidos, 4 aceptados), 2026-11-11

### sdd/verifications
- [portfolio-polish (FTD)](sdd/verifications/portfolio-polish-2026-11-24.md) — PASS (lint, tsc, build; P2 y P3 con datos reales), 2026-11-24
- [portfolio-asset-names (FTD)](sdd/verifications/portfolio-asset-names-2026-11-24.md) — PASS (lint, tsc, build; selectores con datos reales), 2026-11-24
- [portfolio-edit-row-validation (FTD)](sdd/verifications/portfolio-edit-row-validation-2026-11-24.md) — PASS (lint, tsc, build; validación y a11y observadas), 2026-11-24
- [portfolio-refresh-and-entry-time (FTD)](sdd/verifications/portfolio-refresh-and-entry-time-2026-11-23.md) — PASS (264 + 46 tests, API real 409, lint, tsc, build), 2026-11-23
- [portfolio-mobile (FTD)](sdd/verifications/portfolio-mobile-2026-11-23.md) — PASS (lint, tsc, build; 6 anchos), 2026-11-23
- [dashboard-chart-focus-ring (FTD)](sdd/verifications/dashboard-chart-focus-ring-2026-11-23.md) — PASS (lint, tsc, build; foco por ratón y teclado), 2026-11-23
- [dashboard-holdings-sort-state (FTD)](sdd/verifications/dashboard-holdings-sort-state-2026-11-23.md) — PASS (lint, tsc, build; 6 pasos de orden), 2026-11-23
- [dashboard-history-total-in-tooltip (FTD)](sdd/verifications/dashboard-history-total-in-tooltip-2026-11-23.md) — PASS (lint, tsc, build; varios activos y uno), 2026-11-23
- [dashboard-holdings-column-headers (FTD)](sdd/verifications/dashboard-holdings-column-headers-2026-11-23.md) — PASS (lint, tsc, build; estilos calculados), 2026-11-23
- [dashboard-holdings-date-column (FTD)](sdd/verifications/dashboard-holdings-date-column-2026-11-23.md) — PASS (lint, tsc, build; 4 anchos), 2026-11-23
- [dashboard-holdings-header (FTD)](sdd/verifications/dashboard-holdings-header-2026-11-23.md) — PASS (lint, tsc, build; 5 anchos), 2026-11-23
- [dashboard-mobile (FTD)](sdd/verifications/dashboard-mobile-2026-11-23.md) — PASS (lint, tsc, build; 390 y 1440 px), 2026-11-23
- [dashboard-distinct-asset-colors (FTD)](sdd/verifications/dashboard-distinct-asset-colors-2026-11-20.md) — PASS (validador de paleta, lint, tsc, build), 2026-11-20
- [dashboard-allocation-donut-totals (FTD)](sdd/verifications/dashboard-allocation-donut-totals-2026-11-20.md) — PASS (lint, tsc, build; checklist con prueba), 2026-11-20
- [dashboard-remove-duplicate-asset-cards (FTD)](sdd/verifications/dashboard-remove-duplicate-asset-cards-2026-11-20.md) — PASS (lint, tsc, build; checklist con prueba), 2026-11-20
- [dashboard-hero-context-and-history-axis (FTD)](sdd/verifications/dashboard-hero-context-and-history-axis-2026-11-20.md) — PASS (lint, tsc, build; checklist con prueba), 2026-11-20
- [dashboard-notes-privacy-and-palette (FTD)](sdd/verifications/dashboard-notes-privacy-and-palette-2026-11-20.md) — PASS (lint, tsc, build; checklist con prueba), 2026-11-20
- [maf-tool-calling-agent — T035–T036 (checklist con 7B, gate final)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T035-T036.md) — PASS con `qwen2.5:7b-instruct` (9/9), 2026-11-12
- [maf-tool-calling-agent — T034–T036 (regresión, docs, gate final)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T034-T036.md) — FAIL solo por la checklist manual de T035, 2026-11-12
- [maf-tool-calling-agent — T033 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T033.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T031–T032 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T031-T032.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T029–T030 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T029-T030.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T027–T028 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T027-T028.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T025–T026 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T025-T026.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T023–T024 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T023-T024.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — T021–T022 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-12-T021-T022.md) — PASS (alcance de tarea), 2026-11-12
- [maf-tool-calling-agent — arreglo de la revisión de T017–T020](sdd/verifications/maf-tool-calling-agent-2026-11-11-T020-review.md) — PASS, 2026-11-11
- [maf-tool-calling-agent — T019–T020 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T019-T020.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T017–T018 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T017-T018.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T013–T016 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T013-T016.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T002 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T002.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T037–T038 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T037-T038.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T009–T012 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T009-T012.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T003–T008 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T003-T008.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T001 spike (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T001.md) — PASS (alcance de tarea), 2026-11-11
- [maf-tool-calling-agent — T040/T041 (pre-commit)](sdd/verifications/maf-tool-calling-agent-2026-11-11-T041.md) — PASS (alcance de tarea), 2026-11-11

### sdd/progress
- [Agente MAF con tool-calling](sdd/progress/maf-tool-calling-agent.md) — ledger de implement
