# Epic: E29 — OpenAPI Contract Sync

**Prioritás:** P1
**Projekt(ek):** kernel, portal
**Státusz:** BACKLOG_READY
**Előfeltétel:** E28 CLOSED_DONE

---

## Kontextus

A manuális tesztelés során kiderült, hogy a frontend és a Kernel típusai (enumok, DTO mezők, route-ok) többször nem egyeztek. Ez minden alkalommal runtime 500-as hibát okozott, amit csak manuálisan lehetett felfedezni.

Talált eltérések (2026-03-31 — 2026-04-01):
- `WorkStationStatus`: frontend `Idle/Offline` vs Kernel `Available/Outdated`
- `TradeType`: frontend `Generic/MEP` vs Kernel `Plumbing/Mep`
- API route-ok: frontend flat (`/api/workstations`) vs Kernel nested (`/api/facilities/:id/work-stations`)
- Response formátum: frontend `T[]` vs Kernel `PagedList<T>`

**Cél:** A Kernel OpenAPI spec legyen az egyetlen igazság forrása. A frontend típusai ebből generálódjanak, így enum/DTO eltérés → build error, nem runtime 500.

## Scope

### Beletartozik

1. Kernel OpenAPI JSON exportálása dev módban
2. `openapi-typescript` generátor beépítése a Design Portal-ba
3. Frontend service-ek és típusok átállítása a generált típusokra
4. npm script: `npm run sync-types` — újragenerálja a típusokat
5. CI-be integrálható contract teszt

### NEM tartozik bele

- Orchestrator típus generálás (az a proxy-n megy át, nem saját típusok)
- Runtime schema validáció (zod runtime check)
- API verziókezelés (v1/v2)

## Acceptance Criteria

- [ ] `npm run sync-types` lefut és generálja `src/types/generated.ts`-t a Kernel OpenAPI-ból
- [ ] A meglévő `src/types/index.ts` és `src/types/common.ts` a generált típusokra épül (re-export vagy közvetlen használat)
- [ ] Ha a Kernel enum változik és a frontend nem fut `sync-types`-ot → TypeScript build error
- [ ] Minden enum (WorkStationStatus, TradeType, FsmState) a generált fájlból jön
- [ ] Minden DTO (TenantDto, FacilityDto, WorkStationDto, stb.) a generált fájlból jön
- [ ] `npm run build` → 0 error a generált típusokkal
- [ ] `npm test` → 0 fail
- [ ] Contract teszt: compare generált vs jelenlegi — nincs drift

## Érintett projektek és feladataik

| Projekt | Feladat | Prioritás |
|---------|---------|-----------|
| Kernel | OpenAPI JSON elérhetővé tétele dev módban (már kész — `/openapi/v1.json`) | P1 (DONE) |
| Kernel | Swagger enum és DTO annotációk ellenőrzése — minden publikus típus megjelenik az OpenAPI-ban | P1 |
| Portal | `openapi-typescript` package hozzáadása | P1 |
| Portal | `npm run sync-types` script létrehozása | P1 |
| Portal | `src/types/generated.ts` generálása | P1 |
| Portal | Meglévő típusok átállítása generáltra | P2 |
| Portal | Contract teszt hozzáadása | P2 |

## Cross-project függőségek

1. Kernel OpenAPI-nak pontosnak kell lennie (enum értékek, DTO mezők)
2. Portal `sync-types` a futó Kernel-t hívja — dev workflow: Kernel fut → `npm run sync-types` → `npm run build`

## Technikai terv

### Portal oldal

```bash
# 1. Install
npm install -D openapi-typescript

# 2. package.json script
"sync-types": "npx openapi-typescript http://localhost:5000/openapi/v1.json -o src/types/generated.ts"

# 3. Usage in existing types
// src/types/index.ts
import type { components } from './generated';

export type TenantDto = components['schemas']['TenantDto'];
export type WorkStationDto = components['schemas']['WorkStationDto'];
export type WorkStationStatus = components['schemas']['WorkStationStatus'];
export type TradeType = components['schemas']['TradeType'];
```

### Contract teszt

```typescript
// src/api/contract.test.ts
import { describe, it, expect } from 'vitest';
import type { components } from '@/types/generated';

describe('API Contract', () => {
  it('WorkStationStatus has expected values', () => {
    // If the generated type changes, this test catches it
    const valid: components['schemas']['WorkStationStatus'][] = [
      'Available', 'Occupied', 'Maintenance', 'Outdated', 'Active'
    ];
    expect(valid).toHaveLength(5);
  });
});
```
