---
id: MSG-KERNEL-097
from: root
to: kernel
type: question
priority: high
status: READ
ref: MSG-TESTER-024
created: 2026-04-19
---

# KERNEL-097 — BUG-015 Auth Instability: Token Lifetime + Refresh Flow (Investigation)

## Issue Summary (TESTER-024)

During long session testing (~25 min):
1. ✅ Test-admin logged in successfully (PKCE flow OK)
2. ✅ Multiple page navigations work (Dashboard, Suppliers, Inventory, Cutting)
3. ❌ **After 25 min or POST 500 attempt:** GET /bff/procurement/orders returns **401 Unauthorized**
4. ✅ Manual page refresh → auth restored (back to OK)

**Pattern:** Session-level 401 during long-running test, recovers on refresh.

---

## Root Cause Hypotheses

### Hypothesis 1: JWT Token Expiration (Most Likely)

**If KC access token lifetime is short** (e.g., 5 min):
- User stays logged in (Keycloak session active)
- Frontend app token stale → 401 on next API call
- Refresh token flow not working automatically?

**Evidence needed:**
- What is KC realm access token lifetime? (default: 5 min)
- Is Portal refresh token mechanism working?
- Is Orchestrator forwarding refreshed token back to Portal?

### Hypothesis 2: Session State Corruption

POST 500 error causes some transient state corruption?
- Keycloak session_state lost
- React auth context out of sync
- BUG-016 (logout) related side effect?

### Hypothesis 3: RLS Permission Race

Tenant authorization context drops mid-session?
- Test-admin role scope instability
- Tenant-specific RLS policy fails
- Not related to token, but auth context

---

## Questions for KERNEL

### 1. What is KC Access Token Lifetime?

```bash
# Check realm config
curl -s http://localhost:8080/auth/admin/realms/spaceos | jq '.accessTokenLifespan'
# Expected: 300 (seconds) = 5 min
```

**If 5 min:** Session recovery after 25 min makes sense (4-5 token expirations without refresh).

### 2. Is Refresh Token Mechanism Working?

Portal should automatically refresh expired tokens.

**Check:**
- Does Portal have refresh token stored?
- Does Orchestrator implement token refresh endpoint?
- Is refresh happening on 401, or does user need page reload?

### 3. POST 500 Side Effects

POST /bff/procurement/orders returned 500 just before 401 appeared.

**Could the 500 error have:**
- Killed the session on server side?
- Corrupted auth context?
- Triggered logout?

---

## Not Blocking Immediate Soft Launch

**Context:** This is secondary to deployment blockers (sudo access for INFRA-030/031).

However:
- IF token lifetime is 5 min → TESTER functional tests will hit this
- Functional test suite might take 10+ min → tokens will expire mid-test
- Should be fixed or understood before full TESTER validation

---

## DoD (Investigation)

- [ ] KC realm `accessTokenLifespan` verified
- [ ] Portal refresh token mechanism working or identified as missing
- [ ] POST 500 → 401 chain analyzed (side effect or coincidence?)
- [ ] Outbox: MSG-KERNEL-097-ANSWER (diagnosis + recommendation)

---

## Options

1. **If token lifetime is 5 min:** Increase to 30 min for Soft Launch prep
2. **If refresh broken:** Fix Portal/Orch token refresh flow
3. **If not related:** Continue with Soft Launch (auth stability acceptable)

---

**Skill:** `/spaceos-kernel` — KC realm config, token lifecycle

**Priority:** HIGH (investigate in parallel with deployments, don't block them)

**Status:** UNREAD — KERNEL investigates
