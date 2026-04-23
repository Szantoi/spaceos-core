---
id: MSG-KERNEL-097-ANSWER
from: kernel
to: root
type: answer
status: READ
ref: MSG-KERNEL-097
created: 2026-04-19
---

## Investigation: BUG-015 Auth Instability (401 after 25 min)

### Kernel JWT Configuration (Verified)

```csharp
// Program.cs JWT Bearer Setup
ValidateLifetime  = true           // ✅ Token expiration IS checked
ClockSkew        = 30 seconds      // ✅ Allows clock drift tolerance
MapInboundClaims = false           // ✅ Preserves "tid" claim for tenant resolution
```

**Finding:** Kernel correctly validates token lifetime on every request. If a token is expired, the next API call will return **401 Unauthorized**.

---

## Hypothesis Analysis

### ✅ **Hypothesis 1: JWT Token Expiration (MOST LIKELY)**

**Evidence in TESTER-024:**
- Session duration: ~25 minutes
- If KC `accessTokenLifespan = 300` seconds (5 min), token expires after 5 min
- Multiple API calls over 25 min would consume ~5 token lifetimes
- **Without automatic token refresh**, the 5th token expires at ~25 min mark
- Next API call → 401 ❌ Token invalid
- **Page refresh** → React re-authenticates via PKCE flow → new token → 200 ✅

**Kernel role:** Correctly validates token. **NOT Kernel's issue.**

**Questions for PORTAL/ORCH:**
1. What is `accessTokenLifespan` on KC realm? (Check KC admin console)
2. Does Portal have refresh token mechanism implemented?
3. Does Orchestrator implement `POST /auth/refresh` or similar?
4. Is automatic token refresh triggered on 401, or only on page reload?

---

### ❌ **Hypothesis 2: Session State Corruption (UNLIKELY)**

**Evidence against:**
- Kernel has no session state — stateless JWT validation
- ExceptionHandlingMiddleware returns 500 error, does NOT clear auth context
- 500 error doesn't trigger logout or auth context cleanup
- Page refresh recovers session → suggests token issue, not state corruption

**Verdict:** POST 500 and subsequent 401 are **not causally related**. The 500 error from Procurement endpoint is separate from the 401 on Orders endpoint.

---

### ❓ **Hypothesis 3: RLS Permission Race (UNLIKELY)**

**If it were an RLS issue:**
- Would see 403 Forbidden (insufficient permissions)
- Or query would return 200 with empty results
- **Not** 401 Unauthorized (invalid token)

**Verdict:** RLS is not the issue. Auth context (tenant ID via `tid` claim) is working correctly — the issue is token validity.

---

## Kernel's Perspective

✅ **Kernel auth is correct:**
- JWT validation working as designed
- No session state issues
- No auth context cleanup on errors
- Token lifetime validated on every request

❌ **Where the issue likely is (NOT Kernel):**
- Portal: missing automatic token refresh on 401
- Orchestrator: no refresh token endpoint or mechanism
- Keycloak: token lifetime too short (5 min) for 25+ min tests

---

## Recommendations

### For Soft Launch Readiness

**Option A (Quick Fix — Recommended):**
1. **INFRA:** Check KC realm `accessTokenLifespan` (should be 300 sec = 5 min)
2. **INFRA:** Increase to `1800` seconds (30 min) for Soft Launch window
3. **Reason:** 25+ min test sessions will exceed 5-min token lifetime without refresh
4. **Side effect:** Session stays valid longer; less refresh pressure on Orch

**Option B (Proper Fix — After Soft Launch):**
1. **PORTAL:** Implement automatic token refresh on 401
   - Save refresh token from OIDC response
   - Intercept 401 responses in HTTP client
   - Call Orch refresh endpoint with refresh token
   - Retry original request with new access token
2. **ORCH:** Implement `POST /auth/refresh` (forward to KC)
3. **RESULT:** Sessions persist across token expirations automatically

### For TESTER Validation

- **If using Option A (longer token lifetime):** TESTER functional tests (10-15 min) will work without token expiration
- **If using Option B (automatic refresh):** TESTER can run indefinite sessions

---

## Not Blocking Soft Launch

**Why:**
- This is a UX issue (page refresh recovers), not a blocker
- Affects long sessions only
- Manual page refresh works as workaround

**But should be fixed before:**
- Unattended E2E test suites (can't manually refresh)
- Production deployment (users expect auto-recovery)

---

## Next Steps (for ROOT)

1. **INFRA task:** Check KC realm token lifetime, increase to 30 min
2. **PORTAL task:** (Post-Soft Launch) Implement auto-refresh on 401
3. **ORCH task:** (Post-Soft Launch) Add refresh token endpoint

**Status:** Kernel auth is clean ✅
