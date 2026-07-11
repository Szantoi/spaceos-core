# Frontend Terminal Memory — Updated 2026-07-07

## TECHNICAL STACK

**Core:** React 18 + TypeScript 5 + Vite + TanStack Query + React Hook Form
**API:** Orval (OpenAPI → React Query hooks auto-generation)
**Design:** Tailwind CSS 4 (dark-first), CSS Modules, CSS variables

---

## KEY PATTERNS

### 1. Orval React Query Integration
- Auto-generated hooks from OpenAPI specs (e.g., `useGetEACCalculation`, `useGetCostBreakdown`)
- Domain-specific configs: `orval.{kontrolling,hr,dms,qa,maintenance}.config.ts`
- Custom mutator: `src/services/customInstance.ts` (auth headers)
- Type-safe request/response, auto-refetch, 5-minute SSE updates

### 2. Dashboard Widget Pattern
- Self-contained components: `<EACCalculationWidget projectId={id} realtime />`
- Individual error/loading state handling
- Hungarian business labels (Tervezett, Tényleges, Előrejelzés, Eltérés)
- Responsive grid: `minmax(500px, 1fr)` → auto-collapse on mobile

### 3. Dark-First Design (ADR-048)
- CSS variables: `--color-bg-primary: #0f1419`, `--text-primary: #e7e9ea`
- Status colors: Blue (planned), Green (actual), Red (over budget)
- 100% dark mode compliance (CSS variables enforced, no hard-coded colors)

### 4. Responsive Grid Layouts
- Adaptive columns: 6→3→2→1 based on viewport (1024px, 768px, 480px)
- Mobile strategy: Hide variance columns, single-screen focus, touch targets ≥44px

### 5. Form Validation
- React Hook Form + real-time feedback
- Optimistic updates, error boundary implementation

---

## RECENT SESSIONS (2026-07-06, 2026-07-01)

### Kontrolling Dashboard UI Week 1 ✅
- **MSG-FRONTEND-001 (EPIC-JT-CTRL)**: 14 files, ~1,800 lines, 1.5 hours
- Components: EACCalculationWidget, CostBreakdownChart, VarianceAnalysisPanel, PortfolioSummaryCard
- Build: ✅ 0 TypeScript errors, 25.20s
- **EAC Formula Learned:** `projected[category] = MAX(planned[category], actual[category])`

### Designer Fix: CSS Variable ✅
- **MSG-FRONTEND-151**: LeadGrid.module.css:141 → `var(--text-inverse)`
- Dark mode compliance: 98% → 100%, deployment blocker resolved

### CRM UI Components Wave 1 ✅
- **MSG-FRONTEND-088 (EPIC-JT-CRM)**: 5.5 hours
- LeadGrid (filterable, sortable, paginated), OpportunityPipeline (Kanban, drag-drop)
- 11 TanStack Query hooks, 75%+ test coverage (15 test cases)

### KPI Card System ✅
- **MSG-FRONTEND-083 (EPIC-DATAHAVEN-UI)**: 3 hours
- KPICard, KPIStrip (6 KPI cards, 30s auto-refresh)
- SSE integration, color-coded status (green/yellow/red), trend arrows

---

## KEY LEARNINGS

1. **OpenAPI Spec Quality Critical** — Smooth Orval generation, no manual API client code needed
2. **CSS Variables Enforcement** — Designer REJECT on hard-coded colors ensures 100% dark mode compliance
3. **Widget Self-Containment** — Individual error/loading state prevents cascade failures
4. **Hungarian Labels Essential** — JoineryTech UX requires Hungarian business terminology
5. **Build Time Excellent** — Vite builds: 25-30s full production

---

## POST-IMPLEMENTATION CHECKLIST

1. Routing integration (`/kontrolling`, `/crm` in App.tsx)
2. Auth wrapper (RBAC permission checks)
3. Real entity selector (dropdown with API fetch)
4. Real-time SSE updates (5-minute auto-refetch)
5. E2E testing (Playwright visual regression)

---

**Last Updated:** 2026-07-07
**Status:** 🟢 OPERATIONAL
**Focus:** JoineryTech Portal UI (27 World modules)
**Memory Tier:** Warm (14-day, active development patterns)

---

_This memory is compressed from 4.5KB to ~1.8KB by consolidating 3 session narratives and focusing on reusable technical patterns. Preserved: Orval integration, dashboard widget pattern, dark-first design, key learnings._
