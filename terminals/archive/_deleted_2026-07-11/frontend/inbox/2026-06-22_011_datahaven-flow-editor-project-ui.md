---
id: MSG-FRONTEND-011
from: root
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: ADR-041
created: 2026-06-22
content_hash: a67b3d8cd7fa7e55953f27f0f2fd55d11f23bccd29fcd6f7906900c091cd71ac
---

# Datahaven Dashboard — Flow Editor & Project UI

## Kontextus

Az ADR-041 (Graph-Based Workflow Architecture) elkeszult. A backend resz (TypeScript types, operations, mermaidGenerator) mar implementalva van: `spaceos-nexus/knowledge-service/src/graph/`.

Most a **frontend vizualizacio** kell a Datahaven Dashboard-hoz.

## Feladat

Implementald a kovetkezo ket uj oldalt/komponenst a Datahaven Dashboard-on:

### 1. Flow Editor oldal (`/flow` vagy `/graph`)

**Cél:** Epic és task dependency gráf vizualizáció és szerkesztés.

**Funkciók:**
- Mermaid diagram renderelés (API: `GET /api/graph/mermaid/epic/EPICS`)
- Vagy React Flow interaktív gráf (jobb UX)
- Epic node-ok megjelenítése státusszal (pending/active/done/blocked)
- Dependency élek vizualizáció
- Critical path kiemelés
- Node kattintás → részletek panel

**API endpointok (már működnek):**
```
GET  /api/graph/epics                    → WorkflowGraph
GET  /api/graph/mermaid/epic/EPICS       → Mermaid string
GET  /api/graph/critical-path/epic/EPICS → CriticalPathResult
GET  /api/graph/parallel/epic/EPICS      → ParallelGroupsResult
POST /api/graph/validate                 → GraphValidationResult
```

**TypeScript típusok:**
```typescript
// spaceos-nexus/knowledge-service/src/graph/types.ts
interface GraphNode {
  id: string;
  type: 'epic' | 'task' | 'workflow_step';
  name: string;
  status: 'pending' | 'active' | 'done' | 'blocked';
  depends_on: string[];
  triggers: string[];
  parallel_with?: string[];
}

interface WorkflowGraph {
  id: string;
  name: string;
  nodes: GraphNode[];
  execution_order?: string[];
  critical_path?: string[];
  parallel_groups?: string[][];
}
```

### 2. Projects oldal frissítés (`/projects`)

**Jelenlegi állapot:** Gantt timeline + projekt lista.

**Bővítés:**
- Epic dependency vizualizáció integrálás
- Mini-graph preview minden projektnél
- Status badge-ek (pending/active/done/blocked)
- Link a Flow Editor oldalra

## Technikai követelmények

1. **React komponensek:**
   - `FlowEditor.tsx` — fő oldal komponens
   - `EpicGraph.tsx` — gráf renderelő (Mermaid vagy React Flow)
   - `NodeDetails.tsx` — node részletek panel

2. **Mermaid integráció:**
   ```bash
   npm install mermaid
   ```
   Vagy **React Flow** (interaktívabb):
   ```bash
   npm install reactflow
   ```

3. **API hook-ok:**
   - `useEpicGraph()` — graph data fetch
   - `useCriticalPath()` — critical path highlight

## Definition of Done

- [ ] `/flow` vagy `/graph` oldal működik
- [ ] Epic gráf renderelődik (Mermaid vagy React Flow)
- [ ] Node státuszok színekkel jelölve
- [ ] Critical path kiemelés
- [ ] `/projects` oldalon mini-graph preview
- [ ] Responsive design (mobile is)
- [ ] Típusok importálva a backend-ből (vagy duplikálva)

## Referencia

- ADR: `docs/architecture/decisions/ADR-041-graph-based-workflow-architecture.md`
- TypeScript types: `spaceos-nexus/knowledge-service/src/graph/types.ts`
- EPICS.yaml: `docs/projects/EPICS.yaml`
- Datahaven client: `datahaven-web/client/src/`

## Megjegyzés

A Mermaid egyszerűbb megoldás (read-only vizualizáció), de React Flow interaktívabb (drag-drop szerkesztés később). Első körben Mermaid elég, később bővíthető.
