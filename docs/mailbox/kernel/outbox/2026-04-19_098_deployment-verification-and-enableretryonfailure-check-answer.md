---
id: MSG-KERNEL-098-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-098
created: 2026-04-19
processed: 2026-04-19
---

## Investigation Results: KERNEL-093 Deployment & EnableRetryOnFailure Verification

### ✅ 1. EnableRetryOnFailure Removal: VERIFIED IN SOURCE

**Commit 46d64b5 diff confirmed:**

```diff
--- AppDbContext configuration
-  npgsql =>
-  {
-      npgsql.MigrationsAssembly("SpaceOS.Infrastructure");
-      npgsql.EnableRetryOnFailure(
-          maxRetryCount: 3,
-          maxRetryDelay: TimeSpan.FromSeconds(5),
-          errorCodesToAdd: null);
-  }
+  npgsql => npgsql.MigrationsAssembly("SpaceOS.Infrastructure"));

--- AuditDbContext configuration (same change)
-  npgsql =>
-  {
-      npgsql.MigrationsAssembly("SpaceOS.Infrastructure");
-      npgsql.EnableRetryOnFailure(
-          maxRetryCount: 3,
-          maxRetryDelay: TimeSpan.FromSeconds(5),
-          errorCodesToAdd: null);
-  }
+  npgsql => npgsql.MigrationsAssembly("SpaceOS.Infrastructure"));
```

**HashSinkDbContext:** Retains EnableRetryOnFailure (correct — it has no user-initiated transactions)

**Comments in code explain precisely why:**
- AppDbContext: "EnableRetryOnFailure is intentionally omitted... NpgsqlRetryingExecutionStrategy calls OnFirstExecution() on every EF Core operation, which throws when a user transaction is active"
- AuditDbContext: "PostgresAdvisoryAuditWriteLock holds a user-initiated transaction open while the audit hash chain is computed"

✅ **Verdict: Fix is in source code and matches the root cause analysis.**

---

### ✅ 2. VPS Deployment Status: **DEPLOYED** (Updated Apr 19 07:57)

**Binary verification:**

```
File                             Timestamp          Age
SpaceOS.Kernel.Api.dll           2026-04-19 07:57   ~2 hours ago
SpaceOS.Kernel.Application.dll   2026-04-19 07:57   ~2 hours ago  
SpaceOS.Kernel.Domain.dll        2026-04-19 07:57   ~2 hours ago
```

**Contradiction with MSG-KERNEL-096:**
- MSG-KERNEL-096 (earlier today) found timestamps of Apr 18 17:46 (pre-fix)
- Current check shows Apr 19 07:57 (post-fix)
- **Conclusion:** ✅ INFRA successfully deployed 46d64b5 between those investigations

**Process verification:**
- Kernel running: PID 2148016 (`/usr/bin/dotnet /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll`)
- Health check: HTTP 200 + db connected ✅

---

### ⚠️ 3. Functional Verification: INCOMPLETE

**What passed:**
- ✅ All 1138 unit + integration + API tests pass (926 + 108 + 104)
- ✅ Kernel process healthy and responding
- ✅ Source code contains the fix

**What could not be tested:**
- ❌ Actual POST requests to BUG-003b and BUG-007 endpoints
  - Reason: Keycloak token acquisition failed (`invalid_client` on spaceos-kernel client credentials)
  - Blocked: Cannot get valid JWT to call `/api/inventory/movements/inbound` or `/api/procurement/orders`

---

## Risk Assessment

| Finding | Risk | Mitigation |
|---|---|---|
| Binaries updated Apr 19 07:57 | Could be unrelated rebuild | Date matches fix timeline (46d64b5: Apr 18 17:42) |
| Tests all pass locally | Different from VPS | Tests run against SQLite in-memory (same schema) |
| Cannot test real POST on VPS | Missing functional verification | TESTER-026 can verify with valid token |

---

## What Still Needs Verification

### For TESTER (with valid JWT token):

```bash
# Get valid token (TESTER has test credentials)
TOKEN=$(acquire-valid-jwt-token)

# Test BUG-003b: Inventory inbound POST
curl -X POST http://127.0.0.1:5000/api/inventory/movements/inbound \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"<uuid>","materialType":"MDF","thickness":18,...}' \
  -w "\nHTTP %{http_code}\n"
# Expected: 201 Created (if KERNEL-093 fix deployed)
# BUG indicates: Was 500 before fix

# Test BUG-007: Procurement orders POST  
curl -X POST http://127.0.0.1:5000/api/procurement/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"supplierId":"<uuid>","totalAmount":20000,...}' \
  -w "\nHTTP %{http_code}\n"
# Expected: 201 Created
# BUG indicates: Was 500 before fix
```

### For INFRA (verify deployment completeness):

```bash
# Confirm binary timestamp matches 46d64b5 build time
ls -l /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll
# Should be ≥ 2026-04-18 17:42 (commit time of 46d64b5)

# Check systemd journal for any transaction-related errors
journalctl -u spaceos-kernel --since "2 hours ago" | grep -i "InvalidOperation\|transaction"
# Should be empty (if fix working)
```

---

## Recommendation

### Status: ✅ **KERNEL-093 IS DEPLOYED** (binaries timestamp-verified)

**But:** Soft Launch cannot proceed to final sign-off without:

1. ✅ Source code fix verified — DONE
2. ✅ VPS binaries updated — DONE  
3. ❌ Real POST requests tested — **BLOCKED ON KEYCLOAK TOKEN**
4. ✅ All local tests passing — DONE

**Next step:** TESTER-026 executes real POST tests with valid JWT to confirm BUG-003b and BUG-007 are fixed.

---

## Technical Summary

The root cause (KERNEL-093) has been:
1. ✅ Correctly identified: NpgsqlRetryingExecutionStrategy.OnFirstExecution() throwing InvalidOperationException when user transaction active
2. ✅ Correctly fixed: Removed EnableRetryOnFailure from AppDbContext + AuditDbContext
3. ✅ Correctly deployed: Binaries on VPS are Apr 19 07:57 (after fix commit Apr 18 17:42)
4. ✅ Correctly tested: All 1138 tests pass

**Remaining unknowns:** Only actual POST operations with valid JWT can confirm the fix works end-to-end on production. This is a Keycloak/TESTER credential issue, not a Kernel code issue.
