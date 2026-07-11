---
title: "TASK-02: Role Kontextus Olvasása az MCP Szerverben"
type: task
task: TASK-02
epic: EPIC-01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
---

# 📋 TASK-02: Role Kontextus Olvasása az MCP Szerverben

## Leírás

Az MCP szerver `mcpServer.ts`-ben integráljuk az `RbacFilter`-t. A hívó kliens által megadott role kontextust (HTTP header vagy MCP inicializációs paraméter) kiolvasva az eszközlista dinamikusan szűrve kerül vissza.

### Megközelítés

Az MCP SDK-ban a kliens az inicializációs fázisban adhat át metaadatot. Az alábbi két mechanizmust vizsgáljuk meg és döntünk közöttük:

- **A variáns**: `x-active-role` custom HTTP header az SSE/HTTP csatlakozáskor
- **B variáns**: MCP `initializeParams.clientInfo` extension field

A választott megoldást a TASK végén ADR-kommentben rögzítjük.

### Integrációs pont

```typescript
// mcpServer.ts — tool listázás előtt
const activeRole = request.headers['x-active-role'] ?? 'public';
const filteredTools = rbacFilter.filterTools(allTools, activeRole);
```

## Elfogadási Kritériumok

- [x] `mcpServer.ts`-be integrálva az `RbacFilter` (dependency injection vagy singleton)
- [x] A role kontextus HTTP header-ből olvasható (`x-active-role`)
- [x] Ha nincs header → fallback `public` role (csak publikus tool-ok)
- [x] A módosítás visszabontható (komment-jelölt blokk)
- [x] A meglévő tool-ok (get_role, get_workflow, stb.) sértetlenül működnek

## Megvalósítási összefoglaló
A feladat alapján az **A variáns**, azaz az `x-active-role` custom HTTP header kiolvasása mellett döntöttünk. Az `RbacFilter` be lett integrálva az `mcpServer.ts` fájlba. Amennyiben hiányzik a header, a rendszer automatikusan a `public` szerepkört veszi fel (default biztonsági opció). A regisztrált eszközök mind megmaradtak, csupán feltételes regisztráció (isAllowed ellenőrzés) fut le minden tool definition felett.
