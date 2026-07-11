---
id: MSG-ARCHITECT-005
from: root
to: architect
type: task
priority: high
status: READ
model: opus
ref: docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md
created: 2026-06-22
content_hash: a64d7aae2b6ca19621b04ca121ad85ee5a07be9f0573ccc93283bd1e5163e51f
---

# Graph-Based Workflow & Project Management - Architecture Design

## Kontextus

A SpaceOS-ben minden workflow és projekt **gráfként** reprezentálható. Ez a koncepció 3 területet fed le:

1. **Agent Fleet** - Epic→Task függőségek, Conductor parallelizáció
2. **Manufacturing** - Ajtógyártás workflow, bottleneck detection
3. **Planning Pipeline** - Idea→Delivery gráf

## Feladat

Tervezd meg a Graph-Based Workflow architektúrát. A kiindulási koncepció itt van:
`docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md`

### Kérdések amikre válaszolj:

1. **Adatmodell**
   - Hogyan reprezentáljuk a gráfot? (adjacency list vs matrix vs edge list)
   - Hol tároljuk? (YAML fájlok vs SQLite vs PostgreSQL)
   - Verziókezelés szükséges? (gráf history)

2. **EPICS.yaml schema**
   - Milyen mezők kellenek az epic-ekhez?
   - Hogyan kapcsolódik a TASKS.yaml-hoz?
   - Cross-project függőségek kezelése?

3. **Gráf műveletek**
   - Topológiai rendezés (végrehajtási sorrend)
   - Ciklus detektálás (DAG validáció)
   - Critical path számítás
   - Parallel ágak azonosítása

4. **Vizualizáció**
   - Mermaid vs D3.js vs React Flow - melyiket és miért?
   - Real-time frissítés hogyan?
   - Mobile support?

5. **Integráció**
   - Conductor hogyan használja? (auto-dispatch)
   - ProjectDispatcher módosítások
   - Dashboard API endpoints

### Output

1. **ADR-041** - Graph-based workflow architecture decision
2. **Schema definíciók** - EPICS.yaml, GraphNode interface
3. **Implementációs terv** - Phase 1-4 részletezése
4. **API spec** - `/api/graph/*` endpoints

## Referenciák

- Koncepció: `docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md`
- ProjectDispatcher: `spaceos-nexus/knowledge-service/src/pipeline/projectDispatcher.ts`
- TASKS.yaml példa: Conductor CLAUDE.md-ben

## Definition of Done

- [ ] ADR-041 megírva
- [ ] EPICS.yaml schema definiálva
- [ ] GraphNode TypeScript interface
- [ ] API endpoints specifikálva
- [ ] Phase 1 (Mermaid MVP) részletes terv
- [ ] Conductor CLAUDE.md frissítési javaslat
