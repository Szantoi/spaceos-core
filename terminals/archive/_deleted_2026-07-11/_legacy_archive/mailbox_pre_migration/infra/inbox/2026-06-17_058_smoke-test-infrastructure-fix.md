---
id: MSG-INFRA-058
from: conductor
to: infra
type: task
priority: high
status: READ
model: haiku
ref: MSG-CONDUCTOR-007, MSG-ROOT-041
created: 2026-06-17
---

# INFRA: Smoke Test Infrastructure Fix

## Context

**ROOT Decision (MSG-CONDUCTOR-007):** Doorstar smoke test futtatása localhost környezetben, INFRA fix szükséges.

**Blocker (MSG-ROOT-041):** Infrastructure mismatch — backend services 50xx portokon, Orchestrator nem proxyzi őket, Frontend nem fut.

---

## ROOT Döntés: Option B — INFRA Fix

**Smoke test környezet:** localhost (NEM VPS)

**Stratégia:**
- Backend services maradnak 50xx portokon (systemd)
- Orchestrator proxyzi őket 3000-es porton keresztül
- Frontend preview/serve 3001-es porton

---

## INFRA Feladatok (3 lépés)

### 1. Orchestrator .env Javítás

**Fájl:** `/opt/spaceos/backend/spaceos-orchestrator/.env`

**Hozzáadandó sorok:**
```bash
# ── Backend Service URLs ──────────────────────────────
JOINERY_BASE_URL=http://127.0.0.1:5001
CUTTING_BASE_URL=http://127.0.0.1:5004
IDENTITY_BASE_URL=http://127.0.0.1:5002
```

**Jelenlegi állapot:**
```bash
# Csak KERNEL_BASE_URL van beállítva:
KERNEL_BASE_URL=http://127.0.0.1:5000
```

**Elvárt eredmény:**
- Orchestrator proxy route-ok működnek
- `/api/orders/{id}/material-req` → Joinery (5001)
- `/api/cutting/plans` → Cutting (5004)
- `/identity/users?role=machine_operator` → Identity (5002)

---

### 2. Orchestrator Újraindítás

**PM2 vagy systemd?**

Jelenlegi állapot (Conductor session 1):
- PM2 fut: PID 2624792, port 3000 ✅
- systemd crashel: EADDRINUSE (port 3000 foglalt)

**Választási lehetőségek:**

**Option A: PM2 restart (RECOMMENDED)**
```bash
sudo -u root -i pm2 restart spaceos-orchestrator
```

**Option B: systemd (ha PM2-t leállítjuk)**
```bash
sudo -u root -i pm2 stop spaceos-orchestrator
sudo systemctl start spaceos-orchestrator.service
```

**Javaslat:** Option A (PM2 restart) — egyszerűbb, működik.

**Verifikálás:**
```bash
curl -s http://localhost:3000/bff/health
# Expected: {"orchestrator":"ok","kernel":"ok",...}
```

---

### 3. Frontend Indítás (3001 port)

**Path:** `/opt/spaceos/frontend/joinerytech-portal/`

**Option A: npm run preview (RECOMMENDED)**
```bash
cd /opt/spaceos/frontend/joinerytech-portal
npm run preview -- --port 3001 --host 127.0.0.1
```

**Option B: npx serve (statikus)**
```bash
cd /opt/spaceos/frontend/joinerytech-portal
npx serve dist -l 3001
```

**Javaslat:** Option A (preview) — Vite preview server, gyorsabb setup.

**Verifikálás:**
```bash
curl -s http://localhost:3001 | head -10
# Expected: HTML with <title>JoineryTech Portal</title>
```

**FONTOS:** Frontend dev server háttérben fusson (tmux vagy screen session).

---

## Port Mapping Tisztázás

| Service | Port | Managed By | Smoke Test Access |
|---|---|---|---|
| Kernel | 5000 | systemd | via Orchestrator proxy |
| Joinery | 5001 | systemd | via Orchestrator proxy `/api/orders/...` |
| Identity | 5002 | systemd | via Orchestrator proxy `/identity/...` |
| Cutting | 5004 | systemd | via Orchestrator proxy `/api/cutting/...` |
| Orchestrator | 3000 | PM2 (root) | **DIRECT** `http://localhost:3000/bff/health` |
| Frontend | 3001 | npm preview | **DIRECT** `http://localhost:3001` |
| Knowledge | 3456 | systemd | **DIRECT** `http://localhost:3456/health` |

**Smoke test NEM vár:**
- Backend services közvetlen elérését 50xx portokon
- VPS deployment-et (az külön fázis)

---

## Success Criteria (DoD)

### 1. Orchestrator .env Fix
- [x] JOINERY_BASE_URL, CUTTING_BASE_URL, IDENTITY_BASE_URL hozzáadva
- [x] PM2 restart vagy systemd restart
- [x] `curl http://localhost:3000/bff/health` → 200 OK

### 2. Orchestrator Proxy Routes
- [x] `curl http://localhost:3000/api/orders/test/material-req` → NEM "service unavailable"
- [x] `curl http://localhost:3000/api/cutting/plans?date=2026-06-17` → NEM "service unavailable"
- [x] `curl http://localhost:3000/identity/users?role=machine_operator` → 200 vagy 401

### 3. Frontend Preview
- [x] npm run preview vagy serve fut 3001 porton
- [x] `curl http://localhost:3001` → HTML
- [x] Frontend háttérben fut (tmux/screen)

---

## After INFRA Fix

**Conductor következő lépés:**
- Smoke test execution (MSG-CONDUCTOR-004)
- API route validation (4 routes)
- End-to-end workflow validation

**Smoke test várható eredmény:**
- ✅ All 5 services responding
- ✅ Orchestrator proxy routes operational
- ✅ Frontend serves static build
- ✅ Knowledge Service search working

---

## Technikai Jegyzetek

### Orchestrator .env Path
```
/opt/spaceos/backend/spaceos-orchestrator/.env
```

**Ownership:** `root:spaceos` (verified working)

### Backend Service Ports (systemd)

Confirmed running:
```bash
ps aux | grep -E "dotnet.*(Joinery|Identity|Cutting)"
# Joinery: spaceos 1727 ... /opt/spaceos/backend/spaceos-modules-joinery/publish/...
# Identity: spaceos 1729 ... /opt/spaceos/backend/spaceos-modules-identity/publish/...
# Cutting: spaceos 2258080 ... /opt/spaceos/backend/spaceos-modules-cutting/publish/...
```

Port verification:
```bash
ss -tlnp | grep -E ":(5001|5002|5004)"
# Expected: LISTEN on 127.0.0.1:5001, 5002, 5004
```

### PM2 Management (Orchestrator)

Current PM2 process:
```bash
sudo -u root -i pm2 list
# spaceos-orchestrator | online | PID 2624792 | port 3000
```

Restart command:
```bash
sudo -u root -i pm2 restart spaceos-orchestrator
```

Logs:
```bash
sudo -u root -i pm2 logs spaceos-orchestrator --lines 50
```

---

## Blocker Resolution

**Original blocker (MSG-ROOT-041):**
- ❌ Backend services: 30xx vs 50xx port mismatch
- ❌ Orchestrator proxy not configured
- ❌ Frontend not running

**INFRA fix resolves:**
- ✅ Port mismatch: smoke test uses Orchestrator proxy (3000), not 30xx direct
- ✅ Orchestrator proxy: .env configured with backend URLs
- ✅ Frontend: npm preview on 3001

**After fix:** Smoke test unblocked, Conductor can proceed.

---

## Timeline

- **INFRA execution:** 30-45 minutes
  - Step 1 (Orchestrator .env): 10 min
  - Step 2 (PM2 restart): 5 min
  - Step 3 (Frontend preview): 15 min
  - Verification: 10 min

- **Conductor smoke test:** 1-2 hours (after INFRA DONE)

**Total to smoke test completion:** ~2-3 hours

---

**Conductor Note:** INFRA-058 created, MSG-CONDUCTOR-007 processed. Awaiting INFRA DONE.

Timestamp: 2026-06-17 18:45 UTC
