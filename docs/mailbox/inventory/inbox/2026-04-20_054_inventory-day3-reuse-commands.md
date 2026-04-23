---
id: MSG-INVENTORY-054
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-054-DAY2-DONE
created: 2026-04-20
---

# INVENTORY-054 — Day 3: Reuse Commands (Reserve, Approve, Use)

## Day 2 Accepted ✅

- CuttingJobCompletedEvent + Handler ✅
- 125/125 tests ✅
- Design note acknowledged: event in Application layer (correct)

---

## Day 3 Task: 3 Reuse Commands

### 1. ReserveOffcutCommand

```csharp
public record ReserveOffcutCommand(Guid OffcutId, Guid JobId) : IRequest<ReserveOffcutResponse>;
public record ReserveOffcutResponse(Guid ReservationId, DateTime ExpiresAt);

// Handler:
// 1. Load Offcut — 404 if not found
// 2. Guard: Offcut.Status == Available → 409 if not
// 3. Create OffcutReservation(offcutId, jobId, expiresAt: now+7d)
// 4. offcut.Reserve() → status = Reserved (or leave Available until Approved?)
// 5. Save, return reservationId + expiresAt
```

### 2. ApproveOffcutReservationCommand

```csharp
public record ApproveOffcutReservationCommand(Guid ReservationId) : IRequest<ApproveOffcutReservationResponse>;
public record ApproveOffcutReservationResponse(string Status);

// Handler:
// 1. Load OffcutReservation — 404 if not found
// 2. Guard: not expired → 410 if expired
// 3. reservation.Approve()
// 4. Load Offcut, offcut.Reserve() (status → Reserved)
// 5. Save, return "Approved"
```

### 3. UseOffcutInJobCommand

```csharp
public record UseOffcutInJobCommand(Guid OffcutId, Guid JobId) : IRequest<UseOffcutInJobResponse>;
public record UseOffcutInJobResponse(string Status, Guid UsedInJobId, DateTime UsedAt);

// Handler:
// 1. Load Offcut — 404 if not found
// 2. Guard: Status == Reserved → 409 if not
// 3. offcut.MarkUsed(jobId)
// 4. Save, return "Used" + usedAt
```

---

## Validation & Error Codes

| Scenario | HTTP |
|---|---|
| Offcut not found | 404 |
| Offcut not Available (Reserve) | 409 |
| Reservation not found | 404 |
| Reservation expired | 410 |
| Offcut not Reserved (Use) | 409 |

---

## Tests (8+)

- Reserve: success, 404, 409 (not available)
- Approve: success, 404, 410 (expired)
- Use: success, 404, 409 (not reserved)
- Full lifecycle: Reserve → Approve → Use (1 test)

---

## Acceptance

- ✅ 3 commands implemented + registered in DI
- ✅ 8+ tests
- ✅ Total tests ≥ 133
- ✅ Build green

---

## Next

Day 3 DONE → outbox: `2026-04-20_055_inventory-day3-reuse-commands-done.md`

Then Day 4: HTTP endpoints (6 routes).
