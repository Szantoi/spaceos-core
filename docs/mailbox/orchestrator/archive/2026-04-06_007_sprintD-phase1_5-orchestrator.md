---
id: MSG-O007
from: architect
to: orchestrator
type: task
status: DONE
priority: P0
sprint: "Sprint D · Phase 1.5"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase1_5_v4.md"
---

# Sprint D · Phase 1.5 — ES256 JWT + Refresh Token BFF (Orchestrator)

## Összefoglaló

A Kernel RS256-ról ES256-ra vált (Sprint D Phase 1.5 T-03). Az Orchestrator BFF-nek frissítenie kell a JWT validációt és proxy-znia kell az új refresh/logout endpointokat.

**Blokkolva:** A Kernel T-03 (ES256 + RefreshToken) befejezéséig NEM tudod tesztelni a valós integrációt. Addig unit tesztekkel dolgozz.

---

## Feladat 1 — RS256 → ES256 JWT Validáció

### Mi változik

| Elem | Régi (RS256) | Új (ES256) |
|------|-------------|------------|
| Algoritmus | RS256 | ES256 (ECDSA P-256) |
| Kulcs | RSA publikus kulcs file | JWKS endpoint VAGY EC publikus kulcs file |
| Token lifetime | 8 óra | **15 perc** |

### Implementáció

1. **JWT middleware frissítés** (`src/middleware/jwt.middleware.ts` vagy hasonló):
   - Az `algorithm` config-ban RS256 → ES256
   - A publikus kulcs forrása:
     - **Preferált:** `/.well-known/jwks.json` fetch a Kernel-ről (http://127.0.0.1:5001/.well-known/jwks.json)
     - **Fallback:** EC publikus kulcs fájl (`/etc/spaceos/keys/jwt_ec_public.pem`)
   - `jsonwebtoken` verify options: `{ algorithms: ['ES256'] }`

2. **JWKS cache** (opcionális de ajánlott):
   - A Kernel OutputCache 1 órás — az Orchestrator is cache-elje a JWKS-t (pl. `node-cache` 5 perc TTL)
   - Graceful fallback: ha a JWKS fetch fail-el, használja az utolsó cached értéket

3. **jwtKeys konfig frissítés** (`src/config/` vagy `.env`):
   - `JWT_ALGORITHM=ES256`
   - `JWT_PUBLIC_KEY_PATH=/etc/spaceos/keys/jwt_ec_public.pem` (ha fájl-alapú)
   - `KERNEL_JWKS_URL=http://127.0.0.1:5001/.well-known/jwks.json` (ha JWKS-alapú)

### Tesztek
- [ ] Unit teszt: ES256 token validálás sikeres
- [ ] Unit teszt: RS256 token → elutasítva (algorithm mismatch)
- [ ] Unit teszt: lejárt token (15 perc) → 401
- [ ] Unit teszt: érvénytelen signature → 401

---

## Feladat 2 — Refresh Token BFF Proxy

### Új BFF Endpointok

A Portal közvetlenül nem hívja a Kernel-t — minden a BFF-en (Orchestrator) keresztül megy.

```
POST /bff/auth/refresh
  Body: { "refreshToken": "..." }
  → Proxy to: POST http://127.0.0.1:5001/api/auth/refresh
  ← Response: { "accessToken": "...", "refreshToken": "..." }

POST /bff/auth/logout
  Body: { "refreshToken": "..." }
  → Proxy to: POST http://127.0.0.1:5001/api/auth/logout
  ← Response: 200 OK
```

### Implementáció

1. **Route handler** (`src/routes/auth.route.ts` — meglévő fájl bővítése):
   - `POST /bff/auth/refresh` → proxy a Kernel felé
   - `POST /bff/auth/logout` → proxy a Kernel felé
   - Mindkét endpoint: **NEM igényel JWT-t** (a refresh token maga az autentikáció)
   - Rate limiting: mindkét endpointra alkalmaz (bruteforce védelem)

2. **Token kezelés:**
   - A BFF NE tárolja a refresh token-t (stateless proxy)
   - A válaszban jövő új token párt forwarding-olja a Portal-nak

### Tesztek
- [ ] Unit teszt: `/bff/auth/refresh` proxy helyes request forwarding
- [ ] Unit teszt: `/bff/auth/logout` proxy helyes request forwarding
- [ ] Unit teszt: Rate limiting működik mindkét endpointra
- [ ] Unit teszt: Hibás Kernel válasz → helyes error forwarding

---

## Feladat 3 — Cleanup

1. **RS256 referenciák eltávolítása:** Keresd meg és töröld az RS256-specifikus konfigot, ha van (kulcs path, algorithm setting)
2. **JWT_SIGNING_KEY env var:** Ha az Orchestrator-ban is volt ilyen → törölni

---

## DoD

- [ ] ES256 JWT validáció működik
- [ ] RS256 token elutasítva
- [ ] `/bff/auth/refresh` proxy implementálva + tesztelve
- [ ] `/bff/auth/logout` proxy implementálva + tesztelve
- [ ] Rate limiting mindkét új endpointon
- [ ] Meglévő tesztek zöldek (76+)
- [ ] `npm audit` → 0 high/critical
- [ ] Semgrep → 0 finding

---

## ⚠️ Fontos kontextus

- **Kernel API:** `http://127.0.0.1:5001` (production port 5001, NEM 5000!)
- **ES256 publikus kulcs:** `/etc/spaceos/keys/jwt_ec_public.pem` (a Kernel generálja)
- **Access Token lifetime:** 15 perc (korábban 8 óra volt) — a Portal-nak gyakrabban kell refresh-elnie
- **Refresh Token lifetime:** 8 óra
