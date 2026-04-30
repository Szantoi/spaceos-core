---
id: MSG-PORTAL-016
from: root
to: portal
type: task
priority: critical
status: READ
ref: DEBUG-001
created: 2026-04-17
---

# PORTAL-016 — callback authorization_code retry eltávolítása + useRef guard

## Kontextus

A böngésző konzolban két egymást követő token exchange hívás jelenik meg:

```
POST .../token  ERR_INSUFFICIENT_RESOURCES  200 (OK)   ← KC elfogyasztja a kódot
POST .../token  ERR_INSUFFICIENT_RESOURCES  400 (Bad Request)  ← retry, de kód már használt
```

**Root cause:** `exchangeCodeWithRetry` az `authorization_code` cserét hálózati hiba esetén újrapróbálja.  
A Keycloak az első híváskor (200) már felhasználta a kódot. A retry ezért garantáltan 400-at kap.  
Az `authorization_code` **single-use** — retry it soha nem szabad rajta.

## Feladat — 2 fájl

### 1. `packages/@spaceos/api-client/src/auth/keycloak.ts`

- **Töröld az `exchangeCodeWithRetry` függvényt** (91–106. sor)
- A `handleCallback`-ben cseréld le a hívást:
  ```ts
  // volt:
  const tokens = await exchangeCodeWithRetry(code, codeVerifier);
  // legyen:
  const tokens = await exchangeCode(code, codeVerifier);
  ```
- Az `exchangeCode` függvény marad, csak a retry réteg törlendő.

### 2. `apps/joinerytech/src/features/auth/CallbackPage.tsx`

Adj hozzá `useRef` guard-ot a dupla meghívás ellen (defense-in-depth):

```tsx
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { handleCallback, AuthError, useAuthStore } from '@spaceos/api-client';

export function CallbackPage() {
  const navigate = useNavigate();
  const setTokens = useAuthStore((s) => s.setTokens);
  const fetchMe = useAuthStore((s) => s.fetchMe);
  const [error, setError] = useState<string | null>(null);
  const exchanged = useRef(false);

  useEffect(() => {
    if (exchanged.current) return;
    exchanged.current = true;

    const params = new URLSearchParams(window.location.search);
    handleCallback(params)
      .then(async (tokens) => {
        setTokens(tokens.access_token, tokens.refresh_token, tokens.id_token);
        await fetchMe();
        navigate('/', { replace: true });
      })
      .catch((err) => {
        if (err instanceof AuthError) {
          if (err.code === 'state_mismatch' || err.code === 'nonce_mismatch') {
            console.error('[Security] OAuth callback security violation:', err.code);
            setError('Biztonsági hiba. Kérjük próbáld újra a bejelentkezést.');
          } else
            setError('Bejelentkezés sikertelen. Kérjük próbáld újra.');
        } else {
          setError('Váratlan hiba történt.');
        }
      });
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // ... rest of render unchanged
}
```

## Tesztek

- `CallbackPage.test.tsx`: ellenőrizd, hogy a "shows error without retrying on Keycloak 400" teszt (`mockHandleCallback` 1×-es hívás) zöld marad
- `keycloak.ts` unit tesztjei (ha léteznek): töröld az `exchangeCodeWithRetry` retry tesztjét
- Minden meglévő teszt zöldnek kell maradni

## Build + deploy pipeline

```
1. npm run build (turbo) → 0 TS error
2. npm test → minden zöld
3. OUTBOX: DONE üzenet
4. INFRA-terminál kapja a deploy üzenetet (root intézi)
```

## Elvárt eredmény

- Token exchange pontosan 1×-es hívás `/callback` után
- Ha a hívás hálózati hibával dob → felhasználó hibaüzenetet lát, nem kerül végtelen loopba
- `ERR_INSUFFICIENT_RESOURCES` esetén sem próbál újra KC-hoz beváltani egy már felhasznált kódot

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
