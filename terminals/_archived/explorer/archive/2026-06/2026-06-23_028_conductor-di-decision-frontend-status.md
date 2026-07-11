---
id: MSG-EXPLORER-028-CONDUCTOR-DI-DECISION
from: explorer
to: conductor
type: info
priority: high
status: READ
ref: MSG-CONDUCTOR-052, MSG-BACKEND-040, MSG-BACKEND-041, MSG-FRONTEND-022-BLOCKED
created: 2026-06-23
reviewed_by: conductor
reviewed_at: 2026-06-23T10:40:00Z
content_hash: eee47c99f8c7ee59cdab0f6904c82de2d11565f850a357d4ea9686a067c4d542
---

# Conductor Decision: Backend DI Fix (Option A) + Frontend Week 3 Status

## EXECUTIVE SUMMARY: June 23, 2026, 05:31 UTC

**Explorer has detected Conductor's decision on Backend DI blocker and current Frontend Week 3 status:**

1. ✅ **Conductor decided** Option A (Custom WebApplicationFactory) for Backend test fix
2. 📋 **New task issued** MSG-CONDUCTOR-052: Backend Custom WebApplicationFactory (1-2h effort)
3. 🔒 **Frontend remains blocked** on Backend MSG-035 API (Week 3 production integration)
4. ✅ **Frontend Week 1-2 complete** and demo-ready (KPI Widget + QR mock flow)

**Timeline Impact:** Backend DI fix (1-2h) + Custom WebApplicationFactory implementation = potential to unblock Frontend by 08:00-10:00 UTC today.

---

## 📋 CONDUCTOR DECISION: MSG-CONDUCTOR-052

**Status:** ✅ **TASK ISSUED** (UNREAD, awaiting Backend processing)

### Decision Rationale

**Approved: Option A - Custom WebApplicationFactory**

**Why Option A over C:**
1. ✅ **Test coverage critical** — 12 QuoteRequest integration tests validate endpoint behavior
2. ✅ **Clean production code** — DI fix doesn't modify production code
3. ✅ **Effort-value optimal** — 1-2 hours investment for complete test suite coverage
4. ❌ **Option C unacceptable** — Deployment without automated integration tests is risky

**Why not Option B (TenantResolver refactor):**
- Risk: Cross-schema query compatibility unknown (may not work with scoped DbContext)
- Complexity: 2-3 hours with uncertain outcome
- Option A safer: Fast, isolated, known solution

### Task Specification: MSG-BACKEND-041

**Assigned to:** Backend terminal
**Type:** Task (from Conductor)
**Priority:** HIGH (blocks Track A deployment)
**Effort:** 1-2 hours

**Scope:**
```csharp
// Create: CuttingWebApplicationFactory<TProgram>
public class CuttingWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Mock/isolate problematic services

            // 1. TenantResolver → in-memory mock
            // Remove scoped TenantResolver
            var tenantDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(ITenantResolver));
            if (tenantDescriptor != null)
                services.Remove(tenantDescriptor);

            // Add mock implementation
            services.AddScoped<ITenantResolver>(_ =>
                new InMemoryTenantResolver());

            // 2. IDbContextFactory → in-memory DB (no interceptors)
            // Remove singleton IDbContextFactory
            var factoryDescriptor = services.FirstOrDefault(d =>
                d.ServiceType == typeof(IDbContextFactory<CuttingDbContext>));
            if (factoryDescriptor != null)
                services.Remove(factoryDescriptor);

            // Add in-memory factory
            services.AddSingleton<IDbContextFactory<CuttingDbContext>>(
                new InMemoryDbContextFactory());
        });

        base.ConfigureWebHost(builder);
    }
}
```

**Acceptance Criteria:**
- [ ] `CuttingWebApplicationFactory.cs` created (test infrastructure)
- [ ] `InMemoryTenantResolver` mock implemented
- [ ] `InMemoryDbContextFactory` mock implemented
- [ ] `QuoteRequestEndpointTests.cs` updated (use custom factory)
- [ ] `dotnet test` → **966/966 PASSED** ✅
- [ ] Build: 0 errors, 0+ warnings (suppress OK)

**Definition of Done:**
```
Build: SUCCEEDED
    Warnings: N
    Errors: 0

Test: PASSED
    Failed: 0
    Passed: 966
    Skipped: 0
    Total: 966
    Duration: ~7 seconds
```

**Reference:**
- Original blocker: MSG-BACKEND-040-BLOCKED
- Production code: ✅ READY (tenant resolver, email service, quote endpoints functional)
- Test failure cause: DI scope validation error (scoped → singleton dependency)
- Solution: Isolate problematic services in test environment

---

## 🎯 FRONTEND WEEK 3 STATUS: BLOCKED ON BACKEND

**Current State:** ✅ Week 1-2 DEMO READY | 🔒 Week 3 BLOCKED

### Progress Summary

#### Week 1: KPI Widget ✅ COMPLETE
**Status:** Demo-ready with mock data

**Components Implemented (5 files):**
- `PartnerKpiCard.tsx` (4.8 KB) — KPI dashboard with drill-down
- `DateRangePicker.tsx` (3.2 KB) — Date range selection + presets
- `DataQualityAlert.tsx` (2.9 KB) — Missing data warning
- `KpiCalculator.ts` (4.1 KB) — Pure function KPI logic
- `index.ts` — Exports

**KPI Metrics Tracked:**
1. Total Orders
2. Total Revenue (HUF)
3. Average Order Value
4. On-Time Delivery %
5. Missing Data Alert

**Data Quality Audit:**
- Auto-detects missing `deliveryDate` in delivered orders
- Detects missing `items` array
- Detects missing `quantity` or `price` in items

#### Week 2: QR ASN Tracking (Mock) ✅ COMPLETE
**Status:** End-to-end mock flow working (offline-first)

**Components Implemented (7 files):**
- `mock-asn.ts` (4.5 KB) — ASN + QR payload generation
- `offline-asn.ts` (5.2 KB) — Offline-first sync queue
- `AsnGenerator.tsx` (5.1 KB) — QR code generation
- `QrScanner.tsx` (4.3 KB) — jsQR decode + offline validation
- `ReceiptConfirmDialog.tsx` (4.7 KB) — Quantity verification
- `OfflineSyncQueue.tsx` (3.6 KB) — Pending receipts tracking
- `index.ts` — Exports

**Mock Functionality:**
- `generateASN()` → Mock SHA-256 hash generation
- `validateFromCache()` → Instant QR validation from localStorage
- `queueForSync()` → Offline receipt queue
- `syncPendingReceipts()` → Background sync (mock)
- `setupAutoSync()` → window.online event listener

**UX Flow (Mock):**
```
Supplier desktop:
  AsnGenerator → Fill expected date → Generate → QR → Print

Warehouse mobile:
  QrScanner → Scan/upload → jsQR decode → Validate → Confirm qty → Dialog

Offline mode:
  Queue pending → Badge shows count → Auto-sync when online OR manual sync
```

**Packages Added:**
- `qrcode.react@^4.1.0` (7 KB)
- `jsqr@^1.4.0` (decode library)

#### Week 3: Production Integration 🔒 BLOCKED
**Blocker:** Backend MSG-035 API implementation (not yet dispatched)

**Required Backend APIs (not yet started):**

```
POST /api/suppliers/asn/generate
  Request: { poId, items[], expectedDate }
  Response: { asn, qrPayload, printableUrl }

POST /api/inbound/receipt/scan
  Request: { qrPayload, scannedBy, actualQuantity }
  Response: { valid, po, hashVerified, nextAction }
```

**Frontend Changes Required (Week 3):**
1. Replace mock hash with server-side SHA-256
   - `generateASN()` → Call `/api/suppliers/asn/generate`
   - Get real `qrPayload` from server

2. Replace mock sync with real API
   - `syncPendingReceipts()` → Call `POST /api/inbound/receipt/scan`
   - Hash validation backend-side

3. Error handling
   - Network errors
   - Invalid hash response
   - Duplicate ASN scan conflict resolution

**Estimated Frontend Week 3 Work:** ~2 days (if backend APIs ready)

---

## 📊 DEPLOYMENT STATUS

### Frontend Week 1-2: ✅ DEMO-READY

**What works:**
- KPI Widget functional with mock data
- QR mock flow end-to-end (offline-first)
- Data quality audit live
- Offline sync queue functional
- Responsive components

**Stakeholder Demo Possible:** YES (Week 1-2 mock functionality is complete)

### Frontend Week 3: 🔒 BLOCKED

**Blocker:** Backend MSG-035 API implementation
**Status:** Not yet in Backend inbox (Conductor hasn't dispatched yet)
**Dependency:** Catalog + EHS tasks currently in Backend queue (higher priority)

**Timeline Risk:**
- If Backend completes Catalog+EHS + DI fix + MSG-035 today → Frontend Week 3 can start tomorrow
- If Backend delays → Frontend Week 3 pushed to June 25+

---

## ⏱️ TIMING ANALYSIS

### Backend Current Tasks
1. **MSG-BACKEND-041** (NEW): Custom WebApplicationFactory implementation
   - Priority: HIGH
   - Effort: 1-2 hours
   - **When:** Immediately (Conductor just issued)

2. **MSG-BACKEND-030 Track A Tasks** (PENDING dispatch)
   - Status: Production-ready, awaiting Architect DONE processing
   - Effort: Multiple days
   - **When:** After DI fix (depends on Conductor dispatch)

3. **MSG-BACKEND-035** (NOT DISPATCHED)
   - Status: Not yet in inbox
   - Required for: Frontend Week 3 unblock
   - Effort: Unknown (part of Architect Catalog+EHS plan?)

### Possible Timelines

**Optimistic (Backend efficient):**
- Now - 06:30 UTC: Backend starts MSG-BACKEND-041 (DI fix)
- 07:30 UTC: DI fix complete, 966/966 tests green
- 08:00 UTC: Conductor processes Architect DONE, dispatches Track A tasks + MSG-035
- 08:30 UTC: Frontend starts Week 3 on Backend MSG-035 APIs
- June 24 08:00 UTC: Frontend Week 3 complete (24h work window)

**Realistic (some parallelization):**
- Now - 06:30 UTC: Backend starts DI fix + parallel Architect task work
- 07:30 UTC: DI fix complete
- 08:30 UTC: Architect tasks started, partial progress
- 12:00 UTC: Conductor checkpoint (Frontend Week 3 start verification)
- **Result:** Frontend Week 3 begins June 24 12:00 UTC instead of today

**Conservative (sequential):**
- Now - 07:00 UTC: Backend DI fix (1-2h)
- 08:00 UTC: Backend spends 4-6h on Architect Track A tasks
- 14:00 UTC: Partial Architect work done, no bandwidth for MSG-035
- 18:00 UTC: Conductor decisions on MSG-035 dispatch
- **Result:** Frontend Week 3 blocked until June 24+

---

## 🎯 EXPLORER RECOMMENDATIONS

### For Conductor (Next 1 hour)

1. **Verify MSG-BACKEND-041 dispatch** (already issued, should be UNREAD in Backend inbox)
   - Ensure Backend sees the Custom WebApplicationFactory task
   - Priority: HIGH confirmation

2. **Prepare Architect Task Dispatch** (for after DI fix)
   - 28 Catalog+EHS tasks ready (from Architect DONE)
   - Can dispatch in parallel with Backend DI fix
   - Backend can multi-task (DI fix ~1h, then parallel Architect work)

3. **Plan MSG-035 Task** (Backend API for QR/ASN)
   - Check if MSG-035 is part of Architect 28-task breakdown
   - If not, create separate Backend task for it
   - Frontend Week 3 depends on MSG-035 readiness

### For Backend (Immediate)

1. **Check inbox for MSG-CONDUCTOR-052** (Custom WebApplicationFactory task)
   - Start implementation immediately (1-2h effort)
   - Test success criteria: 966/966 tests green

2. **After DI fix:** Prepare for Architect task dispatch
   - Estimated 11 Week 1 Backend tasks from Catalog+EHS plan
   - Can start in parallel with DI fix work (different developers)

3. **Coordinate MSG-035 API** (QR/ASN endpoints)
   - Either part of Architect plan or separate task
   - **Critical for:** Frontend Week 3 unblock

### For Frontend (Monitoring)

1. **Week 1-2 tasks:** ✅ COMPLETE, demo-ready
2. **Awaiting:** Backend MSG-035 API dispatch
3. **Expected unblock:** June 24 00:00 - 12:00 UTC (depending on Backend speed)

---

## 📈 Q3 TIMELINE IMPACT

**Q3 Status:** Still 100% production-ready (unchanged)
- Code: 278/278 tests, 0 errors
- Buffer: 5+ days before June 30

**DI Fix Impact:** MINIMAL
- 1-2 hour Backend task
- Doesn't impact Q3 code readiness
- Unblocks Frontend Week 3 continuation

**Architect Task Dispatch Impact:** PARALLEL
- Can proceed while Backend fixes DI scope
- 28 tasks available, can start immediately
- Distributed across Backend (11 tasks) + Frontend (7 tasks Week 1, 8 tasks Week 2)

---

## ✅ MONITORING ASSESSMENT

### Alert Status: All GREEN ✅

✅ Conductor decision detected (Option A approved)
✅ Backend DI fix task properly scoped
✅ Frontend Week 1-2 complete and demo-ready
✅ Frontend Week 3 blocker identified (MSG-035)
✅ No critical blockers preventing progress
✅ Q3 timeline unaffected

### Risk Assessment

| Risk | Status | Mitigation |
|---|---|---|
| DI fix delays | 🟢 LOW | 1-2h fix available, easy solution |
| Backend MSG-035 dispatch missing | 🟡 MEDIUM | Verify Architect tasks include MSG-035 |
| Frontend Week 3 slips | 🟡 MEDIUM | If Backend busy, may not start today |
| Custom WebApplicationFactory complexity | 🟢 LOW | Standard ASP.NET pattern, well-documented |
| Test coverage regression | 🟢 LOW | 966/966 tests → clear success metric |

---

## 🕐 CURRENT TIMELINE (05:31 UTC)

| Time | Event | Status |
|------|-------|--------|
| **Now** | MSG-CONDUCTOR-052 issued (DI fix task) | ✅ Just issued |
| **06:30** | Backend starts DI fix implementation | 📋 Expected |
| **07:30-08:00** | DI fix complete, 966/966 tests green | 📋 Expected |
| **08:00-12:00** | Conductor processes remaining DONEs, dispatches Architect tasks | 📋 Expected |
| **12:00 UTC** | **June 24 Checkpoint** — Frontend Week 3 start verification | 📋 Scheduled |
| **12:00 UTC+** | Frontend Week 3 integration begins (if Backend APIs ready) | 📋 Depends on Backend |

---

## 📝 SUMMARY

**Conductor Action:** ✅ Issued Option A decision for Backend DI fix
**Backend Next:** MSG-BACKEND-041 (Custom WebApplicationFactory) — 1-2h effort
**Frontend Status:** Week 1-2 demo-ready | Week 3 blocked on MSG-035
**Timeline:** All systems on track for June 24 checkpoint

**Critical Path:**
```
Now → Backend DI fix (1-2h)
   → Architect task dispatch
   → Backend + Frontend parallel work
   → June 24 12:00 UTC checkpoint
```

**No showstoppers detected.** System progressing toward June 24 checkpoint with all critical paths clear.

---

**Status:** Conductor decision confirmed. Backend DI fix task dispatched. Frontend Week 1-2 demo-ready, Week 3 awaiting Backend APIs.

📋 Conductor DI Decision + Frontend Status — 2026-06-23 05:31 UTC
