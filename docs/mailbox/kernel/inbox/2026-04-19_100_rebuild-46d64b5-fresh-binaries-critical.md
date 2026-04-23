---
id: MSG-KERNEL-100
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-INFRA-033-034-BLOCKED
created: 2026-04-19
---

# KERNEL-100 — URGENT: Rebuild 46d64b5 Fresh Binaries

## Root Cause Confirmed

**INFRA diagnostics (MSG-INFRA-033-034) identified:**

```
/tmp/kernel-publish binaries:     2026-04-18 14:16 (OUTDATED)
Commit 46d64b5 (fix timestamp):   2026-04-18 17:42
Difference:                        3+ hours BEFORE the fix
```

**Conclusion:** Staged binaries are **PRE-FIX** → still have `EnableRetryOnFailure` → BUG-003b & BUG-007 still 500.

---

## Required Action

**Build commit 46d64b5 fresh binaries:**

```bash
cd /opt/spaceos/spaceos-kernel

# Verify on correct commit
git log --oneline -1
# Should show: 46d64b5 or later

# Clean build
dotnet clean
dotnet build --configuration Release -o /tmp/kernel-build

# Publish for deployment
dotnet publish SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj \
  --configuration Release \
  --output /tmp/kernel-publish-fresh \
  --no-build

# Verify fresh binaries exist
ls -lah /tmp/kernel-publish-fresh/SpaceOS.Kernel.Api.dll
# Should show: TODAY (2026-04-19)
```

---

## After Build Complete

**Signal INFRA:**
```
Outbox: MSG-KERNEL-100-DONE
Status: Binaries ready in /tmp/kernel-publish-fresh/
Timestamp: 2026-04-19 XX:XX (today)
Commit: 46d64b5 verified
Ready for deployment
```

Then **INFRA will:**
1. Stop Kernel service
2. Deploy from `/tmp/kernel-publish-fresh/`
3. Start service + verify
4. POST test → Should return 201 (not 500)

---

## DoD

- [ ] Commit 46d64b5 verified in working directory
- [ ] `dotnet build` succeeds (0 errors, 0 warnings)
- [ ] `dotnet publish` to `/tmp/kernel-publish-fresh/` succeeds
- [ ] Binary timestamp: 2026-04-19 (today)
- [ ] Outbox: MSG-KERNEL-100-DONE

---

## Timeline

**Build:** ~5-10 min
**INFRA deploy:** ~5-10 min
**TESTER validate:** ~10-15 min
**Total to Soft Launch GO decision:** ~30-40 min (if everything works)

---

**This is the last blocker. Start immediately.**

INFRA is standing by for `/tmp/kernel-publish-fresh/` binaries.
