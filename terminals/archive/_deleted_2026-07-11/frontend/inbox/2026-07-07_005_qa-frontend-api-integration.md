---
id: MSG-FRONTEND-005
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-FRONTEND
ref: CP-QA-BACKEND
created: 2026-07-07
estimated_nwt: 15
completed: 2026-07-07
content_hash: 15c9cc3dbfed8e4b6d1ef29d042cbf63e6de184b7b3a101a2d19e9e38e77d983
---

# QA Frontend API Integration

**Epic:** EPIC-JT-QA
**Checkpoint:** CP-QA-FRONTEND
**Backend Checkpoint:** CP-QA-BACKEND ✅ DONE (MSG-BACKEND-171)
**Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion)
**Estimated:** 15 NWT (~30 min with pattern reuse, 67% acceleration validated)

---

## Context — Pattern Reuse Validated

CRM Frontend (MSG-FRONTEND-001) completed in **15 minutes** (vs 45 NWT estimate = **67% acceleration**).

**Key finding:** All integration code already existed. Only needed:
1. `.env` file with `VITE_USE_MOCK_API=false`
2. Backend API endpoints ready ✅

**Backend QA API Ready:**
- **MSG-BACKEND-171**: Week 4 API Layer (14 endpoints, 0 errors/warnings)
- **Endpoints:** QA Checkpoint CRUD, Inspection FSM (Draft→InProgress→Pass/Fail), Ticket FSM (Reported→InProgress→Resolved/Closed), Production blocking pattern
- **FSM Patterns:** Inspection transitions (Draft→InProgress→Pass→Fail), Ticket state machine, FSM result enum handling (Pass/Fail)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-qa-v1.yaml` (MSG-ARCHITECT-065)

---

## Task — Apply CRM Pattern to QA

**Expected outcome:** QA Dashboard with Inspection forms, Ticket FSM management, QA Checkpoint tracking, and Production blocking indicator.

### Pattern Reuse (from CRM)

**1. TanStack Query Hook Pattern**
Create `src/hooks/useQA.ts` with query and mutation hooks (following `useCRM.ts` structure):

```typescript
// Query Hooks
export const useQACheckpoints = (filters?: QACheckpointFilters) => {
  return useQuery({
    queryKey: ['qa', 'checkpoints', filters],
    queryFn: () => qaApi.getCheckpoints(filters),
    staleTime: 30000
  });
};

export const useInspections = (filters?: InspectionFilters) => {
  return useQuery({
    queryKey: ['qa', 'inspections', filters],
    queryFn: () => qaApi.getInspections(filters),
    staleTime: 30000
  });
};

export const useInspectionById = (id: string) => {
  return useQuery({
    queryKey: ['qa', 'inspection', id],
    queryFn: () => qaApi.getInspectionById(id),
    enabled: !!id
  });
};

export const useTickets = (filters?: TicketFilters) => {
  return useQuery({
    queryKey: ['qa', 'tickets', filters],
    queryFn: () => qaApi.getTickets(filters),
    staleTime: 30000
  });
};

export const useBlockingInspections = (orderId: string) => {
  return useQuery({
    queryKey: ['qa', 'blocking', orderId],
    queryFn: () => qaApi.getBlockingInspections(orderId),
    enabled: !!orderId,
    staleTime: 10000 // 10 sec (production-critical)
  });
};

// Mutation Hooks
export const useCreateInspection = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: qaApi.createInspection,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['qa', 'inspections'] });
    }
  });
};

export const useStartInspection = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: qaApi.startInspection,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['qa', 'inspections'] });
    }
  });
};

export const useCompleteInspection = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: qaApi.completeInspection,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['qa', 'inspections'] });
      queryClient.invalidateQueries({ queryKey: ['qa', 'blocking'] }); // Update production block status
    }
  });
};

export const useReportTicket = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: qaApi.reportTicket,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['qa', 'tickets'] });
    }
  });
};

export const useResolveTicket = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: qaApi.resolveTicket,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['qa', 'tickets'] });
    }
  });
};
```

**2. FSM Integration Pattern**
Apply CRM's FSM action pattern to Inspection & Ticket workflows:
- **Inspection FSM:** Draft→InProgress→Pass/Fail
- **Ticket FSM:** Reported→InProgress→Resolved→Closed
- **Optimistic updates:** UI updates immediately, rollback on error
- **Production blocking:** Red alert badge if hasBlockingInspections=true

**3. Feature Flag Pattern**
Reuse `.env` file approach:
- `VITE_USE_MOCK_API=false` (already set from CRM)
- `qaApi.ts` checks feature flag to toggle mock/real API

**4. Dashboard Page Structure**
Create `src/pages/QADashboardPage.tsx` (follow `CRMLeadsPage.tsx` structure):
- Route: `/qa/inspections`
- Tabs: Inspections | Tickets | Checkpoints
- State management: React Query only (no local state)

**5. Activity Logging Pattern**
Log QA actions (inspection completed, ticket resolved) using `useActivityLog` hook.

---

## Acceptance Criteria

### 1. ✅ QA Dashboard Page Created
- **File:** `src/pages/QADashboardPage.tsx`
- **Route:** `/qa/inspections` (default), `/qa/tickets`, `/qa/checkpoints`
- **Layout:** Tabbed interface with 3 sections
- **Integration:** Real Backend API (not mock)

### 2. ✅ 4 QA Components Implemented

**InspectionFSMPanel** (`src/components/QA/InspectionFSMPanel.tsx`):
- Props: `{ inspections: Inspection[] }`
- API: `useInspections(filters)`, `useStartInspection()`, `useCompleteInspection()`
- Display: Inspection list with FSM status badges
- FSM Actions: Start/Complete buttons with Pass/Fail result selection
- Optimistic updates: Status changes immediately
- Result handling: Pass=green, Fail=red badge

**TicketFSMPanel** (`src/components/QA/TicketFSMPanel.tsx`):
- Props: `{ tickets: Ticket[] }`
- API: `useTickets(filters)`, `useResolveTicket()`, `useCloseTicket()`
- Display: Ticket list with FSM status badges
- FSM Actions: Resolve/Close buttons based on status
- Type badges: Warranty (blue), Defect (red), Maintenance (orange)
- Optimistic updates: Status changes immediately

**QACheckpointGrid** (`src/components/QA/QACheckpointGrid.tsx`):
- Props: `{ filters?: QACheckpointFilters }`
- API: `useQACheckpoints(filters)`
- Display: Checkpoint table with search/filter (Name, Type, Frequency)
- Actions: View linked inspections, Create inspection from checkpoint
- Pagination: 20 items per page

**ProductionBlockingAlert** (`src/components/QA/ProductionBlockingAlert.tsx`):
- Props: `{ orderId: string }`
- API: `useBlockingInspections(orderId)`
- Display: Alert banner if hasBlockingInspections=true
- Visual: Red alert with "Production Blocked" message
- CTA: "View Blocking Inspections" button → navigate to inspections filtered by order
- Auto-refresh: 10 sec polling

### 3. ✅ TanStack Query Hooks Created
- **File:** `src/hooks/useQA.ts` (follow `useCRM.ts` pattern)
- **Query hooks:** 5 (Checkpoints, Inspections, Inspection by ID, Tickets, Blocking Inspections)
- **Mutation hooks:** 5 (Create Inspection, Start Inspection, Complete Inspection, Report Ticket, Resolve Ticket)
- **Pattern:** Query invalidation on mutation success

### 4. ✅ API Service Layer
- **File:** `src/services/qaApi.ts` (follow `crmApi.ts` pattern)
- **Feature flag:** Check `VITE_USE_MOCK_API` env var
- **Endpoints:** 14 total (from MSG-BACKEND-171)

### 5. ✅ Error Handling + Loading States
- Loading: Skeleton loaders (follow CRM pattern)
- Error: Error alert with retry button
- Empty: "No inspections/tickets" placeholder with "Add" CTA

### 6. ✅ FSM State Management
- Inspection FSM transitions (Draft→InProgress→Pass/Fail)
- Ticket FSM transitions (Reported→InProgress→Resolved→Closed)
- Optimistic updates with rollback on error
- UI disabled during mutation

### 7. ✅ Production Blocking Pattern
- `useBlockingInspections(orderId)` hook
- Red alert banner component
- Auto-refresh every 10 seconds
- Query invalidation on inspection completion

### 8. ✅ Activity Logging
- Log inspection completion, ticket resolution
- Reuse `useActivityLog` hook from CRM

### 9. ✅ Build Gates
```bash
npm run build  # 0 errors, 0 warnings
npm run typecheck  # PASS
npm run lint  # PASS (or warnings only, no errors)
```

---

## Files to Create/Modify

**New files (10 files):**
```
src/pages/QADashboardPage.tsx                     (NEW)
src/pages/QADashboardPage.module.css             (NEW, optional)
src/components/QA/InspectionFSMPanel.tsx         (NEW)
src/components/QA/TicketFSMPanel.tsx             (NEW)
src/components/QA/QACheckpointGrid.tsx           (NEW)
src/components/QA/ProductionBlockingAlert.tsx    (NEW)
src/components/QA/index.ts                       (NEW, barrel export)
src/hooks/useQA.ts                               (NEW)
src/services/qaApi.ts                            (NEW)
```

**Modified files (1 file, if routes need update):**
```
src/main.tsx  (add routes for /qa/*)
```

---

## Backend API Endpoints

**QA Checkpoint Endpoints (3):**
1. `GET /api/qa/checkpoints` — List checkpoints with filters
2. `GET /api/qa/checkpoints/{id}` — Get checkpoint details
3. `POST /api/qa/checkpoints` — Create checkpoint

**Inspection Endpoints (6):**
1. `GET /api/qa/inspections` — List inspections with filters
2. `GET /api/qa/inspections/{id}` — Get inspection details
3. `POST /api/qa/inspections` — Create inspection
4. `POST /api/qa/inspections/{id}/start` — FSM: Draft→InProgress
5. `POST /api/qa/inspections/{id}/complete` — FSM: InProgress→Pass/Fail
6. `GET /api/qa/orders/{orderId}/blocking-inspections` — Production blocking check

**Ticket Endpoints (5):**
1. `GET /api/qa/tickets` — List tickets with filters
2. `GET /api/qa/tickets/{id}` — Get ticket details
3. `POST /api/qa/tickets` — Report ticket
4. `POST /api/qa/tickets/{id}/resolve` — FSM: Reported→Resolved
5. `POST /api/qa/tickets/{id}/close` — FSM: Resolved→Closed

**Full OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-qa-v1.yaml` (MSG-ARCHITECT-065)

---

## Expected Timeline

**Baseline (CRM):** 15 minutes (67% acceleration from 45 NWT estimate)

**QA estimate:** ~15-30 minutes (4 components, 2 FSM patterns similar to CRM/HR/Maintenance)

---

## DONE Outbox Format

**File:** `terminals/frontend/outbox/2026-07-07_NNN_msg-frontend-005-qa-done.md`

**Frontmatter:**
```yaml
---
id: MSG-FRONTEND-005-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-005
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-FRONTEND
created: 2026-07-07
content_hash: <auto>
---
```

**Content sections:**
1. **Executive Summary** — What was completed in how much time
2. **Deliverables** — 4 components + hooks + API service
3. **Build Status** — 0 errors/warnings, TypeScript clean
4. **Pattern Reuse** — Which patterns from CRM/HR/Maintenance were applied
5. **FSM Integration** — Inspection & Ticket workflow transitions implemented
6. **Production Blocking** — Alert component with auto-refresh
7. **Files Changed** — List of created/modified files
8. **Next Steps** — DMS Frontend (MSG-FRONTEND-006) ready for dispatch

---

## References

- **Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion report)
- **Backend Checkpoint:** CP-QA-BACKEND (MSG-BACKEND-171, 14 endpoints, 0E/0W)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-qa-v1.yaml`
- **Epic:** EPIC-JT-QA
- **Design System:** Datahaven Bento Grid (ADR-048)
- **FSM Patterns:** Inspection (Draft→InProgress→Pass/Fail), Ticket (Reported→InProgress→Resolved→Closed)
- **Production Integration:** GetBlockingInspections endpoint for order validation

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
