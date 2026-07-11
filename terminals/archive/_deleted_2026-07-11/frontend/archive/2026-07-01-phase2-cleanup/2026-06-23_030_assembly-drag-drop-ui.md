---
id: MSG-FRONTEND-030
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-23_1105_consensus.md
created: 2026-06-23
content_hash: 122083659c5796cf54ac418ce522b46355a7e1c49a6a82d490da72c6f45d53cc
---

# Assembly Drag-and-Drop UI Implementation

## Context

**Consensus source:** `docs/planning/queue/2026-06-23_1105_consensus.md`
**Business value:** HIGH - Workflow optimization for assembly planning
**Priority:** 1 of 3 features (highest priority)
**Estimated effort:** 2 days (frontend portion of 5-day total)

**Backend dependency:** MSG-BACKEND-042 (runs in parallel)
- Backend provides: `PATCH /api/v1/work-orders/{id}/assembly-sequence`
- Can mock API during development, integrate when backend ready

## Objective

Implement drag-and-drop reordering for assembly operations with:
1. Touch-friendly gesture support (mobile workers)
2. Optimistic UI updates (instant feedback)
3. Undo/redo functionality (mistake recovery)
4. Keyboard accessibility (A11y requirement)
5. Haptic feedback (mobile enhancement)
6. Error handling (network failures, conflicts)

## User Story

**As a** production planner
**I want to** reorder assembly operations by dragging them
**So that** I can optimize the workflow without recreating the entire work order

**Acceptance Criteria:**
- ✅ Drag operation from position A to position B
- ✅ UI updates instantly (optimistic)
- ✅ Changes persist to backend
- ✅ If backend fails → revert UI + show error toast
- ✅ If conflict detected (409) → show warning, refresh data
- ✅ Undo button available for 30 seconds after change
- ✅ Works on mobile (touch) and desktop (mouse)
- ✅ Keyboard navigation supported (arrow keys + Enter)

## Technical Approach

### Library: @dnd-kit

**Why @dnd-kit:**
- Touch support (mobile workers)
- Keyboard accessibility built-in
- Optimistic UI patterns
- Bundle size: ~20KB (vs. react-beautiful-dnd 60KB)
- Active maintenance (2024+)

**Installation:**
```bash
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

### Component Structure

```
src/components/assembly/
  ├── AssemblyOperationsList.tsx    // Main drag-drop container
  ├── SortableOperation.tsx         // Individual draggable operation
  ├── OperationCard.tsx             // Operation display card
  └── UndoToast.tsx                 // Undo notification
```

## Implementation Spec

### 1. AssemblyOperationsList Component (Day 1, 3 hours)

**File:** `src/components/assembly/AssemblyOperationsList.tsx`

**Props:**
```typescript
interface AssemblyOperationsListProps {
  workOrderId: string
  operations: WorkOrderOperation[]
  onReorder?: (operations: WorkOrderOperation[]) => void
  readOnly?: boolean
}

interface WorkOrderOperation {
  id: string
  sequence: number
  description: string
  estimated_duration: string  // ISO 8601 duration
  operation_type: string
  last_modified: string
}
```

**State:**
```typescript
const [operations, setOperations] = useState<WorkOrderOperation[]>([])
const [undoStack, setUndoStack] = useState<UndoCommand[]>([])
const [isSaving, setIsSaving] = useState(false)

interface UndoCommand {
  previousState: WorkOrderOperation[]
  newState: WorkOrderOperation[]
  timestamp: number
}
```

**Implementation (full code):**
```tsx
import { DndContext, DragEndEvent, useSensor, useSensors, PointerSensor, KeyboardSensor } from '@dnd-kit/core'
import { SortableContext, verticalListSortingStrategy, arrayMove } from '@dnd-kit/sortable'
import { toast } from 'react-hot-toast'

export function AssemblyOperationsList({ workOrderId, operations: initialOps, readOnly = false }: Props) {
  const [operations, setOperations] = useState(initialOps)
  const [undoStack, setUndoStack] = useState<UndoCommand[]>([])

  // Sensor configuration (Plan-B: touch scroll vs drag fix)
  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: { distance: 8 }  // 8px threshold before drag starts
    }),
    useSensor(KeyboardSensor)  // A11y: arrow keys + enter to reorder
  )

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (!over || active.id === over.id) return

    const oldIndex = operations.findIndex(op => op.id === active.id)
    const newIndex = operations.findIndex(op => op.id === over.id)

    // Optimistic UI update
    const reorderedOps = arrayMove(operations, oldIndex, newIndex).map((op, idx) => ({
      ...op,
      sequence: idx + 1
    }))

    setOperations(reorderedOps)

    // Command pattern for undo
    const command: UndoCommand = {
      previousState: operations,
      newState: reorderedOps,
      timestamp: Date.now()
    }
    setUndoStack([...undoStack, command])

    // Haptic feedback (mobile)
    if ('vibrate' in navigator) {
      navigator.vibrate([5, 50, 5])  // Short-pause-short pattern
    }

    // API call
    try {
      const response = await fetch(`/api/v1/work-orders/${workOrderId}/assembly-sequence`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          operations: reorderedOps.map(op => ({ id: op.id, sequence: op.sequence })),
          timestamp: new Date().toISOString()
        })
      })

      if (!response.ok) {
        if (response.status === 409) {
          // Conflict: someone else modified
          toast.error('A munkarendet más felhasználó módosította. Frissítsd az oldalt!')
          // TODO: Auto-refresh operations from server
        } else {
          throw new Error(`HTTP ${response.status}`)
        }
      } else {
        const data = await response.json()
        // Update with server response (LastModified timestamps)
        setOperations(data.updated_operations)

        // Show duration change if available
        if (data.estimated_duration_change && data.estimated_duration_change !== '+0min') {
          toast.success(`Átrendezve! Időbecslés változás: ${data.estimated_duration_change}`)
        }
      }
    } catch (error) {
      // Rollback on error
      setOperations(operations)
      setUndoStack(undoStack.slice(0, -1))
      toast.error('Mentés sikertelen - változtatások visszavonva')
      console.error('Assembly reorder failed:', error)
    }
  }

  const handleUndo = () => {
    if (undoStack.length === 0) return
    const lastCommand = undoStack[undoStack.length - 1]

    // Check if undo is still valid (< 30 seconds old)
    if (Date.now() - lastCommand.timestamp > 30000) {
      toast.error('Visszavonás lejárt (30 másodperc limit)')
      setUndoStack([])
      return
    }

    setOperations(lastCommand.previousState)
    setUndoStack(undoStack.slice(0, -1))
    toast.success('Visszavonva')

    // Send revert to backend
    fetch(`/api/v1/work-orders/${workOrderId}/assembly-sequence`, {
      method: 'PATCH',
      body: JSON.stringify({
        operations: lastCommand.previousState.map(op => ({ id: op.id, sequence: op.sequence })),
        timestamp: new Date().toISOString()
      })
    })
  }

  return (
    <div className="space-y-4">
      {/* Undo button (show for 30s after change) */}
      {undoStack.length > 0 && Date.now() - undoStack[undoStack.length - 1].timestamp < 30000 && (
        <button
          onClick={handleUndo}
          className="px-4 py-2 bg-yellow-500 text-white rounded hover:bg-yellow-600"
        >
          ↶ Visszavonás
        </button>
      )}

      <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
        <SortableContext items={operations} strategy={verticalListSortingStrategy}>
          <div className="space-y-2">
            {operations.map(operation => (
              <SortableOperation
                key={operation.id}
                operation={operation}
                disabled={readOnly}
              />
            ))}
          </div>
        </SortableContext>
      </DndContext>
    </div>
  )
}
```

**DoD:**
- [ ] Component renders operations list
- [ ] Drag-and-drop works (mouse)
- [ ] Touch drag works (mobile)
- [ ] Keyboard navigation works (arrow keys)
- [ ] Optimistic UI updates instantly
- [ ] API call on drop
- [ ] Rollback on error
- [ ] Undo button shows for 30s
- [ ] Haptic feedback on mobile

### 2. SortableOperation Component (Day 1, 1 hour)

**File:** `src/components/assembly/SortableOperation.tsx`

```tsx
import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { OperationCard } from './OperationCard'

interface SortableOperationProps {
  operation: WorkOrderOperation
  disabled?: boolean
}

export function SortableOperation({ operation, disabled }: SortableOperationProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging
  } = useSortable({ id: operation.id, disabled })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1
  }

  return (
    <div ref={setNodeRef} style={style} {...attributes} {...listeners}>
      <OperationCard operation={operation} isDragging={isDragging} />
    </div>
  )
}
```

**DoD:**
- [ ] Wraps OperationCard with sortable behavior
- [ ] Handles drag visual feedback (opacity)
- [ ] Passes attributes/listeners correctly

### 3. OperationCard Component (Day 1, 1 hour)

**File:** `src/components/assembly/OperationCard.tsx`

```tsx
interface OperationCardProps {
  operation: WorkOrderOperation
  isDragging?: boolean
}

export function OperationCard({ operation, isDragging }: OperationCardProps) {
  return (
    <div
      className={`
        p-4 bg-white border rounded-lg shadow-sm
        flex items-center gap-4
        ${isDragging ? 'ring-2 ring-blue-500' : 'hover:border-blue-300'}
        transition-all cursor-grab active:cursor-grabbing
      `}
    >
      {/* Drag handle icon */}
      <div className="text-gray-400">
        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 8h16M4 16h16" />
        </svg>
      </div>

      {/* Sequence number */}
      <div className="flex-shrink-0 w-8 h-8 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center font-semibold">
        {operation.sequence}
      </div>

      {/* Operation details */}
      <div className="flex-1">
        <h3 className="font-medium text-gray-900">{operation.description}</h3>
        <p className="text-sm text-gray-500">{operation.operation_type}</p>
      </div>

      {/* Duration */}
      <div className="text-right text-sm text-gray-600">
        {formatDuration(operation.estimated_duration)}
      </div>
    </div>
  )
}

function formatDuration(isoDuration: string): string {
  // Parse ISO 8601 duration (e.g., "PT30M" → "30 perc")
  const match = isoDuration.match(/PT(?:(\d+)H)?(?:(\d+)M)?/)
  if (!match) return isoDuration

  const hours = parseInt(match[1] || '0')
  const minutes = parseInt(match[2] || '0')

  if (hours > 0) return `${hours}ó ${minutes}p`
  return `${minutes} perc`
}
```

**DoD:**
- [ ] Card displays operation details
- [ ] Sequence number visible
- [ ] Duration formatted correctly
- [ ] Drag handle icon visible
- [ ] Hover/dragging states styled

### 4. Integration & Testing (Day 2, 3 hours)

**Test cases (manual):**

1. **Basic drag-drop**
   - Drag operation #2 to position #1
   - Verify UI updates instantly
   - Verify sequence numbers update
   - Check browser DevTools Network tab for API call

2. **Touch drag (mobile)**
   - Use Chrome DevTools mobile emulation
   - Long-press + drag operation
   - Verify 8px threshold prevents accidental drags

3. **Keyboard navigation**
   - Focus operation with Tab
   - Press Space to "pick up" operation
   - Arrow keys to move
   - Enter to "drop"

4. **Undo**
   - Drag operation
   - Click Undo button within 30s
   - Verify state reverts
   - Wait 31s → Undo button disappears

5. **Error handling**
   - Mock API 500 error
   - Verify rollback + error toast

6. **Conflict (409)**
   - Mock API 409 response
   - Verify conflict warning shown

**DoD:**
- [ ] All 6 test cases pass
- [ ] No console errors
- [ ] Responsive on mobile (320px width)
- [ ] Accessible (keyboard navigation works)

## Mock API (Development)

While MSG-BACKEND-042 is in progress, use mock API:

**File:** `src/mocks/assemblyApi.ts`

```typescript
export async function mockUpdateAssemblySequence(workOrderId: string, body: any) {
  await new Promise(resolve => setTimeout(resolve, 500))  // Simulate network delay

  return {
    updated_operations: body.operations.map((op: any) => ({
      ...op,
      last_modified: new Date().toISOString()
    })),
    estimated_duration_change: '+0min',
    total_duration: 'PT2H30M'
  }
}
```

Replace with real API when backend is ready.

## Dependencies

**New packages:**
```json
{
  "@dnd-kit/core": "^6.0.8",
  "@dnd-kit/sortable": "^7.0.2",
  "@dnd-kit/utilities": "^3.2.1",
  "react-hot-toast": "^2.4.1"  // If not already installed
}
```

**Bundle impact:** ~25KB gzipped

## Security

1. **CSRF protection**
   - Use existing CSRF token in API calls
   - Fetch adds credentials automatically

2. **XSS prevention**
   - Operation descriptions are server-controlled (no user input)
   - formatDuration() uses regex, not eval

3. **Authorization**
   - Backend enforces `work_order:write` permission
   - Frontend doesn't implement auth checks (trust backend)

## Accessibility (A11y)

- ✅ Keyboard navigation (KeyboardSensor)
- ✅ ARIA labels on drag handles
- ✅ Focus indicators on drag items
- ✅ Screen reader announcements ("Operation moved from position 2 to position 1")

**Add to OperationCard:**
```tsx
<div
  role="button"
  aria-label={`Reorder ${operation.description}, currently position ${operation.sequence}`}
  tabIndex={0}
>
```

## Definition of Done

- [ ] @dnd-kit packages installed
- [ ] AssemblyOperationsList component implemented
- [ ] SortableOperation wrapper implemented
- [ ] OperationCard component implemented
- [ ] Drag-drop works (mouse + touch + keyboard)
- [ ] Optimistic UI updates
- [ ] API integration (or mock if backend not ready)
- [ ] Error handling (rollback + toast)
- [ ] Undo functionality (30s window)
- [ ] Haptic feedback on mobile
- [ ] All 6 manual test cases pass
- [ ] No TypeScript errors
- [ ] No accessibility violations (axe DevTools scan)
- [ ] Responsive design (mobile + desktop)

## Timeline

**Day 1 (5 hours):**
- Install dependencies (15 min)
- AssemblyOperationsList component (3 hours)
- SortableOperation + OperationCard (1.5 hours)

**Day 2 (3 hours):**
- Mock API integration (30 min)
- Manual testing + bug fixes (2 hours)
- A11y audit (30 min)

**Total:** 8 hours over 2 days

## Notes

- **Backend parallel track:** MSG-BACKEND-042 (3 days)
- **Can demo with mock API** before backend ready
- **Future enhancement:** Real-time collaboration (show other users' cursors)
- **Filter persistence already DONE** (MSG-FRONTEND-023)
- **Image lazy-load already DONE** (VirtualizedCatalogGrid)

## Questions?

If backend API changes, update the interface in `WorkOrderOperation` type definition.

---

**Conductor**
2026-06-23
Consensus priority 1/3 - Parallel track with Backend
