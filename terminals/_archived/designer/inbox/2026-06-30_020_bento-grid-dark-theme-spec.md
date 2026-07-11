---
id: MSG-DESIGNER-020
from: conductor
to: designer
type: task
priority: high
status: READ
model: sonnet
ref: IDEA-2026-06-30-003
created: 2026-06-30
content_hash: 9cf7b7b0525c3949e1699566b62d877cf6b5198314b148fbf0965a1358c6ea4d
---

# Datahaven Dashboard — Dark-First Bento Grid Layout Design Spec

## Kontextus

A Discovery ciklus **8 UI fejlesztési ötletet** generált. Az első (KPI Dashboard) kész, most a **2. prioritás: Dark-First Bento Grid Layout** következik.

**Forrás ötlet:** `docs/planning/ideas/2026-06-30_003_dark-first-bento-layout.md` (Explorer UX pattern kutatás alapján)

## Feladat

Készíts részletes **design specifikációt** a Datahaven Dashboard teljes layout redesign-jához, amely dark-first, Bento grid alapú, és data-dense pattern-t alkalmaz.

## Design Elvárások

### 1. Layout Struktúra (Bento Grid)

**12 column CSS Grid layout:**
```
┌───────────────────────────────────────┐
│   KPI Strip (6 cards, sticky top)     │  ← Full width, 1/6 height
├───────────────┬──────────────────────┤
│ Kanban Board  │  Timeline/Gantt      │  ← 2 col, 50% each, 4 rows
│ (Discovery)   │  (Project Track)     │
├────────┬──────┼───────┬──────────────┤
│ Side-  │ Task │ Task  │ Alert Panel  │  ← Asymmetric (3+6+3 col)
│ bar    │ List │ List  │              │
└────────┴──────┴───────┴──────────────┘
```

**Grid cell sizes:**
- KPI Strip: 12/12 col, 1 row
- Kanban: 6/12 col, 4 rows
- Timeline: 6/12 col, 4 rows
- Sidebar: 3/12 col, 4 rows
- Task List: 6/12 col, 4 rows
- Alert Panel: 3/12 col, 2 rows

**Responsive breakpoints:**
- Desktop: ≥1200px → 12 col grid
- Tablet: 768-1200px → 6 col grid, Kanban/Timeline full width
- Mobile: ≤768px → 1 col grid, stacked vertically

### 2. Color System (Dark-First)

**Primary dark theme (default):**
```css
--bg-primary: #1a1d23;      /* Near-black background */
--bg-card: #242931;         /* Card background (slightly lighter) */
--bg-hover: #2f3440;        /* Hover state */
--border-color: #3f444f;    /* Card borders */
--text-primary: #e5e7eb;    /* Light gray text (not white) */
--text-secondary: #9ca3af;  /* Muted text */
--text-muted: #6b7280;      /* Disabled/inactive */
```

**Status colors (Neon accents):**
```css
--status-healthy: #10b981;  /* Green */
--status-warning: #f59e0b;  /* Orange */
--status-critical: #ef4444; /* Red */
--status-info: #3b82f6;     /* Blue */
```

**Light theme (adapted from dark):**
- `--bg-primary: #ffffff`
- `--bg-card: #f9fafb`
- `--text-primary: #1f2937`
- **Status colors inverted:** Darker shades (#059669, #d97706, #dc2626, #2563eb)

### 3. Component Specs

**Card component:**
- Background: `--bg-card`
- Border: 1px solid `--border-color`
- Border-radius: 8px
- Padding: 16px (desktop), 12px (mobile)
- Shadow: `0 2px 8px rgba(0,0,0,0.2)` (dark theme)
- Hover: `background: --bg-hover`, `border-color: --status-info`

**Typography:**
- Headings: Inter/Roboto/System font stack
- Body: 14px (desktop), 12px (mobile)
- Monospace (terminal output): `Fira Code` or `Consolas`

**Spacing (16px base unit):**
- Gap between cards: 16px (desktop), 12px (mobile)
- Card padding: 16px (desktop), 12px (mobile)
- Section margin: 24px

### 4. Data-Dense Patterns

**Compact table layout:**
- Row height: 32px (desktop), 40px (mobile, touch target)
- Progressive disclosure: Click row → expand details
- Sortable columns (icon indicator)
- Status badges: ✅ (green), ⚠️ (yellow), ❌ (red)
- Max 20 rows visible → scroll or pagination

**Progressive disclosure example:**
```
┌─────────────────────────────────┐
│ Task ID  │ Status │ Terminal  ▼ │  ← Collapsed row
├─────────────────────────────────┤
│ MSG-001  │ ✅     │ Backend     │  ← Click to expand
└───────────────────┬─────────────┘
                    │ ▼ Details   │  ← Expanded section
                    │ Description │
                    │ Timeline    │
                    │ Dependencies│
                    └─────────────┘
```

### 5. Accessibility (WCAG AA+)

- **Contrast ratio:** Min 4.5:1 (text/background)
- **Keyboard nav:** Tab order logical, focus-visible outline
- **Screen reader:** ARIA labels, role attributes
- **Touch targets:** ≥44px (mobile)
- **Motion:** Respect `prefers-reduced-motion`

### 6. Visual Refinements

**Micro-interactions:**
- Hover: 200ms ease transition (background, border)
- Click: Scale 0.98 (button press feedback)
- Loading: Pulse animation (opacity 0.6 → 1.0)

**Shadows (dark theme):**
- Card: `0 2px 8px rgba(0,0,0,0.2)`
- Hover: `0 4px 12px rgba(0,0,0,0.3)`
- Focus: `0 0 0 3px rgba(59, 130, 246, 0.5)` (blue outline)

## Deliverables

### 1. Design Spec Document

**File:** `docs/design/datahaven-dashboard-bento-grid-spec.md`

**Tartalom:**
- [ ] Layout grid definition (12 col, responsive breakpoints)
- [ ] Color system (dark theme + light theme)
- [ ] Component specs (card, table, button, input)
- [ ] Typography scale
- [ ] Spacing system (16px base unit)
- [ ] Accessibility guidelines
- [ ] Micro-interaction specs (hover, click, loading)

### 2. CSS Variables Definition

**File:** `datahaven-web/client/src/styles/theme-dark-bento.css`

**Tartalom:**
- [ ] All color variables (--bg-*, --text-*, --status-*)
- [ ] Spacing variables (--space-xs, --space-sm, --space-md, --space-lg)
- [ ] Typography variables (--font-*, --line-height-*)
- [ ] Border-radius, shadow, transition variables

### 3. Visual Reference (Optional)

**Figma mockup vagy ASCII diagram:**
- Desktop layout (1920x1080 reference)
- Tablet layout (768px breakpoint)
- Mobile layout (480px)
- Card component spec (hover states)

## Acceptance Criteria

- [ ] Design spec document created (markdown format)
- [ ] CSS variables defined (theme-dark-bento.css)
- [ ] Color contrast validated (4.5:1 minimum)
- [ ] Responsive breakpoints specified (1200, 768, 480px)
- [ ] Component specs documented (card, table, button)
- [ ] Accessibility guidelines included (WCAG AA)
- [ ] Visual reference or mockup (ASCII diagram minimum)
- [ ] Ready for Frontend implementation (MSG-FRONTEND-064)

## Timeline

**Estimated:** 2-3 hours (design spec + CSS variables)

## References

**Explorer UX Research:**
- `docs/planning/ideas/2026-06-30_003_dark-first-bento-layout.md`

**External sources:**
- SaaS Dashboard Design 2026: https://www.925studios.co/blog/saas-dashboard-design-examples-2026
- Dark Mode Trends 2026: https://www.asappstudio.com/admin-dashboard-designs-2026/
- Bento Grid Patterns: https://wrappixel.com/blog/best-dark-mode-dashboard-designs-and-templates

## Next Steps

**After DONE:**
- Conductor sends MSG-FRONTEND-064 (Bento Grid implementation)
- Frontend integrates CSS variables and layout structure
- Testing: Contrast validation, keyboard nav, responsive

---

**Priority:** HIGH — 2. ötlet a Discovery ciklus 8-ból
**Model:** Sonnet (design spec + CSS részletezés igényel)
