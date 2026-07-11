---
id: SPEC-008
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-17_010_phase3-adr-review-done.md
type: Architecture Decision Records (ADR)
scope: [nexus, librarian, kernel, orch, portal, infra]
priority: high
complexity: 4
dependencies: [ADR-042 (Knowledge Service Phase 1), PHASE 3 planning]
status: NEW
created: 2026-06-17
---

# PHASE 3 ADR Review — 3 ADR elkészítve

## Összefoglaló

Architect 3 Architecture Decision Record-ot (ADR-043, ADR-044, ADR-045) elkészített a PHASE 3 tervdokumentumok alapján. Az ADR-ek a Marvin orchestration pattern, Knowledge Service system-wide integráció, és McpServer RBAC kiterjesztést definiálják.

## Scope

Az ADR-ek az alábbi terminálokat és komponenseket érintik:
- **NEXUS**: Marvin Python agent pipeline implementáció (ADR-043)
- **LIBRARIAN**: Knowledge Service system-wide integráció (ADR-044)
- **INFRA**: VPS memória bővítés szükséges (8GB → 16GB)
- **KERNEL, ORCH, PORTAL**: Knowledge Service consumer interfaces (ADR-044, ADR-045)

## Elkészült ADR-ek

### ADR-043: Marvin Orchestration Pattern
- **Döntés:** Marvin (Python) veszi át a bash planning pipeline-t
- **Komponensek:** Resumable threads (SQLite history), 5 agent (Scanner, Selector, Debater A/B, Synthesizer), structured output (Pydantic), parallel execution (asyncio)
- **Fázis:** Fázis 2 (Marvin Planning Pipeline ~6-7 nap), Fázis 3 (Reviewer + Nightwatch ~8-10 nap)
- **Golden Rule:** ✅ Data → Rules → Geometry, ✅ Modular Monolith, ✅ Immutability, ⚠️ RBAC (Fázis 3)

### ADR-044: Knowledge Service System Integration
- **Döntés:** Knowledge Service scale-up full Datahaven/Resonance foundation-hez
- **Komponensek:** ChromaDB + Voyage AI embeddings, in-memory fallback, system-wide integration
- **Fázis:** Fázis 2 (System-wide integration ~3-4 nap), Fázis 3 (Full Datahaven/Resonance ~5-6 nap)
- **Golden Rule:** ✅ Data → Rules → Geometry, ✅ Modular Monolith, ✅ Immutability, ⚠️ RBAC (Fázis 3)

### ADR-045: McpServer Standard Tools & RPC Interface
- **Döntés:** McpServer toolkit kiterjesztése + RbacFilter middleware
- **Komponensek:** `discovery_search` (COMPLETE), `submitArtifact` (Fázis 2), `getWorkflowState`/`updateWorkflowState` (Fázis 3)
- **Fázis:** Fázis 2 (submitArtifact ~2-3 nap), Fázis 3 (Workflow tracking + RBAC ~4-5 nap)
- **Golden Rule:** ✅ Összes rule teljesül, ✅ RbacFilter gépi enforcement

## Implementációs javaslat

### Fázis 2 (3-4 hét)
1. **NEXUS**: Marvin Planning Pipeline implementáció (6-7 nap)
   - SQLite history schema definiálása
   - 5 agent explicit logika
   - Asyncio orchestration és resumable threads
   - Parallel execution gates

2. **LIBRARIAN + NEXUS**: Knowledge Service system-wide integráció (3-4 nap)
   - Architect terminálokból ChromaDB vector search
   - Planning selector vectorDB-vel
   - Terminál contextus injection

3. **NEXUS**: submitArtifact McpServer tool (2-3 nap)
   - Workflow artifact capture
   - Pydantic model validation
   - RbacFilter middleware

### Fázis 3 (2-3 hét)
1. **NEXUS**: Reviewer + Nightwatch Marvin integrációja (8-10 nap)
2. **NEXUS**: Full Datahaven/Resonance foundation (5-6 nap)
3. **NEXUS**: Workflow state tracking + RBAC finalizáció (4-5 nap)

## Kockázatok

1. **Infrastruktúra blokkoló**: VPS memória bővítés szükséges (8GB → 16GB)
   - Marvin + ChromaDB + McpServer párhuzamos futása memória-igényes
   - **Mitigation**: ASAP VPS upgrade szervezése

2. **Fázis 2 függőségek**: ADR-043, ADR-044, ADR-045 közötti tight coupling
   - Planning pipeline és Knowledge Service egymásra épülnek
   - **Mitigation**: Parallel development, clear interface contracts (Contracts NuGet minta)

3. **RBAC komplexitás**: Need-to-Know RBAC Fázis 3-ban implementálva
   - Interim (Fázis 2): Tool listing szinten NEM szűr
   - **Mitigation**: RbacFilter middleware előbb tervezni, deferred implementation

## Golden Rule ellenőrzés

Mindhárom ADR megfelel az 5 Golden Rule-nak:
- ✅ Data → Rules → Geometry
- ✅ Modular Monolith
- ✅ Immutability & Trust
- ⚠️ Need-to-Know RBAC (Fázis 3 completion-ben)
- ✅ Walking Skeleton First (fázisonként)

## Conductor követő lépések

Az ADR-ek PROPOSED státuszban vannak. Conductor a planning queue-ba kiadhatja:

**PHASE 3 Queue:**
1. ADR-043 (Marvin) → NEXUS terminál
2. ADR-044 (RAG) → LIBRARIAN + NEXUS
3. ADR-045 (MCP) → NEXUS

**Blokkoló (CRITICAL):** VPS memória bővítés (8GB → 16GB)

## Létrehozott fájlok

```
docs/architecture/decisions/
  ADR-043-marvin-orchestration-pattern.md              (NEW)
  ADR-044-knowledge-service-system-integration.md      (NEW)
  ADR-045-mcpserver-standard-tools.md                  (NEW)

docs/knowledge/architecture/
  ADR_CATALOGUE.md                                     (UPDATED)
```

## Eredeti dokumentum

- Architect outbox: `/opt/spaceos/docs/mailbox/architect/outbox/2026-06-17_010_phase3-adr-review-done.md`
- Architect message ID: `MSG-ARCH-010-DONE`
- Status: APPROVED_BY_ROOT (MSG-ROOT-038-ADR-APPROVED)
