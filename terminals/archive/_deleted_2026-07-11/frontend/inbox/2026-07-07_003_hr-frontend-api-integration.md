---
id: MSG-FRONTEND-003
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-HR
checkpoint_id: CP-HR-FRONTEND
ref: CP-HR-BACKEND
created: 2026-07-07
completed: 2026-07-07
estimated_nwt: 15
---

# HR Frontend API Integration

**Epic:** EPIC-JT-HR
**Checkpoint:** CP-HR-FRONTEND
**Backend Checkpoint:** CP-HR-BACKEND ✅ DONE (MSG-BACKEND-169)
**Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion)
**Estimated:** 15 NWT (~30 min with pattern reuse, 67% acceleration validated)

---

## Context — Pattern Reuse Validated

CRM Frontend (MSG-FRONTEND-001) completed in **15 minutes** (vs 45 NWT estimate = **67% acceleration**).

**Key finding:** All integration code already existed. Only needed:
1. `.env` file with `VITE_USE_MOCK_API=false`
2. Backend API endpoints ready ✅

**Backend HR API Ready:**
- **MSG-BACKEND-169**: Week 4 API Layer (12 endpoints, 0 errors/warnings)
- **Endpoints:** Employee CRUD, Absence FSM (Submit→Approve→Cancel), Capacity calculation, Skill matrix, Work log
- **FSM Patterns:** Absence transitions (Pending→Approved→Cancelled), complex DTO mapping with nested owned entities
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml` (MSG-ARCHITECT-061, 25 endpoints)

---

## Task — Apply CRM Pattern to HR

**Expected outcome:** HR Dashboard with Employee registry, Capacity calendar, Absence FSM management, and Skill matrix.

### Pattern Reuse (from CRM)

**1. TanStack Query Hook Pattern**
Create `src/hooks/useHR.ts` with query and mutation hooks (following `useCRM.ts` structure):

```typescript
// Query Hooks
export const useEmployees = (filters?: EmployeeFilters) => {
  return useQuery({
    queryKey: ['hr', 'employees', filters],
    queryFn: () => hrApi.getEmployees(filters),
    staleTime: 30000
  });
};

export const useEmployeeById = (id: string) => {
  return useQuery({
    queryKey: ['hr', 'employee', id],
    queryFn: () => hrApi.getEmployeeById(id),
    enabled: !!id
  });
};

export const useAbsences = (filters?: AbsenceFilters) => {
  return useQuery({
    queryKey: ['hr', 'absences', filters],
    queryFn: () => hrApi.getAbsences(filters),
    staleTime: 30000
  });
};

export const useCapacityCalendar = (dateRange: { start: Date; end: Date }) => {
  return useQuery({
    queryKey: ['hr', 'capacity', dateRange],
    queryFn: () => hrApi.getCapacity(dateRange),
    staleTime: 60000 // 1 min
  });
};

export const useSkillMatrix = () => {
  return useQuery({
    queryKey: ['hr', 'skill-matrix'],
    queryFn: () => hrApi.getSkillMatrix(),
    staleTime: 300000 // 5 min (skill matrix changes infrequently)
  });
};

// Mutation Hooks
export const useCreateEmployee = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: hrApi.createEmployee,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['hr', 'employees'] });
    }
  });
};

export const useSubmitAbsence = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: hrApi.submitAbsence,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['hr', 'absences'] });
    }
  });
};

export const useApproveAbsence = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: hrApi.approveAbsence,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['hr', 'absences'] });
    }
  });
};

export const useRejectAbsence = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: hrApi.rejectAbsence,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['hr', 'absences'] });
    }
  });
};
```

**2. FSM Integration Pattern**
Apply CRM's FSM action pattern to Absence workflow:
- **Pending → Approved**: Approve button (manager only)
- **Pending → Rejected**: Reject button with reason
- **Approved → Cancelled**: Cancel button (with note)
- **Optimistic updates**: UI updates immediately, rollback on error

**3. Feature Flag Pattern**
Reuse `.env` file approach:
- `VITE_USE_MOCK_API=false` (already set from CRM)
- `hrApi.ts` checks feature flag to toggle mock/real API

**4. Dashboard Page Structure**
Create `src/pages/HRDashboardPage.tsx` (follow `CRMLeadsPage.tsx` structure):
- Route: `/hr/employees`
- Tabs: Employees | Absences | Capacity | Skills
- State management: React Query only (no local state)

**5. Activity Logging Pattern**
Log HR actions (employee created, absence approved) using `useActivityLog` hook.

---

## Acceptance Criteria

### 1. ✅ HR Dashboard Page Created
- **File:** `src/pages/HRDashboardPage.tsx`
- **Route:** `/hr/employees` (default), `/hr/absences`, `/hr/capacity`, `/hr/skills`
- **Layout:** Tabbed interface with 4 sections
- **Integration:** Real Backend API (not mock)

### 2. ✅ 4 HR Components Implemented

**EmployeeGrid** (`src/components/HR/EmployeeGrid.tsx`):
- Props: `{ filters?: EmployeeFilters }`
- API: `useEmployees(filters)`
- Display: Employee table with search/filter (Name, Position, Status)
- Actions: View details, Edit, Deactivate
- Pagination: 20 items per page

**AbsenceFSMPanel** (`src/components/HR/AbsenceFSMPanel.tsx`):
- Props: `{ absences: Absence[] }`
- API: `useAbsences(filters)`, `useApproveAbsence()`, `useRejectAbsence()`
- Display: Absence request list with FSM status badges
- FSM Actions: Approve/Reject buttons (manager only)
- Optimistic updates: Status changes immediately

**CapacityCalendar** (`src/components/HR/CapacityCalendar.tsx`):
- Props: `{ dateRange: { start: Date; end: Date } }`
- API: `useCapacityCalendar(dateRange)`
- Display: Weekly/monthly calendar showing available capacity per employee
- Visual: Heatmap or bar chart showing capacity utilization %
- Legend: Available (green), Absence (red), Overbooked (orange)

**SkillMatrix** (`src/components/HR/SkillMatrix.tsx`):
- Props: None
- API: `useSkillMatrix()`
- Display: Matrix view (Employees × Skills)
- Format: Employee names (rows), Skills (columns), proficiency levels (cells)
- Filter: By skill or by employee
- Export: CSV button (optional for MVP)

### 3. ✅ TanStack Query Hooks Created
- **File:** `src/hooks/useHR.ts` (follow `useCRM.ts` pattern)
- **Query hooks:** 5 (Employees, Employee by ID, Absences, Capacity, Skill Matrix)
- **Mutation hooks:** 4 (Create Employee, Submit Absence, Approve Absence, Reject Absence)
- **Pattern:** Query invalidation on mutation success

### 4. ✅ API Service Layer
- **File:** `src/services/hrApi.ts` (follow `crmApi.ts` pattern)
- **Feature flag:** Check `VITE_USE_MOCK_API` env var
- **Endpoints:** 12 total (from MSG-BACKEND-169)

### 5. ✅ Error Handling + Loading States
- Loading: Skeleton loaders (follow CRM pattern)
- Error: Error alert with retry button
- Empty: "No employees/absences" placeholder with "Add" CTA

### 6. ✅ FSM State Management
- Absence FSM transitions (Pending→Approved/Rejected, Approved→Cancelled)
- Optimistic updates with rollback on error
- UI disabled during mutation

### 7. ✅ Activity Logging
- Log employee creation, absence approval/rejection
- Reuse `useActivityLog` hook from CRM

### 8. ✅ Build Gates
```bash
npm run build  # 0 errors, 0 warnings
npm run typecheck  # PASS
npm run lint  # PASS (or warnings only, no errors)
```

---

## Files to Create/Modify

**New files (9 files):**
```
src/pages/HRDashboardPage.tsx                  (NEW)
src/pages/HRDashboardPage.module.css          (NEW, optional)
src/components/HR/EmployeeGrid.tsx            (NEW)
src/components/HR/AbsenceFSMPanel.tsx         (NEW)
src/components/HR/CapacityCalendar.tsx        (NEW)
src/components/HR/SkillMatrix.tsx             (NEW)
src/components/HR/index.ts                    (NEW, barrel export)
src/hooks/useHR.ts                            (NEW)
src/services/hrApi.ts                         (NEW)
```

**Modified files (1 file, if routes need update):**
```
src/main.tsx  (add routes for /hr/*)
```

---

## Backend API Endpoints

**Query Endpoints (7):**
1. `GET /api/hr/employees` — List employees with filters
2. `GET /api/hr/employees/{id}` — Get employee details
3. `GET /api/hr/absences` — List absences with filters
4. `GET /api/hr/absences/{id}` — Get absence details
5. `GET /api/hr/capacity` — Get capacity calculation
6. `GET /api/hr/skills` — List skills
7. `GET /api/hr/employees/{id}/skills` — Get employee skill matrix

**Command Endpoints (5):**
1. `POST /api/hr/employees` — Create employee
2. `PUT /api/hr/employees/{id}` — Update employee
3. `POST /api/hr/absences` — Submit absence request
4. `POST /api/hr/absences/{id}/approve` — Approve absence (FSM)
5. `POST /api/hr/absences/{id}/reject` — Reject absence (FSM)

**Full OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml` (25 endpoints, MSG-ARCHITECT-061)

---

## Expected Timeline

**Baseline (CRM):** 15 minutes (67% acceleration from 45 NWT estimate)

**HR estimate:** ~15-30 minutes (4 components, FSM integration similar to CRM)

---

## DONE Outbox Format

**File:** `terminals/frontend/outbox/2026-07-07_NNN_msg-frontend-003-hr-done.md`

**Frontmatter:**
```yaml
---
id: MSG-FRONTEND-003-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-003
epic_id: EPIC-JT-HR
checkpoint_id: CP-HR-FRONTEND
created: 2026-07-07
content_hash: <auto>
---
```

**Content sections:**
1. **Executive Summary** — What was completed in how much time
2. **Deliverables** — 4 components + hooks + API service
3. **Build Status** — 0 errors/warnings, TypeScript clean
4. **Pattern Reuse** — Which patterns from CRM/Kontrolling were applied
5. **FSM Integration** — Absence workflow transitions implemented
6. **Files Changed** — List of created/modified files
7. **Next Steps** — Maintenance Frontend (MSG-FRONTEND-004) ready for dispatch

---

## References

- **Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion report)
- **Backend Checkpoint:** CP-HR-BACKEND (MSG-BACKEND-169, 12 endpoints, 0E/0W)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml` (25 endpoints)
- **Epic:** EPIC-JT-HR
- **Design System:** Datahaven Bento Grid (ADR-048)
- **FSM Pattern:** Absence state machine (Pending→Approved→Cancelled)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
