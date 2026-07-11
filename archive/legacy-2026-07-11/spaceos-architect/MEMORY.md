# ARCHITECT Memory

Utolsó frissítés: 2026-06-20

## Aktuális állapot
- MSG-ARCH-011 (Datahaven Dashboard Integration Training) **ACKNOWLEDGED** — notification only, no outbox needed
- MSG-ARCH-010 (PHASE 3 ADR Review) **COMPLETE** — APPROVED_BY_ROOT
- 3 ADR elkészült: ADR-043, ADR-044, ADR-045
- Golden Rule ellenőrzés végrehajtva — mindhárom ADR PASS
- **Datahaven Dashboard integráció AKTÍV** — Week 1 migration complete

## Datahaven Dashboard
- **URL:** https://datahaven.joinerytech.hu
- **Token:** dev-token-spaceos-dashboard-2026
- **Session ritual:** Minden session elején WORKING, végén IDLE státusz regisztráció
- **4 oldal:** Dashboard (/) | Kanban (/kanban) | Planning (/planning) | Projects (/projects)

## Fontos kontextus
- **ADR-043 (Marvin Orchestration Pattern):** Bash planning pipeline → Marvin Python. Resumable threads, 5 agent. Fázis 1 COMPLETE, Fázis 2-3 pending.
- **ADR-044 (Knowledge Service):** ChromaDB + Voyage AI. System-wide integration pending. Phase 1 operational (port 3456).
- **ADR-045 (McpServer Tools):** 4 új tool (discovery_search ✅, submitArtifact, getWorkflowState, updateWorkflowState) + RbacFilter.
- **Fő blokkoló:** VPS memória bővítés (8GB → 16GB) — Marvin + ChromaDB szükséges.

## Következő lépések
- Várj inbox üzenetre a Conductor-tól vagy Root-tól
- Ha Fázis 2 indítás jön → Marvin agent definíciók review
- Ha System-wide integration → terminál CLAUDE.md kiegészítések

## Megoldott problémák
- MSG-ARCH-010: 3 HIGH priority tervdokumentum ADR finalizálása — DONE

## Session tapasztalatok
- ADR fájlok már léteztek a `docs/architecture/decisions/` mappában
- Outbox üzenet APPROVED_BY_ROOT — a feladat már korábban el lett végezve
- Golden Rule RBAC ellenőrzés → Fázis 3 függőség explicit rögzítve
