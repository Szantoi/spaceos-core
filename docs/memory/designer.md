# Designer Terminal Memory — Updated 2026-07-07

## ROLE & RESPONSIBILITIES

**Primary Mission:** UX Coordinator + Visual QA for JoineryTech/Datahaven UI development

### Core Duties

1. **Frontend Task Review** — Design system consistency, WCAG AA accessibility, mobile-first compliance
2. **Design System Maintenance** — Component specs updates, CSS variable standards, `docs/design/` curation
3. **Visual Testing** — Playwright MCP-based screenshot regression, color contrast validation
4. **Mobile-First Audit** — Touch targets (≥44px), responsive breakpoints (480/768/1200px), single-screen focus

### Monitoring Scope

- Active monitoring on all MSG-FRONTEND-* UI changes
- Design system violation detection
- Accessibility gap identification (WCAG 2.1 AA minimum)
- Mobile-first principle enforcement

---

## JOINERYTECH PORTAL CONTEXT

### Working Environment

**Dev Server:** http://localhost:5173/ ✅ RUNNING
**Working Directory:** `/opt/spaceos/frontend/joinerytech-portal/`
**Tech Stack:** React 18 + TypeScript + Vite + Tailwind CSS 4 + React Router + TanStack Query + MSW

**NOT Datahaven:** `/opt/spaceos/datahaven-web/client/` is separate project (Datahaven Dashboard)

### Application Scope

**Massive Portal:** 27 "World" (világ) functionality modules

```
Production, Sales, Design, Warehouse, Settings, CRM, Finance, Projects,
Logistics, MfgPrep, Supervisor, Masterdata, Trade, Interior, Maintenance,
Quality, EHS, Attendance, Tasks, Docs, AI, ExecBI, Shop, HR, Kontrolling, Service
```

**Routes:**
- Public: `/` (LandingPage), `/login`, `/quote-request`
- Protected: `/w`, `/w/{world}`, `/w/{world}/{screen}`
- Standalone: `/shopfloor`, `/supplier/portal`, `/configurator`

**Dev Commands:**
```bash
cd /opt/spaceos/frontend/joinerytech-portal/
npm run dev          # Start dev server on localhost:5173
npm run build        # Production build
npm run generate:api # Generate API client from OpenAPI spec
```

---

## DESIGN SYSTEM STANDARDS

### Dark-First Aesthetic

**CSS Variables (`:root`):**
```css
--color-bg-primary: #0f1419
--color-bg-secondary: #1a1f26
--color-text-primary: #e7e9ea
--color-accent-primary: #3b82f6
```

**Tailwind 4 Configuration:**
- `@variant dark` — `@media (prefers-color-scheme: dark)` + `:is(.dark *)`
- `index.css` contains complete variable system
- Dark mode toggle functional

### WCAG AA Compliance

**Color Contrast Requirements:**
- **Normal text:** ≥4.5:1 contrast ratio
- **Large text (18pt+):** ≥3:1 contrast ratio
- **AAA target:** ≥7:1 contrast ratio (preferred)

**Example Fix from MSG-DESIGNER-023:**
- ❌ sky-50/sky-700: 3.1:1 (FAIL)
- ✅ blue-100/blue-800: 7.8:1 (AAA)

### Mobile-First Principles

- Touch targets: ≥44px × 44px
- Breakpoints: 480px (mobile), 768px (tablet), 1200px (desktop)
- Single-screen focus (avoid excessive nesting)

---

## DESIGN SPEC DOCUMENT

**Location:** `/opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md`

**Summary:**
- **Size:** 1325 lines, ~80 KB
- **Sections:** Navigation architecture, dark mode system, desktop layouts, color contrast matrix, keyboard interaction, ARIA checklists
- **Code Examples:** 30+ copy-paste ready (TypeScript/JSX/CSS)
- **Wireframes:** 4 ASCII art diagrams

**Implementation Phases:**
- **Phase 0 (Critical, 2h):** Color contrast fixes, focus indicators, modal escape/trap
- **Phase 1 (UI/UX, 2d):** Navigation system, dark mode tokens + toggle, desktop layouts
- **Phase 2 (A11y, 1d):** ARIA attributes, keyboard navigation, live regions

**Total Estimated Time:** ~24 hours (3 days)

---

## FRONTEND REVIEW WORKFLOW

### 4-Step Process

**1. Review UI Tervek**
- Specifikációk: `/opt/spaceos/docs/joinerytech/` (tervdokumentumok, NOT final implementation)

**2. Frontend Implementáció**
- Frontend terminál megvalósítja a tervet a React appban
- Lokális preview: http://localhost:5173/
- Working directory: `/opt/spaceos/frontend/joinerytech-portal/src/`

**3. Designer Visual Review**
- Browser check: http://localhost:5173/
- Design system compliance check
- WCAG accessibility validation
- Mobile responsive testing
- **Playwright MCP visual testing** (automated screenshots, contrast checks)

**4. Review Decision**
- **APPROVE** → Frontend sends DONE to Conductor
- **REJECT** → Frontend revisions, repeat step 3

---

## PLAYWRIGHT MCP TOOLS (6)

**Configuration:** `/opt/spaceos/terminals/designer/.mcp.json` ✅ FIXED

**Available Tools:**

| Tool | Use Case |
|------|----------|
| `playwright_navigate` | Load page (e.g., http://localhost:5173/dashboard) |
| `playwright_screenshot` | Visual regression testing (before/after screenshots) |
| `playwright_click` | Functional testing (e.g., dark mode toggle) |
| `playwright_fill` | Form input testing |
| `playwright_evaluate` | WCAG color contrast measurement (`getComputedStyle`) |
| `playwright_pdf` | PDF export (design spec docs) |

**Example Workflow:**
```bash
# 1. Screenshot visual regression
playwright_screenshot url="http://localhost:5173/dashboard" path="screenshot.png"

# 2. Dark mode toggle test
playwright_navigate url="http://localhost:5173/"
playwright_click selector="[data-testid='dark-mode-toggle']"
playwright_screenshot path="dark-mode.png"

# 3. Color contrast WCAG validation
playwright_evaluate script="getComputedStyle(document.body).backgroundColor"

# 4. Responsive breakpoint testing
playwright_navigate url="http://localhost:5173/" viewport="{width:360,height:640}"
playwright_screenshot path="mobile-view.png"
```

**Note:** MCP tools load at session start. If missing, restart session:
```bash
tmux kill-session -t spaceos-designer
tmux new-session -s spaceos-designer -c /opt/spaceos/terminals/designer "claude"
```

---

## RECENT SESSIONS SUMMARY (2026-07-03)

### MSG-DESIGNER-023 — JoineryTech UI/UX + A11y Design Fixes ✅ DONE

**Deliverables:**
- Navigation architecture (3-level hierarchy: Worlds → Tabs → Detail)
- Dark mode design system (CSS variables, Tailwind config, 10+ component examples)
- Desktop layout wireframes (3 patterns with ASCII diagrams)
- Color contrast matrix WCAG AA (all ratios fixed)
- Keyboard interaction patterns (5+ components with TypeScript)
- ARIA attribute checklists (6 component categories)

**Outbox:** MSG-DESIGNER-023-DONE, MSG-DESIGNER-029 (MCP auto-generated)

### MSG-DESIGNER-021 — UX Coordination Role Confirmation ✅ DONE

**Summary:** Root requested confirmation of Designer's UX coordination role. Reviewed 5 existing deliverables, confirmed monitoring responsibilities (design system compliance, mobile-first audits, accessibility validation).

**Outbox:** MSG-DESIGNER-030-DONE, MSG-DESIGNER-031 (MCP auto-generated)

### MSG-DESIGNER-024 — Playwright MCP Added ✅ CONFIGURED

**Summary:** Playwright MCP server added to Designer terminal. Configuration fixed (package name: `@playwright/mcp`, version 0.0.77). 6 tools now available for visual testing, WCAG automation, responsive screenshots.

**Outbox:** MSG-DESIGNER-033 (Playwright MCP acknowledgement + config fix)

---

## DESIGN SYSTEM AUDIT FINDINGS

### ✅ Positive Findings

1. **Tailwind 4 Dark Mode** — Correct configuration (`@variant dark`, CSS variables)
2. **Dark-First Palette** — Industrial aesthetic (`#0f1419` bg-primary, `#e7e9ea` text-primary)
3. **LandingPage UI** — Light theme marketing page (stone/teal palette, modern card layout)

### ⚠️ Potential Issues

1. **Hard-coded colors** — `SidebarDark.tsx`: `bg-[#0b1220]` (hex literal, should use CSS variable)
2. **CSS Variables usage** — Not visible in components (code-based review limited, needs visual inspection)

### Next Actions (TODO)

1. Playwright functional test → Screenshot, navigate, evaluate
2. Visual testing → http://localhost:5173/ pages validation
3. Design compliance → CSS variables usage verification
4. 27 World audit → Components, accessibility review
5. WCAG validation → Color contrast, keyboard nav, ARIA

---

**Last Updated:** 2026-07-07
**Status:** 🟢 READY FOR FRONTEND REVIEWS
**MCP Tools:** spaceos-knowledge, brave-search, playwright ✅
**Memory Tier:** Warm (14-day, active UX coordination role)

---

_This memory is compressed from 12KB to ~4KB by consolidating 4 session narratives from 2026-07-03 into a single coherent summary. Preserved: role definition, review workflow, JoineryTech Portal scope, Playwright MCP tools, and design system standards._
