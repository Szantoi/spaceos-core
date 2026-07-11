---
id: MSG-BACKEND-006
from: root
to: backend
type: task
priority: high
status: READ
model: opus
ref: MSG-ROOT-001
product: spaceos-nexus
created: 2026-06-21
content_hash: 7d6bdd85a58d9008205fd4f800944a071701097cd7becfa6495b64f430f90eea
---

# BACKEND-006: Nexus Project Automation System

## FONTOS: Ez NEM SpaceOS kód!

A **spaceos-nexus** egy **külön termék** — agent orkesztrációs szerver.
- **Lokáció:** `/opt/spaceos/spaceos-nexus/knowledge-service/`
- **Nyelv:** TypeScript/Node.js
- **Cél:** AI agent flották koordinálása, nem faipar

---

## Feladat

Implementáld a **Project Automation System**-et a Knowledge Service-be.

**Specifikáció:** `docs/tasks/new/SpaceOS_Project_Automation_Architecture_v4.md`

---

## Implementációs Track-ek

| Track | Fájlok | Effort |
|-------|--------|--------|
| **A: YAML Processor** | `src/pipeline/projectDispatcher.ts`, `projectMatcher.ts` | 1 nap |
| **B: Generator Core** | `src/generators/generateModule.ts`, `generateEndpoint.ts` | 1.5 nap |
| **C: Templates** | `src/generators/templates/*.cs.tmpl` | 0.5 nap |
| **D: MCP Integration** | `src/projectTools.ts`, `src/mcp.ts` (+6 tool) | 1 nap |
| **E: Tests** | `src/__tests__/pipeline/*.test.ts` | 1 nap |

---

## Új MCP Tools (6 db)

| Tool | Leírás |
|------|--------|
| `create_project` | Projekt struktúra létrehozása |
| `get_project_status` | Projekt státusz lekérdezés |
| `dispatch_next` | Következő task manuális dispatch |
| `list_blocked` | BLOCKED task-ok listázása |
| `generate_skeleton` | Modul skeleton generálás |
| `generate_endpoint` | API endpoint scaffolding |

---

## Fájl Struktúra

```
spaceos-nexus/knowledge-service/
  src/
    pipeline/
      projectDispatcher.ts    ← Fő dispatcher logika (Chokidar watcher)
      projectMatcher.ts       ← DONE→task matching
      statusUpdater.ts        ← STATUS.md generálás
    generators/
      generateModule.ts       ← .NET skeleton generator
      generateEndpoint.ts     ← Endpoint scaffolder
      generateInbox.ts        ← Inbox template
      templates/
        *.cs.tmpl             ← C# templates
    projectTools.ts           ← MCP tool handlers
    mcp.ts                    ← +6 új tool (23 → 29)
```

---

## Definition of Done

- [ ] YAML séma dokumentálva és validálva
- [ ] projectDispatcher működik (unit test)
- [ ] generateModule létrehoz 9+ fájlt (integration test)
- [ ] generateEndpoint append működik
- [ ] MCP tools regisztrálva (23 → 29)
- [ ] Telegram értesítés működik
- [ ] E2E teszt: create_project → add task → DONE → auto-dispatch

---

## Megjegyzés

Ez a rendszer a **Conductor terminál automatizálása**:
- DONE outbox → auto-dispatch következő task
- YAML-ben definiált task chain-ek
- Skeleton generálás → kevesebb boilerplate

**Kezdd Track A-val (YAML Processor)** — ez a P0 prioritás.
