---
id: MSG-ORCH-002
from: conductor
to: orch
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-INFRA-058-DONE, MSG-CONDUCTOR-004
created: 2026-06-17
---

# ORCH: Implement Backend Service Proxy Routes

## Context

**INFRA-058-DONE discovered:** Orchestrator env vars are set correctly, but **proxy route handlers are missing in code**.

**Blocker:** Smoke test (MSG-CONDUCTOR-004) cannot proceed until Orchestrator implements proxy middleware for backend services.

---

## Current State

### ✅ Infrastructure Ready (INFRA confirmed)

| Service | Port | Status | Env Var Set |
|---------|------|--------|-------------|
| Joinery | 5002 | ✅ LISTENING | `JOINERY_BASE_URL=http://127.0.0.1:5002` |
| Identity | 5003 | ✅ LISTENING | `IDENTITY_BASE_URL=http://127.0.0.1:5003` |
| Cutting | 5004 | ✅ LISTENING | `CUTTING_BASE_URL=http://127.0.0.1:5004` |
| Orchestrator | 3000 | ✅ LISTENING | PM2 restarted with `--update-env` |

### ❌ Code Missing

**Test results (from INFRA):**
```bash
curl http://localhost:3000/api/orders/test/material-req
# {"error":"Joinery service unavailable"}

curl http://localhost:3000/api/cutting/plans
# {"error":"Cutting service unavailable"}

curl http://localhost:3000/identity/users
# Cannot GET /identity/users (404)
```

**Problem:** No Express route handlers or proxy middleware implemented.

---

## Required Implementation

### Proxy Routes (4 endpoints minimum)

You must implement these routes in the Orchestrator code:

#### 1. Joinery Material Requisition
```
GET /api/orders/:id/material-req → http://127.0.0.1:5002/api/orders/:id/material-req
```

#### 2. Joinery Hardware Specs
```
GET /api/orders/:id/hardware-list → http://127.0.0.1:5002/api/orders/:id/hardware-list
```

#### 3. Cutting Plan Generation
```
POST /api/cutting/plans → http://127.0.0.1:5004/api/cutting/plans
```

#### 4. Get Cutting Plans
```
GET /api/cutting/plans → http://127.0.0.1:5004/api/cutting/plans
```

#### 5. Identity Users (bonus)
```
GET /identity/users → http://127.0.0.1:5003/identity/users
```

---

## Implementation Options

### Option A: http-proxy-middleware (RECOMMENDED)

Install dependency:
```bash
npm install http-proxy-middleware
```

Add proxy routes in Orchestrator server setup:
```javascript
const { createProxyMiddleware } = require('http-proxy-middleware');

// Proxy to Joinery (5002)
app.use('/api/orders', createProxyMiddleware({
  target: process.env.JOINERY_BASE_URL || 'http://127.0.0.1:5002',
  changeOrigin: true,
  logLevel: 'debug'
}));

// Proxy to Cutting (5004)
app.use('/api/cutting', createProxyMiddleware({
  target: process.env.CUTTING_BASE_URL || 'http://127.0.0.1:5004',
  changeOrigin: true,
  logLevel: 'debug'
}));

// Proxy to Identity (5003)
app.use('/identity', createProxyMiddleware({
  target: process.env.IDENTITY_BASE_URL || 'http://127.0.0.1:5003',
  changeOrigin: true,
  logLevel: 'debug'
}));
```

### Option B: Manual Fetch Handlers

If you don't want a dependency:
```javascript
app.all('/api/orders/*', async (req, res) => {
  const targetUrl = `${process.env.JOINERY_BASE_URL}${req.url}`;
  try {
    const response = await fetch(targetUrl, {
      method: req.method,
      headers: req.headers,
      body: ['POST', 'PUT', 'PATCH'].includes(req.method) ? JSON.stringify(req.body) : undefined
    });
    const data = await response.json();
    res.status(response.status).json(data);
  } catch (error) {
    res.status(502).json({ error: 'Joinery service unavailable' });
  }
});
```

**Recommendation:** Use Option A (http-proxy-middleware) — it's standard, battle-tested, and handles edge cases.

---

## Success Criteria (DoD)

### 1. Routes Respond
- [ ] `curl http://localhost:3000/api/orders/test/material-req` → NOT "service unavailable"
- [ ] `curl http://localhost:3000/api/cutting/plans` → NOT 404
- [ ] `curl http://localhost:3000/identity/users` → NOT 404

### 2. Proper Error Handling
- [ ] If backend service down → return 502 with JSON error
- [ ] If route not found on backend → return 404 from backend
- [ ] If validation error → return 400/422 from backend

### 3. Logging
- [ ] Proxy requests logged (debug level)
- [ ] Errors logged with backend service name

### 4. PM2 Restart
- [ ] After code changes: `sudo -u root -i pm2 restart spaceos-orchestrator`
- [ ] Verify health: `curl http://localhost:3000/bff/health` → still OK

---

## Testing Procedure

After implementation:

```bash
# Test 1: Health check still works
curl http://localhost:3000/bff/health
# Expected: {"orchestrator":"ok",...}

# Test 2: Joinery material-req route
curl http://localhost:3000/api/orders/test/material-req
# Expected: 404 Not Found (valid response from Joinery) OR actual data

# Test 3: Cutting plans route
curl -X POST http://localhost:3000/api/cutting/plans \
  -H "Content-Type: application/json" \
  -d '{"date":"2026-06-17","capacity":1000,"orders":[]}'
# Expected: 200 OK with response OR validation error (not 502)

# Test 4: Identity route
curl http://localhost:3000/identity/users
# Expected: 401 Unauthorized OR 200 with users (not 404)
```

---

## Files to Modify

**Path:** `/opt/spaceos/backend/spaceos-orchestrator/`

**Likely files:**
- `src/index.ts` or `src/server.ts` — Express app setup
- `src/routes/` — route definitions
- `package.json` — add `http-proxy-middleware` dependency

**Steps:**
1. Add proxy middleware to Express app
2. Test locally (curl commands above)
3. Commit changes
4. PM2 restart
5. Create DONE message with test results

---

## Blocker Resolution

**Original blocker (MSG-ROOT-041):**
- ❌ Orchestrator proxy routes not configured

**INFRA-058 partial fix:**
- ✅ Env vars set (JOINERY_BASE_URL, CUTTING_BASE_URL, IDENTITY_BASE_URL)
- ✅ Backend services confirmed running
- ❌ **Code not implemented** ← THIS TASK

**After ORCH-002 completion:**
- ✅ All proxy routes operational
- ✅ Smoke test can proceed

---

## Timeline

**Estimated implementation:** 30-60 minutes
- Research/install middleware: 10 min
- Implement routes: 20 min
- Test locally: 15 min
- PM2 restart + verify: 10 min

**After completion:** Conductor executes smoke test (MSG-CONDUCTOR-004)

---

## Technical Notes

### Current Orchestrator Health Check

```bash
curl http://localhost:3000/bff/health
# {"orchestrator":"ok","kernel":"ok","llmProvider":"openai","timestamp":"2026-06-17T16:40:22.328Z"}
```

**Status:** ✅ Orchestrator operational, just missing proxy routes.

### Backend Service Ports (Verified)

```bash
netstat -tlnp | grep -E "5[0-9]{3}"
# 5002: Joinery (ASPNETCORE_URLS confirmed)
# 5003: Identity (inferred from netstat)
# 5004: Cutting (confirmed)
```

**Status:** All backend services listening and ready.

### PM2 Management

Restart after code changes:
```bash
sudo -u root -i pm2 restart spaceos-orchestrator
sudo -u root -i pm2 logs spaceos-orchestrator --lines 50
```

---

## Dependencies

**Before you start:**
- Orchestrator source code access: `/opt/spaceos/backend/spaceos-orchestrator/`
- Node.js environment: Available (PM2 already running)
- npm install: Available

**After completion:**
- Conductor can execute smoke test
- Doorstar soft launch unblocked

---

**ORCH Priority:** CRITICAL — blocks entire Doorstar deployment.

Timestamp: 2026-06-17 18:50 UTC
