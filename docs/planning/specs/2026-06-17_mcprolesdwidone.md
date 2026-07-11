---
id: SPEC-006
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_008_mcp-roles-dwi-done.md
type: Architecture spec + Workflow definition
scope: [Root, Architect, Kernel, Joinery, FE, Tester, Planning Pipeline]
priority: high
complexity: 3
dependencies: [plan-scan.sh, plan-select.sh, plan-debate.sh, CLAUDE.md variants]
status: NEW
created: 2026-06-17
---

# MCP Role Integration + DWI Workflow Finalization

## Összefoglaló

Architect elkészítette az 5 role fájl (Orchestrator, Architect, Backend Dev, Frontend Dev, QA Tester) konkrét CLAUDE.md mappálását, valamint a SpaceOS tervezési pipeline (DWI) 4 fázisának strukturált leírását. Kritikus gap azonosítva: QA Handoff kritérium hiányzott — mikor kell TESTER-t bevonni és mikor nem.

## Scope

- **Root terminál:** CLAUDE.md context hygiene + kommunikációs minták frissítés
- **Architect terminál:** Döntési keretrendszer (3 alternatíva, Chain of Thought) integrálása
- **Backend (Kernel/Joinery/stb.):** Implementációs checklist + **QA Handoff kritérium** beépítése
- **Frontend (FE/FE2):** Tipizálás szabályok + FE QA Handoff definíciója
- **Tester terminál:** Zero Trust + Destructive Testing + bug report formátum specifikálása
- **Planning Pipeline:** DWI_WORKFLOW.md definiálása (IDEATION → VALIDATION → ITERATION → DELIVERY_HANDOFF)
- **Knowledge Base:** engineering knowledge fájlok import (3 ÁTVÉTEL + 2 ADAPTÁLÁS)

## Output Artifacts

1. **`docs/planning/DWI_WORKFLOW.md`** (ÚJ)
   - 4 fázis: entrance criteria, exit criteria, artifact lista
   - RAG placeholder (knowledge_search fallback grep-re)

2. **`docs/tasks/new/MCP_Integration_Plan_v1.md`** (v2)
   - Infra review kész, nincs DB impakt

3. **CLAUDE.md kiegészítések** (5 terminál-specifikus)
   - Root: context hygiene, kommunikációs minták
   - Architect: 3-alternatíva döntési keretrendszer
   - Backend: QA Handoff kritérium
   - Frontend: FE tipizálás + QA Handoff
   - Tester: Zero Trust + Destructive Testing

4. **`docs/knowledge/engineering/`** (5 fájl import)
   - ÁTVÉTEL: testing_backend.md, testing_frontend.md, testing_strategy.md
   - ADAPTÁLÁS: backend_dotnet.md, frontend_react.md

## Implementációs Sorrend

1. **ROOT (1 session):** CLAUDE.md kiegészítések beépítése
2. **LIBRARIAN (1 session):** engineering knowledge fájlok import
3. **ROOT (0.5 session):** dry run tesztelés

## Kritikus Gap Azonosítás

**QA Handoff kritérium** hiányzott a SpaceOS workflow-ból:
- Mikor kell TESTER-t bevonni (mandatory gates)?
- Mikor egyedi backend/frontend QA elég?
- Destructive Testing vs. standard testing separation

**Megoldás:** Tester CLAUDE.md-be Zero Trust + bug report formátum + Destructive Testing guide

## Kockázatok

**Nincsenek** — dokumentáció-szintű változás, nincs deployment vagy DB impakt.

## Eredeti Dokumentum

`/opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_008_mcp-roles-dwi-done.md`
