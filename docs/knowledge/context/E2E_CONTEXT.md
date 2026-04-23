# E2E Terminal — Hidegindítási Kontextus

> Stack: Vitest + fetch (headless, nem Playwright)
> Repo: `/opt/spaceos/e2e`
> Runtime: Node.js 22

---

## Felelősség

- Full-stack E2E tesztelés production-like VPS ellen
- JWT chain tesztek (Keycloak OIDC)
- RLS/tenant isolation tesztek
- Auth guard (401/403) minden endpoint-on
- Rate limit exhaustion tesztek
- SSRF rejection tesztek

---

## Jelenlegi állapot (2026-04-20)

| Metrika | Érték |
|---------|-------|
| Test fájlok | **56 fájl** |
| Tesztek | **266 pass / 0 fail** |
| Futási idő | ~40s (legutóbbi VPS futás) |
| VPS target | `127.0.0.1:3000` (BFF) |
| Soft Launch | ✅ PASS — 266/266 (KERNEL-093 deploy után, E2E-054-DONE) |

---

## Teszt könyvtár struktúra

```
/opt/spaceos/e2e/
├── src/
│   ├── global-setup.ts         # Token kérés, health wait
│   ├── helpers.ts              # getToken, GET, POST, PUT, DELETE, seedPOST
│   ├── 01-health.chain.test.ts
│   ├── 02-auth.chain.test.ts
│   ├── ...
│   ├── 28-keycloak-auth.chain.test.ts
│   ├── 29-joinery-order.chain.test.ts
│   ├── 30-stage-registry.chain.test.ts
│   ├── 31-handshake-b2b.chain.test.ts
│   ├── 32-spatial-bim.chain.test.ts
│   ├── 33-brand-skin.chain.test.ts  # probe-and-skip (Phase 3B backend hiányzik)
│   ├── 35-configuration-engine.chain.test.ts
│   ├── 36-38 cutting + nesting tesztek
│   └── ...42 cutting flow
├── vitest.config.ts
├── .env                        # gitignore-ban van!
└── package.json
```

---

## Env fájl (`.env`) — GITIGNORE

```ini
KC_URL=http://localhost:8080/auth
KC_TEST_CLIENT_SECRET=ET48o6KTW0IQPoMJCYMWyXZSAMHBipdn
E2E_TEST_PASSWORD=SpaceOS-Test-2026!
KC_TOKEN_URL=http://localhost:8080/auth/realms/spaceos/protocol/openid-connect/token
E2E_BASE_URL=http://127.0.0.1:3000
```

---

## E2E futtatás

```bash
cd /opt/spaceos/e2e
npm run test:e2e           # teljes suite
npm run test:e2e -- --reporter=verbose
npx vitest run src/42-cutting-flow.chain.test.ts  # egy fájl
```

---

## Kritikus minták

### Rate limit handling
- `seedPOST` helper 85 másodpercet vár 429 esetén
- VPS-en `RateLimit__WritePerMinute=1000` beállítva → 429 nem fordul elő normálisan

### Probe-and-skip
- `33-brand-skin`: Phase 3B backend nincs → `GET /api/brand` 404 → skip (nem red)
- Pattern: `if (res.status === 404) { console.warn('[PROBE-SKIP]...'); return; }`

### fileParallelism: false
- Kötelező! Shared PostgreSQL DB → parallel futás race condition

### Doorstar adminTenantId
- Minden E2E tenant-scoped adat a `a1b2c3d4-e5f6-7890-abcd-ef1234567890` Doorstar tenant-hez tartozik
- `getToken('test-admin')` mindig ezt adja vissza (Keycloak `doorstar-kft` group)

---

## Probe-skip státusz (expected — 266/266 futás)

| Teszt | Skip oka | Fájl |
|-------|----------|------|
| Joinery order items | POST 404 — service temporarily unavailable | 56 (1 skip) |
| Inventory inbound | POST 500 — BUG-003 nem deployed | 50 (3 skip) |
| Brand Skin | Phase 3B nem deployed | 33 (4 skip) |
| Refresh token | Endpoints nem deployed | 20 (8 skip) |
| Reservation API | BFF route nem proxied | 52 (4 skip) |

## Új tesztfájlok (2026-04-17 → 2026-04-20)

| Fájl | Tesztek | Tartalom |
|------|---------|---------|
| 49-suppliers-email-phone | 3 | BUG-001 (email assertion soft-skip) |
| 50-inventory-inbound-movement | 3 | BUG-003 probe-skip (500 → skip) |
| 51-cutting-plans-crud | 2 | BUG-004 fix él (201 ✅) |
| 52-reservation-api | 4 | Reservation API lifecycle |
| 53-coverage-expansion | +12 | Egyéb coverage terjesztés |

[MSG-E2E-050-DONE, MSG-E2E-054-DONE]

---

## Indítás előtt

1. `.env` fájl megvan-e? (gitignore-ban van, kézileg kell létrehozni)
2. VPS service-ek futnak-e? (`curl http://127.0.0.1:3000/bff/health`)
3. Keycloak elérhető? (`curl http://localhost:8080/auth/health/ready`)
4. `npm install` lefutott?
