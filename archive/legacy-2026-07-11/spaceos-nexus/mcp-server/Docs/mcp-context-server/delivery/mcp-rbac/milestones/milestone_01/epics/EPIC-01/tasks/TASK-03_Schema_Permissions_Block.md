---
title: "TASK-03: Role Schema mcp_tool_permissions Blokk Definiálása"
type: task
task: TASK-03
epic: EPIC-01
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
---

# 📋 TASK-03: Role Schema `mcp_tool_permissions` Blokk Definiálása

## Leírás

A meglévő `*.schema.yaml` fájlokat egészítjük ki egy új `mcp_tool_permissions` szekcióval, amely deklaratívan rögzíti, hogy az adott szerepkör milyen MCP eszközöket érhet el. Ez lesz a `RbacFilter` permission map-jének forrása.

### Séma Kiterjesztés Példa

```yaml
# backend_developer.schema.yaml
role: Backend Developer
domain: engineering
# ... meglévő mezők ...
mcp_tool_permissions:
  - get_role
  - get_workflow
  - get_knowledge
  - get_policy
  - validate_workflow_step
  - request_workflow_transition
  # Nincs: write_file, delete_role, stb.
```

### Érintett Fájlok

Legalább 2 role schema-t kell frissíteni a tesztelhetőség érdekében:
1. `engineering/backend_developer/backend_developer.schema.yaml`
2. Egy "alacsonyabb" jogosultságú role (pl. `discovery/explorer` ha létezik)

## Elfogadási Kritériumok

- [x] A `mcp_tool_permissions` mező definiálva legalább 2 role schema-ban
- [x] Az `RbacFilter` képes beolvasni a YAML-ból a permissions listát
- [x] A schema-ban nem szereplő tool-ok automatikusan tiltottak az adott role számára
- [x] A schema módosítás visszafelé kompatibilis (ha a mező hiányzik → fallback: full access VAGY no access — döntés dokumentálva)

## Megvalósítási összefoglaló
A `backend_developer` és a `explorer` YAML sémák (schema.yaml) kaptak `mcp_tool_permissions` blokkokat. Ez a deklaratív megoldás lett az authoritatív forrása a role engedélyeknek. Az `RbacFilter` fájl olvasó rutinja kibővült, így zökkenőmentesen húzza be memóriába ezeket a YAML mezőket az inicializáció során. A visszafelé kompatibilitás egy szigorú (no access / fallback public access) megközelítést kapott a biztonság miatt.
