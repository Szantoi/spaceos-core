---
id: ORCH-STAGE-DISPATCH
title: Orchestrator Stage Dispatch Route (BE-03)
status: active
priority: medium
assignee: orchestrator terminál
epic: workflow-stage-architecture
blocked_by: ""
created: 2026-04-10
updated: 2026-04-10
docs:
  - docs/SpaceOS_WorkflowStage_Architecture_v4.md
---

# ORCH-STAGE-DISPATCH — Stage Dispatch Route

## Feladat

`stageDispatch.ts` route az Orchestratorban — BFF kérések proxyzása a Stage module-okhoz.

## Scope

- `src/routes/stageDispatch.ts` — endpointCache (TTL 5 min) + resolveStageEndpoint + proxy route
- Nginx `/bff/stages/` location (ha hiányzik)
- ≥6 teszt

## Blokkoló

MSG-KERNEL-054 (Kernel Stage Registry) DONE szükséges — a `/api/stages` endpoint a Kernel-ben kell.

## Mailbox

MSG-ORCHESTRATOR-055 (kiadva: 2026-04-10)
