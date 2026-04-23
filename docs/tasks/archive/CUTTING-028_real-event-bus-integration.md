---
id: CUTTING-028
title: Cutting — Real Integration Event Bus (CuttingJobCompletedEvent)
status: new
priority: medium
assignee: CUTTING
epic: inventory-planning-v1-phase2
blocked_by: none
created: 2026-04-20
updated: 2026-04-20
docs:
  - docs/tasks/archive/INVENTORY-PLANNING-V1_offcut-tracking-phase1.md
---

# CUTTING-028 — Real Integration Event Bus

## Context

INVENTORY Phase 1 implements `CuttingJobCompletedEvent` handler (stub, Option A).
Currently the event is published manually in tests only.

**This task:** Wire real event publishing when CuttingJob.Status → "Cut"

## Scope

1. Add `ICuttingEventPublisher` interface to Cutting module
2. Publish `CuttingJobCompletedEvent` on status transition → "Cut"
3. INVENTORY handler consumes it automatically (already wired)
4. Remove stub / mock from INVENTORY Day 2 tests → replace with real integration test

## Why

CUTTING-028 is the **highest priority Phase 2 item** (per INVENTORY-051-DONE recommendation).
Real event bus enables proper offcut lifecycle without manual intervention.

## Timeline

~2 days when JOINERY Phase 1 DONE and bandwidth available.

**Status: BACKLOG — queue after JOINERY Phase 1 DONE**
