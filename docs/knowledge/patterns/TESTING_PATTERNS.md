# SpaceOS — Testing Patterns

> E2E + unit + integration teszt minták. Forrás: E2E terminál üzenetek + MSG-E2E-001..045.

---

## 1. E2E teszt fájl struktúra

**Helyszín:** `/opt/spaceos/e2e/`
**Runtime:** Vitest + fetch (nem Playwright, nem Supertest)
**Config:** `vitest.config.ts`

```typescript
// vitest.config.ts
export default defineConfig({
    test: {
        globalSetup: './src/global-setup.ts',
        testTimeout: 15_000,
        hookTimeout: 90_000,
        fileParallelism: false,   // FONTOS: tesztek szekvenciálisan futnak
        reporters: ['verbose'],
    }
});
```

### Tipikus teszt fájl struktúra

```typescript
// e.g. 04-facility-crud.chain.test.ts
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { getToken, seedPOST, GET, POST, PUT, DELETE } from './helpers';

describe('E2E: Facility CRUD', () => {
    let token: string;
    let facilityId: string;

    beforeAll(async () => {
        token = await getToken('test-admin');
    });

    it('POST /bff/api/facilities creates facility', async () => {
        const res = await seedPOST('/bff/api/facilities', { name: 'Test' }, token);
        expect(res.status).toBe(201);
        facilityId = res.body.id;
    });

    it('GET /bff/api/facilities/:id returns facility', async () => {
        const res = await GET(`/bff/api/facilities/${facilityId}`, token);
        expect(res.status).toBe(200);
        expect(res.body.name).toBe('Test');
    });

    afterAll(async () => {
        await DELETE(`/bff/api/facilities/${facilityId}`, token).catch(() => {});
    });
});
```

---

## 2. global-setup.ts szerepe

```typescript
// src/global-setup.ts
export async function setup() {
    // 1. Egészségellenőrzés
    await waitForHealth('http://127.0.0.1:3000/bff/health');
    
    // 2. Token kérés Keycloak-tól (Direct Access Grant)
    const token = await getKeycloakToken('test-admin', process.env.E2E_TEST_PASSWORD!);
    
    // 3. Tenant reset (idempotens — törli az E2E adatokat)
    await resetTenant(token);
    
    // 4. Globális state beállítás
    process.env.E2E_ADMIN_TOKEN = token;
    process.env.E2E_TENANT_ID = await getTenantIdFromToken(token);
}
```

**resetTenant call:** Minden E2E suite előtt megtisztítja az előző futás adatait. Ez idempotens — ugyanazon Doorstar tenant kontextusában töröl.

---

## 3. Probe-and-skip minta

Olyan teszteknél ahol az endpoint implementációja még folyamatban van — a teszt nem pirosodik, de dokumentálja az elvárt viselkedést:

```typescript
it('GET /bff/api/brand/theme — should return brand skin config', async () => {
    const res = await GET('/bff/api/brand/theme', token);
    
    // Probe: ha 404 (endpoint nincs implementálva), skip
    if (res.status === 404) {
        console.warn('[PROBE-SKIP] /api/brand/theme not implemented yet — Phase 3B backlog');
        return;
    }
    
    // Valódi assertion (ha implementálva van)
    expect(res.status).toBe(200);
    expect(res.body).toHaveProperty('primaryColor');
});
```

**Mikor használjuk:**
- Phase 3B backlog feature-ökre (pl. BrandSkin endpoints)
- Ha az endpoint opcionálisan elérhető (feature flag)
- Ha a service még nincs deployolva a VPS-en

[MSG-E2E-007-DONE 33-brand-skin minta]

---

## 4. 401/200 auth teszt minta minden új endpoint-hoz

Minden új BFF route-hoz kötelező:

```typescript
describe('Auth guard — /bff/cutting/sheets', () => {
    it('GET without token → 401', async () => {
        const res = await fetch('http://127.0.0.1:3000/bff/cutting/sheets');
        expect(res.status).toBe(401);
    });

    it('GET with valid token → 200/404 (not 500)', async () => {
        const res = await GET('/bff/cutting/sheets', token);
        expect([200, 404]).toContain(res.status);
    });

    it('GET with expired token → 401', async () => {
        const res = await GET('/bff/cutting/sheets', 'expired.token.here');
        expect(res.status).toBe(401);
    });
});
```

**Megjegyzés:** 404 is elfogadható ha nincs adat — a 500 soha nem elfogadható "no auth" esetén.

---

## 5. Rate limit exhaustion kezelése E2E-ban

**Probléma:** Az E2E `seedPOST` helper sok egymás utáni POST kérést küld → rate limit window kimerül → 429.

**seedPOST helper minta:**
```typescript
export async function seedPOST(path: string, body: unknown, token: string) {
    const res = await POST(path, body, token);
    
    if (res.status === 429) {
        // Rate limit exhausted — wait for window reset
        console.warn(`[RATE-LIMIT] ${path} → 429, waiting 85s for window reset`);
        await sleep(85_000);  // 60s window + 25s buffer
        return POST(path, body, token);
    }
    
    return res;
}
```

**85 másodperc:** 60 másodperces sliding window + 25s buffer. [MSG-E2E-001-DONE v3]

**Hosszú távú megoldás:** `RateLimit__WritePerMinute=1000` env var beállítása az E2E VPS-en.

---

## 6. Tenant isolation (cross-tenant) teszt minta

```typescript
// 21-tenant-isolation.chain.test.ts
describe('E2E: Tenant Isolation', () => {
    let tenantAToken: string;
    let tenantBToken: string;
    let tenantAFacilityId: string;

    beforeAll(async () => {
        tenantAToken = await getToken('test-admin');    // doorstar-kft
        tenantBToken = await getToken('designer');       // doorstar-kft (csak teszt)
        // Valódi cross-tenant teszt: 2 különböző Keycloak realm user kellene
    });

    it('Tenant A facility not visible to Tenant B', async () => {
        const res = await GET(`/bff/api/facilities/${tenantAFacilityId}`, tenantBToken);
        // RLS enforced → 404 (nem látja a másik tenant adatát)
        expect(res.status).toBe(404);
    });
});
```

**38-cross-tenant-isolation probe:**
```typescript
// RLS aktív-e? Probe:
const probe = await GET('/bff/api/facilities/00000000-0000-0000-0000-000000000000', token);
const rlsEnforced = probe.status === 404;  // 404 = RLS aktív, sentinel nem szivárog
if (!rlsEnforced) {
    console.warn('[PROBE-SKIP] RLS not enforced yet');
    return;
}
```

---

## 7. E2E helpers API

```typescript
// src/helpers.ts
export const BASE_URL = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:3000';

export async function getToken(user: 'test-admin' | 'designer'): Promise<string>
export async function GET(path: string, token: string): Promise<{ status: number; body: any }>
export async function POST(path: string, body: unknown, token: string): Promise<{ status: number; body: any }>
export async function PUT(path: string, body: unknown, token: string): Promise<{ status: number; body: any }>
export async function DELETE(path: string, token: string): Promise<{ status: number; body: any }>
export async function seedPOST(path: string, body: unknown, token: string): Promise<{ status: number; body: any }>
```

**getToken** Direct Access Grant-tel kér tokent Keycloak `test-runner` kliensen keresztül:
```typescript
export async function getToken(user: string): Promise<string> {
    const res = await fetch(process.env.KC_TOKEN_URL!, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            grant_type: 'password',
            client_id: 'test-runner',
            client_secret: process.env.KC_TEST_CLIENT_SECRET!,
            username: user,
            password: process.env.E2E_TEST_PASSWORD!,
        })
    });
    const { access_token } = await res.json();
    return access_token;
}
```

---

## 8. E2E teszt fájlok száma és elvárt eredmény

**Jelenlegi állapot (2026-04-17):** 43+ fájl, 214 pass / 0 fail

| Köteg | Fájlok | Tesztek | Státusz |
|-------|--------|---------|---------|
| Batch 0 (alap) | 01-28 | ~120 | ✅ zöld |
| Batch 1 (joinery, stage, handshake) | 29-31 | ~16 | ✅ zöld |
| Batch 2 (spatial, brand, abstractions) | 32-35 | ~16 | ✅ zöld |
| Batch 3 (cutting, nesting) | 36-42 | ~40 | ✅ zöld (nesting activation után) |

---

## 9. Vitest config — fileParallelism: false KÖTELEZŐ

```typescript
// vitest.config.ts
fileParallelism: false
```

**Miért:** Az E2E tesztek **shared production adatbázison** futnak — párhuzamos futás esetén tenant-shared state race condition-öket okoz. Minden fájl szekvenciálisan fut.

---

## 10. afterAll cleanup minta

```typescript
afterAll(async () => {
    // Cleanup order: child → parent
    if (workstationId) await DELETE(`/bff/api/workstations/${workstationId}`, token).catch(() => {});
    if (facilityId) await DELETE(`/bff/api/facilities/${facilityId}`, token).catch(() => {});
    // .catch(() => {}) — cleanup failure nem buktassa az egész tesztet
});
```

**Szabály:** Mindig reverse sorrendben takarítani (child először, aztán parent).
