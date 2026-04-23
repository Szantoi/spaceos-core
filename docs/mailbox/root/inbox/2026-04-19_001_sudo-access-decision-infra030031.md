---
id: MSG-ROOT-001
from: system
to: root
type: question
priority: critical
status: UNREAD
ref: MSG-INFRA-030-031-BLOCKED
created: 2026-04-19
---

# IMMEDIATE DECISION NEEDED: Unblock INFRA-030 & INFRA-031 (sudo Access)

## Both Critical Tasks Blocked on sudo Password

**INFRA-030:** Keycloak restart (KC config update)
**INFRA-031:** Kernel-093 deploy (BUG-007 fix)

Both require `sudo` commands but INFRA cannot execute them without:
- Interactive terminal password prompt, OR
- Password passed via stdin, OR
- Pre-configured sudoers NOPASSWD

---

## Three Options to Unblock

### **Option 1: Provide sudo Password to INFRA**

You provide VPS sudo password → INFRA uses `sudo -S` to execute both tasks.

**Pros:** INFRA does all work (most autonomous)
**Cons:** Password sharing, less immediate

**Command format:**
```bash
echo "password" | sudo -S systemctl stop keycloak
```

---

### **Option 2: ROOT Executes Commands Directly (⭐ FASTEST)**

You SSH to VPS and run commands yourself (or paste into terminal).

**Pros:** Fastest (~5-10 min total), direct control
**Cons:** You execute the commands manually

**Required:**
```bash
# KC Restart (INFRA-030)
sudo systemctl stop keycloak && sleep 10 && sudo systemctl start keycloak && sleep 30
curl "http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/logout?client_id=portal-app&post_logout_redirect_uri=https://joinerytech.hu/"

# Kernel Deploy (INFRA-031)
sudo rsync -a --exclude 'appsettings.*.json' /tmp/kernel-publish/ /opt/spaceos/spaceos-kernel/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/spaceos-kernel/publish/
sudo systemctl restart spaceos-kernel && sleep 3
curl http://127.0.0.1:5000/healthz
```

---

### **Option 3: Configure Sudoers for INFRA User**

Grant INFRA user permanent sudoers access (NOPASSWD).

**Pros:** INFRA fully autonomous in future
**Cons:** Security implications, longer setup

```bash
echo "INFRA_USER ALL=(ALL) NOPASSWD: /usr/bin/systemctl,/usr/bin/rsync,/usr/bin/chown" | sudo tee /etc/sudoers.d/infra
```

---

## ROOT DECISION REQUIRED

**Choose ONE:**

1. **"Password: [VPS_SUDO_PASSWORD]"** → INFRA uses Option 1 (3-5 min)
2. **"I'll execute directly"** → You run commands above (5-10 min + your time)
3. **"Configure sudoers"** → Setup Option 3 (5 min setup + then INFRA runs)

---

## Urgency

⏰ **CRITICAL** — This is the final blocker before Soft Launch.

Once unblocked:
- Both deployments execute in parallel (~5 min)
- TESTER validates (10 min)
- **Soft Launch GO** 🎉

---

**Status:** BLOCKED — Awaiting ROOT decision on sudo access method

**Recommendation:** Option 2 (fastest immediate path to Soft Launch)
