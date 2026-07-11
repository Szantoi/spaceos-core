---
title: "TASK-04: E2E Teszt — Role-Alapú Tool Szűrés Verifikációja"
type: task
task: TASK-04
epic: EPIC-01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
---

# 📋 TASK-04: E2E Teszt — Role-Alapú Tool Szűrés Verifikációja

## Leírás

Két MCP tool list lekérdezést hajtunk végre különböző role kontextusban, és assertáljuk, hogy az `explorer` role nem látja a privilegizált tool-okat, a `tech_lead` (vagy `backend_developer`) pedig igen.

### Teszt Szcenáriók

| Szcenárió | Role | Várt Eredmény |
|:----------|:-----|:--------------|
| Publikus eszközök | `explorer` | Csak `get_role`, `get_policy` jelenik meg |
| Teljes hozzáférés | `backend_developer` | `validate_workflow_step`, `request_workflow_transition` is megjelenik |
| Ismeretlen role | `unknown_role` | Fallback: csak publikus eszközök |

### Implementáció

```typescript
// src/tests/e2e/mcp-rbac-test.ts
const explorerTools = await fetchMcpTools({ role: 'explorer' });
const devTools = await fetchMcpTools({ role: 'backend_developer' });

assert(!explorerTools.includes('validate_workflow_step'));
assert(devTools.includes('validate_workflow_step'));
```

## Elfogadási Kritériumok

- [x] `src/tests/e2e/mcp-rbac-test.ts` létrehozva
- [x] `npm run test:e2e:rbac` script hozzáadva `package.json`-ba
- [x] `explorer` role esetén a privilegizált tool-ok **nem jelennek meg** a listában
- [x] `backend_developer` role esetén a teljes MCP eszközkészlet elérhető
- [x] Ismeretlen role esetén graceful fallback, nem szerver hiba
- [x] Teszt sikeresen fut (`exit code: 0`)

## Megvalósítási összefoglaló
Létrehoztam és sikeresen lefuttattam a `Playwright` teszteket az API/SSE végpontokon. A feladat során kiderült, hogy a natív HTTP json-rpc fetch requestekkel kell megszólítanunk az endpointsot az E2E tesztekhez. A szcenáriók - ismeretlen role, explorer és backend_developer - zölden futottak le, sikeresen bizonyítva az RBAC szűrés tökéletes működését és elválasztását. A `package.json` új fejlesztői script-et is kapott.
