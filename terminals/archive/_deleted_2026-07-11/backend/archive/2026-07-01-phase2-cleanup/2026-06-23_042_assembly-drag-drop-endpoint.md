---
id: MSG-BACKEND-042
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-23_1105_consensus.md
created: 2026-06-23
completed: 2026-06-23
content_hash: 4b17ceaa82cf6e836dc42c61df56b715ebc5348ff4e54e238e67fc94d8cd6f2b
---

# Assembly Drag-and-Drop Backend Implementation

## Context

**Consensus source:** `docs/planning/queue/2026-06-23_1105_consensus.md`
**Business value:** HIGH - Workflow optimization for assembly planning
**Priority:** 1 of 3 features (highest priority)
**Estimated effort:** 3 days (backend portion of 5-day total)

## Objective

Implement backend support for reordering assembly operations via drag-and-drop. Users need to optimize assembly sequences in real-time, and the backend must:
1. Persist sequence changes
2. Detect conflicts (optimistic locking)
3. Validate business rules
4. Calculate duration impact

## API Specification

### Endpoint

```
PATCH /api/v1/work-orders/{id}/assembly-sequence
```

### Request Body

```json
{
  "operations": [
    { "id": "op-1", "sequence": 1 },
    { "id": "op-2", "sequence": 2 },
    { "id": "op-3", "sequence": 3 }
  ],
  "timestamp": "2026-06-23T10:30:00Z"
}
```

**Fields:**
- `operations`: Array of operation ID + new sequence number
- `timestamp`: ISO 8601 timestamp for optimistic locking

### Response (200 OK)

```json
{
  "updated_operations": [
    {
      "id": "op-1",
      "sequence": 1,
      "description": "Cut wood panels",
      "estimated_duration": "PT30M",
      "last_modified": "2026-06-23T10:30:01Z"
    }
  ],
  "estimated_duration_change": "+15min",
  "total_duration": "PT2H45M"
}
```

**Fields:**
- `updated_operations`: Full operation objects with updated sequences
- `estimated_duration_change`: Human-readable delta (e.g., "+15min", "-5min")
- `total_duration`: ISO 8601 duration for entire work order

### Error Responses

**409 Conflict** - Concurrent modification detected:
```json
{
  "error": "CONCURRENT_MODIFICATION",
  "message": "Work order was modified by another user. Please refresh and try again.",
  "latest_timestamp": "2026-06-23T10:30:05Z"
}
```

**400 Bad Request** - Validation failures:
```json
{
  "error": "VALIDATION_FAILED",
  "message": "Invalid sequence numbers",
  "details": [
    { "field": "operations[2].sequence", "error": "Gap detected: sequence jumps from 2 to 4" }
  ]
}
```

**404 Not Found** - Work order or operation not found
**422 Unprocessable Entity** - Work order completed (cannot modify)

## Data Model Changes

### WorkOrderOperation (C#)

```csharp
public class WorkOrderOperation
{
    public string Id { get; set; }

    // ✅ NEW FIELD - Migration required
    public int Sequence { get; set; }

    // ✅ NEW FIELD - Optimistic locking
    public DateTime LastModified { get; set; }

    // Existing fields
    public string WorkOrderId { get; set; }
    public string Description { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public string OperationType { get; set; }
}
```

### Migration Script

```sql
-- Add new columns to WorkOrderOperations table
ALTER TABLE "WorkOrderOperations"
  ADD COLUMN "Sequence" INTEGER NOT NULL DEFAULT 0,
  ADD COLUMN "LastModified" TIMESTAMP NOT NULL DEFAULT NOW();

-- Populate sequence numbers for existing operations (ordered by created date)
UPDATE "WorkOrderOperations" AS wo
SET "Sequence" = subquery.row_num
FROM (
  SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "WorkOrderId" ORDER BY "CreatedAt") AS row_num
  FROM "WorkOrderOperations"
) AS subquery
WHERE wo."Id" = subquery."Id";

-- Create index for faster sequence lookups
CREATE INDEX "IX_WorkOrderOperations_WorkOrderId_Sequence"
  ON "WorkOrderOperations" ("WorkOrderId", "Sequence");
```

## Validation Rules

Implement these validations in the endpoint handler:

1. **Operation IDs exist**
   - Every `operation.id` must belong to the work order
   - Return 400 if unknown operation ID

2. **Sequence continuity**
   - Sequences must be [1, 2, 3, ..., N] (no gaps)
   - Return 400 with "Gap detected" error

3. **Work order status**
   - Work order must NOT be in "completed" status
   - Return 422 if completed

4. **Optimistic locking**
   - Compare request timestamp with `LastModified` of ALL operations
   - If ANY operation was modified after request timestamp → 409 Conflict
   - Return latest timestamp in error response

5. **Future: Business rule validation** (Phase 2, not now)
   - Example: "painting" cannot come before "priming"
   - Skip for initial implementation

## Implementation Steps

### 1. Create Migration (Day 1, 1 hour)

**File:** `spaceos-backend/migrations/YYYYMMDDHHMMSS_AddSequenceToWorkOrderOperations.cs`

- Add `Sequence` and `LastModified` columns
- Populate existing data with sequential numbers
- Create index

**DoD:**
- [ ] Migration runs successfully
- [ ] Existing operations have valid sequence numbers
- [ ] Index created

### 2. Update Domain Model (Day 1, 30 min)

**File:** `spaceos-backend/Domain/Entities/WorkOrderOperation.cs`

- Add `Sequence` property
- Add `LastModified` property
- Update constructor/factory methods

**DoD:**
- [ ] Properties added
- [ ] No compilation errors

### 3. Implement Endpoint (Day 1-2, 4 hours)

**File:** `spaceos-backend/API/Controllers/WorkOrdersController.cs`

**Method signature:**
```csharp
[HttpPatch("{id}/assembly-sequence")]
public async Task<IActionResult> UpdateAssemblySequence(
    string id,
    [FromBody] UpdateAssemblySequenceRequest request
)
```

**Request DTO:**
```csharp
public record UpdateAssemblySequenceRequest(
    List<OperationSequenceUpdate> Operations,
    DateTime Timestamp
);

public record OperationSequenceUpdate(string Id, int Sequence);
```

**Implementation logic:**
1. Load work order + operations
2. Validate work order status != completed
3. Validate all operation IDs exist
4. Validate sequence continuity (1..N, no gaps)
5. Check optimistic locking (timestamp vs LastModified)
6. Update sequences in DB (single transaction)
7. Calculate duration change (optional - can return "+0min" initially)
8. Return updated operations

**DoD:**
- [ ] Endpoint handles PATCH request
- [ ] All validations implemented
- [ ] Returns 200 with correct response format
- [ ] Returns 409 on conflict
- [ ] Returns 400 on validation errors
- [ ] Returns 422 if work order completed

### 4. Write Integration Tests (Day 2, 3 hours)

**File:** `spaceos-backend/Tests/Integration/WorkOrdersControllerTests.cs`

**Test cases:**
```csharp
[Fact]
public async Task UpdateAssemblySequence_ValidRequest_Returns200()

[Fact]
public async Task UpdateAssemblySequence_ConcurrentModification_Returns409()

[Fact]
public async Task UpdateAssemblySequence_GapInSequence_Returns400()

[Fact]
public async Task UpdateAssemblySequence_UnknownOperationId_Returns400()

[Fact]
public async Task UpdateAssemblySequence_CompletedWorkOrder_Returns422()

[Fact]
public async Task UpdateAssemblySequence_CalculatesDurationChange()
```

**DoD:**
- [ ] All 6 tests passing
- [ ] Test coverage > 85% for endpoint logic

### 5. Duration Calculation (Day 3, 2 hours - Optional Enhancement)

**File:** `spaceos-backend/Domain/Services/AssemblyDurationCalculator.cs`

Calculate `estimated_duration_change` based on operation dependencies:
- If critical path changes → calculate impact
- Otherwise → "+0min"

**For initial implementation, return "+0min" and log a TODO**

**DoD:**
- [ ] Basic calculation implemented (can be "+0min" stub)
- [ ] TODO logged for advanced dependency-based calculation

## Security Considerations

1. **Authorization**
   - Only users with `work_order:write` permission can reorder
   - Use existing RBAC middleware

2. **Tenant isolation**
   - Verify work order belongs to user's tenant
   - Use existing RLS policies

3. **Input validation**
   - Sequence numbers: positive integers only
   - Timestamp: valid ISO 8601 format
   - Operation IDs: UUID format

4. **Rate limiting**
   - Use existing API rate limiter (100 req/min per user)

## Definition of Done

- [ ] Migration created and tested
- [ ] Domain model updated (Sequence, LastModified)
- [ ] Endpoint implemented with all validations
- [ ] Integration tests passing (6 tests minimum)
- [ ] Manual smoke test: Postman/curl request works
- [ ] API documentation updated (Swagger)
- [ ] No new compiler warnings
- [ ] Code review approved (self-review OK for now)

## Testing Plan

### Manual Testing

```bash
# 1. Create test work order with 3 operations
POST /api/v1/work-orders
{
  "title": "Test Assembly",
  "operations": [
    { "description": "Cut", "estimated_duration": "PT30M" },
    { "description": "Sand", "estimated_duration": "PT20M" },
    { "description": "Paint", "estimated_duration": "PT45M" }
  ]
}

# 2. Reorder operations (reverse order)
PATCH /api/v1/work-orders/{id}/assembly-sequence
{
  "operations": [
    { "id": "op-3", "sequence": 1 },
    { "id": "op-2", "sequence": 2 },
    { "id": "op-1", "sequence": 3 }
  ],
  "timestamp": "2026-06-23T15:00:00Z"
}

# Expected: 200 OK with updated sequences

# 3. Conflict test (stale timestamp)
PATCH /api/v1/work-orders/{id}/assembly-sequence
{
  "operations": [...],
  "timestamp": "2026-06-23T14:00:00Z"  // Old timestamp
}

# Expected: 409 Conflict
```

## Notes

- **Duration calculation enhancement** can be deferred to Phase 2
- **Business rule validation** (operation dependencies) is future work
- **Frontend integration** handled by MSG-FRONTEND-030 (parallel track)
- **Multi-tenant isolation** already handled by existing RLS

## Questions

None - spec is complete. Proceed with implementation.

---

**Conductor**
2026-06-23
Consensus priority 1/3
