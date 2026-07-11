---
id: MSG-FRONTEND-100
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FRONTEND-099
created: 2026-07-04
completed: 2026-07-04
---

# CRM Wave 2 Phase 2: Drag & Drop Implementation

**Epic:** EPIC-JT-CRM Frontend Wave 2
**Phase:** 2 of 3 (Phase 1 Form Validation ✅ COMPLETE)

---

## Task Summary

Implement **drag & drop functionality** for the OpportunityPipeline component using **@dnd-kit** library.

Users should be able to drag opportunity cards between pipeline stages (Draft → Proposal → Negotiation → Won/Lost/Abandoned).

---

## Acceptance Criteria

- [ ] OpportunityPipeline wrapped with `<DndContext>`
- [ ] 6 droppable zones implemented (Draft, Proposal, Negotiation, Won, Lost, Abandoned)
- [ ] Opportunity cards are draggable (`useDraggable` hook)
- [ ] `onDragEnd` handler updates opportunity stage via `useUpdateOpportunity` mutation
- [ ] Optimistic UI update + rollback on error
- [ ] Touch support for mobile (mobile sensors)
- [ ] Build verification: 0 TypeScript errors
- [ ] Manual testing: drag & drop works in browser

---

## Context: Phase 1 Complete

**Already done (60% → 80%):**
- ✅ ActivityLog component (timeline view, dark-first design)
- ✅ useActivityLog hook (TanStack Query)
- ✅ Mock CRM API (25 leads, 18 opportunities, 60+ activities)
- ✅ CRM API wrapper (feature flag, real API stubs)
- ✅ SSE client (EventSource, auto-invalidation)
- ✅ Dependencies installed (@dnd-kit/core, @dnd-kit/sortable, zod, react-hook-form)
- ✅ **Phase 1: Form Validation** (production-ready)

**Remaining:**
- ⏳ Phase 2: Drag & Drop (THIS TASK)
- 📋 Phase 3: Advanced Filters (URL state sync)

---

## Implementation Steps

### 1. OpportunityPipeline Component Update

**Location:** `src/components/OpportunityPipeline.tsx` (or create if missing)

**Import @dnd-kit:**
```typescript
import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  PointerSensor,
  TouchSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import { SortableContext, useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
```

**Define stages:**
```typescript
const PIPELINE_STAGES = [
  { id: 'draft', name: 'Draft', color: '#6b7280' },
  { id: 'proposal', name: 'Proposal', color: '#3b82f6' },
  { id: 'negotiation', name: 'Negotiation', color: '#f59e0b' },
  { id: 'won', name: 'Won', color: '#10b981' },
  { id: 'lost', name: 'Lost', color: '#ef4444' },
  { id: 'abandoned', name: 'Abandoned', color: '#6b7280' },
];
```

### 2. DndContext Wrapper

```tsx
export function OpportunityPipeline() {
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 5 }, // 5px drag threshold
    }),
    useSensor(TouchSensor, {
      activationConstraint: { delay: 200, tolerance: 5 }, // mobile support
    })
  );

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;

    const opportunityId = active.id as string;
    const newStage = over.id as string;

    // Optimistic UI update (TanStack Query mutation)
    updateOpportunity.mutate(
      { id: opportunityId, stage: newStage },
      {
        onError: () => {
          // Rollback on error (TanStack Query auto-rollback)
        },
      }
    );
  };

  return (
    <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
      <div className="pipeline-container">
        {PIPELINE_STAGES.map(stage => (
          <DroppableStage key={stage.id} stage={stage}>
            {/* Render opportunity cards here */}
          </DroppableStage>
        ))}
      </div>
    </DndContext>
  );
}
```

### 3. Droppable Stage Component

```tsx
function DroppableStage({ stage, children }: { stage: Stage; children: React.ReactNode }) {
  const { setNodeRef } = useDroppable({ id: stage.id });

  return (
    <div
      ref={setNodeRef}
      className="droppable-stage"
      style={{ borderTopColor: stage.color }}
    >
      <h3>{stage.name}</h3>
      <div className="opportunity-cards">
        {children}
      </div>
    </div>
  );
}
```

### 4. Draggable Opportunity Card

```tsx
function DraggableOpportunityCard({ opportunity }: { opportunity: Opportunity }) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useDraggable({ id: opportunity.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...attributes}
      {...listeners}
      className="opportunity-card"
    >
      <h4>{opportunity.title}</h4>
      <p>{opportunity.customerName}</p>
      <p className="value">{opportunity.value} Ft</p>
    </div>
  );
}
```

### 5. Mutation Hook (useUpdateOpportunity)

**Location:** `src/hooks/useUpdateOpportunity.ts` (create if missing)

```typescript
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { crmApi } from '@/services/crmApi';

export function useUpdateOpportunity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, stage }: { id: string; stage: string }) =>
      crmApi.updateOpportunity(id, { stage }),
    onSuccess: () => {
      // Invalidate opportunities query to refetch
      queryClient.invalidateQueries({ queryKey: ['opportunities'] });
    },
    // Optimistic update (optional, advanced)
    onMutate: async ({ id, stage }) => {
      await queryClient.cancelQueries({ queryKey: ['opportunities'] });
      const previous = queryClient.getQueryData(['opportunities']);

      // Update cache optimistically
      queryClient.setQueryData(['opportunities'], (old: any) => ({
        ...old,
        opportunities: old.opportunities.map((opp: any) =>
          opp.id === id ? { ...opp, stage } : opp
        ),
      }));

      return { previous };
    },
    onError: (err, vars, context) => {
      // Rollback on error
      if (context?.previous) {
        queryClient.setQueryData(['opportunities'], context.previous);
      }
    },
  });
}
```

---

## Mock API Update

**File:** `src/services/mockCrmApi.ts`

Add `updateOpportunity` method:

```typescript
async updateOpportunity(id: string, updates: Partial<Opportunity>): Promise<Opportunity> {
  await this.simulateLatency();

  const opportunities = this.getOpportunities();
  const index = opportunities.findIndex(o => o.id === id);

  if (index === -1) throw new Error('Opportunity not found');

  const updated = { ...opportunities[index], ...updates };
  opportunities[index] = updated;

  this.saveToLocalStorage('opportunities', opportunities);
  return updated;
}
```

---

## Build Verification

```bash
cd /opt/spaceos/frontend/joinerytech-portal
npm run build
```

**Expected:** 0 TypeScript errors

---

## Manual Testing Checklist

- [ ] Drag opportunity card from Draft to Proposal → card moves
- [ ] Drag opportunity card from Negotiation to Won → card moves
- [ ] Drop outside stage → card returns to original position
- [ ] Touch support (mobile) → drag works on mobile simulator
- [ ] Optimistic UI → card moves immediately, no flicker
- [ ] Error simulation → card returns on API error

---

## CSS Styling (Optional Enhancement)

**File:** `src/components/OpportunityPipeline.module.css`

```css
.pipeline-container {
  display: flex;
  gap: 16px;
  overflow-x: auto;
  padding: 16px;
}

.droppable-stage {
  flex: 1;
  min-width: 250px;
  background: #1f2937;
  border-radius: 8px;
  border-top: 3px solid;
  padding: 16px;
}

.opportunity-card {
  background: #374151;
  border-radius: 6px;
  padding: 12px;
  margin-bottom: 8px;
  cursor: grab;
  transition: opacity 0.2s;
}

.opportunity-card:active {
  cursor: grabbing;
}

.opportunity-card.dragging {
  opacity: 0.5;
}
```

---

## Next Phase: Advanced Filters (Phase 3)

After Phase 2 complete:
- LeadGrid filters (date range, search, multi-select status)
- OpportunityPipeline filters (assignedTo, closeDate, value sliders)
- URL state sync (query params, shareable filtered views)

---

## Timeline

**ETA:** 2-3 hours

**Priority:** HIGH (CRM Wave 2 critical feature)

**Model:** sonnet (React + TypeScript + TanStack Query)

---

**Expected DONE outbox:**
- Drag & Drop working (6 stages)
- Build verification (0 errors)
- Manual testing results
- Ready for Phase 3 (Advanced Filters)
