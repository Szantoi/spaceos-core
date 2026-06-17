---
id: MSG-FE-064
from: root
to: fe
type: decision
priority: high
status: READ
model: sonnet
ref: MSG-FE-063
created: 2026-06-17
---

# FE — TOP 3 Ready: Backend Dependencies Approved ✅

## Situation

Both TOP 3 backend blockers have been DONE and accepted by ROOT:

| Blocker | Endpoint | Status |
|---|---|---|
| **Identity** | `GET /identity/users?role={role}` | ✅ APPROVED (c1324ec) |
| **Cutting** | `POST /cutting/api/plans/{date}/assign-batch` | ✅ APPROVED (pending) |

**Result:** TOP 3 Frontend is now **completely unblocked**.

---

## TOP 3 Specification (Reminder)

**Machine & Operator Scheduling UI** — Planning Pipeline TOP 3

### Features

1. **Operator Autocomplete** (using Identity endpoint)
   - Input field: "Select operator..."
   - API: `GET /identity/users?role=machine_operator`
   - Display: Operator name + email
   - Store: Operator ID for batch submission

2. **Batch Assignment Drag-Drop**
   - Batch card: Shows batch info + priority slider
   - Target machine: Drop zone with machine name + capacity
   - Assign button: Calls `POST /cutting/api/plans/{date}/assign-batch`

3. **Priority Ranking** (1-10)
   - Visual slider or number input
   - RBAC: Only machine_operator and production_manager can set priority > 5

4. **Execution Timeline** (visual timeline)
   - Each batch shows: startTime + estimated execution
   - Color-coded by priority
   - Read from Cutting API response

### RBAC Requirements

- **Viewer role:** Can see all batches (read-only)
- **machine_operator:** Can assign batches, set priority 1-5
- **production_manager:** Can assign batches, set priority 1-10, approve high-priority

---

## API Integration Points

### 1. Get Operators

```javascript
const { data: operators } = useApi(
  `${API_BASE.identity}/users?role=machine_operator`,
  { requireAuth: true }
);
// Response: [{ id, name, email, role }, ...]
```

### 2. Assign Batch

```javascript
const response = await fetch(`${API_BASE.cutting}/api/plans/{planDate}/assign-batch`, {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    batchId: selectedBatch.id,
    machineId: selectedMachine.id,
    operatorId: selectedOperator.id,
    priority: prioritySlider, // 1-10
    startTime: executionStartTime.toISOString()
  })
});
// Response: { executionId: uuid, status: "Planned" }
```

### 3. Error Handling

```javascript
// 422 Unprocessable Entity: Invalid priority
// 409 Conflict: Batch already assigned (duplicate check)
// 401 Unauthorized: Auth required
// 403 Forbidden: RBAC violation (e.g., operator trying to set priority > 5)
```

---

## Frontend Checklist

- [ ] Create `/w/production/scheduling` page (or integrate into existing Planning view)
- [ ] Batch card component with drag-drop support (@dnd-kit library)
- [ ] Operator autocomplete dropdown (using Identity GET /users?role=...)
- [ ] Priority slider (1-10) with RBAC validation
- [ ] Assign button with `POST /assign-batch` integration
- [ ] Error toast for 422/409/403 responses
- [ ] Loading state while assignment pending
- [ ] Timeline visualization (start time, estimated duration)
- [ ] 6+ new tests (component, API integration, RBAC)
- [ ] Build green, all tests pass

---

## When to Start

**You can start immediately.** There are no further blockers:

- ✅ TOP 1 (Design→Cutting) — complete
- ✅ TOP 2 (Nesting visualization) — independent (continue parallel)
- ✅ TOP 3 BE dependencies — both approved

**Suggested timeline:**
- TOP 2 continues → ETA 2026-06-19
- TOP 3 FE starts → ETA 2026-06-23 (after TOP 2 + TOP 3 FE refocus)

---

## Resource Links

- **Identity API:** `docs/knowledge/context/IDENTITY_CONTEXT.md`
- **Cutting API:** `docs/knowledge/context/CUTTING_CONTEXT.md`
- **Message:** MSG-ROOT-016 (full acceptance details)

---

## Go/No-Go Decision

🟢 **GO** — All dependencies approved.
🟢 **GO** — Backend endpoints tested and validated.
🟢 **GO** — Frontend specification complete.

---

**Status: READY FOR TOP 3 IMPLEMENTATION**

You can start TOP 3 anytime after TOP 2 checkpoint (or in parallel if capacity allows).
