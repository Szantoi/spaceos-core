---
id: CUTTING-002
title: Inventory Core — MaterialCatalog + PanelStock + Offcut + StockMovement + API
status: active
priority: high
assignee: cutting
epic: cutting-v1
blocked_by: ~
created: 2026-04-15
updated: 2026-04-15
docs:
  - docs/tasks/new/SpaceOS_Modules_Cutting_Vision_v1.md
  - spaceos-modules-cutting/CLAUDE.md
---

## Feladat

Implementálj Inventory Core-t a Modules.Cutting solutionbe:

- `SpaceOS.Modules.Inventory.Domain` — MaterialCatalog, PanelStock, Offcut, StockMovement aggregatok + domain events
- `SpaceOS.Modules.Inventory.Application` — CQRS handlerek (GetStock, GetOffcuts, RecordConsumption, RecordInbound, RecordOffcut, GetConsumptionTrend)
- `SpaceOS.Modules.Inventory.Infrastructure` — EF Core 8, schema `spaceos_inventory`, migrations
- `SpaceOS.Modules.Inventory.Api` — Minimal API endpoints

## DoD

≥40 teszt (domain 15 · EF/repo 10 · API 10 · security 5)

## Prereq

CUTTING-001 (Contracts) ✅ — NuGet packages kész (84bb708)
