---
title: "EPIC-01: RbacFilter Service & MCP Tool Registry Szűrés"
type: epic
epic: EPIC-01
milestone: M01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
effort_estimate: L
origin: exp-mcp-rbac-constraints
---

# 🎯 EPIC-01: RbacFilter Service & MCP Tool Registry Szűrés

## Cél

Implementáljuk a role-alapú eszközszűrést az MCP szerveren. Az MCP kliens által megadott role kontextus alapján a szerver csak az adott szerepkör számára engedélyezett eszközöket adja vissza.

## Sikerességi Kritérium

- `explorer` role **nem látja** a `write_to_file` és egyéb privilegizált eszközöket
- `tech_lead` role a teljes eszközkészletet megkapja
- A meglévő eszközök és route-ok **sértetlenek** maradnak

## Feladatok

- [x] **TASK-01**: `RbacFilter.ts` service létrehozása
  - Tool → engedélyezett role-ok mapping (konfiguráció, nem hard-code)
  - `filterTools(tools, activeRole): Tool[]` metódus
- [x] **TASK-02**: Role kontextus olvasása az MCP szerverben
  - HTTP header (`x-active-role`) vagy MCP init param vizsgálata
  - Fallback: ha nincs role megadva → csak public tools
- [x] **TASK-03**: `mcp_tool_permissions` blokk definiálása a role schema-kban
  - Pl. `src/agent-system/database/roles/engineering/backend_developer/backend_developer.schema.yaml`-ban
- [x] **TASK-04**: E2E teszt létrehozása
  - `explorer` & `tech_lead` lekérdezések összehasonlítása
  - Assertion: `write_to_file` megjelenik / nem jelenik meg

## Visszabonthatóság

Rollback: a `mcpServer.ts`-ből a szűrő logika egy komment-jelölt blokk — 1 lépésben törölhető.

## Kapcsolódó Fájlok

- `src/agent-system/server/src/mcp/mcpServer.ts` — tool registry
- `src/agent-system/server/src/mcp/RbacFilter.ts` — [ÚJ]
- `src/agent-system/database/roles/` — schema fájlok kiterjesztése
