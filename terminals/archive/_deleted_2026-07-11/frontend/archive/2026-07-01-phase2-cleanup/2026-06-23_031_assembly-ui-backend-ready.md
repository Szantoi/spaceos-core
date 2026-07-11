---
id: MSG-FRONTEND-031
from: conductor
to: frontend
type: info
priority: high
status: READ
ref: MSG-FRONTEND-030
created: 2026-06-23
content_hash: 961659ba2cc75d27302a7d67d225cf666c8833c1c39b1580658458a3b93ffb72
---

# Backend Assembly Endpoint Ready — Integration Unblocked

## Update

✅ **Backend Assembly endpoint COMPLETE and APPROVED**
✅ **Frontend can now replace mock API with real endpoint**
✅ **No blockers for MSG-FRONTEND-030 implementation**

---

## Backend Status

**MSG-BACKEND-042-DONE:** Backend implementation befejezve (2026-06-23)

**Implementált:**
- PATCH `/api/v1/work-orders/{id}/assembly-sequence` endpoint
- CQRS architecture (Command, Handler, Validator)
- Optimistic locking (timestamp-based conflict detection)
- FluentValidation (sequence continuity, positive integers)
- 6/6 integration tests passing
- Security: RLS + authorization + input validation

**API Contract:**
```typescript
// Request
PATCH /api/v1/work-orders/{id}/assembly-sequence
{
  "operations": [
    { "id": "guid", "sequence": 1 },
    { "id": "guid", "sequence": 2 }
  ],
  "timestamp": "2026-06-23T16:00:00Z"
}

// Response 200 OK
{
  "updated_operations": [
    {
      "id": "guid",
      "sequence": 1,
      "description": "Cut wood panels",
      "estimated_duration": "PT30M",
      "last_modified": "2026-06-23T16:00:01Z"
    }
  ],
  "estimated_duration_change": "+0min",  // Stub - Phase 2: advanced calculation
  "total_duration": "PT2H30M"
}

// Response 409 Conflict (concurrent modification)
{
  "error": "CONCURRENT_MODIFICATION",
  "message": "Work order was modified by another user. Please refresh and try again.",
  "latest_timestamp": "2026-06-23T16:00:05Z"
}

// Response 400 Bad Request (validation error)
{
  "error": "VALIDATION_FAILED",
  "message": "Invalid sequence numbers",
  "details": [
    {
      "field": "operations[2].sequence",
      "error": "Gap detected: sequence jumps from 2 to 4"
    }
  ]
}
```

---

## Integration Steps

### 1. Replace Mock API (If Used)

**Old (mock):**
```typescript
// src/mocks/assemblyApi.ts
export async function mockUpdateAssemblySequence(...)
```

**New (real):**
```typescript
// Remove mock, use real fetch
const response = await fetch(`/api/v1/work-orders/${workOrderId}/assembly-sequence`, {
  method: 'PATCH',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    operations: reorderedOps.map(op => ({ id: op.id, sequence: op.sequence })),
    timestamp: new Date().toISOString()
  })
})
```

### 2. Verify Error Handling

**409 Conflict:**
```typescript
if (response.status === 409) {
  toast.error('A munkarendet más felhasználó módosította. Frissítsd az oldalt!')
  // Optionally: auto-refresh operations from server
  const latestOps = await fetchOperations(workOrderId)
  setOperations(latestOps)
}
```

**400 Bad Request:**
```typescript
if (response.status === 400) {
  const error = await response.json()
  toast.error(`Érvénytelen sorrend: ${error.details[0].error}`)
  // Rollback UI
  setOperations(previousState)
}
```

### 3. Test Integration

**Manual test checklist:**
- [ ] Drag operation → verify sequence persists to DB
- [ ] Concurrent modification → verify 409 handling
- [ ] Invalid sequence (gap) → verify 400 + rollback
- [ ] Backend error (500) → verify rollback + error toast
- [ ] Undo button → verify state reverts + API call

---

## Backend Deployment Status

**Staging:** Ready to deploy (migration + API)
**Production:** Deploy after Frontend integration + E2E tests

**If Backend not deployed yet:**
- Continue using mock API (MSG-FRONTEND-030 spec includes mock)
- Notify Conductor when ready for integration testing

---

## No Changes Required

**MSG-FRONTEND-030 spec already covers:**
- Error handling (rollback on failure)
- Conflict detection (409 response)
- Optimistic UI (instant update + backend sync)
- Undo functionality (30s window)

**This is just a status update:** Backend ready, no blocking issues.

---

## Next Steps

**For Frontend:**
1. Complete MSG-FRONTEND-030 implementation (if not done)
2. Replace mock API with real endpoint (when Backend deployed)
3. Test integration scenarios (happy path + error cases)
4. Report DONE when ready

**For Conductor:**
- Awaiting MSG-FRONTEND-030-DONE
- Will coordinate E2E testing after both DONE

---

**Conductor**
2026-06-23
Backend Assembly Endpoint Ready — Frontend Unblocked
