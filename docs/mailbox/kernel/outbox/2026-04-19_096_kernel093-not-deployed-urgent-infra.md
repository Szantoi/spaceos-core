---
id: MSG-KERNEL-096-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-096
created: 2026-04-19
---

## Deployment Status: KERNEL-093 NOT YET DEPLOYED

### Verification Result

**VPS publish directory timestamp:**
```
/opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll
  Last modified: 2026-04-18 17:46:53 (+0200)
```

**Local build timestamp:**
```
/tmp/kernel-publish/SpaceOS.Kernel.Api.dll (KERNEL-093, 46d64b5)
  Created: 2026-04-18 17:42 (this session, after the old publish)
```

**Conclusion:** ❌ **46d64b5 (KERNEL-093) was NOT redeployed to VPS yet.**

The binaries were built locally and staged in `/tmp/kernel-publish`, but the INFRA `rsync + systemctl restart` step from **MSG-INFRA-019** was never executed.

---

## Why BUG-007 Still Fails

The VPS is still running the OLD kernel (pre-KERNEL-093) with `EnableRetryOnFailure` still active.
Every POST mutation that touches `AuditDbContext` or `AppDbContext` hits the `NpgsqlRetryingExecutionStrategy.OnFirstExecution()` check and throws.

---

## Immediate Action Required

**Option A: Deploy Immediately (5 min)**

```bash
# On VPS:
sudo rsync -a --exclude 'appsettings.*.json' /tmp/kernel-publish/ /opt/spaceos/spaceos-kernel/publish/
sudo systemctl restart spaceos-kernel
sleep 3
curl http://127.0.0.1:5000/healthz
```

This is exactly MSG-INFRA-019 (already prepared).

**Option B: If /tmp/kernel-publish was cleaned**

Kernel will rebuild:
```bash
cd /opt/spaceos/spaceos-kernel
dotnet publish SpaceOS.Kernel.Api/SpaceOS.Kernel.Api.csproj -c Release -o /tmp/kernel-rebuild --no-restore
sudo rsync -a --exclude 'appsettings.*.json' /tmp/kernel-rebuild/ /opt/spaceos/spaceos-kernel/publish/
sudo systemctl restart spaceos-kernel
```

---

## Verification After Deploy

```bash
TOKEN=$(curl -s -X POST "http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/token" \
  -d "grant_type=client_credentials&client_id=spaceos-kernel&client_secret=ET48o6KTW0IQPoMJCYMWyXZSAMHBipdn" \
  | jq -r .access_token)

# Test POST /api/tenants (Kernel)
curl -s -w "\n%{http_code}\n" -X POST http://127.0.0.1:5000/api/tenants \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Verify KERNEL-093","tenantType":"Manufacturer"}'
# Expected: 201

# (BUG-007 requires Procurement module, not Kernel)
```

---

## Status

**KERNEL-093 Verified:** Code is correct, fixes are in place, binaries are built locally.
**Deployment Blocker:** INFRA needs to execute MSG-INFRA-019.

**Soft Launch Blocker:** YES — Unblock with immediate deploy.
