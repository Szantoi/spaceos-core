---
id: MSG-PORTAL-011
from: root
to: portal
type: task
priority: critical
status: READ
ref: MSG-INFRA-025-BLOCKED
created: 2026-04-19
---

# PORTAL-011 — Logout Parameter Regression Revert (FD340BF)

## CRITICAL REGRESSION DISCOVERED

**KC restart (INFRA-025) completed successfully ✅** — but revealed **commit fd340bf introduced a parameter name reversal regression**.

### Root Cause

Commit `fd340bf` was intended to fix logout parameter handling, but **reversed the logic**:

```
c18e00a: post_logout_redirect_uri ✅ (CORRECT)
fd340bf: redirect_uri ❌ (REGRESSION — reversal)
Current: Portal sends redirect_uri → KC rejects with 400 Bad Request
```

**KC Error Log:**
```
Parameter 'redirect_uri' no longer supported. 
Please use 'post_logout_redirect_uri' with 'id_token_hint' for this endpoint.
```

---

## Fix Required (1 line change)

**File:** `/opt/spaceos/design-portal/packages/@spaceos/api-client/src/auth/keycloak.ts`

**Change:**
```diff
- redirect_uri: window.location.origin,
+ post_logout_redirect_uri: window.location.origin,
```

---

## Verification

**Before fix (c18e00a):**
```typescript
export function logoutUrl(idTokenHint?: string): string {
  const params = new URLSearchParams({
    client_id: kcClientId(),
    post_logout_redirect_uri: window.location.origin,  // ✅ CORRECT
  });
  if (idTokenHint) params.set('id_token_hint', idTokenHint);
  return `${kcRealmUrl()}/protocol/openid-connect/logout?${params}`;
}
```

**Current (fd340bf) — BROKEN:**
```typescript
export function logoutUrl(idTokenHint?: string): string {
  const params = new URLSearchParams({
    client_id: kcClientId(),
    redirect_uri: window.location.origin,  // ❌ WRONG PARAMETER
  });
  if (idTokenHint) params.set('id_token_hint', idTokenHint);
  return `${kcRealmUrl()}/protocol/openid-connect/logout?${params}`;
}
```

---

## DoD

- [ ] Revert parameter name: `redirect_uri` → `post_logout_redirect_uri`
- [ ] `npm run build --filter=joinerytech` ✅
- [ ] Bundle deployed to `apps/joinerytech/dist/`
- [ ] Nginx reloaded
- [ ] Outbox: MSG-PORTAL-011-DONE (commit hash + test confirmation)
- [ ] Then: INFRA re-tests logout flow (TESTER validates)

---

## Urgency

**CRITICAL** — Soft Launch blocked on logout flow working. This is a 1-line revert.

**Skill:** `/spaceos-portal` — code change + build + deploy

**Status:** UNREAD — PORTAL terminal handles immediately
