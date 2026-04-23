---
id: MSG-INVENTORY-056
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INVENTORY-056-DAY4-DONE
created: 2026-04-20
---

# INVENTORY-056 — Day 5: E2E Validation + Final DONE

## Day 4 Accepted ✅

- 6 HTTP endpoints ✅
- 3 query handlers (list, detail, stats) ✅
- 410 expiry via Results.Problem ✅
- 147/147 tests ✅

---

## Day 5 Task: E2E Validation

### Full Offcut Lifecycle Test (manual curl vagy dotnet test)

```bash
BASE="http://localhost:5004"
TOKEN=$(curl -s -X POST http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/token \
  -d 'grant_type=password&client_id=portal-app&username=test-admin&password=Test1234!&scope=openid' \
  | jq -r '.access_token')

# 1. Simulate CuttingJobCompleted event (MediatR publish via test endpoint or unit)
# 2. GET /api/inventory/offcuts → offcut megjelenik (status: Available)
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/inventory/offcuts" | jq '.offcuts[0]'

# 3. POST reserve
OFFCUT_ID="<id from step 2>"
RESERVATION=$(curl -s -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"jobId\": \"$(uuidgen)\"}" \
  "$BASE/api/inventory/offcuts/$OFFCUT_ID/reserve")
RESERVATION_ID=$(echo $RESERVATION | jq -r '.reservationId')

# 4. POST approve
curl -s -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"reservationId\": \"$RESERVATION_ID\"}" \
  "$BASE/api/inventory/offcuts/$OFFCUT_ID/approve-reservation"

# 5. POST use
curl -s -X POST -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"jobId\": \"$(uuidgen)\"}" \
  "$BASE/api/inventory/offcuts/$OFFCUT_ID/use"

# 6. GET stats
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/inventory/offcuts/stats/summary" | jq .
```

**Elvárt eredmények:**
- Step 2: offcut status = Available ✅
- Step 3: 201 + reservationId ✅
- Step 4: 200 + status: Approved ✅
- Step 5: 200 + status: Used ✅
- Step 6: usedCount: 1 ✅

---

## Final DONE Outbox

Ha minden E2E lépés PASS → küld:

**File:** `docs/mailbox/inventory/outbox/2026-04-20_057_inventory-phase1-done.md`

```yaml
id: MSG-INVENTORY-051-DONE
type: done
```

**Tartalom:**
- Teljes Phase 1 összefoglaló
- Végső tesztszám
- Commit lista (c022043, ae23cf8, f68441d, 5e99f6d, + Day 5 commit)
- E2E lifecycle: PASS / FAIL
- Security review (végső)
- Phase 2 javaslat (ha van)

---

## Acceptance Criteria (Phase 1 Complete)

- ✅ EF migration + RLS live
- ✅ Event handler (stub): CuttingJobCompleted → Offcut
- ✅ Reuse workflow: Reserve → Approve → Use
- ✅ 6 HTTP endpoints
- ✅ E2E lifecycle PASS
- ✅ 147+ tests
- ✅ Build green

---

**Ez az utolsó nap. Küld MSG-INVENTORY-051-DONE ha minden PASS.**
