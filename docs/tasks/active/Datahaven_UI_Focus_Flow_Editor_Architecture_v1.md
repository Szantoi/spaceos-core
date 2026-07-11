# Datahaven UI — Focus Area Panel + Flow/Workflow Editor

> **Architecture Design Document v1**
> Status: DRAFT
> Created: 2026-06-23
> Architect Terminal
> Request: MSG-ARCHITECT-010

---

## Executive Summary

This document proposes two new UI components for the Datahaven Dashboard:

1. **Focus Area Panel** — Displays and edits the `docs/planning/domain-focus.md` file (7 domain options + criteria list)
2. **Flow/Workflow Editor** — Visualizes and edits the `docs/projects/EPICS.yaml` file using an interactive Mermaid graph

**Key Design Principles:**
- **Consistent with existing Datahaven style** — Dark theme, card-based layout, accent colors
- **Progressive disclosure** — Simple view first, advanced controls on demand
- **API-first** — All state changes via REST API (no direct file writes)
- **Mobile-friendly** — Planning pipeline works on tablets (workflow editor desktop-only)

**Placement Strategy:**
- **Focus Area Panel** → Dashboard page (new sidebar section) OR Planning page (top panel)
- **Flow/Workflow Editor** → Planning page, `Workflow` tab (currently empty placeholder)

---

## 1. Focus Area Panel — UI Design

### 1.1 Purpose

The Focus Area Panel allows the **Conductor** terminal (or Root) to:
- **Select the active planning domain** from 7 predefined domains
- **View the domain criteria list** (markdown-formatted guidance)
- **Edit the criteria** when planning focus shifts (e.g., from manufacturing to sales)

This panel visualizes the `/opt/spaceos/docs/planning/domain-focus.md` file.

### 1.2 UI Mockup Description

```
┌─────────────────────────────────────────────────────────┐
│ Focus Area                                 🔄 Sync      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Domain:  [▼ manufacturing            ]                 │
│                                                         │
│ Criteria:                                               │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ - **Felhasználói érték**: Milyen funkció segíti... │ │
│ │ - **Backend kapcsolhatóság**: Van-e már meglévő... │ │
│ │ - **Iparági minták**: Mi az ami más ERP/MES...     │ │
│ │ - **Mobil első**: A funkciónak működnie kell...    │ │
│ │ - **Offline tűrés**: Ha az internet kimegy...      │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ [Edit Criteria] [Save Changes]                         │
└─────────────────────────────────────────────────────────┘
```

**Visual Elements:**
- **Card style** — `.panel` class (existing Datahaven component)
- **Domain dropdown** — 7 options: `manufacturing`, `sales`, `logistics`, `finance`, `quality`, `hr`, `all`
- **Criteria display** — Markdown rendered to HTML (using `marked.js` library)
- **Edit mode** — Click `[Edit Criteria]` → textarea appears with raw markdown
- **Save button** — Triggers `PUT /api/planning/domain-focus` (new endpoint)
- **Sync indicator** — Shows last sync time or "Not synced" if local edits pending

### 1.3 Component Placement — Option Analysis

| Option | Placement | Pros | Cons | Recommendation |
|--------|-----------|------|------|----------------|
| **A** | Dashboard page, top-right sidebar | Visible to all users; quick access | Clutters dashboard with planning-specific info | ⭐ **Recommended if dashboard is main view** |
| **B** | Planning page, above pipeline diagram | Contextual (planning domain affects pipeline); clear hierarchy | Hidden if user not on Planning page | ⭐⭐ **Recommended** (best contextual fit) |
| **C** | New Settings page (`/settings.html`) | Clean separation of concerns; reusable for other config | Extra navigation step; low discoverability | ⚠️ Use if more settings panels planned |

**Decision Recommendation: Option B (Planning page, top panel)**

Reasoning:
- The domain focus **directly affects the planning pipeline** (Haiku scanner uses this file)
- Planning page already has a `Pipeline Overview` section — Focus Area fits naturally above it
- Users visiting `/planning.html` are already in "planning mode" — this is high-value contextual info

### 1.4 Data Flow

```
User Action                API Call                     Backend                    File Update
─────────────────────────  ──────────────────────────── ────────────────────────── ────────────────────
Load Planning Page      → GET /api/planning/domain-focus  Read domain-focus.md   →  Return JSON
                                                                                     { domain, criteria }

Select Domain (dropdown)→ PUT /api/planning/domain-focus  Validate domain value  →  Write domain-focus.md
                          { domain: "sales" }            Update YAML frontmatter   (atomic write)

Edit Criteria (textarea)→ PUT /api/planning/domain-focus  Validate markdown      →  Write domain-focus.md
                          { criteria: "..." }            Sanitize input            (atomic write)

Click [Sync]            → GET /api/planning/domain-focus  Re-read file           →  Return latest JSON
```

**API Endpoint Proposal:**

```typescript
// GET /api/planning/domain-focus
{
  "domain": "manufacturing",
  "criteria": "- **Felhasználói érték**: ...",
  "updated_at": "2026-06-23T12:34:56Z"
}

// PUT /api/planning/domain-focus
Request Body:
{
  "domain": "sales",           // Optional (if only domain changed)
  "criteria": "- New criteria" // Optional (if only criteria changed)
}

Response:
{
  "success": true,
  "domain": "sales",
  "criteria": "- New criteria",
  "updated_at": "2026-06-23T12:35:01Z"
}
```

---

## 2. Flow/Workflow Editor — UI Design

### 2.1 Purpose

The Flow/Workflow Editor provides an **interactive graph view** of the `EPICS.yaml` dependency structure. Users can:
- **Visualize epic dependencies** (Mermaid graph with status colors)
- **Inspect epic details** (name, description, status, depends_on, parallel_with)
- **Change epic status** (pending → active → done → blocked)
- **Edit dependencies** (add/remove depends_on, parallel_with)
- **Export Mermaid diagram** (for documentation, Slack, etc.)
- **View critical path** (longest dependency chain)

### 2.2 UI Mockup Description

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ Workflow Editor                                   [Export Mermaid] [Validate]  │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│ ┌─ Mermaid Graph View ──────────────────────────────────────────────────────┐  │
│ │                                                                           │  │
│ │   EPIC-KERNEL-STABLE ──────┬─────────→ EPIC-JOINERY-V2                   │  │
│ │        (done, green)        │                (done, green)                │  │
│ │                             │                                             │  │
│ │                             ├─────────→ EPIC-CUTTING-Q3                   │  │
│ │                             │           (active, blue) ← SELECTED        │  │
│ │                             │                                             │  │
│ │                             └─────────→ EPIC-INVENTORY-V1                 │  │
│ │                                         (done, green)                     │  │
│ │                                                                           │  │
│ │   EPIC-JOINERY-V2 ─────────────────→ EPIC-ORCH-V2 ───────→ EPIC-PORTAL-V2│  │
│ │   EPIC-IDENTITY-V1 ────────────────┘    (done, green)      (active, blue)│  │
│ │                                                                           │  │
│ └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
│ ┌─ Epic Details (EPIC-CUTTING-Q3) ─────────────────────────────────────────┐  │
│ │ Name: Cutting Module Q3                                                  │  │
│ │ Status: [▼ active      ] ← editable dropdown                             │  │
│ │ Target Date: 2026-09-30                                                  │  │
│ │                                                                           │  │
│ │ Dependencies (depends_on):                                               │  │
│ │   • EPIC-KERNEL-STABLE (done)                                            │  │
│ │   [+ Add Dependency]                                                     │  │
│ │                                                                           │  │
│ │ Parallel With:                                                           │  │
│ │   • EPIC-JOINERY-V2 (done)                                               │  │
│ │   • EPIC-INVENTORY-V1 (done)                                             │  │
│ │   [+ Add Parallel Epic]                                                  │  │
│ │                                                                           │  │
│ │ Description:                                                             │  │
│ │ Lapszabász modul: nesting, optimization, CNC integration.                │  │
│ │                                                                           │  │
│ │ [Save Changes]                                                           │  │
│ └───────────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**Visual Elements:**
- **Mermaid Graph Canvas** — Top 60% of panel (scrollable, zoomable)
- **Epic Details Panel** — Bottom 40% (shows selected epic, collapses when no selection)
- **Color Coding** (Mermaid node styles):
  - `pending` → Grey fill, grey border
  - `active` → Blue fill, blue border (thick)
  - `done` → Green fill, green border
  - `blocked` → Red fill, red border (thick)
- **Interactive Nodes** — Click to select (highlights node + shows details panel)
- **Dependency Lines** — Solid arrow for `depends_on`, dashed arrow for `parallel_with`

### 2.3 Component Placement

**Location:** Planning page, `Workflow` tab

The Planning page already has 6 tabs:
```
[Workflow] [Ideas] [Selected] [Debate] [Queue] [Pipeline Logs]
   ↑ Currently empty placeholder (planning.html:64)
```

The Workflow tab is **ideal** for the Flow/Workflow Editor because:
- It's a **separate tab** → No visual clutter on the main pipeline view
- The name "Workflow" matches the concept (epic dependency workflow)
- Users who need deep planning coordination will navigate here intentionally

### 2.4 Mermaid Rendering Strategy

**Library:** [mermaid.js](https://mermaid.js.org/) v10+

**Rendering Flow:**
```
1. Frontend calls GET /api/graph/mermaid/epic/EPICS
2. Backend returns { mermaid: "graph LR\n...", node_count: 10 }
3. Frontend injects into <div id="mermaid-container">
4. mermaid.init() renders the diagram
5. Add click handlers to nodes (mermaid supports callbacks)
```

**Interactivity:**
- Mermaid supports `click` events on nodes → attach event listeners
- On node click → fetch epic details, populate details panel
- Use CSS to highlight selected node (add `.selected` class)

**Zoom & Pan:**
- Wrap Mermaid canvas in a `pan-zoom` container (use [panzoom.js](https://github.com/timmywil/panzoom))
- Allow mouse wheel zoom + drag to pan

### 2.5 Data Flow

```
User Action                    API Call                          Backend                          File Update
────────────────────────────── ───────────────────────────────── ──────────────────────────────── ──────────────────
Load Workflow Tab           → GET /api/graph/mermaid/epic/EPICS  Read EPICS.yaml, generate      → Return Mermaid
                                                                 Mermaid diagram

Click Epic Node             → GET /api/graph/epics               Read EPICS.yaml, find epic     → Return epic JSON
                                                                 by ID

Change Status Dropdown      → PUT /api/graph/epics/:id           Validate status transition     → Write EPICS.yaml
                              { status: "done" }                 (pending → active → done)       (atomic write)

Add Dependency              → PUT /api/graph/epics/:id           Validate no cycles introduced  → Write EPICS.yaml
                              { depends_on: ["EPIC-X", "EPIC-Y"] } Run cycle detection           (atomic write)

Remove Dependency           → PUT /api/graph/epics/:id           Remove from depends_on array   → Write EPICS.yaml
                              { depends_on: ["EPIC-X"] }                                         (atomic write)

Export Mermaid              → GET /api/graph/mermaid/epic/EPICS  Return raw Mermaid syntax      → Download .mmd file
```

**API Endpoint Proposals:**

```typescript
// GET /api/graph/epics
// (Already exists — see graphRoutes.ts:56)
{
  "graph": {
    "id": "EPICS",
    "nodes": [
      {
        "id": "EPIC-CUTTING-Q3",
        "name": "Cutting Module Q3",
        "status": "active",
        "depends_on": ["EPIC-KERNEL-STABLE"],
        "parallel_with": ["EPIC-JOINERY-V2"],
        "target_date": "2026-09-30",
        "description": "Lapszabász modul...",
        ...
      }
    ]
  }
}

// GET /api/graph/mermaid/epic/EPICS
// (Already exists — see graphRoutes.ts:252)
{
  "mermaid": "graph LR\n  EPIC-KERNEL-STABLE(...)",
  "node_count": 10
}

// PUT /api/graph/epics/:id (NEW endpoint needed)
Request Body:
{
  "status": "done",                          // Optional
  "depends_on": ["EPIC-X", "EPIC-Y"],        // Optional
  "parallel_with": ["EPIC-Z"],               // Optional
  "target_date": "2026-12-31"                // Optional
}

Response:
{
  "success": true,
  "epic": { ... },                           // Updated epic object
  "validation": {
    "valid": true,
    "cycles": []                             // Empty if valid DAG
  }
}

// POST /api/graph/validate
// (Already exists — see graphRoutes.ts:133)
// Used before saving to check for cycles
```

### 2.6 Status Transition Rules

**Valid transitions:**
```
pending → active → done
pending → blocked
active → blocked
blocked → active (retry)
```

**Invalid transitions (blocked by validation):**
```
done → pending  ❌ (cannot un-complete an epic)
done → active   ❌
```

**Backend validation:**
- Check transition is valid (state machine)
- If status = `done`, verify all `depends_on` epics are also `done`
- If adding dependency, run cycle detection (use `detectCycles()` from graph/operations.ts)

---

## 3. Component Placement Strategy — Summary

| Component | Page | Position | Justification |
|-----------|------|----------|---------------|
| **Focus Area Panel** | Planning | Top panel, above pipeline overview | Contextual fit — domain affects planning pipeline |
| **Flow/Workflow Editor** | Planning | `Workflow` tab (existing placeholder) | Separate view for power users; no clutter on main page |

**Layout Hierarchy (Planning page after implementation):**

```
Planning Page
├── Header (nav links, health indicator)
├── Pipeline Overview (Ideas → Selected → Debate → Queue)
├── ⭐ Focus Area Panel (NEW) ← Domain dropdown + criteria
├── Stage Tabs ([Workflow] [Ideas] [Selected] [Debate] [Queue] [Logs])
└── Stage Content
    ├── ⭐ Workflow Tab (NEW) ← Flow/Workflow Editor
    ├── Ideas Tab
    ├── Selected Tab
    ├── Debate Tab
    ├── Queue Tab
    └── Logs Tab
```

---

## 4. Data Flow Diagrams

### 4.1 Focus Area Panel — Read Flow

```
┌─────────────┐
│   Browser   │
│ Planning Pg │
└──────┬──────┘
       │ 1. Page load
       ▼
┌─────────────────────────────────┐
│ GET /api/planning/domain-focus  │
└──────┬──────────────────────────┘
       │ 2. API call
       ▼
┌───────────────────────────────────────┐
│ Knowledge Service                     │
│ src/api/planningRoutes.ts (NEW)       │
└──────┬────────────────────────────────┘
       │ 3. Read file
       ▼
┌─────────────────────────────────┐
│ docs/planning/domain-focus.md   │
└──────┬──────────────────────────┘
       │ 4. Parse YAML frontmatter + markdown
       ▼
┌────────────────────────────────────┐
│ JSON Response                      │
│ { domain, criteria, updated_at }   │
└──────┬─────────────────────────────┘
       │ 5. Return to browser
       ▼
┌─────────────┐
│   Browser   │
│ Render panel│
└─────────────┘
```

### 4.2 Focus Area Panel — Write Flow

```
┌─────────────┐
│   Browser   │
│ User edits  │
└──────┬──────┘
       │ 1. Click [Save Changes]
       ▼
┌──────────────────────────────────────┐
│ PUT /api/planning/domain-focus       │
│ { domain: "sales", criteria: "..." } │
└──────┬───────────────────────────────┘
       │ 2. API call
       ▼
┌───────────────────────────────────────┐
│ Knowledge Service                     │
│ Validate domain ∈ [7 domains]         │
│ Sanitize markdown (remove scripts)    │
└──────┬────────────────────────────────┘
       │ 3. Write file (atomic)
       ▼
┌─────────────────────────────────┐
│ docs/planning/domain-focus.md   │
│ (updated atomically)            │
└──────┬──────────────────────────┘
       │ 4. Confirm write
       ▼
┌────────────────────────────────────┐
│ JSON Response                      │
│ { success: true, updated_at }      │
└──────┬─────────────────────────────┘
       │ 5. Return to browser
       ▼
┌─────────────┐
│   Browser   │
│ Show toast  │
│ "Saved ✓"   │
└─────────────┘
```

### 4.3 Flow/Workflow Editor — Graph Load Flow

```
┌─────────────┐
│   Browser   │
│ Click        │
│ Workflow tab│
└──────┬──────┘
       │ 1. Tab activated
       ▼
┌─────────────────────────────────┐
│ GET /api/graph/mermaid/epic/EPICS│
└──────┬──────────────────────────┘
       │ 2. API call
       ▼
┌───────────────────────────────────────┐
│ Knowledge Service                     │
│ src/api/graphRoutes.ts:252            │
│ loadEpicGraphCached() ← cache hit     │
└──────┬────────────────────────────────┘
       │ 3. Load EPICS.yaml (cached)
       ▼
┌─────────────────────────────────┐
│ docs/projects/EPICS.yaml        │
└──────┬──────────────────────────┘
       │ 4. Parse + build graph
       ▼
┌───────────────────────────────────────┐
│ graph/mermaidGenerator.ts             │
│ generateMermaid(graph)                │
└──────┬────────────────────────────────┘
       │ 5. Return Mermaid syntax
       ▼
┌────────────────────────────────────┐
│ JSON Response                      │
│ { mermaid: "graph LR\n...", ... }  │
└──────┬─────────────────────────────┘
       │ 6. Return to browser
       ▼
┌─────────────┐
│   Browser   │
│ mermaid.init│
│ Render graph│
└─────────────┘
```

### 4.4 Flow/Workflow Editor — Epic Edit Flow

```
┌─────────────┐
│   Browser   │
│ User changes│
│ status      │
└──────┬──────┘
       │ 1. Status dropdown → "done"
       ▼
┌──────────────────────────────────────┐
│ PUT /api/graph/epics/EPIC-CUTTING-Q3 │
│ { status: "done" }                   │
└──────┬───────────────────────────────┘
       │ 2. API call
       ▼
┌───────────────────────────────────────┐
│ Knowledge Service (NEW route)         │
│ Validate:                             │
│  - Status transition allowed?         │
│  - All dependencies done?             │
└──────┬────────────────────────────────┘
       │ 3. Validation passed
       ▼
┌─────────────────────────────────┐
│ docs/projects/EPICS.yaml        │
│ Update epic status              │
│ (atomic write)                  │
└──────┬──────────────────────────┘
       │ 4. Invalidate cache
       ▼
┌────────────────────────────────────┐
│ clearEpicGraphCache()              │
└──────┬─────────────────────────────┘
       │ 5. Return updated epic
       ▼
┌────────────────────────────────────┐
│ JSON Response                      │
│ { success: true, epic: {...} }     │
└──────┬─────────────────────────────┘
       │ 6. Return to browser
       ▼
┌─────────────┐
│   Browser   │
│ Re-render   │
│ graph (new  │
│ colors)     │
└─────────────┘
```

---

## 5. API Requirements — New Endpoints

### 5.1 Planning Focus API

**File:** `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (NEW)

```typescript
// GET /api/planning/domain-focus
router.get('/domain-focus', async (req, res) => {
  // Read docs/planning/domain-focus.md
  // Parse YAML frontmatter (domain)
  // Parse markdown body (criteria)
  // Return { domain, criteria, updated_at }
});

// PUT /api/planning/domain-focus
router.put('/domain-focus', async (req, res) => {
  // Validate request body { domain?, criteria? }
  // domain must be in [manufacturing, sales, logistics, finance, quality, hr, all]
  // criteria must be sanitized markdown (no <script> tags)
  // Write docs/planning/domain-focus.md atomically
  // Return { success, domain, criteria, updated_at }
});
```

**Security:**
- **Authentication:** Require `Authorization: Bearer dev-token-spaceos-dashboard-2026`
- **Validation:** Domain must be one of 7 predefined values
- **Sanitization:** Strip HTML tags from criteria markdown (use `DOMPurify` or similar)
- **Rate limiting:** Max 10 writes/minute per IP

### 5.2 Epic Update API

**File:** `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` (EXTEND)

```typescript
// PUT /api/graph/epics/:id
router.put('/epics/:id', async (req, res) => {
  const epicId = req.params.id;
  const { status, depends_on, parallel_with, target_date } = req.body;

  // 1. Load EPICS.yaml
  const epicsYaml = await loadEpicsYaml(EPICS_YAML_PATH);

  // 2. Find epic by ID
  const epic = epicsYaml.epics.find(e => e.id === epicId);
  if (!epic) return res.status(404).json({ error: 'Epic not found' });

  // 3. Validate status transition
  if (status && !isValidStatusTransition(epic.status, status)) {
    return res.status(400).json({ error: 'Invalid status transition' });
  }

  // 4. Validate dependencies (cycle detection)
  if (depends_on) {
    const testGraph = buildEpicGraph({...epicsYaml, epics: [
      ...epicsYaml.epics.filter(e => e.id !== epicId),
      {...epic, depends_on}
    ]});
    const cycles = detectCycles(testGraph);
    if (cycles.length > 0) {
      return res.status(400).json({ error: 'Cycle detected', cycles });
    }
  }

  // 5. Update epic
  if (status) epic.status = status;
  if (depends_on) epic.depends_on = depends_on;
  if (parallel_with) epic.parallel_with = parallel_with;
  if (target_date) epic.target_date = target_date;

  // 6. Write EPICS.yaml atomically
  await writeEpicsYaml(EPICS_YAML_PATH, epicsYaml);

  // 7. Invalidate cache
  clearEpicGraphCache();

  // 8. Return updated epic
  res.json({ success: true, epic, validation: { valid: true, cycles: [] } });
});
```

**Validation Rules:**
- **Status transitions:** `pending → active`, `active → done`, `active → blocked`, `blocked → active`
- **Dependency cycles:** Use `detectCycles()` from `graph/operations.ts`
- **Done status:** If setting to `done`, verify all `depends_on` epics are also `done`

---

## 6. CSS/Design Guidelines

### 6.1 Color Palette (from existing styles.css)

```css
--bg-primary: #0f1419      /* Main background (dark) */
--bg-secondary: #1a1f26    /* Card/panel background */
--bg-card: #242b33         /* Nested card */
--text-primary: #e7e9ea    /* Main text */
--text-secondary: #8b98a5  /* Muted text */
--accent-blue: #1d9bf0     /* Active, links */
--accent-green: #00ba7c    /* Success, done */
--accent-yellow: #ffd400   /* Warning, pending */
--accent-red: #f4212e      /* Error, blocked */
--accent-purple: #7856ff   /* Special highlight */
--border-color: #2f3336    /* Borders */
```

### 6.2 Focus Area Panel — CSS Classes

```css
/* Focus Area Panel */
.focus-area-panel {
  background: var(--bg-card);
  border-radius: 12px;
  border: 1px solid var(--border-color);
  margin-bottom: 1.5rem;
}

.focus-area-header {
  padding: 1rem 1.5rem;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.focus-area-body {
  padding: 1.5rem;
}

.domain-selector {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.domain-selector label {
  font-size: 0.875rem;
  color: var(--text-secondary);
  font-weight: 500;
}

.domain-selector select {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 0.5rem 1rem;
  font-size: 0.875rem;
  min-width: 200px;
}

.criteria-display {
  background: var(--bg-secondary);
  padding: 1rem;
  border-radius: 6px;
  border: 1px solid var(--border-color);
  max-height: 300px;
  overflow-y: auto;
}

.criteria-display ul {
  list-style: disc;
  padding-left: 1.5rem;
}

.criteria-display strong {
  color: var(--accent-blue);
}

.criteria-edit-mode textarea {
  width: 100%;
  min-height: 200px;
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px solid var(--border-color);
  border-radius: 6px;
  padding: 1rem;
  font-family: monospace;
  font-size: 0.875rem;
}

.focus-area-actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
}

.btn-edit, .btn-save {
  background: var(--accent-blue);
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  font-weight: 500;
}

.btn-edit:hover, .btn-save:hover {
  opacity: 0.9;
}
```

### 6.3 Flow/Workflow Editor — CSS Classes

```css
/* Flow/Workflow Editor */
.workflow-editor {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 300px); /* Full viewport minus header/tabs */
}

.mermaid-container {
  flex: 1;
  background: var(--bg-secondary);
  border-radius: 12px;
  border: 1px solid var(--border-color);
  margin-bottom: 1rem;
  overflow: hidden;
  position: relative;
}

.mermaid-canvas {
  width: 100%;
  height: 100%;
  overflow: auto;
}

/* Mermaid node color overrides */
.mermaid-canvas .pending {
  fill: #f9f9f9 !important;
  stroke: #999 !important;
}

.mermaid-canvas .active {
  fill: #e6f3ff !important;
  stroke: var(--accent-blue) !important;
  stroke-width: 2px !important;
}

.mermaid-canvas .done {
  fill: #e6ffe6 !important;
  stroke: var(--accent-green) !important;
}

.mermaid-canvas .blocked {
  fill: #ffe6e6 !important;
  stroke: var(--accent-red) !important;
  stroke-width: 2px !important;
}

.mermaid-canvas .selected {
  stroke: var(--accent-purple) !important;
  stroke-width: 3px !important;
  filter: drop-shadow(0 0 8px var(--accent-purple));
}

.epic-details-panel {
  background: var(--bg-card);
  border-radius: 12px;
  border: 1px solid var(--border-color);
  padding: 1.5rem;
  max-height: 400px;
  overflow-y: auto;
}

.epic-details-panel h3 {
  margin-bottom: 1rem;
  color: var(--accent-blue);
}

.epic-detail-row {
  display: flex;
  margin-bottom: 0.75rem;
}

.epic-detail-label {
  width: 140px;
  color: var(--text-secondary);
  font-size: 0.875rem;
}

.epic-detail-value {
  flex: 1;
  color: var(--text-primary);
}

.epic-dependencies-list {
  list-style: none;
  padding-left: 0;
}

.epic-dependencies-list li {
  padding: 0.5rem;
  background: var(--bg-secondary);
  border-radius: 6px;
  margin-bottom: 0.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.dep-badge {
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
}

.dep-badge.done { background: var(--accent-green); color: white; }
.dep-badge.active { background: var(--accent-blue); color: white; }
.dep-badge.pending { background: var(--accent-yellow); color: var(--bg-primary); }
.dep-badge.blocked { background: var(--accent-red); color: white; }

.btn-add-dependency {
  background: var(--bg-secondary);
  color: var(--text-primary);
  border: 1px dashed var(--border-color);
  padding: 0.5rem 1rem;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.875rem;
  margin-top: 0.5rem;
}

.btn-add-dependency:hover {
  background: var(--border-color);
}
```

### 6.4 Responsive Design

**Focus Area Panel:**
- **Desktop (>1024px):** Full width, criteria list scrollable at 300px max height
- **Tablet (768-1024px):** Same layout, criteria list 200px max height
- **Mobile (<768px):** Domain dropdown full width, criteria list 150px max height

**Flow/Workflow Editor:**
- **Desktop only (>1024px):** Graph visualization requires large screen
- **Tablet/Mobile:** Show message: "Workflow editor requires desktop screen (min 1024px width)"

```css
@media (max-width: 1024px) {
  .workflow-editor {
    display: none;
  }

  .workflow-editor-mobile-message {
    display: block;
    text-align: center;
    padding: 3rem;
    color: var(--text-secondary);
  }
}

@media (min-width: 1025px) {
  .workflow-editor-mobile-message {
    display: none;
  }
}
```

---

## 7. Implementation Roadmap

### Phase 1: Focus Area Panel (5-7 days)

**Backend:**
- [ ] Create `src/api/planningRoutes.ts`
- [ ] Implement `GET /api/planning/domain-focus`
- [ ] Implement `PUT /api/planning/domain-focus`
- [ ] Add markdown sanitization (DOMPurify)
- [ ] Add authentication middleware

**Frontend:**
- [ ] Add Focus Area Panel HTML to `planning.html`
- [ ] Create `public/js/planning-focus.js`
- [ ] Implement domain dropdown
- [ ] Implement criteria display (markdown → HTML)
- [ ] Implement edit mode (textarea)
- [ ] Add save handler (PUT API call)
- [ ] Add CSS styles (see section 6.2)

**Testing:**
- [ ] API endpoint tests (GET/PUT)
- [ ] Frontend integration test (load → edit → save)
- [ ] Validation tests (invalid domain, XSS attempts)

### Phase 2: Flow/Workflow Editor (10-14 days)

**Backend:**
- [ ] Implement `PUT /api/graph/epics/:id` (graphRoutes.ts)
- [ ] Add status transition validator
- [ ] Add cycle detection validator
- [ ] Add cache invalidation on write
- [ ] Add EPICS.yaml atomic write helper

**Frontend:**
- [ ] Add Workflow tab content to `planning.html`
- [ ] Include `mermaid.js` library (CDN or npm)
- [ ] Create `public/js/planning-workflow.js`
- [ ] Implement graph loading (GET /api/graph/mermaid)
- [ ] Implement Mermaid rendering
- [ ] Add node click handlers
- [ ] Implement epic details panel
- [ ] Add status dropdown (PUT on change)
- [ ] Add dependency management UI
- [ ] Add panzoom.js for graph navigation
- [ ] Add CSS styles (see section 6.3)

**Testing:**
- [ ] API endpoint tests (PUT /api/graph/epics/:id)
- [ ] Cycle detection tests
- [ ] Status transition validation tests
- [ ] Frontend integration test (load → select → edit → save)
- [ ] Mermaid rendering test (visual regression)

### Phase 3: Polish & Optimization (3-5 days)

- [ ] Add loading spinners
- [ ] Add error toast notifications
- [ ] Add success toast notifications
- [ ] Optimize Mermaid rendering (lazy load, debounce)
- [ ] Add keyboard shortcuts (Esc to close details panel)
- [ ] Add export Mermaid button (download .mmd file)
- [ ] Add "Refresh Graph" button
- [ ] Mobile responsive refinement

**Total Estimate:** 18-26 days (3.5-5 weeks)

---

## 8. Security & Performance Considerations

### 8.1 Security

**Focus Area Panel:**
- ✅ **Authentication required** — All API calls require bearer token
- ✅ **Input validation** — Domain must be in predefined list
- ✅ **Markdown sanitization** — Strip HTML tags to prevent XSS
- ✅ **Rate limiting** — Max 10 writes/minute per IP
- ⚠️ **File write atomicity** — Use `fs.writeFile` with temp file + rename pattern

**Flow/Workflow Editor:**
- ✅ **Authentication required** — All API calls require bearer token
- ✅ **Cycle detection** — Prevent invalid DAGs
- ✅ **Status transition validation** — Prevent invalid state changes
- ✅ **Cache invalidation** — Prevent stale data display
- ⚠️ **YAML injection** — Validate all inputs before YAML serialization

### 8.2 Performance

**Focus Area Panel:**
- **File caching:** Cache `domain-focus.md` in memory (invalidate on write)
- **Debounced save:** Wait 500ms after last edit before enabling Save button

**Flow/Workflow Editor:**
- **Graph caching:** Use `loadEpicGraphCached()` (already implemented)
- **Mermaid rendering:** Lazy load library (only when Workflow tab is activated)
- **Large graphs:** If EPICS.yaml grows >50 nodes, consider:
  - Zoom-to-fit button
  - Minimap navigation (see [mermaid-minimap](https://github.com/mermaid-js/mermaid-minimap))
  - Node search/filter

**Optimizations:**
- Cache TTL: 60 seconds for EPICS.yaml (auto-reload on file change via `fs.watch`)
- GraphQL consideration: If UI needs to fetch individual epic details frequently, consider adding GraphQL layer

---

## 9. Open Questions & Trade-offs

### 9.1 Focus Area Panel Placement

**Decision Needed:**
- **Option A:** Dashboard page (sidebar) → Always visible, but clutters dashboard
- **Option B:** Planning page (top panel) → ⭐ **Recommended** (contextual fit)
- **Option C:** Settings page → Clean separation, but extra navigation step

**Recommendation: Option B** (see section 1.3)

### 9.2 EPICS.yaml Write Strategy

**Options:**
1. **Direct file write** (current approach) → Simple, but requires file lock
2. **Git commit on write** → Full audit trail, but adds complexity
3. **Hybrid: Auto-commit on write** → Best of both worlds

**Trade-off Analysis:**

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| Direct write | Fast, simple | No audit trail, race condition risk | ⚠️ OK for Phase 1 |
| Git commit | Full history, revert capability | Slower, requires git setup | ⭐ **Recommended for Phase 2** |
| Hybrid | Audit + rollback | Adds 200-500ms latency | ⭐⭐ **Best long-term** |

**Recommendation: Hybrid (auto-commit on write)**

Implementation:
```typescript
async function writeEpicsYaml(path: string, data: EpicsYaml) {
  // 1. Write file atomically
  await fs.writeFile(path, yaml.dump(data));

  // 2. Git commit (async, non-blocking)
  exec(`cd /opt/spaceos && git add ${path} && git commit -m "Auto-update EPICS.yaml via UI"`);
}
```

### 9.3 Real-time Sync

**Question:** Should multiple users see live updates when someone edits EPICS.yaml?

**Options:**
1. **Polling** — Frontend polls `GET /api/graph/epics` every 5 seconds
2. **SSE (Server-Sent Events)** — Backend pushes updates to connected clients
3. **No sync** — Manual refresh button only

**Trade-off:**

| Approach | Pros | Cons | Recommendation |
|----------|------|------|----------------|
| Polling | Simple, works everywhere | Wasteful (95% no-change polls) | ⚠️ OK for MVP |
| SSE | Efficient, real-time | Requires `EventSource` support, connection overhead | ⭐ **Recommended for Phase 2** |
| No sync | Zero overhead | Poor UX for multi-user | ❌ Not recommended |

**Recommendation: Polling for Phase 1, SSE for Phase 2**

### 9.4 Dependency Add/Remove UI

**Question:** How should users add/remove dependencies?

**Options:**
1. **Drag-and-drop** — Drag epic node onto another to add dependency
2. **Modal form** — Click `[+ Add Dependency]` → modal with epic dropdown
3. **Inline dropdown** — Dropdown list in details panel

**Recommendation: Option 2 (modal form)** for Phase 1 (simple, accessible)

**Phase 2 Enhancement:** Add drag-and-drop support (requires Mermaid.js event handling)

---

## 10. Alternative Approaches Considered

### 10.1 Flow Editor Library Choice

**Alternatives to Mermaid.js:**

| Library | Pros | Cons | Decision |
|---------|------|------|----------|
| **Mermaid.js** | Text-based, version control friendly, already used in docs | Limited interactivity customization | ⭐ **Selected** |
| **React Flow** | Full interactive control, drag-drop built-in | Requires React port, heavier bundle | ❌ Too heavy for this use case |
| **D3.js DAG** | Maximum flexibility | High development cost, reinvent wheel | ❌ Overkill |
| **vis.js Network** | Good interactivity | Poor text-based representation | ❌ Not version control friendly |

**Decision: Mermaid.js** (best fit for text-based YAML source of truth)

### 10.2 EPICS.yaml Storage

**Alternatives to YAML file:**

| Storage | Pros | Cons | Decision |
|---------|------|------|----------|
| **YAML file (current)** | Simple, Git-friendly, human-readable | Concurrent write issues, no transactions | ⭐ **Keep for now** |
| **PostgreSQL** | ACID transactions, multi-user safe | Requires migration, loses Git history | ❌ Phase 3+ consideration |
| **SQLite** | ACID + file-based | Harder to review in Git | ❌ Not recommended |

**Decision: Keep YAML file** (add file locking in Phase 2 if needed)

---

## 11. Success Criteria

**Focus Area Panel:**
- ✅ Conductor can change planning domain in <5 clicks
- ✅ Domain change reflects in next `plan-scan.sh` run
- ✅ Criteria edits persist across page reloads
- ✅ No XSS vulnerabilities (pass security scan)

**Flow/Workflow Editor:**
- ✅ Epic graph renders in <2 seconds
- ✅ User can change epic status in <3 clicks
- ✅ Dependency changes are validated (no cycles allowed)
- ✅ Graph updates reflect in Git commit within 1 second
- ✅ Mobile users see helpful message (not broken layout)

**Overall:**
- ✅ Zero backend errors in production (first 7 days)
- ✅ Dashboard page load time <1.5 seconds
- ✅ Planning page load time <2 seconds

---

## 12. Appendix

### 12.1 Mermaid.js Example Output

```mermaid
graph LR
  EPIC-KERNEL-STABLE(["Kernel Stability"])
  EPIC-JOINERY-V2(["Joinery Module v2"])
  EPIC-CUTTING-Q3(["Cutting Module Q3"])
  EPIC-INVENTORY-V1(["Inventory Module v1"])
  EPIC-IDENTITY-V1(["Identity Module v1"])
  EPIC-ORCH-V2(["Orchestrator BFF v2"])
  EPIC-PORTAL-V2(["Customer Portal v2"])
  EPIC-NEXUS-V1(["Nexus Agent Infra"])
  EPIC-GRAPH-WORKFLOW(["Graph-Based Workflow"])
  EPIC-DOORSTAR-SOFTLAUNCH(["Doorstar Soft Launch"])

  EPIC-KERNEL-STABLE --> EPIC-JOINERY-V2
  EPIC-KERNEL-STABLE --> EPIC-CUTTING-Q3
  EPIC-KERNEL-STABLE --> EPIC-INVENTORY-V1
  EPIC-KERNEL-STABLE --> EPIC-IDENTITY-V1
  EPIC-JOINERY-V2 --> EPIC-ORCH-V2
  EPIC-KERNEL-STABLE --> EPIC-ORCH-V2
  EPIC-IDENTITY-V1 --> EPIC-PORTAL-V2
  EPIC-ORCH-V2 --> EPIC-PORTAL-V2
  EPIC-NEXUS-V1 --> EPIC-GRAPH-WORKFLOW
  EPIC-PORTAL-V2 --> EPIC-DOORSTAR-SOFTLAUNCH
  EPIC-CUTTING-Q3 --> EPIC-DOORSTAR-SOFTLAUNCH

  EPIC-CUTTING-Q3 -.->|parallel| EPIC-JOINERY-V2
  EPIC-CUTTING-Q3 -.->|parallel| EPIC-INVENTORY-V1

  classDef pending fill:#f9f9f9,stroke:#999,stroke-width:1px
  classDef active fill:#e6f3ff,stroke:#0066cc,stroke-width:2px
  classDef done fill:#e6ffe6,stroke:#00cc00,stroke-width:1px
  classDef blocked fill:#ffe6e6,stroke:#cc0000,stroke-width:2px

  class EPIC-KERNEL-STABLE,EPIC-JOINERY-V2,EPIC-INVENTORY-V1,EPIC-IDENTITY-V1,EPIC-ORCH-V2 done
  class EPIC-CUTTING-Q3,EPIC-PORTAL-V2,EPIC-NEXUS-V1 active
  class EPIC-GRAPH-WORKFLOW,EPIC-DOORSTAR-SOFTLAUNCH pending
```

### 12.2 API Endpoint Summary

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | `/api/planning/domain-focus` | Get current domain & criteria | 🆕 NEW |
| PUT | `/api/planning/domain-focus` | Update domain & criteria | 🆕 NEW |
| GET | `/api/graph/epics` | Get epic dependency graph | ✅ Exists |
| GET | `/api/graph/mermaid/epic/EPICS` | Get Mermaid diagram | ✅ Exists |
| GET | `/api/graph/critical-path/epic/EPICS` | Get critical path | ✅ Exists |
| POST | `/api/graph/validate` | Validate YAML | ✅ Exists |
| PUT | `/api/graph/epics/:id` | Update epic (status, deps) | 🆕 NEW |

---

## Document Status

- **Version:** v1 (DRAFT)
- **Next Steps:**
  1. Review by Root or Conductor
  2. Approval decision (approve / revise / reject)
  3. If approved → create implementation tasks
  4. Backend terminal: API endpoints
  5. Frontend terminal: UI components

**Questions for Reviewer:**
- Focus Area Panel placement: Dashboard or Planning page?
- Real-time sync: Polling (Phase 1) or SSE (immediate)?
- EPICS.yaml write: Direct or auto-commit?

---

**END OF DOCUMENT**
