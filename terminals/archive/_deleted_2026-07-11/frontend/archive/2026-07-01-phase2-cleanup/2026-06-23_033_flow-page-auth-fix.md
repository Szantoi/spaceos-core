---
id: MSG-FRONTEND-033
from: root
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FRONTEND-032
created: 2026-06-23
content_hash: cd338d02d6f637c37a48c8a69e46724b205faef08cf2503b347c33adaad6bddc
---

# Flow Page Auth Overlay Fix

## Probléma

A `/flow` oldalon az AuthOverlay eltakarja a tartalmat, annak ellenére, hogy:
- Az auth disabled a szerveren (`/api/auth/verify` → `{"valid":true,"message":"Auth disabled"}`)
- A `useAuth` hook `isAuthenticated` alapértéke `true`

A fekete overlay (`rgba(0,0,0,0.85)`) megjelenik és eltakarja a Flow Editor tartalmat.

## Érintett fájlok

1. `/opt/spaceos/datahaven-web/client/src/hooks/useAuth.ts`
2. `/opt/spaceos/datahaven-web/client/src/components/Auth/AuthOverlay.tsx`
3. `/opt/spaceos/datahaven-web/client/src/App.tsx`

## Jelenlegi állapot

- `useAuth.ts`: `isAuthenticated` alapértelmezett `true`, csak 401/403-ra áll `false`-ra
- `AuthOverlay.tsx`: már inline style-okkal van (ind-chassis design)
- `App.tsx`: `<AuthOverlay isVisible={!isAuthenticated} ... />`

## Probléma részletei

Valami még mindig `false`-ra állítja az `isAuthenticated`-et, pedig:
- A szerver mindig 200 OK-t ad vissza `{"valid":true,"message":"Auth disabled"}`
- Nincs explicit 401/403 válasz

## Debug lépések

1. Adj hozzá console.log-okat a `useAuth.ts`-hez minden state változáskor
2. Ellenőrizd a böngésző konzolt, mi történik mount-kor
3. Nézd meg, hogy a `verifyToken` vagy `checkAuthDisabled` fut-e és mit kap vissza

## Lehetséges fix

Ha az auth disabled, egyszerűen soha ne mutasd az overlay-t:

```typescript
// useAuth.ts - módosítási javaslat
const [authDisabled, setAuthDisabled] = useState(false);

// checkAuthDisabled-ben:
if (res.ok && data.valid && data.message?.includes('disabled')) {
  setAuthDisabled(true);
  setIsAuthenticated(true);
}

// return-ben:
return { ..., authDisabled };

// App.tsx-ben:
<AuthOverlay isVisible={!authDisabled && !isAuthenticated} ... />
```

## Build után

```bash
cd /opt/spaceos/datahaven-web/client && npm run build
rm -rf /opt/spaceos/datahaven-web/public/client/*
cp -r /opt/spaceos/datahaven-web/client/dist/* /opt/spaceos/datahaven-web/public/client/
```

## Elvárt eredmény

A `/flow` oldal megjelenik AuthOverlay nélkül, ha az auth disabled a szerveren.
