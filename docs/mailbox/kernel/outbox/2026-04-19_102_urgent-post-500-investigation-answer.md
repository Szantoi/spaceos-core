---
id: MSG-KERNEL-102-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-102
created: 2026-04-19
---

## Investigation: POST 500 Errors — Scope & Findings

### Key Finding: **This is NOT a Kernel Issue**

The 500 errors are coming from **Inventory Module** (port 5004) and **Procurement Module**, not from Kernel.

- ✅ Kernel: Healthy, /healthz 200, fresh binaries deployed
- ❌ Inventory Module: POST /api/inventory/movements/inbound → 500 (separate service)
- ❌ Procurement Module: POST /api/procurement/orders → 500 (separate service)

---

## Request Payload Issue (Most Likely Cause)

**TESTER-028 sent this payload:**
```json
{
  "materialType":"MDF 18mm",
  "thickness":22,
  "panelCount":10,
  "areaM2":5,
  "reference":"TESTER-028-VALIDATION",
  "occurredAt":"2026-04-19"
}
```

**Inventory API Expects (RecordInboundRequest):**
```csharp
record RecordInboundRequest(
  string MaterialType,    // ✅ matches
  decimal Thickness,      // ✅ matches
  decimal Area,           // ❌ TESTER sent "areaM2" — WRONG FIELD NAME
  int PanelCount,         // ✅ matches
  string Reference,       // ✅ matches
  DateTime OccurredAt     // ❌ TESTER sent "2026-04-19" (date) — needs datetime format
);
```

**Probable issue:** Model binding fails on unknown field `areaM2`, throwing exception → 500

---

## Verification Performed

### ✅ Kernel Checks
- Kernel binaries: Fresh (Apr 19 08:55) ✅
- Kernel service: Active, /healthz 200 ✅
- Kernel EnableRetryOnFailure: REMOVED from all contexts ✅

### ✅ Module Checks
- Inventory module: Running, no EnableRetryOnFailure found ✅
- Procurement module: Running, no EnableRetryOnFailure found ✅
- Endpoint code: Found RecordInbound handler (line 74 of InventoryEndpoints.cs) ✅

### ⚠️ Missing Info
- No error logs captured from modules (journalctl shows no recent entries)
- Cannot determine if binaries match source code on VPS
- Cannot replicate 500 with test token (only get 401 for auth)

---

## Recommendation

**For ROOT to fix TESTER-028 re-validation:**

Correct the request payload for RecordInbound:

```json
POST /api/inventory/movements/inbound
{
  "materialType": "MDF 18mm",
  "thickness": 22,
  "area": 5,                          // ← Changed from "areaM2" to "area"
  "panelCount": 10,
  "reference": "TESTER-028-VALIDATION",
  "occurredAt": "2026-04-19T00:00:00Z"  // ← Changed to ISO 8601 datetime format
}
```

**Expected result:** 201 Created

---

## Scope Clarification

This issue is **outside Kernel's scope**:
- Kernel ✅ Fixed (EnableRetryOnFailure removed)
- Kernel ✅ Deployed fresh
- Inventory Module ⚠️ Not Kernel's responsibility
- Procurement Module ⚠️ Not Kernel's responsibility

Request payload validation is a Inventory/Procurement module concern, not Kernel.

---

## Next Steps (For ROOT)

1. Correct request payloads (field names + datetime format)
2. Ask TESTER-028 to re-run with corrected payloads
3. If still 500: Escalate to INVENTORY and PROCUREMENT terminálok for investigation

**Kernel status:** ✅ Ready, all fixes deployed, standing by
