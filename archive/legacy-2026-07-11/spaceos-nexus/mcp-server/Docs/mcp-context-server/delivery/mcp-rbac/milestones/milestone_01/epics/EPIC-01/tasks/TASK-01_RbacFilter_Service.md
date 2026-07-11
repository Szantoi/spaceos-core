---
title: "TASK-01: RbacFilter Service Implementálása"
type: task
task: TASK-01
epic: EPIC-01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
---

# 📋 TASK-01: RbacFilter Service Implementálása

## Leírás

Létrehozunk egy önálló `RbacFilter.ts` TypeScript service-t, amely a role neve alapján eldönti, hogy az MCP szerver mely eszközeit (tool-jait) szabad megjeleníteni az adott kliens számára. A konfiguráció nem hard-code — egy külső permission map-ból olvassa az engedélyeket.

### Architektúra

```typescript
// src/mcp/RbacFilter.ts
export class RbacFilter {
    filterTools(tools: Tool[], activeRole: string): Tool[]
    hasPermission(toolName: string, activeRole: string): boolean
    loadPermissions(permissionsConfig: Record<string, string[]>): void
}
```

A permission map forrása: a role schema YAML-okban definiált `mcp_tool_permissions` lista.

## Elfogadási Kritériumok

- [x] `src/agent-system/server/src/mcp/RbacFilter.ts` létrehozva
- [x] `filterTools(tools, role)` metódus implementálva — csak az engedélyezett tool-okat adja vissza
- [x] `hasPermission(toolName, role)` segédmetódus implementálva
- [x] Ha az `activeRole` ismeretlen / nem adott → csak a `public` tagek tool-jai jelennek meg (fail-safe)
- [x] Unit teszt: legalább 2 role, legalább 2 tool kombinációra

## Megvalósítási összefoglaló

A feladat sikeresen implementálva lett. Létrehoztam az `RbacFilter.ts` fájlt az `src/agent-system/server/src/mcp/` elérési úton. A service tartalmazza a szükséges `getAllowedTools`, `hasPermission` funkciókat. Képes rekurzívan beolvasni az összes `.schema.yaml` fájlt a `database/roles` könyvtárból és felépíteni egy memóriában tartott `permissionMap`-et. Sikeresen kombinálja a schema alapú toolokat a publikus ("public") toolokkal (mint pl. a `search_knowledge` és `get_policy`), amely biztosítja a graceful fallbacks-et is. Készült hozzá unit teszt is (`RbacFilter.test.ts`), ami igazolja a működést.
