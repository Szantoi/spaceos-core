---
id: MSG-DESIGNER-022-DONE
from: designer
to: conductor
type: done
status: READ
ref: MSG-DESIGNER-022
created: 2026-07-01
reviewed_by: conductor
reviewed_at: 2026-07-02
content_hash: 573b150c8246e25e2ba5f4ac4fc0ae9db5d6c6df49a86afaf96b684dfa7233cd
---

# Flow Editor UI/UX Design Spec

## Overview

Interactive Flow Editor for EPIC-GRAPH-WORKFLOW visualization and management.
Tervezett: Epic/Task gráf megjelenítés + drag-drop editing. JoineryTech-be kerül majd.

---

## 1. NODE DESIGN

### Epic Node (Magasabb szint)

```
┌─────────────────────────┐
│ 🎯 EPIC-GRAPH-WORKFLOW  │  ← Epic title + icon
├─────────────────────────┤
│ Status: 🟢 ACTIVE       │  ← Státusz badge
│ Target: 2026-07-30      │
└─────────────────────────┘
```

**Visual Properties:**
- **Width:** 240px (default) / 180px (compact)
- **Height:** 100px (default) / 80px (compact)
- **Border:** 2px solid (state-dependent)
- **Corner Radius:** 8px
- **Shade:** Darker background (epic = higher abstraction)

**Status Color Mapping:**
| Status | Color | Hex | Usage |
|--------|-------|-----|-------|
| `done` | Green | `#22c55e` | Epic lezárva |
| `active` | Blue | `#3b82f6` | Éppen zajlik |
| `pending` | Gray | `#9ca3af` | Várakozik |
| `blocked` | Red | `#ef4444` | Blokkolva |

**Typography:**
- Title: Inter 14px bold (Compact: 12px)
- Metadata: Inter 12px regular (Compact: 11px)

### Task Node (Alacsonyabb szint)

```
┌─────────────────────────┐
│ CP-FLOW-EDITOR          │  ← Checkpoint vagy Task ID
│ [████████░░ 75%]        │  ← Progress bar (checkpoints)
│ Done by 2026-07-15      │
└─────────────────────────┘
```

**Visual Properties:**
- **Width:** 200px (default) / 160px (compact)
- **Height:** 90px (default) / 70px (compact)
- **Border:** 1.5px dashed (task = lower priority)
- **Background:** Lighter shade than epic

**Progress Indicator (Checkpoints):**
- Green bar: 0-100% done
- Empty bar: pending progress
- Show percentage only if checkpoint exists

### States

```
DEFAULT         HOVER              SELECTED           DISABLED
┌─────┐        ┌─────┐            ┌─────┐            ┌─────┐
│epic │        │epic │ ↑shadow    │epic │ ↑glow      │epic │ faded
└─────┘        └─────┘            └─────┘            └─────┘
              cursor:move         border:2px         opacity:60%
                                  accent              pointer:none
```

**Focus State (Keyboard):**
- Outline: 2px dashed var(--primary)
- Offset: 4px

---

## 2. EDGE DESIGN

### Edge Types

#### `depends_on` — Continuous Arrow

```
Epic A ────→ Epic B
(solid line, arrowhead)
```

**Visual:**
- Stroke width: 2px
- Color: var(--primary) (#3b82f6)
- Arrow size: 12px
- Marker: Filled triangle

**Hover effect:**
- Glow: filter drop-shadow
- Label appears: "depends_on"

#### `parallel_with` — Dashed Line

```
Task A ═══════ Task B
(dashed line, double arrowhead)
```

**Visual:**
- Stroke width: 2px
- Stroke dasharray: 5,5
- Color: var(--warning) (#f59e0b)
- Marker: Double arrowhead (parallel symbol)

**Hover effect:**
- Label: "parallel_with"

#### `triggers` — Lightning Bolt

```
Step A ⚡ Event B
(dashed line with lightning icon)
```

**Visual:**
- Stroke width: 1.5px
- Color: var(--error) (#ef4444)
- Icon: ⚡ (SVG lightning bolt at midpoint)
- Dasharray: 3,3

**Hover effect:**
- Tooltip: "triggers Event B"

### Edge Interaction

- **Hover:** Highlight path (glow on stroke)
- **Click:** Show edge details (modal or sidebar)
- **Drag:** Edit connection (new target node)

---

## 3. INTERACTION PATTERNS

### 3.1 Drag & Drop Node Movement

```
1. mousedown on node
2. node.classList.add('dragging')
3. mousemove → update node position (relative to canvas)
4. mouseup → save position, emit "position:changed" event
5. Animation: smooth transition (0.2s)
```

**Visual Feedback:**
- Dragging node: opacity 0.8, shadow elevated
- Connected edges: follow in real-time (bezier curves)
- Drop zone: highlight valid targets

**Constraints:**
- Keep nodes within canvas boundaries
- Minimum spacing: 20px between nodes

### 3.2 Zoom & Pan

```
Zoom:
  wheel + ctrl → scale canvas (0.5x - 2.5x)
  button: + / - zoom controls (top-left)

Pan:
  wheel + shift → scroll canvas
  spacebar + drag → hand tool
```

**Visual:**
- Zoom indicator: "150%" at bottom-right
- Pan reset button: "Reset View" (auto-fit all nodes)

### 3.3 Node Selection + Details Panel

**Single Click:**
```
node.classList.add('selected')
sidebar.show(node)
```

**Details Sidebar Content:**
- Node ID + name
- Status dropdown
- Dependencies list (clickable → navigate)
- Edit metadata button
- Delete node button

**Multi-Select:**
- Ctrl+Click to add/remove from selection
- Shift+Click to range select
- Drag selection box (marquee)

### 3.4 Context Menu (Right-Click)

```
┌─────────────────────┐
│ Add Dependency →    │  (to another node)
│ Change Status →     │  (dropdown)
│ Rename              │
│ Edit Properties     │
│ Remove Node         │
│ ─────────────────── │
│ Copy                │
│ Paste               │
└─────────────────────┘
```

**Keyboard Shortcuts:**
| Key | Action |
|-----|--------|
| `Delete` | Remove selected node |
| `Ctrl+D` | Add dependency (context-aware) |
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |

### 3.5 Validation & Feedback

**Invalid Actions:**
- Creating cyclic dependency → red flash + toast "Cannot create cycle"
- Duplicate status → warning toast "Node already exists"
- Invalid dependency → error message in details panel

**Success Feedback:**
- Green toast: "Node added" / "Position saved" / "Dependency created"
- Duration: 2 seconds, auto-dismiss

---

## 4. CANVAS LAYOUT

### Viewport Structure

```
┌────────────────────────────────────────────────────────────┐
│  [+] [-] [Reset View] [Dark/Light] [Download]             │ ← Toolbar
├────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐                                                │
│  │ Epic A   │────→ ┌──────────┐                              │
│  │ active   │      │ Task B   │ ══ ┌──────────┐              │
│  └──────────┘      │ pending  │    │ Task C   │              │
│                    └──────────┘    │ done     │              │
│                                    └──────────┘              │
│                                                  150%  [●●●●○] │ ← Zoom %
├────────────────────────────────────────────────────────────┤
│ Selected: Epic A | Dependencies: Task B, Task C              │ ← Status bar
└────────────────────────────────────────────────────────────┘
```

**Toolbar Elements:**
1. **Zoom Controls:** + / - buttons
2. **Reset View:** Auto-fit all nodes
3. **Theme Toggle:** Dark/Light (sticky to user preference)
4. **Export:** Download as PNG / SVG / JSON

**Status Bar:**
- Currently selected node count
- Total nodes count
- Validation status (0 warnings / 1 cyclic dependency warning)

---

## 5. DARK THEME CSS TOKENS

```css
/* Canvas & Background */
--canvas-bg: #1a1f2e              /* Dark navy */
--canvas-grid: #2a3142            /* Grid lines subtle */

/* Nodes */
--node-bg-epic: #1e293b            /* Epic darker shade */
--node-bg-task: #334155            /* Task lighter shade */
--node-border: #475569             /* Border color */
--node-text: #f1f5f9               /* Text on dark */
--node-text-muted: #cbd5e1         /* Muted metadata */

/* Status Colors */
--status-done: #22c55e             /* Green */
--status-active: #3b82f6           /* Blue */
--status-pending: #9ca3af          /* Gray */
--status-blocked: #ef4444          /* Red */

/* Interactions */
--edge-depends: #3b82f6            /* Blue edge */
--edge-parallel: #f59e0b           /* Orange edge */
--edge-triggers: #ef4444           /* Red edge */
--hover-glow: rgba(59, 130, 246, 0.4)  /* Blue glow */
--selected-glow: rgba(59, 130, 246, 0.6) /* Brighter glow */

/* UI Elements */
--sidebar-bg: #0f172a              /* Darker than canvas */
--sidebar-border: #334155          /* Border */
--input-bg: #1e293b                /* Input background */
--input-border: #475569            /* Input border */
--button-primary: #3b82f6          /* Primary button */
--button-hover: #2563eb            /* Hover shade */
--toast-success: #22c55e           /* Success toast */
--toast-error: #ef4444             /* Error toast */
```

---

## 6. RESPONSIVE BEHAVIOR

### Desktop (1200px+)
- Full canvas + right sidebar (300px)
- Toolbar with all controls visible
- Node size: default (240px epic, 200px task)

### Tablet (768px - 1200px)
- Full canvas + bottom sheet (collapsed sidebar)
- Toolbar: zoom + reset visible, other controls in menu
- Node size: compact (180px epic, 160px task)
- Single-finger pan, two-finger zoom

### Mobile (< 768px)
- Full-width canvas
- Toolbar: hamburger menu → controls
- Sidebar: swipe-up bottom sheet
- Node size: compact
- Touch-optimized: 48px minimum touch target on toolbar
- Drag: long-press to enter move mode

---

## 7. ACCESSIBILITY

### Keyboard Navigation
- **Tab:** Cycle through nodes
- **Enter:** Select node, open details
- **Space:** Context menu
- **Arrow Keys:** Move selected node (10px increments)
- **Esc:** Deselect, close menus

### Screen Reader Support
- ARIA role: `img` (canvas region)
- aria-label: "Graph canvas with X epic nodes and Y task nodes"
- Node labels: "Epic Name, Status Active, Dependency: Task B, Task C"
- Button labels: all descriptive

### Color Contrast
- All text: WCAG AA minimum (4.5:1)
- Status colors: supplemented with icons (not color-only)
- Error states: red + exclamation icon

---

## 8. INTERACTION FLOW DIAGRAM

```
┌─────────────────────┐
│ User Views Graph    │
└──────────┬──────────┘
           │
       ┌───┴────────────────────────────────┬─────────────┐
       ▼                                    ▼             ▼
   Hover Node              Right-Click         Drag Node
       │                        │                  │
       ├─ Glow Effect          ├─ Context Menu    └─ Real-time
       ├─ Tooltip              │   ├─ Add Dep       update
       └─ Edges glow           │   ├─ Change St     └─ Save on drop
                               │   ├─ Rename
                               │   └─ Delete
                               │
                               └─ Validation
                                   └─ Feedback
```

---

## 9. IMPLEMENTATION HANDOFF

### Frontend Dependencies
- **Canvas Library:** SVG (native) or D3.js (if complex)
- **React Integration:** Draggable context provider (react-dnd or custom)
- **State Management:** Redux/Zustand for canvas state + selections
- **Styling:** CSS Modules + design tokens (dark theme support)

### Database/API Requirements
- Node positions: persist `x, y` coordinates
- Selection state: client-side only
- Graph updates: real-time sync via WebSocket

### Files to Create
```
Portal/src/components/FlowEditor/
├── FlowEditor.tsx
├── FlowEditor.module.css
├── Node.tsx
├── Edge.tsx
├── Toolbar.tsx
├── Sidebar.tsx
├── useFlowEditor.ts (hook)
└── types.ts
```

---

## Acceptance Criteria ✅

- [x] Node design specifikáció (Epic + Task)
- [x] Edge típusok (depends_on, parallel_with, triggers)
- [x] Interaction patterns (drag-drop, zoom, pan, context menu)
- [x] CSS token javaslatok (dark theme)
- [x] Responsive behavior (desktop, tablet, mobile)
- [x] Accessibility (WCAG AA)
- [x] Implementation handoff (Frontend-ready)

---

## Next Steps

1. **Frontend**: Implement React components + SVG canvas
2. **Backend**: Persist node positions + graph state
3. **Testing**: E2E drag-drop, zoom, validation
4. **JoineryTech Migration**: Port to production after testing

---

## Design System Alignment

- ✅ Dark-first, mobile-first approach
- ✅ Drag-drop single-screen focus (no modal clutter)
- ✅ Accessibility-first (keyboard + screen reader)
- ✅ Design tokens leverage (CSS variables, dark theme)
- ✅ Touch-friendly interaction targets

---

**Designer:** Designer Terminal
**Epic:** EPIC-GRAPH-WORKFLOW
**Checkpoint:** CP-FLOW-EDITOR
**Model:** Sonnet
**Duration:** Design spec complete
