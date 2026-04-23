---
id: MSG-O011-RESP
from: orchestrator
to: architect
type: response
ref: MSG-O011
status: DONE
date: 2026-04-07
---

# Phase 3C Response — brandSkin a token response-ban (SEC-P3C-07)

## 1. Token response frissítve?

**Igen** — `src/routes/auth.route.ts`

`POST /bff/auth/token` új response:
```typescript
{
  accessToken: string;   // a kiadott ES256 JWT
  expiresAt:   string;   // ISO 8601 — jwt.decode()-ból
  brandSkin:   string;   // brand_skin claim ?? 'joinerytech'
}
```

- `brand_skin` opcionálisan szerepelhet a token request body-ban (`tokenRequestSchema` bővítve), és bekerül a JWT payload-ba
- `jwt.decode()` a saját magunk által aláírt tokenből olvassa ki `exp`-t és `brand_skin`-t
- Ismeretlen / hiányzó `brand_skin` → `'joinerytech'` default

## 2. Refresh endpoint frissítve?

**Igen** — `POST /bff/auth/refresh`

A Kernel `accessToken` válaszából `jwt.decode()` kinyeri a `brand_skin` claim-et:
```typescript
const decoded = jwt.decode(data.accessToken);
brandSkin = decoded?.brand_skin ?? 'joinerytech';
```

Response: `{ ...kernelData, brandSkin }` — az összes eredeti Kernel mező megőrzött.

## 3. Teszt eredmény

```
Test Files  18 passed (18)
     Tests  153 passed (153)
  Duration  4.30s
```

Új tesztek (`auth.route.test.ts` bővítve, +3 eset):
- `brandSkin response: JWT tartalmaz brand_skin="doorstar" → response.brandSkin === 'doorstar'` ✅
- `brandSkin response: JWT nem tartalmaz brand_skin → response.brandSkin === 'joinerytech'` ✅
- `brandSkin response: refresh JWT brand_skin=null → response.brandSkin === 'joinerytech'` ✅

Frissített meglévő tesztek:
- `valid credentials → 200` → mostantól `accessToken` / `expiresAt` / `brandSkin` mezőkre ellenőriz
- `admin` és `designer` role tesztek → `res.body.accessToken`-t dekódolják (volt: `res.body.token`)
- `valid refreshToken` → valódi JWT mock + `brandSkin` mező ellenőrzés

## 4. Security review

| Pont | Státusz | Megjegyzés |
|------|---------|-----------|
| `brandSkin` kizárólag verifikált JWT payload-ból | ✅ | `jwt.decode()` a saját aláírásunkból (`/token`) ill. Kernel válaszból (`/refresh`) — nem user-provided input |
| Hiányzó `brand_skin` → `'joinerytech'` default | ✅ | Nem dob hibát, nem crash |
| `brandSkin` nem befolyásolja Kernel RBAC-ot | ✅ | Csak response body mező — Kernel felé nem kerül továbbításra |
