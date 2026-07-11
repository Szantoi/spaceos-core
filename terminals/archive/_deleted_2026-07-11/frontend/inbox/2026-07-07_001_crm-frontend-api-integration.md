---
id: MSG-FRONTEND-001
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-113
epic_id: EPIC-JT-CRM
checkpoint_id: CP-CRM-FRONTEND
estimated_nwt: 45
created: 2026-07-07
completed: 2026-07-07
---

# CRM Frontend API Integration — Partial → Complete

## 🎯 Feladat Összefoglalás

**Cél:** CP-CRM-FRONTEND checkpoint befejezése (PARTIAL → DONE)

**Jelenlegi állapot:** UI komponensek kész (LeadGrid, OpportunityPipeline, filters)
**Hiányzik:** Backend API integráció (real data fetching)

**Backend API:** ✅ READY (CP-CRM-BACKEND done 2026-07-04)

---

## 📋 Acceptance Criteria

1. ✅ **LeadGrid komponens API integráció**
   - GET `/api/crm/leads` paginated endpoint
   - TanStack Query hook (`useLeads`)
   - Real-time data display
   - Filter/search működik

2. ✅ **OpportunityPipeline komponens API integráció**
   - GET `/api/crm/opportunities` with forecast
   - TanStack Query hook (`useOpportunities`)
   - Kanban board stages real data
   - Drag-drop FSM transition

3. ✅ **Lead FSM Action integráció**
   - POST `/api/crm/leads/{id}/convert`
   - POST `/api/crm/leads/{id}/qualify`
   - POST `/api/crm/leads/{id}/disqualify`
   - Optimistic updates

4. ✅ **Opportunity FSM Action integráció**
   - POST `/api/crm/opportunities/{id}/win`
   - POST `/api/crm/opportunities/{id}/lose`
   - POST `/api/crm/opportunities/{id}/activate`
   - Optimistic updates

5. ✅ **Activity Log integráció**
   - GET `/api/crm/activities`
   - POST `/api/crm/activities`
   - Real-time activity stream

6. ✅ **Error Handling & Loading States**
   - Error boundaries
   - Loading skeletons
   - Toast notifications (success/error)

7. ✅ **Build & Test Gates**
   - `npm run build` → 0 errors
   - Komponensek render without crash
   - API calls verified (network tab)

---

## 🔧 Technikai Iránymutatás

### Backend API Endpoints (READY ✅)

```typescript
// Lead endpoints
GET    /api/crm/leads?page=1&pageSize=20&status=New
POST   /api/crm/leads
GET    /api/crm/leads/{id}
PUT    /api/crm/leads/{id}
POST   /api/crm/leads/{id}/convert
POST   /api/crm/leads/{id}/qualify
POST   /api/crm/leads/{id}/disqualify

// Opportunity endpoints
GET    /api/crm/opportunities?stage=Proposal&forecastMonth=2026-07
POST   /api/crm/opportunities
GET    /api/crm/opportunities/{id}
PUT    /api/crm/opportunities/{id}
POST   /api/crm/opportunities/{id}/win
POST   /api/crm/opportunities/{id}/lose
POST   /api/crm/opportunities/{id}/activate

// Activity endpoints
GET    /api/crm/activities?entityType=Lead&entityId={id}
POST   /api/crm/activities
```

### TanStack Query Hooks Pattern

**Példa: `useLeads.ts`**
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/services/api';

export const useLeads = (filters?: LeadFilters) => {
  return useQuery({
    queryKey: ['leads', filters],
    queryFn: () => api.get('/api/crm/leads', { params: filters }),
    staleTime: 30000, // 30 sec
  });
};

export const useConvertLead = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (leadId: string) =>
      api.post(`/api/crm/leads/${leadId}/convert`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leads'] });
      queryClient.invalidateQueries({ queryKey: ['opportunities'] });
    },
  });
};
```

### Optimistic Updates Pattern

```typescript
export const useUpdateLead = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: UpdateLeadRequest) =>
      api.put(`/api/crm/leads/${id}`, data),
    onMutate: async ({ id, data }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['leads', id] });

      // Snapshot previous value
      const previous = queryClient.getQueryData(['leads', id]);

      // Optimistically update
      queryClient.setQueryData(['leads', id], (old: any) => ({
        ...old,
        ...data,
      }));

      return { previous };
    },
    onError: (err, vars, context) => {
      // Rollback on error
      queryClient.setQueryData(['leads', vars.id], context?.previous);
    },
    onSettled: (data, error, vars) => {
      // Refetch after mutation
      queryClient.invalidateQueries({ queryKey: ['leads', vars.id] });
    },
  });
};
```

### Error Handling Pattern

```typescript
// Error boundary wrapper
<ErrorBoundary fallback={<ErrorFallback />}>
  <LeadGrid />
</ErrorBoundary>

// Query error handling
const { data, error, isLoading } = useLeads();

if (error) {
  return <ErrorState error={error} retry={refetch} />;
}

// Mutation error handling
const convertLead = useConvertLead();

const handleConvert = async (leadId: string) => {
  try {
    await convertLead.mutateAsync(leadId);
    toast.success('Lead converted to Opportunity!');
  } catch (error) {
    toast.error('Failed to convert lead: ' + error.message);
  }
};
```

---

## 📁 Fájlok (Becsült)

**Új hook fájlok:**
```
src/hooks/
  useLeads.ts              (Query + Mutations)
  useOpportunities.ts      (Query + Mutations)
  useActivities.ts         (Query + Mutations)
```

**Módosított komponensek:**
```
src/components/crm/
  LeadGrid.tsx             (API integration)
  OpportunityPipeline.tsx  (API integration)
  ActivityLog.tsx          (API integration)
  LeadFilters.tsx          (Filter state sync)
```

**API service:**
```
src/services/
  api.ts                   (Axios instance + base config)
  crm/
    leadApi.ts             (Lead endpoints wrapper)
    opportunityApi.ts      (Opportunity endpoints wrapper)
    activityApi.ts         (Activity endpoints wrapper)
```

**Összesen:** ~10-12 fájl módosítás/létrehozás

---

## 🎯 Stratégia: Quick Win Approach

**Miért ez a prioritás?**
1. **UI komponensek már kész** → csak API integráció hiányzik
2. **Backend API ready** → nincs blocker
3. **Partial → Complete** → kis lépés, gyors checkpoint completion
4. **Pattern library** → többi 5 modul ugyanez a minta (kontrolling, HR, stb.)

**Time estimate:** 30-45 NWT (~1-1.5 óra)

**Következő lépések utána:**
- CP-CRM-FRONTEND DONE → trigger Conductor
- Remaining 5 frontend modules planning
- Frontend cascade dispatch

---

## 📊 Pattern Reuse

**Ez az első teljes frontend API integration!**

A minták itt lesznek:
- TanStack Query hook pattern
- Optimistic updates
- Error handling
- Loading states
- FSM action integration

**Többi 5 modul ugyanez lesz:** Kontrolling, HR, Maintenance, QA, DMS
→ 50-62% velocity acceleration (proven pattern reuse)

---

## 🚀 Build & Verification

```bash
# 1. Dependencies
npm install @tanstack/react-query axios

# 2. Build
npm run build
# Verify: 0 errors

# 3. Dev server
npm run dev

# 4. Manual testing
# - LeadGrid: látod-e a lead-eket?
# - OpportunityPipeline: látod-e a stage-eket?
# - Lead convert: működik-e az FSM transition?
# - Error handling: API hiba esetén toast notification?

# 5. Network tab verification
# - GET /api/crm/leads → 200 OK
# - POST /api/crm/leads/{id}/convert → 200 OK
```

---

## 🔗 Referenciák

**Backend API spec:** CP-CRM-BACKEND done (MSG-BACKEND-103)
**UI komponensek:** CP-CRM-FRONTEND partial (MSG-FRONTEND-001 created 2026-07-03)
**Conductor strategy:** MSG-CONDUCTOR-113 (Quick win approach)
**EPICS.yaml:** EPIC-JT-CRM, CP-CRM-FRONTEND checkpoint

---

## 📝 DONE Outbox Expected

**Fájlnév:** `2026-07-07_001_crm-frontend-api-integration-done.md`

**Tartalmaznia kell:**
- ✅ Acceptance criteria teljesítve (7/7)
- 📁 Files changed lista (~10-12 fájl)
- 🔧 Build verification (0 errors)
- 📸 Screenshot (LeadGrid + OpportunityPipeline működés közben)
- 🎯 CP-CRM-FRONTEND checkpoint DONE

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
