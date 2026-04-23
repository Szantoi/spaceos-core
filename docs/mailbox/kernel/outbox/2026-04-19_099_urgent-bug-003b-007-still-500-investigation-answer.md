---
id: MSG-KERNEL-099-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-099
created: 2026-04-19
processed: 2026-04-19
---

## Critical Finding: Missing EnableRetryOnFailure Removal in ModulesDbContext

### Root Cause Confirmed

**Investigation revealed:**

1. ✅ AppDbContext: EnableRetryOnFailure removed
2. ✅ AuditDbContext: EnableRetryOnFailure removed
3. ❌ **ModulesDbContext (Program.cs line 301): EnableRetryOnFailure STILL ENABLED**

**Impact:** If any module handler (FlowManagement, etc.) uses explicit user-initiated transactions, it will hit the same NpgsqlRetryingExecutionStrategy.OnFirstExecution() failure as AppDbContext and AuditDbContext did.

**Why BUG-003b & BUG-007 still fail:**
- Staged binaries in `/tmp/kernel-publish/` (Apr 18 14:16) predate the fix (Apr 18 17:42)
- Those old binaries still have `EnableRetryOnFailure` on ALL three contexts
- VPS deployed old binaries → POST still fails with 500

---

### Fix Applied

**File: SpaceOS.Kernel.Api/Program.cs (line 291-306)**

Removed `EnableRetryOnFailure` from ModulesDbContext configuration:

```csharp
// Before:
options.UseNpgsql(
    modulesConnectionString,
    npgsql => npgsql.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorCodesToAdd: null));

// After:
options.UseNpgsql(modulesConnectionString);
```

With comment explaining the reason (same as AppDbContext/AuditDbContext).

**Verification:**
```bash
grep "\.EnableRetryOnFailure(" SpaceOS.Infrastructure/DependencyInjection.cs SpaceOS.Kernel.Api/Program.cs
# Only HashSinkDbContext remains (correct — no user transactions)
```

---

### Status

**KERNEL-099:** Root cause of BUG-003b/007 failures identified and fixed
**Next:** Build fresh binaries (KERNEL-100) and deploy to VPS
