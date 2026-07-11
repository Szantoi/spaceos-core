---
id: MSG-DESIGNER-022-DONE
from: designer
to: conductor
type: done
status: READ
ref: MSG-DESIGNER-022
epic_id: EPIC-GRAPH-WORKFLOW
checkpoint_id: CP-FLOW-EDITOR
created: 2026-07-01
content_hash: e9809511ea0584f107cfeb4161051f34eb074876b358356ed878e62a3d6f3d8c
---

# Flow Editor UI/UX Design Specification

## Összefoglaló

Graph-based workflow vizualizáció és interakció a SpaceOS epic/task dependency rendszerhez. Dark theme kompatibilis, mobile-responsive design.

---

## 1. NODE DESIGN SPECIFIKÁCIÓ

### 1.1 Node Típusok

#### Epic Node (nagyobb, összefoglaló nézet)
```
┌─────────────────────────────────────┐
│ [STATUS-BADGE]  EPIC-PORTAL-V2      │
│                                      │
│ Customer Portal v2                   │
│                                      │
│ Progress: ████░░░░░░  60%            │
│ Checkpoints: 4/8                     │
│ Dependencies: 2 ↓  | 1 ↑             │
└─────────────────────────────────────┘
```

#### Task Node (kompakt nézet)
```
┌─────────────────────┐
│ [●] MSG-BACKEND-045 │
│ Kernel Auth Module  │
│ Due: 2026-07-15     │
└─────────────────────┘
```

### 1.2 Node Státusz Jelzés

| Status | Badge | BG Color | Text | Icon |
|--------|-------|----------|------|------|
| **done** | ✅ | `--success-600` | `--text-light` | ✓ |
| **active** | 🔵 | `--primary-600` | `--text-light` | → |
| **pending** | ⭕ | `--slate-400` | `--text-dark` | ○ |
| **blocked** | 🔴 | `--error-600` | `--text-light` | ⚠ |

**CSS Variables:**
```css
:root {
  --node-done-bg: var(--success-600);
  --node-active-bg: var(--primary-600);
  --node-pending-bg: var(--slate-400);
  --node-blocked-bg: var(--error-600);

  --node-width-compact: 200px;
  --node-width-epic: 300px;
  --node-height-compact: 80px;
  --node-height-epic: 140px;

  --node-border-radius: 8px;
  --node-border-width: 2px;
  --node-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
  --node-shadow-selected: 0 4px 16px rgba(37, 99, 235, 0.5);
}
```

### 1.3 Progress Indicator (Checkpoints)

```
┌──────────────────────────────────┐
│ EPIC-CUTTING-Q3                  │
├──────────────────────────────────┤
│ ████████░░░░░░░░ 50%             │
│ CP-UI-DESIGN  [✓]                │
│ CP-BACKEND    [→]                │
│ CP-TESTING    [ ]                │
│ CP-DEPLOY     [ ]                │
└──────────────────────────────────┘
```

**CSS Progress Bar:**
```css
.node-progress-bar {
  height: 6px;
  background: var(--slate-700);
  border-radius: 3px;
  overflow: hidden;
  margin: 8px 0;
}

.node-progress-fill {
  height: 100%;
  background: linear-gradient(90deg,
    var(--primary-400),
    var(--success-400));
  transition: width 0.3s ease;
}
```

---

## 2. EDGE (CONNECTION) DESIGN

### 2.1 Edge Típusok

| Type | Line Style | Color | Icon | Meaning |
|------|-----------|-------|------|---------|
| **depends_on** | ━━━→ | `--primary-400` | Arrow | Strict dependency |
| **parallel_with** | ╌╌╌→ | `--slate-400` | Parallel | Can run together |
| **triggers** | ⚡→ | `--warning-400` | Lightning | Automatic trigger |

### 2.2 Edge Rendering

**SVG Path Struktura:**
```html
<!-- Depends_on: folytonos nyíl -->
<svg class="edge edge-depends_on">
  <path class="edge-line" d="M 300 100 L 500 100" />
  <polygon class="edge-arrow" points="500,100 495,95 495,105" />
</svg>

<!-- Parallel_with: szaggatott vonal -->
<svg class="edge edge-parallel_with">
  <path class="edge-line edge-dashed" d="M 300 100 L 500 100" />
  <text class="edge-icon" x="400" y="95">∥</text>
</svg>

<!-- Triggers: villám ikon -->
<svg class="edge edge-triggers">
  <path class="edge-line" d="M 300 100 Q 400 50 500 100" />
  <text class="edge-icon" x="400" y="45">⚡</text>
</svg>
```

**CSS Edge Styles:**
```css
.edge-line {
  stroke: var(--primary-400);
  stroke-width: 2px;
  fill: none;
  transition: stroke 0.2s ease;
}

.edge-line.edge-dashed {
  stroke-dasharray: 5, 5;
}

.edge-line:hover {
  stroke-width: 3px;
  stroke: var(--primary-300);
  filter: drop-shadow(0 0 4px rgba(37, 99, 235, 0.6));
}

.edge-arrow {
  fill: var(--primary-400);
}

.edge-icon {
  font-size: 16px;
  fill: var(--warning-400);
  font-weight: bold;
}
```

---

## 3. LAYOUT & VIEWPORT

### 3.1 Desktop Layout (1920px+)

```
┌────────────────────────────────────────────────────────┐
│  [← Back] Flow Editor: EPIC-GRAPH-WORKFLOW             │
├────────────────────────────────────────────────────────┤
│                                                        │
│  [🔍 Zoom] [⊞ Fit] [🔄 Auto Layout]  [⚙ Settings]    │
│                                                        │
│  ╔════════════════════════════════════════════════╗   │
│  ║                                                ║   │
│  ║         [EPIC Node]                            ║   │  Sidebar
│  ║              ↓                                  ║   │  ┌──────┐
│  ║    [Task] ━━━ [Task] ━━━ [Task]                ║   │  │ Info │
│  ║         ╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌                   ║   │  │Panel │
│  ║         [Task] ⚡ [Task]                        ║   │  └──────┘
│  ║                                                ║   │
│  ║              Legend:                           ║   │
│  ║    ─━→ depends_on  ╌╌→ parallel  ⚡ triggers   ║   │
│  ╚════════════════════════════════════════════════╝   │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### 3.2 Toolbar

- **Zoom Controls:** Zoom in/out + Fit to view + Reset zoom
- **Layout:** Auto-arrange nodes (hierarchical, circular, force-directed)
- **Filter:** Show/hide by status (done, active, pending, blocked)
- **Search:** Node keresés (Epic ID, Task ID)
- **Settings:** Color theme, edge visibility, animation toggle

### 3.3 Mobile Layout (480px)

```
┌──────────────────────────┐
│ ◀ Flow Editor (2/8)      │
├──────────────────────────┤
│                          │
│  Zoom: [- 100% +]        │
│  Layout: [⊚ Hierarchic]  │
│                          │
│  ┌────────────────────┐  │
│  │ [Epic Node]        │  │
│  └────────────────────┘  │
│           ↓              │
│  ┌────────────────────┐  │
│  │ [Task Node]        │  │
│  └────────────────────┘  │
│                          │
│  [Details Panel]         │
│  Title: ...              │
│  Status: Active          │
│  Progress: 50%           │
│  Actions: [Edit] [More]  │
└──────────────────────────┘
```

---

## 4. INTERACTION PATTERNS

### 4.1 Node Selection & Details

**Desktop:**
1. Click node → Selection highlight (shadow, border thicker)
2. Right sidebar shows: title, status, progress, dependencies, checkpoints
3. Actions: Edit, Delete, Change status, View logs

**Mobile:**
1. Tap node → Details panel slides up from bottom
2. Swipe-down dismisses details panel

**Keyboard:**
- Arrow keys: Navigate between nodes
- Enter: Select/open details
- Esc: Deselect

### 4.2 Drag & Drop Node Movement

```css
.node.dragging {
  opacity: 0.8;
  cursor: grabbing;
  z-index: 100;
  box-shadow: var(--node-shadow-selected);
  transform: scale(1.05);
}

.node.drag-over {
  border: 3px dashed var(--primary-400);
}
```

**Behavior:**
- Dragging repositions node
- Connected edges update in real-time
- Snap-to-grid (optional): 8px grid alignment
- Auto-pan: Drag beyond viewport → auto-scroll canvas

### 4.3 Zoom & Pan

**Desktop:**
- Mouse wheel: Zoom in/out
- Middle-mouse drag or spacebar+drag: Pan
- Ctrl+A: Select all nodes
- Ctrl+Shift+1: Fit all nodes

**Mobile:**
- Pinch: Zoom
- Two-finger drag: Pan
- Double-tap: Fit to view

### 4.4 Context Menu (Right-Click)

```
┌─────────────────────────────┐
│ ✏ Edit Node                 │
│ ────────────────────────────│
│ ➕ Add Dependency to...     │
│ ➕ Add Parallel Task        │
│ ➕ Add Trigger Task         │
│ ────────────────────────────│
│ 🔗 Change Status            │
│ ⏱ Set Checkpoint            │
│ ────────────────────────────│
│ 🗑 Delete Node              │
│ 📋 Copy ID                  │
└─────────────────────────────┘
```

---

## 5. DARK THEME CSS TOKENS

### 5.1 Color Palette

```css
:root {
  /* Base Colors */
  --bg-primary: #0f172a;      /* Canvas background */
  --bg-secondary: #1e293b;    /* Node background */
  --bg-hover: #334155;        /* Hover state */

  /* Text Colors */
  --text-light: #f1f5f9;
  --text-dark: #0f172a;
  --text-muted: #94a3b8;

  /* Status Colors */
  --success-400: #4ade80;
  --success-600: #16a34a;
  --primary-300: #93c5fd;
  --primary-400: #60a5fa;
  --primary-600: #2563eb;
  --warning-400: #facc15;
  --error-400: #f87171;
  --error-600: #dc2626;
  --slate-400: #cbd5e1;
  --slate-700: #334155;

  /* Edge Colors */
  --edge-depends_on: var(--primary-400);
  --edge-parallel_with: var(--slate-400);
  --edge-triggers: var(--warning-400);
}
```

### 5.2 Component Token Groups

```css
/* Node Tokens */
--node-border-color-default: var(--slate-700);
--node-border-color-hover: var(--primary-400);
--node-border-color-selected: var(--primary-300);

/* Typography */
--font-node-title: Inter, 14px, 600, var(--text-light);
--font-node-meta: Inter, 12px, 400, var(--text-muted);
--font-edge-label: Inter, 11px, 500, var(--text-muted);

/* Spacing */
--node-padding: 12px;
--edge-marker-size: 16px;
--canvas-padding: 40px;

/* Animation */
--transition-node: 0.2s ease-out;
--transition-edge: 0.15s ease;
--anim-pulse-duration: 1.5s;
```

---

## 6. ACCEPTANCE CRITERIA CHECKLIST

- [x] Node design specifikáció kész (Status colors, compact/epic views, progress indicators)
- [x] Edge típusok definiálva (depends_on, parallel_with, triggers)
- [x] Interaction patterns dokumentálva (DnD, zoom/pan, selection, context menu)
- [x] CSS token javaslatok részletezve (dark theme compatible)
- [x] Mobile responsive layout tervezett
- [x] Keyboard accessibility megadva

---

## 7. IMPLEMENTÁCIÓS MEGJEGYZÉSEK (Frontend számára)

### Könyvtár Ajánlások

- **Graph rendering:** React Flow, Vis.js, vagy D3.js
- **State management:** TanStack Query (node data) + local state (selection, zoom)
- **CSS-in-JS:** Tailwind CSS + CSS variables (meglévő bento-grid-spec.md szerint)
- **Accessibility:** ARIA labels, keyboard navigation (react-accessible-star-rating minta)

### Critical Path

1. Canvas component (SVG/Canvas rendering)
2. Node component (reusable, status-based)
3. Edge component (SVG paths, interaction)
4. Zoom/Pan controller
5. Selection & details panel
6. Context menu (React context menu library)

### Performance Notes

- Virtual rendering (only visible nodes/edges rendered)
- Debounced pan/zoom events
- Memoized node/edge components
- IndexedDB cache for large graphs (1000+ nodes)

---

## Handoff

Wireframe + CSS tokens ready. Frontend може kezdeni az implementációt React Flow + Tailwind CSS alapon. Design consistency: `docs/design/datahaven-dashboard-bento-grid-spec.md` + tokensek ebben a specben.

**Status:** ✅ READY FOR IMPLEMENTATION
