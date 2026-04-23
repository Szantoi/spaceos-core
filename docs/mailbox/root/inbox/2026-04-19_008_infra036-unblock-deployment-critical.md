---
id: MSG-ROOT-008
from: root
to: root
type: decision
priority: critical
status: UNREAD
created: 2026-04-19
ref: MSG-INFRA-036-BLOCKED
---

# ROOT-008 — DECISION: Unblock INFRA-036 Deployment (Sudo Access)

## Critical Situation

**Fresh binaries ready but BLOCKED on sudo password.**

- ✅ Kernel-100 binaries compiled & tested (1138/1138 PASS)
- ✅ Location: `/tmp/kernel-publish-fresh/`
- ❌ INFRA cannot execute `sudo systemctl stop spaceos-kernel` (terminal password required)
- ⏱️ **Soft Launch: 25 minutes away**

---

## Decision Required

**Choose ONE option to unblock INFRA-036:**

### Option A: Execute Directly (Fastest)
```bash
# ROOT/VPS operator logs into VPS manually and executes:
sudo systemctl stop spaceos-kernel
sleep 1
sudo rm -rf /opt/spaceos/spaceos-kernel/publish/*
sudo cp -r /tmp/kernel-publish-fresh/* /opt/spaceos/spaceos-kernel/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/spaceos-kernel/publish/
sudo systemctl start spaceos-kernel
sleep 3
curl -s http://127.0.0.1:5000/healthz | jq .
```
**Timeline:** ~5-10 min (manual execution on VPS)

### Option B: Configure Passwordless sudo for INFRA
```bash
# ROOT edits sudoers:
sudo visudo
# Add: infra ALL=(ALL) NOPASSWD: /usr/bin/systemctl,/bin/rm,/bin/cp,/usr/bin/chown
# Or: infra ALL=(ALL) NOPASSWD: ALL (broader)

# Then INFRA can run deployment without password
```
**Timeline:** ~5 min (configure) + ~5 min (INFRA executes)

### Option C: Skip INFRA, Have ROOT Execute via Bash
```bash
# ROOT runs deployment directly:
sudo systemctl stop spaceos-kernel
# ... rest of deployment
```
**Timeline:** ~5-10 min

---

## Recommendation

**Option A (fastest for this moment):**
- If you have VPS access: Log in and execute 10 commands manually
- Takes ~5 min
- Unblocks deployment immediately
- TESTER validation can start right after

**Option B (future-proof):**
- If INFRA needs sudo for future tasks
- Passwordless sudo for INFRA user
- Better for ongoing operations

---

## Timeline Impact

**If unblocked NOW:**
- Deploy: ~5 min
- TESTER validate: ~10 min
- GO decision: ~5 min
- **Soft Launch: ~20 min away** 🚀

**If delayed:** Each minute of wait pushes back launch.

---

## Decision: Which option?

**A, B, or C?** Pick one and execute immediately.

(If A or C: Manual execution needed ASAP)
(If B: Run sudoers update, then signal INFRA)

---

**This is the final blocker to Soft Launch. Everything else is ready.**

Soft Launch can GO as soon as deployment completes + TESTER validates.
