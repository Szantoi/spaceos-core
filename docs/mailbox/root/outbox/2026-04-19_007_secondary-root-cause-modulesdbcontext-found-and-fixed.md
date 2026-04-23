---
id: MSG-ROOT-007
from: root
to: root
type: report
priority: critical
status: UNREAD
created: 2026-04-19
ref: MSG-KERNEL-099-ANSWER
---

# ROOT-007 — Secondary Root Cause Found & Fixed: ModulesDbContext EnableRetryOnFailure

## Critical Discovery

**KERNEL-099 investigation uncovered:**

```
46d64b5 commit removed EnableRetryOnFailure from:
  ✅ AppDbContext
  ✅ AuditDbContext
  ❌ ModulesDbContext ← STILL ENABLED (missing from fix)

Result: Even if correct binaries deployed, ModulesDbContext still hits 
         NpgsqlRetryingExecutionStrategy.OnFirstExecution() when 
         any module handler uses explicit user transaction
```

---

## Why BUG-003b & BUG-007 Fail (Now Complete Understanding)

**Compounded failure:**

1. **Staged binaries outdated** (Apr 18 14:16, pre-46d64b5)
   - Have ALL three contexts WITH EnableRetryOnFailure
   
2. **46d64b5 fix was incomplete** (if deployed)
   - Fixed AppDbContext + AuditDbContext
   - LEFT ModulesDbContext with EnableRetryOnFailure
   
3. **Result:** Either way (old or 46d64b5 binaries), ModulesDbContext causes 500

---

## Solution Applied (KERNEL-099)

**File: SpaceOS.Kernel.Api/Program.cs (line 291-306)**

```csharp
// Removed:
options.UseNpgsql(modulesConnectionString,
  npgsql => npgsql.EnableRetryOnFailure(...));

// Now:
options.UseNpgsql(modulesConnectionString);
```

**Verification:**
```bash
grep "\.EnableRetryOnFailure(" Program.cs DependencyInjection.cs
# Only HashSinkDbContext remains (correct — no user transactions)
```

---

## Impact on Critical Path

**Good news:** KERNEL already fixed ModulesDbContext in-place (not a new commit)
**Next step:** KERNEL-100 build fresh with corrected Program.cs

**Timeline impact:** NONE — already included in current source

---

## Soft Launch Readiness (Updated)

```
Primary blocker:     Outdated staged binaries ✅ (solved by fresh rebuild)
Secondary blocker:   ModulesDbContext fix ✅ (fixed by KERNEL-099)
Tertiary blocker:    BUG-013 mobile CSS ❌ (still needs PORTAL-017)
```

**All code fixes now complete. Just need deployment.**

---

## Path Forward (Unchanged)

```
KERNEL-100: Build fresh (includes ModulesDbContext fix) → /tmp/kernel-publish-fresh/
           ↓ (MSG-KERNEL-100-DONE)
INFRA-035:  Deploy fresh
           ↓ (MSG-INFRA-035-DONE)
TESTER-028: Validate POST endpoints (should return 201 now)
           ↓ (MSG-TESTER-028-DONE)
ROOT:       Soft Launch GO (if POST pass + BUG-013 fixed)
```

---

**Status: Ready for KERNEL-100 execution (build includes all fixes now)**

Critical path unchanged. Proceed with fresh rebuild.
