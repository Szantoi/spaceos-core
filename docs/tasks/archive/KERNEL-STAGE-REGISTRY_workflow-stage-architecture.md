---
id: KERNEL-STAGE-REGISTRY
title: Kernel Stage Registry — Workflow Stage Architecture v4
status: active
priority: high
assignee: kernel terminál
epic: workflow-stage-architecture
blocked_by: ""
created: 2026-04-10
updated: 2026-04-10
note: "Implementáció elfogadva (kód OK), tesztek pótlás folyamatban MSG-KERNEL-057 alatt (vár: 978+)"
docs:
  - docs/SpaceOS_WorkflowStage_Architecture_v4.md
  - docs/SpaceOS_WorkflowStage_Summary.md
---

# KERNEL-STAGE-REGISTRY — Workflow Stage Architecture v4

## Feladat

Implementáld a `SpaceOS_WorkflowStage_Architecture_v4.md` szerint a Stage Registry infrastruktúrát a Kernel-ben.

## Scope

- **Domain:** StageDefinition · StageChainTemplate · StageChainStep · StageHandoff + IStageChainValidator + FlowEpic bővítés + 6 domain event + 4 Ardalis.Specification
- **Infrastructure:** Migration 0027 + RLS FORCE + triggers + Doorstar seed + EF Core config
- **Application:** 13 CQRS handler + FluentValidation + advisory lock + idempotency
- **API:** 15 endpoint + RBAC (SystemAdmin/TenantAdmin/StageOperator/TenantUser)
- **Tests:** ≥45 db

## Effort

~8 fejlesztői nap

## Mailbox

MSG-KERNEL-054 (kiadva: 2026-04-10)
