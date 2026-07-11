---
id: MSG-FRONTEND-099
from: conductor
to: frontend
type: task
priority: critical
status: SUPERSEDED
superseded_by: MSG-FRONTEND-001
superseded_reason: CRM frontend API integration completed in MSG-001 (2026-07-07). Wave 2 features (Activity Log, drag-drop, advanced filters) implemented as part of CP-CRM-FRONTEND checkpoint completion.
injected: 2026-07-03
model: sonnet
epic_id: EPIC-JT-CRM
checkpoint: CP-CRM-FRONTEND
ref: MSG-ROOT-003
created: 2026-07-03
completed: 2026-07-07
content_hash: 48c2eda0b331e413f03bbf8178cca1250d30b8e4d00998cac08c9f9590c710f4
---

# EPIC-JT-CRM: Frontend Wave 2 — Pipeline Enhancement & Mock API Integration

**Epic:** EPIC-JT-CRM (activated 2026-07-03)
**Checkpoint:** CP-CRM-FRONTEND (Pipeline kanban + forecast + activity log)
**Priority:** CRITICAL
**Timeline:** 4-6 days (parallel with Backend NuGet unblock)

---

## Context

**Root Decision (MSG-ROOT-003):**
- ✅ EPIC-JT-CRM activated with **parallel track approach**
- ✅ Frontend can proceed with **mock API integration** (doesn't wait for Backend)
- ✅ Backend will catch up once NuGet fixed (24-48h)

**Wave 1 Status (MSG-FRONTEND-088):**
- ✅ LeadGrid component (260 lines)
- ✅ OpportunityPipeline kanban (185 lines)
- ✅ Forecast KPI cards (Pipeline, Weighted, Won)
- ✅ useCRM hooks (8 query + 9 mutation, 400+ lines)
- ✅ LeadForm, OpportunityForm components
- ✅ CRMLeadsPage, CRMOpportunitiesPage

**Wave 2 Focus:** Complete CP-CRM-FRONTEND checkpoint + prepare for backend integration.

---

## Acceptance Criteria (CP-CRM-FRONTEND)

### 1. Activity Log Component ✨ NEW
- [ ] **ActivityLog.tsx** component (timeline view)
  - Display lead/opportunity activity history
  - Activity types: StatusChange, EmailSent, PhoneCalled, NoteAdded, ConvertedToOpportunity
  - Timeline format: timestamp, user, action, details
  - Dark-first design (ADR-048)
  - Responsive layout

- [ ] **useActivityLog** hook (TanStack Query)
  - `useActivityLog(entityType, entityId)` — fetch activities
  - Mock data structure ready for backend API
  - Cache invalidation on mutations

- [ ] **Integration:**
  - Add ActivityLog to CRMLeadsPage (right panel or modal)
  - Add ActivityLog to CRMOpportunitiesPage (expandable section)
  - Test with mock data (5-10 activities per entity)

**Acceptance:** Activity log visible on lead/opportunity detail view, mock data working.

---

### 2. Mock API Integration — Finalize 🔧

**Current:** useCRM hooks have TanStack Query setup, but no mock API layer.

**Task:**
- [ ] Create **Mock API Service** (`src/services/mockCrmApi.ts`)
  - `mockLeads` dataset (20-30 leads with realistic data)
  - `mockOpportunities` dataset (15-20 opportunities)
  - `mockActivities` dataset (50+ activities)
  - CRUD operations with localStorage persistence (optional)
  - Simulated latency (300-500ms) + random errors (5% failure rate)

- [ ] Update **useCRM hooks** to use mock API
  - Replace placeholder returns with `fetch('/api/crm/leads')` → mock service
  - Test all 8 query hooks (useLeads, useOpportunities, useForecast, etc.)
  - Test all 9 mutation hooks (create, contact, qualify, convert, etc.)
  - Verify cache invalidation works

- [ ] **Backend API Readiness:**
  - Document expected API contract (OpenAPI spec or TypeScript types)
  - Create `src/services/crmApi.ts` stub (for real backend later)
  - Feature flag: `USE_MOCK_API` (env var, default: true)

**Acceptance:** All CRM pages work with mock API, data persists, mutations trigger re-fetches.

---

### 3. Drag & Drop Integration — dnd-kit 🎯

**Current:** OpportunityPipeline has drag handlers stubbed (Wave 1).

**Task:**
- [ ] Install **@dnd-kit/core** + **@dnd-kit/sortable**
  ```bash
  npm install @dnd-kit/core @dnd-kit/sortable
  ```

- [ ] Implement **Drag & Drop in OpportunityPipeline:**
  - DndContext wrapping kanban board
  - Droppable zones for each stage (Draft, Proposal, Negotiation, Won, Lost, Abandoned)
  - Draggable opportunity cards
  - `onDragEnd` handler → update opportunity stage (via `useUpdateOpportunity` mutation)
  - Optimistic UI update (card moves before API confirmation)
  - Rollback on error (if mutation fails)

- [ ] **Touch Support:**
  - Test on mobile (touch gestures)
  - Pointer sensors config for desktop + mobile

- [ ] **Tests:**
  - Test drag & drop with @testing-library/react
  - Mock dnd-kit events
  - Verify stage transitions (Draft → Proposal, Proposal → Won, etc.)

**Acceptance:** Drag opportunity card between stages, stage updates, UI reflects change immediately.

---

### 4. Filters Enhancement — Advanced Search 🔍

**Current:** LeadGrid has basic filters (status, assignedTo, source). OpportunityPipeline has no filters.

**Task:**
- [ ] **LeadGrid Advanced Filters:**
  - Date range filter: `createdAt` (from/to date pickers)
  - Search filter: company name, contact name, email (debounced input)
  - Multi-select: status (allow multiple statuses)
  - Clear all filters button

- [ ] **OpportunityPipeline Filters:**
  - `assignedTo` dropdown (user list)
  - `expectedCloseDate` range (from/to)
  - `minValue` / `maxValue` sliders (pipeline value)
  - Stage visibility toggles (show/hide specific stages)

- [ ] **URL State Management:**
  - Filters sync to URL query params (e.g., `?status=New,Contacted&assignedTo=john`)
  - Browser back/forward works
  - Shareable filtered views

**Acceptance:** Advanced filters work, URL updates, page reloads preserve filters.

---

### 5. Real-time Updates — SSE Preparation 📡

**Current:** Real-time deferred to Wave 2 (MSG-FRONTEND-088).

**Task:**
- [ ] **SSE Client Setup:**
  - Create `src/services/sseClient.ts`
  - EventSource connection to `/api/crm/events` (mock for now)
  - Event types: `lead.updated`, `opportunity.updated`, `activity.created`
  - Reconnect logic (exponential backoff)

- [ ] **TanStack Query Integration:**
  - SSE event listeners → invalidate queries
  - Example: `lead.updated` event → `queryClient.invalidateQueries(['leads'])`
  - Optimistic updates vs server events (conflict resolution)

- [ ] **Mock SSE Server:**
  - Mock EventSource with random events (every 10-30s)
  - Simulate lead status changes, new opportunities, activities
  - Feature flag: `ENABLE_SSE` (default: false for now)

- [ ] **UI Indicators:**
  - "Live" badge when SSE connected
  - Notification toast for real-time updates (optional)

**Acceptance:** SSE connection works, events trigger UI updates, queries refetch automatically.

---

### 6. Form Validation — Enhanced Rules ✅

**Current:** LeadForm, OpportunityForm exist but validation is basic.

**Task:**
- [ ] **Install Zod** (TypeScript-first validation)
  ```bash
  npm install zod react-hook-form @hookform/resolvers
  ```

- [ ] **LeadForm Validation:**
  - Company: required, min 2 chars
  - ContactName: required, min 2 chars
  - Email: required, valid email format
  - Phone: optional, valid phone format (regex)
  - Source: required (dropdown)
  - AssignedTo: required (dropdown)

- [ ] **OpportunityForm Validation:**
  - Title: required, min 3 chars
  - Value: required, positive number, max 2 decimal places
  - Probability: required, 0-100 range
  - ExpectedCloseDate: required, future date only
  - Stage: required (dropdown)
  - Description: optional, max 500 chars

- [ ] **Error Messages:**
  - Field-level errors (below input)
  - Form-level errors (top of form)
  - Hungarian error messages (user-facing)

- [ ] **Submit Behavior:**
  - Disable submit button while validating
  - Loading spinner during mutation
  - Success toast on save
  - Error toast on failure

**Acceptance:** Forms validate correctly, errors shown, submit only when valid.

---

## Technical Requirements

**Technology Stack:**
- ✅ React 18 + TypeScript (existing)
- ✅ TanStack Query v5 (existing)
- ✅ CSS Modules (existing)
- 🆕 @dnd-kit/core + @dnd-kit/sortable
- 🆕 Zod + react-hook-form
- 🆕 EventSource (SSE)

**Design:**
- ✅ Dark-first (ADR-048)
- ✅ Mobile responsive (100% mobile, 90% tablet)
- ✅ Bento grid layout (if applicable)

**Testing:**
- ✅ Vitest + @testing-library/react (existing)
- 🆕 Test drag & drop (dnd-kit mocks)
- 🆕 Test form validation (Zod schemas)
- 🆕 Test SSE events (EventSource mocks)

---

## Implementation Plan

### Day 1-2: Activity Log + Mock API
1. ActivityLog component (timeline design)
2. useActivityLog hook (TanStack Query)
3. Mock API service (mockLeads, mockOpportunities, mockActivities)
4. Update useCRM hooks to use mock API
5. Test all CRM pages with mock data

### Day 2-3: Drag & Drop + Filters
1. Install @dnd-kit
2. Implement drag & drop in OpportunityPipeline
3. Test touch support (mobile)
4. Advanced filters for LeadGrid
5. Filters for OpportunityPipeline
6. URL state sync

### Day 3-4: SSE + Form Validation
1. SSE client setup (EventSource)
2. TanStack Query SSE integration
3. Mock SSE server (random events)
4. Install Zod + react-hook-form
5. LeadForm validation
6. OpportunityForm validation
7. Error handling + toast notifications

### Day 4: Testing + Polish
1. Test all components (Wave 2 features)
2. Test drag & drop
3. Test form validation
4. Test SSE events
5. Build verification (0 TypeScript errors)
6. Accessibility audit (ARIA labels, keyboard nav)

---

## Success Metrics

**Checkpoint Completion (CP-CRM-FRONTEND):**
- [ ] Activity log visible on all CRM pages
- [ ] Mock API integration complete (all CRUD works)
- [ ] Drag & drop working (desktop + mobile)
- [ ] Advanced filters functional (URL sync)
- [ ] SSE prepared (mock events working)
- [ ] Form validation robust (Zod schemas)
- [ ] Build: 0 TypeScript errors
- [ ] Test coverage: 70%+ (new components)

**Backend Readiness:**
- [ ] API contract documented (types or OpenAPI spec)
- [ ] Feature flag: `USE_MOCK_API` → easy switch to real backend
- [ ] All mutations ready for backend API swap

---

## Backend Integration Plan (Post-NuGet Fix)

**When Backend Week 2 Complete (JWT/OAuth):**
1. Replace mock API with real backend endpoints
2. Switch `USE_MOCK_API = false`
3. Test authentication flow (login, token refresh)
4. Test RLS (multi-tenant data isolation)

**When Backend Week 3 Complete (Catalog):**
1. No CRM dependency (catalog is separate)

**When Backend Week 4 Complete (CRM Domain):**
1. Test Lead FSM transitions (New → Contacted → Qualified → Converted)
2. Test Opportunity FSM transitions (Draft → Proposal → Negotiation → Won)
3. Test activity log (real backend events)
4. Enable SSE (real backend EventSource)
5. E2E tests (Playwright or Cypress)

---

## Risk Mitigation

**Risk:** Backend delayed beyond 48h (NuGet fix takes longer)
**Mitigation:** Mock API allows full frontend development without backend dependency.

**Risk:** Drag & drop complexity (dnd-kit learning curve)
**Mitigation:** Start with simple kanban, iterate. Reference existing examples (dnd-kit docs).

**Risk:** SSE browser compatibility (EventSource not in all browsers)
**Mitigation:** Polyfill for older browsers, feature flag to disable.

---

## Files to Create/Modify

### New Files (Wave 2)
```
src/components/features/ActivityLog/
  ├── ActivityLog.tsx
  ├── ActivityLog.module.css
  ├── ActivityLog.test.tsx
  └── index.ts

src/services/
  ├── mockCrmApi.ts
  ├── crmApi.ts (stub for real backend)
  └── sseClient.ts

src/hooks/
  └── useActivityLog.ts
```

### Modified Files
```
src/hooks/useCRM.ts (update to use mock API)
src/components/features/OpportunityPipeline/OpportunityPipeline.tsx (drag & drop)
src/components/features/LeadGrid/LeadGrid.tsx (advanced filters)
src/components/features/LeadForm/LeadForm.tsx (Zod validation)
src/components/features/OpportunityForm/OpportunityForm.tsx (Zod validation)
src/pages/CRMLeadsPage.tsx (activity log integration)
src/pages/CRMOpportunitiesPage.tsx (activity log integration)
package.json (new dependencies: @dnd-kit, zod, react-hook-form)
```

---

## Coordination Notes

**Parallel Work:**
- Backend working on NuGet fix (VPS operator, 24-48h)
- Frontend proceeds with Wave 2 (mock API, no blocker)
- Architect available for consultation (domain model, API contract)

**Epic Timeline:**
- EPIC-JT-CRM target: 2026-08-31
- Frontend Wave 2: 4-6 days (2026-07-03 → 2026-07-09)
- Backend catch-up: After NuGet fix (2026-07-05+)

**Next Checkpoint:**
- CP-CRM-BACKEND (Backend API Ready) — after Backend Week 2-4
- CP-CRM-INTEGRATION (Sales Integration) — after both checkpoints done

---

**Prioritás:** CRITICAL — EPIC-JT-CRM production suite aktiválva, Frontend párhuzamos track indulhat AZONNAL.

**Model:** sonnet (multi-component implementation, drag & drop, validation)

**Good luck!** 🚀
