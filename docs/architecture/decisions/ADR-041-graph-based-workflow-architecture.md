# ADR-041: Graph-Based Workflow & Project Management Architecture

> **Döntés:** 2026-06-22 PROPOSED
> **Státusz:** DRAFT → pending review
> **Szerző:** Architect terminál (MSG-ARCHITECT-005)

---

## Kontextus

A SpaceOS-ben három területen van igény gráf-alapú workflow reprezentációra:

1. **Agent Fleet** — Epic→Task függőségek, Conductor parallelizáció
2. **Manufacturing** — Ajtógyártás workflow, bottleneck detection
3. **Planning Pipeline** — Idea→Delivery gráf

Jelenleg a `TASKS.yaml` fájlok már tartalmaznak dependency információkat (`blocked_by`, `triggers_on_done`), de nincs:
- Epic-szintű dependency tracking (cross-project)
- Vizualizáció (Mermaid/D3/React Flow)
- Critical path számítás
- Gráf API endpoints

---

## Döntés

### Összefoglaló tábla

| Item | Decision |
|---|---|
| Gráf reprezentáció | **Adjacency List** (blocked_by + triggers_on_done) |
| Tárolás | **YAML fájlok** (EPICS.yaml, TASKS.yaml) |
| Gráf algoritmusok | **TypeScript runtime** (topological sort, cycle detection, critical path) |
| Vizualizáció Phase 1 | **Mermaid.js** (STATUS.md auto-gen) |
| Vizualizáció Phase 2+ | **React Flow** (Datahaven Dashboard) |
| Cross-project deps | **EPICS.yaml** a `/docs/projects/` root-ban |
| API | **Express endpoints** `/api/graph/*` |

---

## Alternatívák elemzése

### 1. Gráf reprezentáció

| Opció | Előny | Hátrány | Döntés |
|---|---|---|---|
| **Adjacency List** | Egyszerű YAML, sparse gráfokhoz optimális | N/A | **VÁLASZTVA** |
| Adjacency Matrix | Konstans lookup | Memória pazarlás sparse gráfnál | ❌ |
| Edge List | Egyszerű | Lassú neighbor lookup | ❌ |

**Indoklás:** A task dependency gráfok sparse-ok (átlagosan 1-3 dependency/task). Az adjacency list (blocked_by array) már létezik a TASKS.yaml-ban, nincs értelme változtatni.

### 2. Tárolás

| Opció | Előny | Hátrány | Döntés |
|---|---|---|---|
| **YAML fájlok** | Git verziókezelés, emberi olvashatóság | Nincs query | **VÁLASZTVA** |
| SQLite | Query, joins | Verziókezelés komplex | ❌ (Phase 2+ opció) |
| PostgreSQL | Production-ready | Overkill kis gráfokra | ❌ |

**Indoklás:** A YAML fájlok git-ben verziókezelhetők, a terminálok könnyen olvashatják, és a meglévő infrastruktúra (ProjectDispatcher) már YAML-ra épül.

### 3. Vizualizáció

| Opció | Komplexitás | Use Case | Phase |
|---|---|---|---|
| **Mermaid.js** | Low | STATUS.md generálás, statikus | **Phase 1** |
| D3.js | Medium | Datahaven read-only view | Phase 2 opció |
| **React Flow** | High | Interaktív szerkesztő | **Phase 2+** |

**Indoklás:** Phase 1 célja MVP — Mermaid elegendő. Phase 2-ben React Flow-ra váltunk a Datahaven Dashboard-ban az interaktivitáshoz.

---

## Architektúra

### Adatmodell

```typescript
// GraphNode — univerzális gráf node
interface GraphNode {
  id: string;                           // Unique ID (EPIC-CUTTING-Q3, TASK-001)
  type: 'epic' | 'task' | 'workflow_step' | 'milestone';
  name: string;
  status: 'pending' | 'active' | 'done' | 'blocked';
  depends_on: string[];                 // Bejövő élek (blocked_by equivalent)
  triggers: string[];                   // Kimenő élek (triggers_on_done equivalent)
  parallel_with?: string[];             // Explicit parallel ágak (hint)
  terminal?: string;                    // Assigned terminal (tasks only)
  metadata: Record<string, any>;        // Extra data (priority, model, etc.)
}

// WorkflowGraph — teljes gráf wrapper
interface WorkflowGraph {
  id: string;
  name: string;
  type: 'project' | 'epic_dependency' | 'manufacturing';
  nodes: GraphNode[];

  // Computed (runtime)
  critical_path?: string[];             // Longest path (node IDs)
  parallel_groups?: string[][];         // Parallel execution groups
  blocked_chains?: string[][];          // Currently blocked chains
  completion_percentage?: number;       // Done nodes / total
}

// EpicDependency — EPICS.yaml entry
interface EpicDependency {
  id: string;                           // EPIC-CUTTING-Q3
  name: string;                         // Human-readable name
  project: string;                      // Project slug (cutting-q3)
  depends_on: string[];                 // Epic IDs
  parallel_with?: string[];             // Hint for parallel execution
  status: 'pending' | 'active' | 'done' | 'blocked';
  target_date?: string;                 // YYYY-MM-DD
  tasks_yaml: string;                   // Relative path to TASKS.yaml
}
```

### EPICS.yaml Schema

```yaml
# /opt/spaceos/docs/projects/EPICS.yaml
version: "1.0"
updated: "2026-06-22"
epics:
  - id: EPIC-KERNEL-STABLE
    name: "Kernel Stability (L1)"
    project: spaceos/kernel
    depends_on: []
    status: done
    tasks_yaml: "spaceos/kernel/TASKS.yaml"

  - id: EPIC-CUTTING-Q3
    name: "Cutting Module Q3"
    project: spaceos/cutting
    depends_on: ["EPIC-KERNEL-STABLE"]
    parallel_with: ["EPIC-PORTAL-V2"]
    status: active
    target_date: "2026-09-30"
    tasks_yaml: "spaceos/cutting/TASKS.yaml"

  - id: EPIC-PORTAL-V2
    name: "Customer Portal v2"
    project: spaceos/portal
    depends_on: ["EPIC-IDENTITY-V1"]
    parallel_with: ["EPIC-CUTTING-Q3"]
    status: pending
    tasks_yaml: "spaceos/portal/TASKS.yaml"

  - id: EPIC-IDENTITY-V1
    name: "Identity Module v1"
    project: spaceos/identity
    depends_on: ["EPIC-KERNEL-STABLE"]
    status: done
    tasks_yaml: "spaceos/identity/TASKS.yaml"
```

### Gráf műveletek (TypeScript)

```typescript
// graph-operations.ts

/**
 * Topológiai rendezés (Kahn's algorithm)
 * Megadja a helyes végrehajtási sorrendet
 */
function topologicalSort(graph: WorkflowGraph): string[] {
  const inDegree = new Map<string, number>();
  const adjacency = new Map<string, string[]>();

  // Initialize
  for (const node of graph.nodes) {
    inDegree.set(node.id, node.depends_on.length);
    adjacency.set(node.id, node.triggers);
  }

  // Queue nodes with no dependencies
  const queue: string[] = [];
  for (const [id, degree] of inDegree) {
    if (degree === 0) queue.push(id);
  }

  const sorted: string[] = [];
  while (queue.length > 0) {
    const current = queue.shift()!;
    sorted.push(current);

    for (const neighbor of adjacency.get(current) || []) {
      const newDegree = inDegree.get(neighbor)! - 1;
      inDegree.set(neighbor, newDegree);
      if (newDegree === 0) queue.push(neighbor);
    }
  }

  // Check for cycle
  if (sorted.length !== graph.nodes.length) {
    throw new Error('Cycle detected in graph');
  }

  return sorted;
}

/**
 * Ciklus detektálás (DFS)
 * Már implementálva: yamlValidator.ts detectCircularDependencies()
 */
function detectCycles(graph: WorkflowGraph): string[][] {
  // Reuse existing implementation from yamlValidator.ts
}

/**
 * Critical path számítás (longest path DAG-ban)
 * A "legsürgősebb" útvonal a starttól a végig
 */
function findCriticalPath(graph: WorkflowGraph): string[] {
  // Topological sort first
  const sorted = topologicalSort(graph);

  // Distance from start
  const dist = new Map<string, number>();
  const prev = new Map<string, string | null>();

  for (const id of sorted) {
    dist.set(id, 0);
    prev.set(id, null);
  }

  // Find start nodes (no dependencies)
  const startNodes = graph.nodes
    .filter(n => n.depends_on.length === 0)
    .map(n => n.id);

  // Process in topological order
  for (const id of sorted) {
    const node = graph.nodes.find(n => n.id === id)!;
    for (const triggerId of node.triggers) {
      const newDist = dist.get(id)! + 1;
      if (newDist > dist.get(triggerId)!) {
        dist.set(triggerId, newDist);
        prev.set(triggerId, id);
      }
    }
  }

  // Find end node with max distance
  let maxDist = 0;
  let endNode = sorted[0];
  for (const [id, d] of dist) {
    if (d > maxDist) {
      maxDist = d;
      endNode = id;
    }
  }

  // Backtrack to get path
  const path: string[] = [];
  let current: string | null = endNode;
  while (current) {
    path.unshift(current);
    current = prev.get(current) || null;
  }

  return path;
}

/**
 * Párhuzamos ágak azonosítása
 * Két node párhuzamos, ha nincs köztük dependency path
 */
function findParallelGroups(graph: WorkflowGraph): string[][] {
  const sorted = topologicalSort(graph);
  const levels: string[][] = [];
  const nodeLevel = new Map<string, number>();

  for (const id of sorted) {
    const node = graph.nodes.find(n => n.id === id)!;
    const maxDepLevel = Math.max(
      0,
      ...node.depends_on.map(d => nodeLevel.get(d) || 0)
    );
    const level = maxDepLevel + 1;
    nodeLevel.set(id, level);

    if (!levels[level]) levels[level] = [];
    levels[level].push(id);
  }

  // Filter out single-node levels
  return levels.filter(l => l && l.length > 1);
}
```

### API Endpoints

```
GET  /api/graph/epics
     Returns: WorkflowGraph of all epics from EPICS.yaml

GET  /api/graph/project/:slug
     Returns: WorkflowGraph of tasks from project's TASKS.yaml

GET  /api/graph/workflow/:id
     Returns: WorkflowGraph of manufacturing workflow (future)

POST /api/graph/validate
     Body: { yaml: string }
     Returns: { valid: boolean, errors: string[], cycles: string[][] }

GET  /api/graph/critical-path/:type/:id
     Params: type = 'epic' | 'project' | 'workflow'
     Returns: { path: string[], length: number }

GET  /api/graph/parallel/:type/:id
     Returns: { groups: string[][] }

GET  /api/graph/mermaid/:type/:id
     Returns: { mermaid: string } — Mermaid diagram source
```

### Mermaid generátor

```typescript
/**
 * Generate Mermaid diagram from WorkflowGraph
 */
function generateMermaid(graph: WorkflowGraph): string {
  const lines = ['graph TD'];

  // Status → style mapping
  const statusStyle: Record<string, string> = {
    pending: ':::pending',
    active: ':::active',
    done: ':::done',
    blocked: ':::blocked',
  };

  // Nodes
  for (const node of graph.nodes) {
    const style = statusStyle[node.status] || '';
    lines.push(`    ${node.id}[${node.name}]${style}`);
  }

  // Edges
  for (const node of graph.nodes) {
    for (const depId of node.depends_on) {
      lines.push(`    ${depId} --> ${node.id}`);
    }
  }

  // Styles
  lines.push('');
  lines.push('    classDef pending fill:#f9f9f9,stroke:#999');
  lines.push('    classDef active fill:#e6f3ff,stroke:#0066cc');
  lines.push('    classDef done fill:#e6ffe6,stroke:#00cc00');
  lines.push('    classDef blocked fill:#ffe6e6,stroke:#cc0000');

  return lines.join('\n');
}
```

---

## Implementációs fázisok

### Phase 1: EPICS.yaml + Mermaid MVP (~3-4 nap)

| # | Task | Terminál | Output |
|---|---|---|---|
| 1.1 | EPICS.yaml schema + validator | Backend | `src/pipeline/epicsValidator.ts` |
| 1.2 | GraphNode interface + graph-ops | Backend | `src/graph/types.ts`, `operations.ts` |
| 1.3 | Mermaid generator | Backend | `src/graph/mermaidGenerator.ts` |
| 1.4 | `/api/graph/*` endpoints | Backend | `src/routes/graphRoutes.ts` |
| 1.5 | STATUS.md auto-update with Mermaid | Backend | `src/pipeline/statusUpdater.ts` |
| 1.6 | Conductor CLAUDE.md update | Architect | `terminals/conductor/CLAUDE.md` |

**Definition of Done:**
- EPICS.yaml létrehozva a SpaceOS epic-ekhez
- `GET /api/graph/epics` működik
- STATUS.md tartalmaz Mermaid gráfot
- Conductor tud epic-szintű függőségeket kezelni

### Phase 2: Dashboard vizualizáció (~5-6 nap)

| # | Task | Terminál | Output |
|---|---|---|---|
| 2.1 | React Flow component | Frontend | `src/components/Graph/WorkflowGraph.tsx` |
| 2.2 | Epic graph page | Frontend | `src/pages/EpicGraphPage.tsx` |
| 2.3 | Project task graph page | Frontend | `src/pages/ProjectGraphPage.tsx` |
| 2.4 | Real-time SSE integration | Frontend | SSE for status updates |
| 2.5 | Click-to-navigate (epic → project → task) | Frontend | Router integration |

**Definition of Done:**
- Datahaven Dashboard `/graph` oldalon látható a gráf
- Kattintással navigálhatunk epic → project → task
- Real-time frissül a státusz

### Phase 3: Workflow Builder (~8-10 nap)

| # | Task | Terminál | Output |
|---|---|---|---|
| 3.1 | Drag & drop node editor | Frontend | React Flow editor |
| 3.2 | Template library | Designer + Backend | Predefined workflows |
| 3.3 | YAML export/import | Backend | API endpoints |
| 3.4 | Validation feedback | Frontend | Inline error display |

### Phase 4: Manufacturing Integration (future)

- SpaceOS Kernel workflow engine
- Real-time order tracking
- SLA/deadline calculation
- Bottleneck detection

---

## Conductor CLAUDE.md frissítési javaslat

A Conductor CLAUDE.md-hez hozzáadandó szekció:

```markdown
## EPIC DEPENDENCY KEZELÉS

A Conductor figyelembe veszi az epic-szintű függőségeket is a task dispatch-nál.

### EPICS.yaml location

`/opt/spaceos/docs/projects/EPICS.yaml`

### Epic dispatch szabályok

1. **Blokkolt epic = blokkolt task dispatch**
   Ha egy epic függ egy másik epic-től, és az még nem `done`, akkor az epic taskjait NE indítsd el.

2. **Parallel epics**
   Ha két epic `parallel_with` kapcsolatban van, mindkettő taskjai indíthatók párhuzamosan (feltéve, hogy nincs más blokkoló).

3. **Epic státusz frissítés**
   Ha egy epic összes taskja `done`, frissítsd az epic státuszát is `done`-ra az EPICS.yaml-ban.

### Példa ellenőrzés

```bash
# Epic dependency check
yq '.epics[] | select(.id == "EPIC-CUTTING-Q3") | .depends_on[]' /opt/spaceos/docs/projects/EPICS.yaml
# Ha bármely dependency nincs done → ne indíts CUTTING taskot
```

### API használat

```bash
# Epic gráf lekérdezés
curl http://localhost:3456/api/graph/epics

# Critical path
curl http://localhost:3456/api/graph/critical-path/epic/EPICS

# Mermaid diagram
curl http://localhost:3456/api/graph/mermaid/epic/EPICS
```
```

---

## Integráció meglévő rendszerekkel

### ProjectDispatcher módosítások

A `ProjectDispatcher` (yamlValidator.ts, projectMatcher.ts, projectDispatcher.ts) már tartalmazza a core logikát:
- Cycle detection ✅
- Dependency tracking ✅
- Task dispatch ✅

**Szükséges kiegészítések:**
1. Epic-level dependency check dispatch előtt
2. EPICS.yaml watcher (epic status updates)
3. Graph API exposure

### Datahaven Dashboard

Új routes:
- `/graph` — Epic dependency view
- `/graph/:projectSlug` — Project task graph
- `/kanban` már létezik, kiegészül gráf linkkel

### Knowledge Service

Az MCP toolok bővítése:
- `mcp__spaceos-knowledge__get_epic_graph` — EPICS.yaml as graph
- `mcp__spaceos-knowledge__get_project_graph` — Project TASKS.yaml as graph
- `mcp__spaceos-knowledge__validate_graph` — Cycle/orphan detection

---

## Következmények

### Pozitív
- Conductor látja a teljes dependency fát → jobb parallelizáció
- Root vizuálisan követheti a project progress-t
- Blocker detection automatikus
- Cross-project coordination lehetséges

### Negatív / Trade-offs
- YAML file-ok kézi szerkesztésekor cycle-t okozhatunk → validator kötelező
- EPICS.yaml manuális karbantartást igényel egyelőre
- Phase 2 előtt nincs interaktív vizualizáció

### Kockázatok
- **Mermaid limit:** Nagy gráfok (100+ node) olvashatatlanok Mermaid-del → React Flow szükséges
- **Stale EPICS.yaml:** Ha nem frissítik, a dependency info elavul → automatikus status sync tervben

---

## Referenciák

- Kiindulási koncepció: `docs/knowledge/architecture/GRAPH_BASED_WORKFLOW.md`
- Meglévő infrastructure: `spaceos-nexus/knowledge-service/src/pipeline/`
- Conductor CLAUDE.md: `terminals/conductor/CLAUDE.md`
- ADR Catalogue: `docs/knowledge/architecture/ADR_CATALOGUE.md`

---

**Létrehozva:** 2026-06-22 Architect session (MSG-ARCHITECT-005)
**Következő lépés:** Root review → APPROVED → Phase 1 implementáció Backend terminálnak
