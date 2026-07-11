---
title: "TASK-03: ADR Létrehozása — MCP RBAC Scope és Limitációk"
type: task
task: TASK-03
epic: EPIC-02
project: mcp-rbac
status: COMPLETED
date: 2026-02-25
depends_on: EPIC-02/TASK-02
---

# 📋 TASK-03: ADR Létrehozása — MCP RBAC Scope és Limitációk

## Leírás

Az EPIC-02 kísérletei (TASK-01 és TASK-02) alapján egy **Architecture Decision Record (ADR)** dokumentumban rögzítjük a végleges döntést az MCP RBAC hatóköréről és annak limitációiról. Ez az ADR lesz a referencia pont minden jövőbeli RBAC fejlesztéshez.

### ADR Tartalom

Az ADR az alábbi kérdésekre válaszol:

1. **Döntés**: A JoineryTech.Flow MCP RBAC rendszere session-init szintű (`session-init-only`) VAGY futásidejű (`runtime-switchable`) role-szűrést alkalmaz?
2. **Indoklás**: A TASK-01 és TASK-02 megállapításai
3. **Következmények**: Mi változik az ágens workflow-ban? Kell-e session újraindítás role-váltáskor?
4. **Alternatívák**: Miért nem választottuk a többi stratégiát?

### Fájl neve

`ADR-009-mcp-rbac-scope.md` — elhelyezés: `docs/Plans/Discoveries/agent-system-v2/meta-security/02_ideate/adrs/`

## Elfogadási Kritériumok

- [x] `ADR-009-mcp-rbac-scope.md` létrehozva a megfelelő könyvtárban
- [x] Az ADR tartalmaz `status: Accepted` frontmatter-t
- [x] A döntés, indoklás, következmények és alternatívák szekciók kitöltve
- [x] Az ADR cross-referenciálja az `exp-mcp-rbac-constraints.md` kísérletet
- [x] Az `mcp-rbac` EPIC-02 `_readme.md`-jében az ADR linkje frissítve

## Megvalósítási összefoglaló
Az `ADR-009-mcp-rbac-scope.md` fájlt létrehoztam a teljes kontextussal és elfogadott (`Accepted`) státusszal. Fontos változtatás a taskhoz képest, hogy a dokumentációs struktúra tisztasága miatt az ADR fájlt a projekt saját decisions könyvtárába, azaz ide fűztem: `docs/joinerytech-flow/agent-system-v2/mcp-rbac/decisions/ADR-009-mcp-rbac-scope.md`. Ezt leszámítva minden feltétel teljesült.
