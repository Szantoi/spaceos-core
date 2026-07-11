---
id: MSG-FRONTEND-004
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-MAINT
checkpoint_id: CP-MAINT-FRONTEND
ref: CP-MAINT-BACKEND
created: 2026-07-07
estimated_nwt: 15
completed: 2026-07-07
---

# Maintenance Frontend API Integration

**Epic:** EPIC-JT-MAINT
**Checkpoint:** CP-MAINT-FRONTEND
**Backend Checkpoint:** CP-MAINT-BACKEND ✅ DONE (MSG-BACKEND-170)
**Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion)
**Estimated:** 15 NWT (~30 min with pattern reuse, 67% acceleration validated)

---

## Context — Pattern Reuse Validated

CRM Frontend (MSG-FRONTEND-001) completed in **15 minutes** (vs 45 NWT estimate = **67% acceleration**).

**Key finding:** All integration code already existed. Only needed:
1. `.env` file with `VITE_USE_MOCK_API=false`
2. Backend API endpoints ready ✅

**Backend Maintenance API Ready:**
- **MSG-BACKEND-170**: Week 4 API Layer (12 endpoints, 5 Asset + 7 WorkOrder, 0 errors/warnings)
- **Endpoints:** Asset CRUD + Retire, WorkOrder FSM (Planned→InProgress→Completed), MaintenancePlan management, WorkOrderPart addition
- **FSM Patterns:** WorkOrder transitions (Planned→InProgress→Completed), owned collection management
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml` (MSG-ARCHITECT-062, 31 endpoints)

---

## Task — Apply CRM Pattern to Maintenance

**Expected outcome:** Maintenance Dashboard with Asset registry, WorkOrder FSM management, MaintenancePlan schedule view, and downtime tracking.

### Pattern Reuse (from CRM)

**1. TanStack Query Hook Pattern**
Create `src/hooks/useMaintenance.ts` with query and mutation hooks (following `useCRM.ts` structure):

```typescript
// Query Hooks
export const useAssets = (filters?: AssetFilters) => {
  return useQuery({
    queryKey: ['maintenance', 'assets', filters],
    queryFn: () => maintenanceApi.getAssets(filters),
    staleTime: 30000
  });
};

export const useAssetById = (id: string) => {
  return useQuery({
    queryKey: ['maintenance', 'asset', id],
    queryFn: () => maintenanceApi.getAssetById(id),
    enabled: !!id
  });
};

export const useWorkOrders = (filters?: WorkOrderFilters) => {
  return useQuery({
    queryKey: ['maintenance', 'work-orders', filters],
    queryFn: () => maintenanceApi.getWorkOrders(filters),
    staleTime: 30000
  });
};

export const useWorkOrdersByAsset = (assetId: string) => {
  return useQuery({
    queryKey: ['maintenance', 'work-orders', 'asset', assetId],
    queryFn: () => maintenanceApi.getWorkOrdersByAsset(assetId),
    enabled: !!assetId
  });
};

export const useMaintenanceSchedule = (dateRange: { start: Date; end: Date }) => {
  return useQuery({
    queryKey: ['maintenance', 'schedule', dateRange],
    queryFn: () => maintenanceApi.getSchedule(dateRange),
    staleTime: 60000 // 1 min
  });
};

// Mutation Hooks
export const useCreateAsset = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: maintenanceApi.createAsset,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance', 'assets'] });
    }
  });
};

export const useRetireAsset = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: maintenanceApi.retireAsset,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance', 'assets'] });
    }
  });
};

export const useCreateWorkOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: maintenanceApi.createWorkOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance', 'work-orders'] });
    }
  });
};

export const useStartWorkOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: maintenanceApi.startWorkOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance', 'work-orders'] });
    }
  });
};

export const useCompleteWorkOrder = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: maintenanceApi.completeWorkOrder,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['maintenance', 'work-orders'] });
    }
  });
};
```

**2. FSM Integration Pattern**
Apply CRM's FSM action pattern to WorkOrder workflow:
- **Planned → InProgress**: Start button (technician can start work)
- **InProgress → Completed**: Complete button with actual hours + completion note
- **Optimistic updates**: UI updates immediately, rollback on error
- **RequiresDowntime flag**: Visual indicator if asset needs to be offline

**3. Feature Flag Pattern**
Reuse `.env` file approach:
- `VITE_USE_MOCK_API=false` (already set from CRM)
- `maintenanceApi.ts` checks feature flag to toggle mock/real API

**4. Dashboard Page Structure**
Create `src/pages/MaintenanceDashboardPage.tsx` (follow `CRMLeadsPage.tsx` structure):
- Route: `/maintenance/assets`
- Tabs: Assets | Work Orders | Schedule
- State management: React Query only (no local state)

**5. Activity Logging Pattern**
Log Maintenance actions (asset created, work order started/completed) using `useActivityLog` hook.

---

## Acceptance Criteria

### 1. ✅ Maintenance Dashboard Page Created
- **File:** `src/pages/MaintenanceDashboardPage.tsx`
- **Route:** `/maintenance/assets` (default), `/maintenance/work-orders`, `/maintenance/schedule`
- **Layout:** Tabbed interface with 3 sections
- **Integration:** Real Backend API (not mock)

### 2. ✅ 4 Maintenance Components Implemented

**AssetRegistry** (`src/components/Maintenance/AssetRegistry.tsx`):
- Props: `{ filters?: AssetFilters }`
- API: `useAssets(filters)`
- Display: Asset table with search/filter (Code, Name, Location, Kind, Status)
- Actions: View details, Edit, Add MaintenancePlan, Retire
- Pagination: 20 items per page
- Visual: Status badges (Active=green, Retired=gray)

**WorkOrderFSMPanel** (`src/components/Maintenance/WorkOrderFSMPanel.tsx`):
- Props: `{ workOrders: WorkOrder[] }`
- API: `useWorkOrders(filters)`, `useStartWorkOrder()`, `useCompleteWorkOrder()`
- Display: Work order list with FSM status badges
- FSM Actions: Start/Complete buttons based on status
- Optimistic updates: Status changes immediately
- RequiresDowntime: Red badge if asset must be offline

**MaintenanceScheduleView** (`src/components/Maintenance/MaintenanceScheduleView.tsx`):
- Props: `{ dateRange: { start: Date; end: Date } }`
- API: `useMaintenanceSchedule(dateRange)`
- Display: Weekly/monthly schedule showing upcoming maintenance tasks
- Visual: Calendar or Gantt chart showing scheduled maintenance
- Legend: Preventive (blue), Corrective (red), Predictive (orange)
- Filter: By asset, by type, by priority

**AssetDetailPanel** (`src/components/Maintenance/AssetDetailPanel.tsx`):
- Props: `{ assetId: string }`
- API: `useAssetById(assetId)`, `useWorkOrdersByAsset(assetId)`
- Display: Asset details + MaintenancePlans + linked WorkOrders
- Format: Asset info (top), MaintenancePlans (middle), WorkOrder history (bottom)
- Actions: Add MaintenancePlan, Create WorkOrder, Retire Asset

### 3. ✅ TanStack Query Hooks Created
- **File:** `src/hooks/useMaintenance.ts` (follow `useCRM.ts` pattern)
- **Query hooks:** 5 (Assets, Asset by ID, WorkOrders, WorkOrders by Asset, Schedule)
- **Mutation hooks:** 5 (Create Asset, Retire Asset, Create WorkOrder, Start WorkOrder, Complete WorkOrder)
- **Pattern:** Query invalidation on mutation success

### 4. ✅ API Service Layer
- **File:** `src/services/maintenanceApi.ts` (follow `crmApi.ts` pattern)
- **Feature flag:** Check `VITE_USE_MOCK_API` env var
- **Endpoints:** 12 total (5 Asset + 7 WorkOrder from MSG-BACKEND-170)

### 5. ✅ Error Handling + Loading States
- Loading: Skeleton loaders (follow CRM pattern)
- Error: Error alert with retry button
- Empty: "No assets/work orders" placeholder with "Add" CTA

### 6. ✅ FSM State Management
- WorkOrder FSM transitions (Planned→InProgress→Completed)
- Optimistic updates with rollback on error
- UI disabled during mutation
- RequiresDowntime visual indicator

### 7. ✅ Activity Logging
- Log asset creation, work order started/completed
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
src/pages/MaintenanceDashboardPage.tsx               (NEW)
src/pages/MaintenanceDashboardPage.module.css       (NEW, optional)
src/components/Maintenance/AssetRegistry.tsx        (NEW)
src/components/Maintenance/WorkOrderFSMPanel.tsx    (NEW)
src/components/Maintenance/MaintenanceScheduleView.tsx (NEW)
src/components/Maintenance/AssetDetailPanel.tsx     (NEW)
src/components/Maintenance/index.ts                 (NEW, barrel export)
src/hooks/useMaintenance.ts                         (NEW)
src/services/maintenanceApi.ts                      (NEW)
```

**Modified files (1 file, if routes need update):**
```
src/main.tsx  (add routes for /maintenance/*)
```

---

## Backend API Endpoints

**Asset Endpoints (5):**
1. `POST /api/maintenance/assets` — Create asset
2. `GET /api/maintenance/assets/{id}` — Get asset with MaintenancePlans
3. `GET /api/maintenance/assets` — List assets with filters
4. `PUT /api/maintenance/assets/{id}/maintenance-plans` — Add/update MaintenancePlan
5. `POST /api/maintenance/assets/{id}/retire` — Retire asset

**WorkOrder Endpoints (7):**
1. `POST /api/maintenance/work-orders` — Create work order
2. `GET /api/maintenance/work-orders/{id}` — Get work order with parts
3. `GET /api/maintenance/work-orders` — List work orders with filters
4. `GET /api/maintenance/work-orders/asset/{assetId}` — Filter by asset
5. `POST /api/maintenance/work-orders/{id}/parts` — Add part to work order
6. `POST /api/maintenance/work-orders/{id}/start` — FSM: Planned→InProgress
7. `POST /api/maintenance/work-orders/{id}/complete` — FSM: InProgress→Completed

**Full OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml` (31 endpoints, MSG-ARCHITECT-062)

---

## Expected Timeline

**Baseline (CRM):** 15 minutes (67% acceleration from 45 NWT estimate)

**Maintenance estimate:** ~15-30 minutes (4 components, FSM integration similar to CRM/HR)

---

## DONE Outbox Format

**File:** `terminals/frontend/outbox/2026-07-07_NNN_msg-frontend-004-maintenance-done.md`

**Frontmatter:**
```yaml
---
id: MSG-FRONTEND-004-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-004
epic_id: EPIC-JT-MAINT
checkpoint_id: CP-MAINT-FRONTEND
created: 2026-07-07
content_hash: <auto>
---
```

**Content sections:**
1. **Executive Summary** — What was completed in how much time
2. **Deliverables** — 4 components + hooks + API service
3. **Build Status** — 0 errors/warnings, TypeScript clean
4. **Pattern Reuse** — Which patterns from CRM/HR were applied
5. **FSM Integration** — WorkOrder workflow transitions implemented
6. **Files Changed** — List of created/modified files
7. **Next Steps** — QA Frontend (MSG-FRONTEND-005) ready for dispatch

---

## References

- **Pattern Source:** MSG-FRONTEND-001-DONE (CRM completion report)
- **Backend Checkpoint:** CP-MAINT-BACKEND (MSG-BACKEND-170, 12 endpoints, 0E/0W)
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml` (31 endpoints)
- **Epic:** EPIC-JT-MAINT
- **Design System:** Datahaven Bento Grid (ADR-048)
- **FSM Pattern:** WorkOrder state machine (Planned→InProgress→Completed)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
