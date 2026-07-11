---
id: plan-mcp-maintenance-m03
title: "Milestone 03: Legacy Tool Refactor — DB-First Architecture"
type: milestone
project: mcp-maintenance
project_id: mcp-context-server
status: planned
depends_on: M02
created: 2026-03-04
---

# 🏁 M03: Full Maturity — Multi-Domain & Self-Reflection

## Kontextus

M02 után az `agent.db` élő és a `bootstrap_agent` + episodic memory működik. M03 feladata:

1. **Legacy eszközök modernizálása** — fájlrendszer-alapú wrapperek → DB-first (EPIC-16)
2. **PM Query Tools** — read-only project management state queries (EPIC-15)
3. **Multi-Domain Portability** — domain-agnostic role + knowledge swapping (EPIC-17)
4. **Self-Reflection & Memory Quality** — episodic highlights + continuous improvement (EPIC-18)

## Cél

- Az MCP szerver teljesen DB-first és multi-domain kompatibilis.
- Legacy API külső felület nem törik, belülről mind `agent.db`-ből fut.
- Agents képesek a saját tapasztalataikból tanulni (episodic feedback loop).
- Project management state queryable az MCP-ből (read-only).

## Epicek

| ID | Cím | Prioritás | Állapot |
|:---|:----|:----------|:--------|
| EPIC-15 | PM Query Tools (read-only context-server layer) | P1 — Fontos | ✅ DONE |
| EPIC-16 | Legacy Tool Refactor: fájl-alapú eszközök → DB-wrapper | P0 — Alapkövetelmény | ✅ DONE |
| EPIC-17 | Multi-Domain Configuration & Onboarding | P1 — Fontos | ✅ DONE |
| EPIC-18 | Self-Reflection & Memory Quality (Episodic Highlights) | P2 — Jövőbeli | 🗓️ Planned |

## Sikerkritérium (M03 Success Criteria)

- [x] `get_project_state(project_id)`, `list_my_team_tasks()`, `get_task_context()`, `search_tasks()` MCP tools implementálva és integration teszttel verifikálva (EPIC-15).
- [x] `get_role`, `get_workflow`, `get_template`, `get_core` eszközök `agent.db`-ből olvasnak (EPIC-16).
- [x] A fájlrendszer eltávolítható a futó szerverről — kiszolgálás nem törik (EPIC-16, graceful fallback).
- [x] Meglévő E2E tesztek változtatás nélkül zöldek (EPIC-16 — 24/24 unit tests GREEN).
- [x] Multi-domain seeder: egy ág több domain-t is betöltheti (EPIC-17).
- [x] `switch_domain()` + `list_available_domains()` MCP tools, domain-aware RBAC filtering, session isolation — EPIC-17 DONE.
- [ ] `generate_episode_highlights()` MCP tool működik (EPIC-18).
- [ ] `reflect_session()` tool legalább 3 `episode_highlight` rekordot hoz létre (EPIC-18).
- [ ] Knowledge Base (ChromaDB) szinkronizált az `agent.db` tartalommal és episodic memóriaval (EPIC-18).

## Nem scope (M03-ban)

- PM Engine full server & API (PROJ-B / M04 feladata).
- Multi-domain orchestration framework (M04 feladata).
- Production deployment & operational hardening (M05 feladata).
