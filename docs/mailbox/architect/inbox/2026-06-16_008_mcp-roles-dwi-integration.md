---
id: MSG-ARCH-008
from: root
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-16
---

# Architect — JoineryTech.McpServer role és workflow integráció

## Kontextus

A JoineryTech.McpServer (`https://github.com/Szantoi/JoineryTech.McpServer`) elemzése
alapján a `database/roles/` és a DWI workflow közvetlen értéket ad a SpaceOS rendszernek.

## 1. Feladat — Role definíciók → SpaceOS CLAUDE.md gazdagítás

Olvasd el az alábbi role fájlokat a repo-ból:

```
database/roles/management/orchestrator/orchestrator.role.md
database/roles/discovery/architect/architect.role.md
database/roles/engineering/backend_developer/backend_developer.role.md
database/roles/engineering/frontend_developer/frontend_developer.role.md
database/roles/engineering/qa_tester/qa_tester.role.md
```

Azonosítsd, hogy melyik SpaceOS terminál CLAUDE.md-jébe milyen elemek kerüljenek be.
A konkrét mapping:

| McpServer role | SpaceOS terminál |
|---|---|
| orchestrator | Root (`/opt/spaceos/CLAUDE.md`) |
| architect | Architect (`/opt/spaceos/spaceos-architect/CLAUDE.md`) |
| backend_developer | Kernel, Joinery, Orch, Cutting, Inventory, Procurement |
| frontend_developer | FE, FE2, Portal |
| qa_tester | Tester |

Különösen értékes elemek amiket be kell építeni:
- **Checklist** minden role-hoz (pl. backend: "Are setters private? Domain validation?")
- **QA Handoff kritérium** (mikor kell Tester-t bevonni — jelenleg nincs dokumentálva)
- **Context hygiene szabályok** az Orchestrator role-ból (Root-nak releváns)
- **Persona & Communication** minták (Chain of Thought, Fact Summary, N-shot pattern)

**Output:** Konkrét szövegjavaslatokkal ellátott lista — melyik CLAUDE.md-be mi kerül.
Nem kell minden fájlt átírni — csak a kiegészítő blokkokat add meg, Root beépíti.

## 2. Feladat — DWI Workflow → Planning pipeline formalizálás

A DWI workflow (`database/roles/discovery/workflows/DWI.workflow.md`) fázisai:

```
IDEATION → VALIDATION → ITERATION → DELIVERY_HANDOFF
```

Ez pontosan leírja a SpaceOS planning pipeline-t:
```
plan-scan.sh → plan-select.sh → plan-debate.sh → Architect inbox
```

Amit a DWI-ből hiányzik a SpaceOS-ból:
- **Entrance criteria** minden fázishoz (pl. plan-select csak fut ha 5+ ötlet van — de nincs artifact ellenőrzés)
- **Exit criteria** és kötelező artifact lista fázisonként
- **`reference_prior_discovery()`** analog — RAG lekérdezés korábbi ötletek és döntések alapján

**Output:** `docs/planning/DWI_WORKFLOW.md` — SpaceOS-ra adaptált DWI dokumentum:
- Fázisonként: entrance criteria, tools, exit criteria, artifacts
- Hivatkozás a megfelelő scriptre (plan-scan.sh stb.)
- A RAG lekérdezés helye (placeholder — addig amíg a RAG nem kész)

## 3. Feladat — Engineering knowledge import lista

A `database/knowledge/engineering/` fájljai SpaceOS-ra azonnal alkalmazhatók:

```
backend_dotnet.knowledge.md
database_efcore.knowledge.md
efcore_installation.knowledge.md
frontend_react.knowledge.md
testing_backend_dotnet.knowledge.md
testing_frontend_react.knowledge.md
testing_strategy.knowledge.md
```

Olvasd el ezeket és döntsd el:
- Melyik fájl tartalmaz SpaceOS-specifikus kiegészítést ami hiányzik a `docs/knowledge/`-ből?
- Mi kerül `docs/knowledge/engineering/`-be változtatás nélkül?
- Mi igényel adaptálást (pl. SpaceOS-specifikus path-ok, package verziók)?

**Output:** Import javaslat lista — mit másolunk, mit adaptálunk, mit hagyunk ki.

## Elvárt output összesítve

```
docs/planning/DWI_WORKFLOW.md              ← ÚJ
docs/tasks/new/MCP_Integration_Plan_v1.md  ← role + knowledge import terv
```

Az MCP_Integration_Plan_v1.md tartalmazza:
1. Role → CLAUDE.md mapping táblázat konkrét szövegrészletekkel
2. Engineering knowledge import lista döntésekkel
3. Implementációs sorrend (ki végzi el, mikor)

spaceos-arch-planner pipeline: ez közepes scope → minimum v2 (DB/infra review) kötelező.

DONE outbox mikor mindkét output fájl kész.
