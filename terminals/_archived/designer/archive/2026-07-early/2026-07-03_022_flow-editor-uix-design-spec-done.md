---
id: MSG-DESIGNER-022-DONE
from: designer
to: root
type: done
status: READ
ref: MSG-DESIGNER-022
epic_id: EPIC-GRAPH-WORKFLOW
checkpoint_id: CP-FLOW-EDITOR
created: 2026-07-03
---

# Flow Editor UI/UX Design Specification

## Executive Summary

A Flow Editor egy **React Flow alapú interaktív graph editor** az EPIC-GRAPH-WORKFLOW Phase 2+ implementációjához. A design building blocks a meglévő Phase 1 (Mermaid.js) komponensekre, dark-first Bento Grid design system-re, és ADR-041/ADR-053 architektúrára.

**Deliverables:**
- Node design (kompakt és részletes nézet)
- Edge design (3 típus: depends_on, parallel_with, triggers)
- Interaction patterns (drag & drop, zoom/pan, details panel, context menu)
- CSS token javaslatok (dark theme)

---

## 1. NODE DESIGN

### 1.1 Node Anatomy

```
┌─────────────────────────────────────────┐
│  ┌─┐  [EPIC-001] Project Alpha      [⚡] │  ← Header (status indicator + icon)
│  │█│  3/5 checkpoints                    │  ← Progress bar
│  └─┘  Backend, Frontend (2 tasks)       │  ← Metadata summary
└─────────────────────────────────────────┘
     ↑                                  ↑
  Status      Node content           Action
  indicator                           handle
```

### 1.2 Node Variants

#### Kompakt Nézet (Default)
Minimal footprint, essential info only.

```
┌──────────────────────────────┐
│ 🟢 [EPIC-CUTTING-Q3]         │  ← Status + ID
│    Cutting Module Q3         │  ← Name
│    ▓▓▓▓▓░░░░░ 60%           │  ← Progress (checkpoints)
└──────────────────────────────┘
```

**Dimensions:**
- Width: 240px (min), auto-expand max 360px
- Height: 72px (min touch target compliance)
- Border-radius: 8px
- Padding: 12px

**Status Indicator (left border accent):**
- pending: 4px left border, `--status-warning` (#ffd400)
- active: 4px left border, `--status-info` (#1d9bf0)
- done: 4px left border, `--status-success` (#00ba7c)
- blocked: 4px left border, `--status-error` (#f4212e)

#### Részletes Nézet (Expanded)
Megjelenik amikor:
- Node selected (click)
- Hover (2s delay)
- Single node in viewport (auto-zoom)

```
┌───────────────────────────────────────────────┐
│ 🟢 [EPIC-CUTTING-Q3]                      [📌] │  ← Pin to keep expanded
│    Cutting Module Q3                          │
│    ▓▓▓▓▓░░░░░ 3/5 checkpoints (60%)          │
│                                                │
│ 📦 Depends on: 2                              │
│    • EPIC-KERNEL-STABLE                       │
│    • EPIC-IDENTITY-V1                         │
│                                                │
│ ⚡ Triggers: 1                                │
│    • EPIC-PORTAL-V2                           │
│                                                │
│ 👤 Assigned: Backend, Frontend (2 tasks)     │
│ 📅 Target: 2026-09-30                         │
└───────────────────────────────────────────────┘
```

**Dimensions:**
- Width: 360px (fixed)
- Height: auto (content-based, max 480px with scroll)

### 1.3 Progress Indicator (Checkpoint support)

```html
<!-- Progress bar komponens -->
<div class="node-progress">
  <div class="progress-track">
    <div
      class="progress-fill"
      style="width: 60%; background: var(--status-info);"
    ></div>
  </div>
  <span class="progress-label">3/5 (60%)</span>
</div>
```

**Visual treatment:**
- Track: `--bg-hover` (#242931), 4px height, rounded
- Fill: Status color, smooth transition (0.3s ease)
- Label: `--text-secondary`, `--text-xs` (12px)

### 1.4 Node States

| State | Visual Change |
|-------|---------------|
| **Default** | `--bg-card` background, `--border-default` border |
| **Hover** | `--bg-hover` background, `--border-hover` border, cursor: pointer |
| **Selected** | `--border-focus` border (2px), subtle glow |
| **Dragging** | Opacity 0.8, cursor: grabbing, shadow elevation |
| **Blocked** | Red pulse animation (subtle), icon: ⚠️ |
| **Connected** | Highlight connected edges (both in/out) |

---

## 2. EDGE DESIGN

### 2.1 Edge Types

#### Type 1: depends_on (Dependency)
Folytonos nyíl, egyirányú.

```
[EPIC-KERNEL] ──────────▶ [EPIC-CUTTING]
                (depends on)
```

**Visual Spec:**
- Stroke: `--border-hover` (#484f58)
- Stroke-width: 2px
- Arrow: Filled triangle, 8px
- Animation on hover: Dashed flow animation (right-to-left, 0.5s)

#### Type 2: parallel_with (Parallel execution hint)
Szaggatott vonal, kétirányú.

```
[EPIC-CUTTING] ┈┈┈┈┈┈┈┈┈┈┈ [EPIC-PORTAL]
              (parallel with)
```

**Visual Spec:**
- Stroke: `--status-info` (#1d9bf0), opacity 0.5
- Stroke-width: 2px
- Stroke-dasharray: 6 4
- No arrow (bidirectional hint)

#### Type 3: triggers (Event trigger)
Villám ikon középen, folytonos nyíl.

```
[DONE Task] ──⚡──▶ [New Task]
           (triggers)
```

**Visual Spec:**
- Stroke: `--accent` (#1d9bf0)
- Stroke-width: 2px
- Icon: ⚡ (16px) SVG, positioned at 50% edge length
- Arrow: Filled triangle, 8px

### 2.2 Edge States

| State | Visual Change |
|-------|---------------|
| **Default** | Base stroke color, opacity 0.6 |
| **Hover** | Opacity 1.0, stroke-width 3px, show label |
| **Selected** | Accent color, stroke-width 3px, persistent label |
| **Source/Target selected** | Highlight (opacity 1.0) |

### 2.3 Edge Labels (on hover/select)

```
┌────────────────────┐
│ depends_on         │  ← Edge type
│ Added: 2026-06-22  │  ← Metadata
└────────────────────┘
```

**Positioning:** Center of edge, white background, subtle shadow.

---

## 3. INTERACTION PATTERNS

### 3.1 Drag & Drop

**Node Movement:**
1. Grab node (cursor: grab)
2. Drag to new position (cursor: grabbing, opacity 0.8)
3. Drop (snap-to-grid optional, 20px grid)

**Connection Creation:**
1. Hover over node → show connection handles (4 sides: top/right/bottom/left)
2. Drag from handle → show temporary edge (dashed, follows cursor)
3. Drop on target node handle → create connection
4. Cancel: Drop outside any node (edge disappears)

**Edge type selection:**
- Default: `depends_on`
- Hold `Shift`: `parallel_with`
- Hold `Ctrl/Cmd`: `triggers`
- After drop: Context menu to change type

### 3.2 Zoom & Pan

**Controls:**
- **Zoom:** Mouse wheel, pinch gesture (touch), toolbar buttons (+/-)
- **Pan:** Click + drag canvas background, two-finger drag (touch)
- **Fit to view:** Toolbar button (auto-center all nodes)
- **Mini-map:** Bottom-right corner (optional, 120x80px)

**Zoom levels:**
- Min: 0.25x (25%)
- Max: 2.0x (200%)
- Default: 1.0x (100%)
- Smooth transition: 0.2s ease

### 3.3 Node Selection + Details Panel

**Selection:**
- Single click: Select node (border highlight)
- Ctrl/Cmd + click: Multi-select
- Click canvas background: Deselect all

**Details Panel:**
- Slides in from right (360px width)
- Shows expanded node info (see 1.2 Részletes Nézet)
- "X" close button (top-right)
- Can pin multiple nodes (tabbed interface)

**Keyboard shortcuts:**
- `Esc`: Close panel / Deselect
- `Delete`: Remove selected node(s)
- `Ctrl/Cmd + A`: Select all
- `Ctrl/Cmd + D`: Duplicate node

### 3.4 Context Menu

**Trigger:** Right-click node or edge

**Node Context Menu:**
```
┌────────────────────────┐
│ 📝 Edit details        │
│ 🔗 Add dependency      │
│ ⚡ Add trigger         │
│ 📌 Pin to canvas       │
│ ───────────────────    │
│ 🗑️  Delete             │
└────────────────────────┘
```

**Edge Context Menu:**
```
┌────────────────────────┐
│ 🔄 Change type         │
│   • depends_on         │
│   • parallel_with      │
│   • triggers           │
│ ───────────────────    │
│ 🗑️  Remove connection  │
└────────────────────────┘
```

**Position:** Near cursor, avoid viewport edges (smart positioning).

---

## 4. LAYOUT & CANVAS

### 4.1 Canvas Structure

```
┌─────────────────────────────────────────────────────────────┐
│  [Toolbar]                                           [⚙️]    │  ← Top toolbar (fixed)
├─────────────────────────────────────────────────────────────┤
│                                                               │
│                   [Graph Canvas]                             │  ← React Flow canvas
│                                                               │
│                                                               │
│                                           [Mini-map]         │  ← Bottom-right mini-map
└─────────────────────────────────────────────────────────────┘
                                            [Details Panel] →   ← Slide-in panel (right)
```

### 4.2 Toolbar (Top)

**Left side:**
- 🏠 Fit to view
- ➕ Zoom in
- ➖ Zoom out
- 📐 Grid toggle (snap-to-grid)

**Center:**
- Epic/Project selector dropdown (if multiple graphs)

**Right side:**
- 💾 Save (YAML export)
- ↩️ Undo
- ↪️ Redo
- ⚙️ Settings (auto-layout, theme)

**Styling:**
- Height: 48px (touch-friendly)
- Background: `--bg-secondary` (#15191e)
- Border-bottom: `--border-default`

### 4.3 Mini-map

**Dimensions:** 160x100px (bottom-right corner, 16px margin)

**Features:**
- Simplified node representation (colored rectangles)
- Viewport indicator (blue rectangle)
- Click to jump to area
- Draggable viewport

---

## 5. CSS TOKEN RECOMMENDATIONS

### 5.1 Flow Editor Specific Tokens

```css
:root {
  /* Node tokens */
  --node-bg: var(--bg-card);              /* #1a1d23 */
  --node-bg-hover: var(--bg-hover);       /* #242931 */
  --node-border: var(--border-default);   /* #30363d */
  --node-border-hover: var(--border-hover); /* #484f58 */
  --node-border-focus: var(--border-focus); /* #1d9bf0 */
  --node-border-radius: 8px;
  --node-padding: 12px;
  --node-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
  --node-shadow-hover: 0 8px 20px rgba(0, 0, 0, 0.4);

  /* Edge tokens */
  --edge-stroke: var(--border-hover);     /* #484f58 */
  --edge-stroke-width: 2px;
  --edge-stroke-hover: 3px;
  --edge-accent: var(--accent);           /* #1d9bf0 */

  /* Status tokens (reuse existing) */
  --node-status-pending: var(--status-warning);   /* #ffd400 */
  --node-status-active: var(--status-info);       /* #1d9bf0 */
  --node-status-done: var(--status-success);      /* #00ba7c */
  --node-status-blocked: var(--status-error);     /* #f4212e */

  /* Progress bar */
  --progress-track: var(--bg-hover);      /* #242931 */
  --progress-fill-height: 4px;
  --progress-border-radius: 2px;

  /* Canvas */
  --canvas-bg: var(--bg-primary);         /* #0f1419 */
  --canvas-grid: var(--border-default);   /* #30363d */
  --canvas-grid-size: 20px;

  /* Mini-map */
  --minimap-bg: var(--bg-secondary);      /* #15191e */
  --minimap-border: var(--border-default);
  --minimap-viewport: var(--accent);      /* #1d9bf0 */

  /* Context menu */
  --menu-bg: var(--bg-card);              /* #1a1d23 */
  --menu-item-hover: var(--bg-hover);     /* #242931 */
  --menu-border: var(--border-default);
  --menu-shadow: 0 4px 16px rgba(0, 0, 0, 0.5);
}
```

### 5.2 Dark Theme Compliance

✅ **Checklist:**
- [ ] All backgrounds use `--bg-*` tokens (no hard-coded colors)
- [ ] Text contrast ratio WCAG 2.1 AA compliant (4.5:1 minimum)
- [ ] Focus indicators visible (2px border, high contrast)
- [ ] Status colors distinct (colorblind-friendly palette)
- [ ] Touch targets ≥44px (toolbar buttons, handles)

---

## 6. COMPONENT HIERARCHY (React Flow)

### 6.1 Component Tree

```
<FlowEditor>
  ├── <Toolbar />
  ├── <ReactFlowProvider>
  │   └── <ReactFlow
  │       nodes={nodes}
  │       edges={edges}
  │       nodeTypes={customNodeTypes}
  │       edgeTypes={customEdgeTypes}
  │     >
  │       ├── <Background />          ← Grid pattern
  │       ├── <Controls />            ← Zoom/fit controls
  │       ├── <MiniMap />             ← Mini-map
  │       └── <Panel>                 ← Toolbar overlay
  │   </ReactFlow>
  ├── <NodeDetailsPanel />            ← Slide-in panel
  └── <ContextMenu />                 ← Right-click menu
```

### 6.2 Custom Node Component

```tsx
// components/Graph/FlowNode.tsx
interface FlowNodeProps {
  data: GraphNode;
  isConnectable: boolean;
  selected: boolean;
}

export function FlowNode({ data, isConnectable, selected }: FlowNodeProps) {
  const [expanded, setExpanded] = useState(false);
  const statusColor = STATUS_COLORS[data.status];

  return (
    <div
      className={`flow-node ${selected ? 'selected' : ''}`}
      style={{ borderLeftColor: statusColor }}
    >
      {/* Kompakt nézet */}
      {!expanded && <CompactView node={data} />}

      {/* Részletes nézet */}
      {expanded && <DetailedView node={data} />}

      {/* Connection handles */}
      <Handle type="target" position={Position.Left} />
      <Handle type="source" position={Position.Right} />
    </div>
  );
}
```

### 6.3 Custom Edge Component

```tsx
// components/Graph/FlowEdge.tsx
interface FlowEdgeProps {
  id: string;
  sourceX: number;
  sourceY: number;
  targetX: number;
  targetY: number;
  data: {
    type: 'depends_on' | 'parallel_with' | 'triggers';
    label?: string;
  };
}

export function FlowEdge({ data, ...props }: FlowEdgeProps) {
  const edgeStyle = getEdgeStyle(data.type);

  return (
    <>
      <BaseEdge {...props} style={edgeStyle} />
      {data.type === 'triggers' && (
        <EdgeIcon
          icon="⚡"
          x={(props.sourceX + props.targetX) / 2}
          y={(props.sourceY + props.targetY) / 2}
        />
      )}
    </>
  );
}
```

---

## 7. MOBILE RESPONSIVENESS

### 7.1 Mobile-First Considerations

**Touch Gestures:**
- Pinch-to-zoom: Native support (React Flow)
- Two-finger pan: Canvas navigation
- Long-press: Context menu (instead of right-click)
- Tap: Select node
- Double-tap: Expand node to detailed view

**Viewport Adaptations:**
- `< 768px` (mobile): Hide mini-map, simplify toolbar (icons only)
- `768px - 1024px` (tablet): Show mini-map, full toolbar
- `> 1024px` (desktop): Full features

**Node Scaling:**
- Mobile: Min node width 200px (compact readability)
- Desktop: Min node width 240px

---

## 8. ACCESSIBILITY

### 8.1 Keyboard Navigation

| Key | Action |
|-----|--------|
| `Tab` | Navigate between nodes (focus ring) |
| `Enter` | Select focused node |
| `Space` | Toggle node expansion |
| `Arrow keys` | Pan canvas (when focused) |
| `Delete` | Remove selected node |
| `Esc` | Deselect / Close panel |

### 8.2 Screen Reader Support

```tsx
<div
  role="button"
  aria-label={`Epic node: ${node.name}, status: ${node.status}`}
  aria-pressed={selected}
  tabIndex={0}
>
  {/* Node content */}
</div>
```

### 8.3 Focus Indicators

- 2px solid `--border-focus` (#1d9bf0)
- Offset: 2px (distinct from node border)
- Never remove `:focus` styles

---

## 9. ANIMATION & TRANSITIONS

### 9.1 Smooth Transitions

```css
.flow-node {
  transition:
    border-color 0.2s ease,
    background-color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.flow-edge {
  transition:
    stroke-width 0.2s ease,
    opacity 0.2s ease;
}
```

### 9.2 Micro-interactions

- **Node hover:** Scale 1.02, shadow elevation
- **Edge hover:** Stroke width 2px → 3px
- **Drag start:** Scale 0.98, opacity 0.8
- **Drop:** Subtle "bounce" (scale 1.0 → 1.05 → 1.0, 0.3s)

---

## 10. IMPLEMENTATION NOTES

### 10.1 React Flow Library

**Package:** `reactflow` (v11+)

**Installation:**
```bash
npm install reactflow
```

**Core Features Used:**
- Custom nodes: `nodeTypes={{ epic: FlowNode }}`
- Custom edges: `edgeTypes={{ dependency: FlowEdge }}`
- Controlled flow: `nodes`, `edges`, `onNodesChange`, `onEdgesChange`
- Mini-map: `<MiniMap />`
- Background: `<Background variant="dots" />`

### 10.2 Layout Algorithm

**Phase 1 (MVP):** Manual layout (drag & drop)

**Phase 2 (Auto-layout):** Use `elkjs` for hierarchical layout
```bash
npm install elkjs
```

**Layout options:**
- Hierarchical (top-to-bottom)
- Force-directed (organic)
- Layered (strict hierarchy)

### 10.3 Data Flow

```
1. API call → /api/graph/epics
2. Parse EPICS.yaml → WorkflowGraph
3. Map GraphNode[] → React Flow nodes
4. Map dependencies → React Flow edges
5. Render <ReactFlow />
6. User interaction → onNodesChange/onEdgesChange
7. Update state → optimistic UI update
8. Debounced API call → PATCH /api/graph/epics
```

---

## 11. ACCEPTANCE CRITERIA CHECKLIST

### Design Deliverables
- [x] Node design specifikáció kész (kompakt, részletes, 4 states)
- [x] Edge típusok definiálva (depends_on, parallel_with, triggers)
- [x] Interaction patterns dokumentálva (drag & drop, zoom, panel, context menu)
- [x] CSS token javaslatok (dark theme compatible)
- [x] Wireframe / component hierarchy

### Implementation Ready
- [x] React Flow integration stratégia
- [x] Custom node/edge component spec
- [x] Mobile responsiveness design
- [x] Accessibility (WCAG 2.1 AA)
- [x] Animation/transition guidelines

---

## 12. HANDOFF TO FRONTEND

### 12.1 Priority Tasks

| # | Task | Description | Estimate |
|---|------|-------------|----------|
| 1 | Install React Flow | `npm install reactflow` | 5 min |
| 2 | Create FlowNode component | Kompakt + részletes nézet | 3h |
| 3 | Create FlowEdge components | 3 típus (depends_on, parallel_with, triggers) | 2h |
| 4 | Implement FlowEditor page | Canvas, toolbar, panel integration | 4h |
| 5 | Add drag & drop logic | Node positioning, connection creation | 3h |
| 6 | Context menu | Right-click menu | 1h |
| 7 | CSS styling | Dark theme tokens | 2h |
| 8 | Mobile gestures | Touch support | 2h |

**Total estimate:** ~17 hours (2-3 sprint days)

### 12.2 API Dependencies

Frontend needs these endpoints working:
- `GET /api/graph/epics` — Epic dependency graph
- `GET /api/graph/project/:slug` — Project task graph
- `PATCH /api/graph/epics` — Update node positions/connections

### 12.3 Testing Checklist

- [ ] Node dragging works
- [ ] Connection creation (3 edge types)
- [ ] Zoom/pan smooth
- [ ] Details panel opens/closes
- [ ] Context menu (right-click)
- [ ] Mobile gestures (pinch-zoom)
- [ ] Keyboard navigation
- [ ] Dark theme consistency

---

## 13. FIGMA LINKS

> **Note:** ASCII/Markdown mockups provided above. Figma mockups optional (not blocking).

**If Figma needed:**
- [ ] High-fidelity node designs
- [ ] Edge style variations
- [ ] Context menu UI
- [ ] Toolbar/mini-map
- [ ] Mobile viewport mockups

**Estimated Figma time:** 4-6 hours

---

## 14. FUTURE ENHANCEMENTS (Post-MVP)

### Phase 3+
- [ ] Auto-layout algorithms (elkjs integration)
- [ ] Node grouping / clusters
- [ ] Search/filter nodes
- [ ] Diff view (compare EPICS.yaml versions)
- [ ] Collaborative editing (real-time)
- [ ] Export as PNG/SVG
- [ ] Undo/redo history
- [ ] Node templates library

---

## Conclusion

This design spec provides a **complete blueprint** for the Flow Editor UI/UX. All design decisions are grounded in:
- ✅ Existing design system (dark-first Bento Grid)
- ✅ ADR-041/ADR-053 architecture
- ✅ Mobile-first, touch-optimized principles
- ✅ WCAG 2.1 AA accessibility
- ✅ React Flow library best practices

**Ready for Frontend implementation!** 🚀

---

**Designer sign-off:** Vízió terminál
**Date:** 2026-07-03
**Contact:** `/opt/spaceos/terminals/designer/`
