---
id: MSG-LIBRARIAN-007
from: conductor
to: librarian
type: task
priority: medium
status: READ
model: sonnet
ref: 2026-06-24_consensus_ui-polish-integration.md
epic: EPIC-DATAHAVEN-UI
phase: 3
created: 2026-06-24
content_hash: bc2dc0996b41f9c7eae08143a53d59c925c05db8f71585298b9ee860bc2348eb
---

# Datahaven UI Polish & Integration — Documentation

**Epic:** EPIC-DATAHAVEN-UI Phase 3 (Polish & Integration)
**Priority:** MEDIUM
**Type:** Documentation
**Estimate:** 1 day

---

## Context

A Datahaven Planning UI két új komponenst kapott:
1. **Focus Area Panel** — domain selection és criteria editing
2. **Flow/Workflow Editor** — epic dependency graph és status management

Phase 3-ban készül a teljes dokumentáció:
- API Documentation (developer docs)
- User Guide (end-user guide)
- Architecture Decision Record (ADR)

---

## Documentation Tasks

### DOC-001: API Documentation

**File:** `docs/knowledge/api/DATAHAVEN_PLANNING_API.md`

**Scope:**
Dokumentáld a Planning API összes endpoint-ját, beleértve a Focus Area Panel és Workflow Editor backend API-kat.

**Subtasks:**

#### 1. GET /api/planning/domain-focus

**Purpose:** Retrieve current planning domain and criteria

**Request:**
```http
GET /api/planning/domain-focus
Authorization: Bearer dev-token-spaceos-dashboard-2026
```

**Response (200 OK):**
```json
{
  "domain": "all",
  "criteria": "## Focus Criteria\n\n- Doorstar launch readiness\n- Q3 roadmap priorities\n..."
}
```

**Error codes:**
- `401 Unauthorized` — missing or invalid auth token
- `500 Internal Server Error` — file read error

---

#### 2. PUT /api/planning/domain-focus

**Purpose:** Update planning domain and/or criteria

**Rate limit:** 10 writes/minute per IP

**Request:**
```http
PUT /api/planning/domain-focus
Authorization: Bearer dev-token-spaceos-dashboard-2026
Content-Type: application/json

{
  "domain": "manufacturing",
  "criteria": "## Updated Criteria\n\n- Focus on EHS module\n- ..."
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Domain focus updated successfully",
  "domain": "manufacturing",
  "criteria": "..."
}
```

**Error codes:**
- `400 Bad Request` — invalid domain (not in whitelist)
- `401 Unauthorized` — missing or invalid auth token
- `429 Too Many Requests` — rate limit exceeded (10 writes/min)
- `500 Internal Server Error` — file write error

**Domain whitelist:**
- `all` (default)
- `joinery`
- `cutting`
- `manufacturing`
- `ehs`
- `catalog`

---

#### 3. PUT /api/graph/epics/:id

**Purpose:** Update epic status, dependencies, or target date

**Request:**
```http
PUT /api/graph/epics/EPIC-CUTTING-Q3
Authorization: Bearer dev-token-spaceos-dashboard-2026
Content-Type: application/json

{
  "status": "active",
  "depends_on": ["EPIC-KERNEL-STABLE"],
  "target_date": "2026-09-30"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Epic EPIC-CUTTING-Q3 updated successfully",
  "epic": {
    "id": "EPIC-CUTTING-Q3",
    "name": "Cutting Module Q3 Release",
    "status": "active",
    "depends_on": ["EPIC-KERNEL-STABLE"],
    "target_date": "2026-09-30"
  }
}
```

**Error codes:**
- `400 Bad Request` — validation error (cycle detected, invalid status, etc.)
- `401 Unauthorized` — missing or invalid auth token
- `404 Not Found` — epic not found in EPICS.yaml
- `500 Internal Server Error` — file write error

**Validation rules:**
- Status must be one of: `pending`, `active`, `done`, `blocked`
- Dependencies cannot create cycles (A → B → A)
- Target date must be valid ISO date (YYYY-MM-DD)

---

**Documentation format:**
- Use markdown
- Include curl examples
- Document all error codes
- Include rate limiting rules
- Add authentication requirements

**Estimate:** 2-3 hours

---

### DOC-002: User Guide

**File:** `docs/knowledge/datahaven/PLANNING_UI_USER_GUIDE.md`

**Scope:**
End-user guide for Datahaven Planning UI, including screenshots/diagrams where helpful.

**Subtasks:**

#### 1. Introduction

- [ ] What is the Planning UI?
- [ ] Who should use it? (Conductor, Architect, Root)
- [ ] How to access it? (https://datahaven.joinerytech.hu/planning.html)

---

#### 2. Focus Area Panel

**Section: How to change planning domain**
- [ ] Locate Focus Area Panel (top-right of Planning page)
- [ ] Click domain dropdown
- [ ] Select domain (all, joinery, cutting, manufacturing, ehs, catalog)
- [ ] Domain saved automatically → success toast shown
- [ ] Screenshot: dropdown expanded

**Section: How to edit domain criteria**
- [ ] Click [Edit] button
- [ ] Textarea appears with current criteria (markdown format)
- [ ] Modify text (markdown syntax supported)
- [ ] Click [Save] → criteria saved → success toast shown
- [ ] Click [Cancel] → discard changes
- [ ] Screenshot: edit mode

**Common tasks:**
- Change domain from "all" to "joinery" → criteria auto-updates
- Add new focus criterion → edit criteria, add bullet point
- Format criteria → use markdown (headings, lists, bold)

---

#### 3. Flow/Workflow Editor

**Section: How to view epic dependencies**
- [ ] Navigate to "Workflow" tab
- [ ] Graph loads (Mermaid diagram)
- [ ] Nodes colored by status:
  - Gray: pending
  - Blue: active
  - Green: done
  - Red: blocked
- [ ] Arrows show dependencies (A → B means B depends on A)
- [ ] Screenshot: graph with multiple epics

**Section: How to view epic details**
- [ ] Click any epic node in graph
- [ ] Details panel slides in from right
- [ ] View: name, status, dependencies, target date, tasks
- [ ] Click [Close] or click backdrop → panel closes
- [ ] Screenshot: details panel open

**Section: How to change epic status**
- [ ] Click epic node → details panel opens
- [ ] Status dropdown (pending/active/done/blocked)
- [ ] Select new status → PUT API called → graph updates
- [ ] Graph node color changes immediately
- [ ] Screenshot: status dropdown

**Section: How to add/remove dependencies**
- [ ] Click epic node → details panel opens
- [ ] Dependencies section shows current dependencies
- [ ] Click [+ Add Dependency] → modal opens
- [ ] Select dependency from dropdown → click [Add]
- [ ] Graph updates with new arrow
- [ ] To remove: click [X] next to dependency → confirmation → removed
- [ ] Screenshot: add dependency modal

**Section: Common error messages**
- [ ] "Cycle detected: A → B → A" → cannot create circular dependencies
- [ ] "Invalid status transition" → check epic's current state
- [ ] "Epic not found" → epic may have been deleted

---

**Documentation format:**
- Use markdown with screenshots (save to `docs/knowledge/datahaven/screenshots/`)
- Use step-by-step numbered instructions
- Include "Common tasks" section for each feature
- Add troubleshooting section for errors

**Estimate:** 2-3 hours

---

### DOC-003: Architecture Decision Record

**File:** `docs/knowledge/architecture/ADR-048-Datahaven-UI-Planning-Components.md`

**Scope:**
Document architectural decisions for Focus Area Panel and Flow/Workflow Editor.

**Subtasks:**

#### ADR Structure

**1. Context**
- [ ] Why did we build these components?
  - Planning pipeline visibility needed
  - Domain focus manual editing cumbersome
  - Epic dependencies hard to visualize in YAML
- [ ] What problem do they solve?
  - Conductor needs to see active planning domain at a glance
  - Architect needs to edit domain criteria without SSH/vim
  - Everyone needs to see epic dependency graph

**2. Decision**
- [ ] Focus Area Panel placement: Planning page (top-right)
  - Why? Planning page is where consensus/queue items are reviewed
  - Alternative: Separate page (rejected: adds navigation overhead)
- [ ] Flow/Workflow Editor placement: Planning page (Workflow tab)
  - Why? Logical grouping with planning artifacts
  - Alternative: Dashboard page (rejected: Dashboard is for terminal status)
- [ ] Technology choice: Mermaid.js for graph visualization
  - Why? Simple, lightweight, markdown-compatible
  - Alternatives: React Flow (too heavy), D3.js (too complex)
- [ ] Data storage: YAML files (EPICS.yaml, domain-focus.md)
  - Why? Git-trackable, human-readable, no DB migration
  - Alternative: PostgreSQL (rejected: overkill for planning metadata)
- [ ] Update mechanism: Polling (30s interval)
  - Why? Simple, stateless, no WebSocket infrastructure
  - Alternative: SSE (rejected: added complexity for minimal benefit)

**3. Consequences**

**Positive:**
- [ ] Faster planning domain switching (no SSH needed)
- [ ] Visual epic dependency graph (better understanding)
- [ ] Git-tracked changes (audit trail)
- [ ] Lightweight (no DB, no WebSocket server)

**Negative:**
- [ ] Polling adds 30s delay for updates (acceptable trade-off)
- [ ] YAML parsing on every request (mitigated by caching in Phase 3)
- [ ] Mermaid.js adds ~300KB bundle size (mitigated by lazy loading in Phase 3)

**4. Alternatives Considered**

**React Flow vs Mermaid.js:**
- React Flow: More interactive (drag-to-rearrange) but 2MB+ bundle size
- Mermaid.js: Read-only graph but only 300KB, markdown-compatible
- **Decision:** Mermaid.js (lightweight, sufficient for read-heavy use case)

**PostgreSQL vs YAML:**
- PostgreSQL: Structured queries, transactions, but requires migration
- YAML: Git-trackable, human-editable, no DB overhead
- **Decision:** YAML (planning metadata changes rarely, Git audit trail valuable)

**SSE vs Polling:**
- SSE: Real-time updates, but requires persistent connection
- Polling: 30s delay, but stateless, simple
- **Decision:** Polling (30s delay acceptable for planning updates)

---

**Documentation format:**
- Use ADR template (Context, Decision, Consequences, Alternatives)
- Include trade-off analysis
- Link to related ADRs (ADR-041 Graph-based Workflow)

**Estimate:** 1-2 hours

---

## Definition of Done

- [ ] API documentation complete (DOC-001)
  - All endpoints documented (GET/PUT)
  - Request/response examples included
  - Error codes documented
  - Rate limiting documented
- [ ] User guide complete (DOC-002)
  - Screenshots included (if possible)
  - Step-by-step instructions clear
  - Common tasks section added
  - Troubleshooting section added
- [ ] ADR complete (DOC-003)
  - Context, Decision, Consequences sections filled
  - Alternatives documented
  - Trade-offs explained

---

## Files to Create

**Created:**
- `docs/knowledge/api/DATAHAVEN_PLANNING_API.md`
- `docs/knowledge/datahaven/PLANNING_UI_USER_GUIDE.md`
- `docs/knowledge/architecture/ADR-048-Datahaven-UI-Planning-Components.md`
- `docs/knowledge/datahaven/screenshots/` (optional, if screenshots added)

---

## Reference

**Consensus:** `docs/planning/queue/2026-06-24_consensus_ui-polish-integration.md`
**Architecture:** `docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`
**Related ADR:** `docs/knowledge/architecture/ADR-041-graph-based-workflow-architecture.md`

---

**Kezdd el a DOC-001 feladattal!** Az API dokumentáció a legfontosabb (Backend/Frontend is használja).
