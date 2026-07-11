---
id: MSG-FRONTEND-107
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-194
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 60
created: 2026-07-10
read: 2026-07-10
content_hash: ae2765a86f999159595fb4178659c71e2dcaeea070bbb62093fc98a42215dde7
---

# Doorstar Production UI — Mobile Kiosk (Phase 2 Parallel)

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Backend Spec:** MSG-BACKEND-194 (DONE 2026-07-08)
**Timeline:** 60 NWT (~2 óra, parallel Backend-del)
**User Story:** Műhelyvezető mobil kiosk UI — 6 STAGE követés egy tap-pel

---

## 🎯 SCOPE

Implementáld a **Backend MSG-BACKEND-194 OpenAPI contract alapján** a frontend UI-t:

### Components (Section 3 - Backend spec)
- **ProductionJobCard** — Touch-optimized card (mobile kiosk view)
- **WorkflowStepStepper** — 6 STAGE vertical stepper (grey/yellow/green)
- **KioskMobileLayout** — Minimal navigation, full-screen STAGE view
- **ProductionOverviewPage** — Tulaj/sales dashboard (all projects grid)

### Hooks
- **useProductionQueue()** — TanStack Query: `GET /api/production/jobs`
- **useCompleteStep()** — Mutation: `PUT /api/production/jobs/{jobId}/steps/{stepId}/complete`
- **useProductionSSE()** — SSE channel: `ProductionJobStatusChannel`

### Routes
- `/production/jobs` — ProductionQueuePage (műhelyvezető view)
- `/production/jobs/:jobId` — ProductionJobDetailPage (STAGE stepper)
- `/production/overview` — ProductionOverviewPage (tulaj/sales)

---

## 📋 TECHNICAL SPEC

### 1. ProductionJobCard Component

**File:** `client/src/components/ProductionJobCard.tsx`

**Props:**
```typescript
interface ProductionJobCardProps {
  job: ProductionJobDto;
  onClick?: () => void;
}
```

**UI Requirements:**
- Touch-optimized (min 48px tap target)
- 6 colored circles: grey (Queued) / yellow (InProgress) / green (Done)
- Project name prominent (24px bold)
- Deadline + overdue warning (red border if `isOverdue: true`)
- Tap → navigate to `/production/jobs/:jobId`

**CSS Module:** `ProductionJobCard.module.css`

---

### 2. WorkflowStepStepper Component

**File:** `client/src/components/WorkflowStepStepper.tsx`

**Props:**
```typescript
interface WorkflowStepStepperProps {
  jobId: string;
  steps: WorkflowStepDto[];
  currentStepIndex: number;
  onStepComplete: (stepId: string) => Promise<void>;
}
```

**UI Requirements:**
- Vertical stepper (6 steps)
- Current step highlighted (yellow background)
- "Start" button (if step is Queued) → call `PUT /api/production/jobs/{jobId}/steps/{stepId}/start`
- "Done" button (if step is InProgress) → call `onStepComplete(stepId)`
- Photo upload button (only for "Összeszerelés" step)
- Large touch-friendly buttons (min 56px height)

**State:**
- Optimistic UI update: InProgress → Done immediately, revert on error
- Loading spinner during API call

---

### 3. useProductionQueue Hook

**File:** `client/src/hooks/useProductionQueue.ts`

**Implementation:**
```typescript
import { useQuery } from '@tanstack/react-query';

interface ProductionFilter {
  status?: 'Queued' | 'InProgress' | 'Completed' | 'ShippingReady';
  overdueOnly?: boolean;
}

export function useProductionQueue(filter?: ProductionFilter) {
  return useQuery({
    queryKey: ['production-queue', filter],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filter?.status) params.set('status', filter.status);
      if (filter?.overdueOnly) params.set('overdueOnly', 'true');

      const response = await fetch(`/api/production/jobs?${params}`);
      if (!response.ok) throw new Error('Failed to fetch production queue');
      return response.json() as Promise<{ jobs: ProductionJobDto[]; total: number }>;
    },
    staleTime: 30000, // 30s cache
    refetchOnWindowFocus: true
  });
}
```

---

### 4. useCompleteStep Hook

**File:** `client/src/hooks/useCompleteStep.ts`

**Implementation:**
```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';

export function useCompleteStep() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ jobId, stepId }: { jobId: string; stepId: string }) => {
      const response = await fetch(`/api/production/jobs/${jobId}/steps/${stepId}/complete`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ completedBy: 'user:current' }) // TODO: Replace with auth context
      });
      if (!response.ok) throw new Error('Failed to complete step');
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['production-queue'] });
      queryClient.invalidateQueries({ queryKey: ['production-job'] });
    }
  });
}
```

---

### 5. useProductionSSE Hook

**File:** `client/src/hooks/useProductionSSE.ts`

**Implementation:**
```typescript
import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';

export function useProductionSSE() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const eventSource = new EventSource('/api/sse/production');

    eventSource.addEventListener('WorkflowStepCompleted', (event) => {
      const data = JSON.parse(event.data);
      queryClient.invalidateQueries({ queryKey: ['production-queue'] });
      queryClient.invalidateQueries({ queryKey: ['production-job', data.jobId] });
    });

    eventSource.addEventListener('ProductionJobShippingReady', (event) => {
      const data = JSON.parse(event.data);
      // Show notification: "🚀 {projectName} kiszállítható!"
      queryClient.invalidateQueries({ queryKey: ['production-queue'] });
    });

    return () => eventSource.close();
  }, [queryClient]);
}
```

---

### 6. Routes Integration

**File:** `client/src/main.tsx` (or routing config)

**Add routes:**
```typescript
{
  path: '/production',
  children: [
    { path: 'jobs', element: <ProductionQueuePage /> },
    { path: 'jobs/:jobId', element: <ProductionJobDetailPage /> },
    { path: 'overview', element: <ProductionOverviewPage /> }
  ]
}
```

---

## 🔗 INTEGRATION POINTS

### Backend API (MSG-BACKEND-194)
- **Base Path:** `/api/production`
- **Endpoints Used:**
  - `GET /api/production/jobs` — ProductionQueue
  - `GET /api/production/jobs/{jobId}` — Job detail
  - `PUT /api/production/jobs/{jobId}/steps/{stepId}/start` — Start step
  - `PUT /api/production/jobs/{jobId}/steps/{stepId}/complete` — Complete step
  - `POST /api/production/jobs/{jobId}/steps/{stepId}/photo` — Photo upload (optional)
  - `GET /api/production/overview` — Overview dashboard

### SSE Events
- **Channel:** `/api/sse/production`
- **Events:**
  - `WorkflowStepCompleted` → Invalidate cache, update UI
  - `ProductionJobShippingReady` → Show notification

---

## ✅ ACCEPTANCE CRITERIA

### Components
- [ ] ProductionJobCard renders 6 colored circles (grey/yellow/green)
- [ ] WorkflowStepStepper shows current step highlighted
- [ ] "Start" / "Done" buttons work (optimistic UI)
- [ ] Touch-optimized (min 48px tap targets)

### Hooks
- [ ] useProductionQueue caches for 30s, refetches on focus
- [ ] useCompleteStep optimistic update + cache invalidation
- [ ] useProductionSSE listens to WorkflowStepCompleted + ShippingReady events

### Pages
- [ ] /production/jobs shows active projects (ProductionQueuePage)
- [ ] /production/jobs/:jobId shows 6 STAGE stepper (ProductionJobDetailPage)
- [ ] /production/overview shows tulaj/sales dashboard (ProductionOverviewPage)

### Integration
- [ ] All API calls use Backend MSG-BACKEND-194 OpenAPI contract
- [ ] SSE real-time updates working
- [ ] Overdue projects highlighted (red border)

---

## 📊 TIMELINE

**Estimated:** 60 NWT (~2 óra)

| Task | NWT |
|------|-----|
| Components (ProductionJobCard, WorkflowStepStepper, KioskMobileLayout) | 30 NWT |
| Hooks (useProductionQueue, useCompleteStep, useProductionSSE) | 15 NWT |
| Pages (ProductionQueuePage, ProductionJobDetailPage, ProductionOverviewPage) | 15 NWT |

**Parallel with Backend:** Backend implementing API endpoints, Frontend implementing UI simultaneously.

---

## 🚧 BLOCKERS

**Dependencies:**
- Backend MSG-BACKEND-194 API endpoints must be deployed
- SSE `/api/sse/production` channel must be available

**If blocked:** Wait for Backend DONE, then proceed with UI implementation.

---

## 📁 FILES TO CREATE

```
client/src/components/
  ProductionJobCard.tsx
  ProductionJobCard.module.css
  WorkflowStepStepper.tsx
  WorkflowStepStepper.module.css
  KioskMobileLayout.tsx
  KioskMobileLayout.module.css

client/src/hooks/
  useProductionQueue.ts
  useCompleteStep.ts
  useProductionSSE.ts

client/src/pages/
  ProductionQueuePage.tsx
  ProductionQueuePage.module.css
  ProductionJobDetailPage.tsx
  ProductionJobDetailPage.module.css
  ProductionOverviewPage.tsx
  ProductionOverviewPage.module.css
```

---

## 🎯 SUCCESS METRICS

- ✅ Mobile kiosk UI rendered correctly (6 STAGE circles visible)
- ✅ Tap "Done" → step turns green immediately (optimistic UI)
- ✅ SSE updates → UI refreshes without page reload
- ✅ Overdue projects highlighted (red border)
- ✅ No build errors, TypeScript types correct

---

**Start implementing when Backend API is ready!**

**Parallel coordination:** Backend + Frontend working simultaneously on Phase 2.

---

📋 Generated by Conductor Terminal — Doorstar Phase 2 Frontend Parallel Dispatch (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
