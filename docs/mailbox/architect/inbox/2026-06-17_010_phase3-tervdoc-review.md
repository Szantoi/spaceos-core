---
id: MSG-ARCH-010
from: root
to: architect
type: task
priority: high
status: UNREAD
model: opus
ref: MSG-CONDUCTOR-005
created: 2026-06-17
---

# PHASE 3 Tervdokumentumok — Root Konszultáció

## Felkérés

Root stratégiai konzultáció szükséges **3 HIGH priority tervdokumentum** ADR-finiáziójához, amit a **Conductor** a planning queue-ba fog kiadni.

---

## 3 Tervdokumentum felülvizsgálatra

### 1. **SpaceOS_Marvin_McpServer_Migration_v1.md** (20K)
   - **Fázis:** Infrastructure — Marvin orchestrátor + planning pipeline automatizálása
   - **Fázis 1-2:** plan-scan.sh → Marvin Task, plan-debate.sh, nightwatch.sh → Marvin Scheduler
   - **Fázis 3:** reviewer.sh, nightwatch.sh → Marvin
   - **Kérdés:** ADR-043 (Marvin orchestration pattern) definiálása
   - **Függ-e:** McpServer knowledge_search tool (Phase 1 COMPLETE)

### 2. **RAG_Knowledge_Base_v1.md** (18K)
   - **Fázis:** Infrastructure — Knowledge Service scale-up + system-wide integration
   - **Cél:** Datahaven/Resonance foundation (Librarian + Haiku scanner + Architect + terminálok)
   - **Komponensek:** ChromaDB, Voyage AI embeddings, FTS indexing, Librarian cron
   - **Kérdés:** ADR-044 (Knowledge Service system integration) definiálása
   - **Függ-e:** Nexus Phase 1 (MSG-NEXUS-001) — COMPLETE ✅

### 3. **MCP_Integration_Plan_v1.md** (9.7K)
   - **Fázis:** Infrastructure — McpServer toolkit kiterjesztés
   - **Új toolok:** submitArtifact, getWorkflowState, updateWorkflowState, RbacFilter
   - **Cél:** Marvin + terminálok McpServer interfész standardizálása
   - **Kérdés:** ADR-045 (McpServer standard tools + RPC interface) definiálása

---

## Root elvárások

### ADR előkészítés (Architect döntés)

Minden terv + ADR-ből:
- **Döntési fa:** Mi az architekturális döntés? Mi az alternatíva? Miért ez?
- **Kritikus függőségek:** Mit blokkolhatnak? Milyen erőforrások szükségesek?
- **Fázis lebontás:** Slice 1, Slice 2, Slice 3 során mely feladatok?

### Golden Rule ellenőrzés

Minden ADR-ben meg kell jelennie:

| Szabály | Ellenőrzés |
|---|---|
| **Data → Rules → Geometry** | Marvin/McpServer nem számol, csak paramétereket adnak? |
| **Modular Monolith** | Interface-k decoupled? |
| **Immutability & Trust** | Audit trail van-e? |
| **Need-to-Know RBAC** | RbacFilter implementálva? |
| **Walking Skeleton First** | E2E pipeline előbb? |

---

## Conductor feladat

Miután az **Architect visszaküld 3 ADR-t**, a **Conductor** a planning queue-ba kiadja:

```
PHASE 3 QUEUE:
  1. ADR-043 (Marvin) → Nexus terminál (tervdok implementáció)
  2. ADR-044 (RAG) → Librarian + Nexus (Knowledge Base szintetizálása)
  3. ADR-045 (MCP) → Nexus (McpServer toolkit bővítés)
```

---

## Kontextus (Architect-hez)

**PHASE 2 COMPLETE státusz:**
- ✅ Doorstar Soft Launch LIVE (10:30 UTC, 2026-06-17)
- ✅ Production operational: Orchestrator + Knowledge Service
- ✅ Planning queue üres, new/ mappa 8 terv vár review

**Slice 1 (TOP 1-3) ütemezés:**
- TOP 1: Design→Cutting workflow (FE 2-3 nap, MSG-FE-061 aktív)
- TOP 2: Nesting vizualizáció (FE 3-4 nap, MSG-FE-062 aktív)
- TOP 3: Machine Scheduling UI (1 nap BE + 4-5 nap FE, MSG-CUTTING-054 + MSG-FE-063)

**Slice 2 előkészítés:**
- Sales FrontOffice Contract Reconciliation (v5)
- CuttingUI + NestingViz (TOP 4-5)

---

## Határidő

- **ADR-043, ADR-044, ADR-045** — 2026-06-17 vége vagy 2026-06-18 reggel
- **Root** majd MSG-CONDUCTOR-005 alapján aktiválja az indexer/Marvin feladatokat

---

*Architect megjegyzés: Ez nem tervdokumentum-schrij feladat (azt ALREADY DONE a tervdoc), hanem ADR **fokuszrész** — Marvin/RAG/MCP szükséges feltételrendszere és döntési fa.*
