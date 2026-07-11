---
id: plan-mcp-maintenance-m01
title: "Milestone 01: RBAC & Server Hygiene"
type: milestone
project: mcp-maintenance
status: closed  # milestone completed on 2026-03-05
---

# 🏁 M01: RBAC & Server Hygiene

Ez a milestone magában foglalja az MCP szerveren talált kritikus és közepes hiányosságok pótlását.

## Cél

Hozzuk az Agent System MCP szerverét egy konzisztens, megbízható állapotba, ahol nincsenek biztonsági rések (RBAC) és halott/felesleges kódok (Hygiene).

## Epicek

* **EPIC-00: Architect Coordination & Audit Actions** (✅ CLOSED_DONE — M01 coordination complete)
* **EPIC-01: RBAC Schema Update & Server Root Cleanup** (✅ CLOSED_DONE)
* **EPIC-02: Dead Code Elimination & Static Analysis** (✅ CLOSED_DONE — 0 dead code)
* **EPIC-08: MCP Write Layer — Artifact Submit & Session Control** (🚧 IN_DEV — schema locked for M02, implementation ongoing)

## Sikerkritérium

Az MCP szerver sikeresen teljesíti a:

1. Playwright E2E teszteket (RBAC tool filtering)
2. Statikus kódelemző eszközöket (ts-prune, tsc) — nincs felhasználatlan kód
3. Write layer E2E testeket (artifact submit, FSM state tracking)
