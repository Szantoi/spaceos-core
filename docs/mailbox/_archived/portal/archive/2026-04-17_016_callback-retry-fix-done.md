---
id: MSG-PORTAL-016-DONE
from: portal
to: root
type: done
priority: critical
status: READ
ref: MSG-PORTAL-016
created: 2026-04-17
---

# PORTAL-016 — callback retry fix + useRef guard — KÉSZ

## Összefoglaló

Commit: `408bf1e` (branch: `develop`, pushed)

## Változtatások

### 1. `packages/@spaceos/api-client/src/auth/keycloak.ts`
- `exchangeCodeWithRetry` függvény **törölve** (16 sor eltávolítva)
- `handleCallback` most közvetlenül `exchangeCode`-ot hív — nincs retry az `authorization_code` cserén

### 2. `apps/joinerytech/src/features/auth/CallbackPage.tsx`
- `useRef(false)` guard hozzáadva: az exchange pontosan 1× fut le, React StrictMode double-mount esetén is

## Teszt lefedettség

A meglévő teszt (`shows error without retrying on Keycloak 400`) ellenőrzi:
```ts
expect(mockHandleCallback).toHaveBeenCalledTimes(1);  // ✅ zöld
```

## Build + teszt

```
pnpm turbo build → Tasks: 7 successful, 7 total — 0 error
Tests: 306/306 green
```

## Deploy (INFRA hatáskör)

```bash
sudo cp -r apps/joinerytech/dist/* /opt/spaceos/design-portal/apps/joinerytech/dist/
sudo nginx -t && sudo systemctl reload nginx
```

## Kockázatok / kérdések

Nincsenek. Az `exchangeCode` hálózati hiba esetén `AuthError('exchange_failed')`-t dob — a `CallbackPage` ezt hibaüzenetként jeleníti meg, nem loopol.
