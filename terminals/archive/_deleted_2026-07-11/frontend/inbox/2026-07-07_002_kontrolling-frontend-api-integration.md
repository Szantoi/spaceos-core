---
id: MSG-FRONTEND-002
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-CTRL
checkpoint_id: CP-CTRL-FRONTEND
ref: CP-CTRL-BACKEND
created: 2026-07-07
completed: 2026-07-07
estimated_nwt: 15
---

# Kontrolling Frontend API Integration

**Epic:** EPIC-JT-CTRL
**Checkpoint:** CP-CTRL-FRONTEND
**Backend Checkpoint:** CP-CTRL-BACKEND ✅ DONE (MSG-BACKEND-141)
**Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion)
**Estimated:** 15 NWT (~30 min with pattern reuse, 67% acceleration validated)

---

## Context — Pattern Reuse Validated

CRM Frontend (MSG-FRONTEND-001) completed in **15 minutes** (vs 45 NWT estimate = **67% acceleration**).

**Key finding:** All integration code already existed. Only needed:
1. `.env` file with `VITE_USE_MOCK_API=false`
2. Backend API endpoints ready ✅

**Backend Kontrolling API Ready:**
- **MSG-BACKEND-141**: Week 1 Domain Layer (57 unit tests ✅)
- **Endpoints:** EAC calculation, Cost breakdown, Variance analysis, Portfolio summary, Overhead config
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`

---

## Task — Apply CRM Pattern to Kontrolling

**Expected outcome:** Kontrolling Dashboard with 4 widgets (EAC, Cost Breakdown, Variance, Portfolio) using real Backend API.

### Pattern Reuse (from CRM)

**1. TanStack Query Hook Pattern**
Create `src/hooks/useKontrolling.ts` with query and mutation hooks (following `useCRM.ts` structure):

```typescript
// Query Hooks
export const useEacCalculation = (projectId: string) => {
  return useQuery({
    queryKey: ['kontrolling', 'eac', projectId],
    queryFn: () => kontrollingApi.getEacCalculation(projectId),
    staleTime: 30000, // 30 sec
    enabled: !!projectId
  });
};

export const useCostSummary = (projectId: string) => {
  return useQuery({
    queryKey: ['kontrolling', 'cost-summary', projectId],
    queryFn: () => kontrollingApi.getCostSummary(projectId),
    staleTime: 30000
  });
};

export const useVarianceAnalysis = (projectId: string) => {
  return useQuery({
    queryKey: ['kontrolling', 'variance', projectId],
    queryFn: () => kontrollingApi.getVarianceAnalysis(projectId),
    staleTime: 30000
  });
};

export const usePortfolioSummary = () => {
  return useQuery({
    queryKey: ['kontrolling', 'portfolio-summary'],
    queryFn: () => kontrollingApi.getPortfolioSummary(),
    staleTime: 60000 // 1 min for portfolio-level data
  });
};

// Mutation Hook (Overhead Config)
export const useSetOverheadConfig = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: kontrollingApi.setOverheadConfig,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['kontrolling', 'overhead-config'] });
    }
  });
};
```

**2. Feature Flag Pattern**
Reuse `.env` file approach:
- `VITE_USE_MOCK_API=false` (already set from CRM)
- `kontrollingApi.ts` checks feature flag to toggle mock/real API

**3. Error Handling Pattern**
Apply CRM's loading/error/empty state pattern to all Kontrolling widgets.

**4. Dashboard Page Structure**
Create `src/pages/KontrollingDashboardPage.tsx` (follow `CRMLeadsPage.tsx` structure):
- Route: `/kontrolling/projects/:projectId`
- Layout: 2×2 grid (4 widgets)
- State management: React Query only (no local state)

**5. Activity Logging Pattern**
Log Kontrolling actions (overhead config changes) using `useActivityLog` hook.

---

## Acceptance Criteria

### 1. ✅ Kontrolling Dashboard Page Created
- **File:** `src/pages/KontrollingDashboardPage.tsx`
- **Route:** `/kontrolling/projects/:projectId`
- **Layout:** 2×2 grid with 4 widgets
- **Integration:** Real Backend API (not mock)

### 2. ✅ 4 Kontrolling Widgets Implemented

**EACCalculationWidget** (`src/components/Kontrolling/EACCalculationWidget.tsx`):
- Props: `{ projectId: string }`
- API: `useEacCalculation(projectId)`
- Display: 6 cost categories (Material, Labor, Subcontracting, Logistics, Supplier, Overhead)
- Format: Planned | Actual | EAC per category
- Chart: Bar chart showing variance (optional for MVP)

**CostBreakdownChart** (`src/components/Kontrolling/CostBreakdownChart.tsx`):
- Props: `{ projectId: string }`
- API: `useCostSummary(projectId)`
- Display: Total costs, Revenue, Margins
- Chart: Stacked bar or pie chart

**VarianceAnalysisPanel** (`src/components/Kontrolling/VarianceAnalysisPanel.tsx`):
- Props: `{ projectId: string }`
- API: `useVarianceAnalysis(projectId)`
- Display: Worst performing category, variance %, top 3 outliers
- Alert: Red badge if variance > 15%

**PortfolioSummaryCard** (`src/components/Kontrolling/PortfolioSummaryCard.tsx`):
- Props: None (uses tenant from auth context)
- API: `usePortfolioSummary()`
- Display: Total projects, Total costs (Planned/Actual/EAC), Margins
- Refresh: 1 min cache, manual refresh button

### 3. ✅ TanStack Query Hooks Created
- **File:** `src/hooks/useKontrolling.ts` (follow `useCRM.ts` pattern)
- **Query hooks:** 4 (EAC, Cost Summary, Variance, Portfolio)
- **Mutation hooks:** 1 (Set Overhead Config)
- **Pattern:** Query invalidation on mutation success

### 4. ✅ API Service Layer
- **File:** `src/services/kontrollingApi.ts` (follow `crmApi.ts` pattern)
- **Feature flag:** Check `VITE_USE_MOCK_API` env var
- **Endpoints:** 8 total (4 query + 4 command endpoints from OpenAPI spec)

### 5. ✅ Error Handling + Loading States
- Loading: Skeleton loaders (follow CRM pattern)
- Error: Error alert with retry button
- Empty: "No data" placeholder with action CTA

### 6. ✅ Activity Logging
- Log overhead config changes
- Reuse `useActivityLog` hook from CRM

### 7. ✅ Build Gates
```bash
npm run build  # 0 errors, 0 warnings
npm run typecheck  # PASS
npm run lint  # PASS (or warnings only, no errors)
```

---

## Files to Create/Modify

**New files (8 files):**
```
src/pages/KontrollingDashboardPage.tsx               (NEW)
src/pages/KontrollingDashboardPage.module.css       (NEW, optional)
src/components/Kontrolling/EACCalculationWidget.tsx (NEW)
src/components/Kontrolling/CostBreakdownChart.tsx   (NEW)
src/components/Kontrolling/VarianceAnalysisPanel.tsx (NEW)
src/components/Kontrolling/PortfolioSummaryCard.tsx (NEW)
src/hooks/useKontrolling.ts                         (NEW)
src/services/kontrollingApi.ts                      (NEW)
```

**Modified files (1 file, if routes need update):**
```
src/main.tsx  (add route for /kontrolling/projects/:projectId)
```

---

## Backend API Endpoints

**Query Endpoints (4):**
1. `GET /api/kontrolling/projects/{projectId}/eac` — EAC calculation
2. `GET /api/kontrolling/projects/{projectId}/cost-summary` — Cost breakdown
3. `GET /api/kontrolling/projects/{projectId}/variance` — Variance analysis
4. `GET /api/kontrolling/portfolio/summary` — Portfolio summary

**Command Endpoints (4):**
1. `POST /api/kontrolling/overhead/config` — Set overhead config
2. `PUT /api/kontrolling/overhead/config/{id}` — Update overhead config
3. `DELETE /api/kontrolling/cost-adjustments/{id}` — Delete cost adjustment
4. `GET /api/kontrolling/overhead/config` — Get overhead config

**Full OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml` (1209 lines, MSG-BACKEND-141)

---

## Expected Timeline

**Baseline (CRM):** 15 minutes (67% acceleration from 45 NWT estimate)

**Kontrolling estimate:** ~15-30 minutes (similar pattern reuse, 4 widgets vs CRM's 3 components)

---

## DONE Outbox Format

**File:** `terminals/frontend/outbox/2026-07-07_NNN_msg-frontend-002-kontrolling-done.md`

**Frontmatter:**
```yaml
---
id: MSG-FRONTEND-002-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-002
epic_id: EPIC-JT-CTRL
checkpoint_id: CP-CTRL-FRONTEND
created: 2026-07-07
content_hash: <auto>
---
```

**Content sections:**
1. **Executive Summary** — What was completed in how much time
2. **Deliverables** — 4 widgets + hooks + API service
3. **Build Status** — 0 errors/warnings, TypeScript clean
4. **Pattern Reuse** — Which patterns from CRM were applied
5. **Files Changed** — List of created/modified files
6. **Next Steps** — HR Frontend (MSG-FRONTEND-003) ready for dispatch

---

## References

- **Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion report)
- **Backend Checkpoint:** CP-CTRL-BACKEND (MSG-BACKEND-141, 57 unit tests)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- **Epic:** EPIC-JT-CTRL
- **Design System:** Datahaven Bento Grid (ADR-048)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
