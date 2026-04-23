# SpaceOS — Phase 3C+ Architecture
## `@spaceos/joinery-ui` + Joinery Module System + Live B2BHandshake

> Verzió: v3.0 — 2026-04-07  
> Státusz: **IMPLEMENTÁCIÓRA KÉSZ — minden döntés lezárva**  
> Blokkoló feltétel: Phase 3C DoD teljes (`pnpm turbo build` 0 error)  
> Érintett repók: `spaceos-design-portal` · `spaceos-orchestrator` · `spaceos-kernel`  
> Becsült effort: **16 fejlesztői nap** (v1: 12 → v2: +3 D-03 Live → v3: +1 review findings)  
> Kumulált review: `/senior-frontend` + `/senior-security` → v3 · 10 új finding (2C+4H+4M)

---

## 1. Lezárt döntések

| ID | Döntés | Következmény |
|----|--------|--------------|
| D-01 | `EnabledModules varchar[]` column a Kernel `Tenants` táblájában | Migration 0025; Orchestrator response bővül |
| D-02 | Flat `index.ts` — whitelist re-export | Egyszerűbb import, tree-shaking Vite-ban |
| D-03 | **Live B2BHandshake API** | Orchestrator `/bff/handshakes` proxy szükséges (+3 nap) |
| D-04 | **Modul-rendszer `apps/joinerytech`-ben** — nem külön app | `enabledModules` tenant config dönti el, melyik modul-route látható |
| D-05 | `window/` üres placeholder scaffold | Horizon 3 tölti fel; körhivatkozás kizárva |
| D-06 | `TenantHandshakeAllowlist` join tábla (Opció A) — Migration 0026 | Extra adat és trade-szűrés jövőállósága miatt join tábla, nem uuid[] array |

---

## 2. A kulcsfontosságú felismerés

Egy faiparos cég egyszerre lehet **ajtógyártó** és **szekrénygyártó** — kontextustól függően más workflow-t használ, de az infrastruktúra közös. Külön appok helyett egy app, tenant-onként konfigurálható modul-készlettel.

```
Tenant: Doorstar Kft.
  enabledModules: ['door']
  → /door/cutting, /door/kanban látható
  → /cabinet/* nem renderelődik

Tenant: Nagy Asztalos Kft.
  enabledModules: ['door', 'cabinet']
  → /door/* és /cabinet/* is elérhető
  → egy cég, két szakági workflow

Tenant: Kis Szekrénygyártó Bt.
  enabledModules: ['cabinet']
  → csak /cabinet/* látható
  → + /orders/* (B2BHandshake: ajtót rendel Doorstartól)
```

---

## 3. Teljes struktúra (végállapot)

### 3.1 Turborepo monorepo

```
spaceos-design-portal/
  apps/
    joinerytech/                        ← az egyetlen joinery app
      src/
        modules/
          door/
            routes.tsx                  ← /door/* route-ok (lazy)
            DoorDashboard.tsx
            DoorCuttingListPage.tsx
            DoorKanbanPage.tsx
          cabinet/
            routes.tsx                  ← /cabinet/* route-ok (lazy)
            CabinetDashboard.tsx
            CabinetCuttingListPage.tsx
            CabinetKanbanPage.tsx
            CabinetOrdersPage.tsx       ← B2BHandshake: rendel Doorstartól
          window/
            routes.tsx                  ← placeholder, Horizon 3
        brand/
          overrides.ts                  ← page override-ok (Phase 3C-ból)
        routes/
          moduleRouter.tsx              ← ÚJ: enabledModules alapján lazy-load
          brandRouter.tsx               ← Phase 3C-ból, brand skin
        App.tsx                         ← BrandProvider + ModuleProvider wrapper
      vite.config.ts
      tailwind.config.ts
      package.json
  packages/
    @spaceos/ui/                        ← generikus (Phase 3C)
    @spaceos/domain/                    ← OpenAPI codegen (Phase 3C)
    @spaceos/api-client/                ← typed HTTP (Phase 3C) — bővül /handshakes
    @spaceos/brand-tokens/              ← brand skin (Phase 3C)
    @spaceos/i18n/                      ← en/hu (Phase 3C)
    @spaceos/joinery-ui/                ← ÚJ: shared joinery komponensek
      src/
        base/
          CuttingListBase/
          ProductionKanban/
          MaterialPanel/
          HandshakeOrderPanel/          ← guest oldal: rendel másiktól
          HandshakeHostPanel/           ← host oldal: kiad másnak
        door/
          DoorCuttingList.tsx
          DoorSpecPanel.tsx
        cabinet/
          CabinetCuttingList.tsx
          CabinetSpecPanel.tsx
        window/
          WindowCuttingList.tsx         ← placeholder
        index.ts                        ← whitelist re-export
  vitest.workspace.ts
  turbo.json
  pnpm-workspace.yaml
```

### 3.2 Dependency graph

```
apps/joinerytech
       ↓
@spaceos/joinery-ui ──→ @spaceos/ui
                    ──→ @spaceos/brand-tokens
                    ──→ @spaceos/domain
                    ──→ @spaceos/api-client
                    ──→ @spaceos/i18n

@spaceos/ui              ← nulla internal dep
@spaceos/brand-tokens    ← nulla internal dep
@spaceos/domain          ← nulla internal dep
@spaceos/api-client ──→ @spaceos/domain
@spaceos/i18n            ← nulla internal dep

❌ TILOS: packages/* → apps/*
❌ TILOS: @spaceos/ui → @spaceos/joinery-ui
❌ TILOS: @spaceos/joinery-ui → apps/*
```

---

## 4. `moduleRouter.tsx` — a modul-rendszer motorja

```typescript
// apps/joinerytech/src/routes/moduleRouter.tsx
// ⚠ NOT A SECURITY BOUNDARY — RBAC az Orchestrator-ban
// enabledModules csak UI visibility-t vezérel

import { lazy } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const DoorRoutes    = lazy(() => import('../modules/door/routes'));
const CabinetRoutes = lazy(() => import('../modules/cabinet/routes'));
const WindowRoutes  = lazy(() => import('../modules/window/routes'));   // placeholder

const MODULE_ROUTES: Record<string, React.LazyExoticComponent<() => JSX.Element>> = {
  door:    DoorRoutes,
  cabinet: CabinetRoutes,
  window:  WindowRoutes,
};

export function ModuleRouter() {
  const enabledModules = useAuthStore(s => s.enabledModules);  // Orchestrator response-ból

  if (isAuthLoading) return <PageSkeleton />;  // BE-P3CP-10: auth race condition fix

  return (
    <ErrorBoundary fallback={<ModuleLoadError />}>  {/* BE-P3CP-09 */}
      <Suspense fallback={<PageSkeleton />}>          {/* BE-P3CP-06 */}
        <Routes>
          {(enabledModules ?? []).map(mod => {        /* BE-P3CP-08: nullish coalesce */
            const ModuleComponent = MODULE_ROUTES[mod];
            return ModuleComponent
              ? <Route key={mod} path={`/${mod}/*`} element={<ModuleComponent />} />
              : null;
          })}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
}
```

```typescript
// apps/joinerytech/src/modules/cabinet/routes.tsx
import { Routes, Route } from 'react-router-dom';
import { CabinetDashboard }      from './CabinetDashboard';
import { CabinetCuttingListPage } from './CabinetCuttingListPage';
import { CabinetKanbanPage }      from './CabinetKanbanPage';
import { CabinetOrdersPage }      from './CabinetOrdersPage';

export default function CabinetRoutes() {
  return (
    <Routes>
      <Route index          element={<CabinetDashboard />} />
      <Route path="cutting" element={<CabinetCuttingListPage />} />
      <Route path="kanban"  element={<CabinetKanbanPage />} />
      <Route path="orders"  element={<CabinetOrdersPage />} />   {/* B2BHandshake */}
    </Routes>
  );
}
```

---

## 5. `@spaceos/joinery-ui` — komponens architektúra

### 5.1 Composition + Slot pattern

A `CuttingListBase` nem tud a trade-ről. Specializált komponensek wrapper-ként viselkednek.

```typescript
// @spaceos/joinery-ui/src/base/CuttingListBase/index.tsx
interface CuttingListBaseProps {
  items: CutItem[];
  onExport?: () => void;
  specSlot?:   React.ReactNode;   // trade-specifikus spec panel
  actionSlot?: React.ReactNode;   // trade-specifikus action gombok
}

export function CuttingListBase({ items, specSlot, actionSlot }: CuttingListBaseProps) {
  return (
    <PageShell>
      {specSlot   && <div className="mb-4">{specSlot}</div>}
      <CutItemTable items={items} />
      {actionSlot && <div className="mt-4 flex gap-2">{actionSlot}</div>}
    </PageShell>
  );
}
// ❌ NEM szerepel az index.ts-ben — belső impl detail
```

```typescript
// @spaceos/joinery-ui/src/cabinet/CabinetCuttingList.tsx
import { CuttingListBase } from '../base/CuttingListBase';
import { CabinetSpecPanel } from './CabinetSpecPanel';

export function CabinetCuttingList(props: CabinetCuttingListProps) {
  return (
    <CuttingListBase
      items={props.items}
      specSlot={<CabinetSpecPanel corpus={props.corpus} drawers={props.drawers} />}
      // actionSlot: nincs CNC export — cabinet modul nem exportál Doorstar CNC-re
    />
  );
}
```

### 5.2 `HandshakeOrderPanel` — live B2BHandshake

```typescript
// @spaceos/joinery-ui/src/base/HandshakeOrderPanel/index.tsx
// ⚠ NOT A SECURITY BOUNDARY — RBAC az Orchestrator JWT middleware-ben

interface HandshakeOrderPanelProps {
  // allowedHosts az Orchestrator /bff/api/auth/token response-ból — soha nem URL/localStorage
  allowedHosts: Array<{ tenantId: string; tenantName: string; allowedTradeTypes: string[] }>;
  activeTrade: string;                              // BE-P3CP-07: szűréshez
  onHandshakeCreated: (handshakeId: string) => void;
}
// Belül: const visibleHosts = allowedHosts.filter(h => h.allowedTradeTypes.includes(activeTrade));

export function HandshakeOrderPanel({ allowedHosts, onHandshakeCreated }: HandshakeOrderPanelProps) {
  const { mutate: createHandshake, isPending } = useCreateHandshake();

  return (
    <Card>
      <h2 className="text-lg font-semibold mb-4">Rendelés indítása</h2>
      <HostSelector hosts={allowedHosts} />
      <HandshakeForm onSubmit={data => createHandshake(data, {
        onSuccess: res => onHandshakeCreated(res.handshakeId),
      })} />
      {isPending && <Spinner />}
    </Card>
  );
}
```

### 5.3 Publikus `index.ts`

```typescript
// @spaceos/joinery-ui/src/index.ts
export { DoorCuttingList }     from './door/DoorCuttingList';
export { DoorSpecPanel }       from './door/DoorSpecPanel';
export { CabinetCuttingList }  from './cabinet/CabinetCuttingList';
export { CabinetSpecPanel }    from './cabinet/CabinetSpecPanel';
export { WindowCuttingList }   from './window/WindowCuttingList';   // placeholder stub
export { ProductionKanban }    from './base/ProductionKanban';
export { MaterialPanel }       from './base/MaterialPanel';
export { HandshakeOrderPanel } from './base/HandshakeOrderPanel';
export { HandshakeHostPanel }  from './base/HandshakeHostPanel';
// ❌ CuttingListBase — internal, nem exportált
```

---

## 6. Orchestrator változások

### 6.1 Auth token response bővítése

```typescript
// spaceos-orchestrator/src/routes/auth.ts

// Régi response (Phase 3C):
// { accessToken, brandSkin, expiresAt }

// Új response (Phase 3C+):
{
  accessToken:    string,
  brandSkin:      string,           // 'doorstar' | 'cabinetmaker' | ...
  expiresAt:      string,
  enabledModules: string[],         // ['door'] | ['cabinet'] | ['door', 'cabinet']
  allowedHosts: Array<{             // B2BHandshake célpontok (SEC-P3CP-01 fix)
    tenantId:   string,
    tenantName: string,
  }>,
}
```

Az `enabledModules` és `allowedHosts` a Kernel `Tenants.EnabledModules` és a B2BHandshake
allowlist-ből jön — az Orchestrator RS256-verifikált JWT alapján tölti fel.
Frontend soha nem írhatja felül ezeket.

```typescript
// useAuthStore — Zustand persist partialize (SEC-P3CP-05)
export const useAuthStore = create(
  persist(
    (set) => ({ ... }),
    {
      name: 'spaceos-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        expiresAt:   state.expiresAt,
        // enabledModules: KIZÁRVA — SEC-P3CP-05
        // allowedHosts:   KIZÁRVA — SEC-P3CP-05
        // brandSkin:      KIZÁRVA — Phase 3C SEC-P3C-07 analógia
      }),
    }
  )
);
```

### 6.2 `/bff/handshakes` proxy — ÚJ (D-03 szükséglet)

```typescript
// spaceos-orchestrator/src/routes/handshakes.ts
// HIÁNYZÓ ENDPOINT — Phase 3C+-ban implementálandó

import { Router } from 'express';
import { requireAuth } from '../middleware/auth';
import { kernelProxy } from '../lib/kernelProxy';

const router = Router();

router.use(requireAuth);

// POST /bff/handshakes           → POST /api/handshakes
// GET  /bff/handshakes           → GET  /api/handshakes
// GET  /bff/handshakes/:id       → GET  /api/handshakes/:id
// PUT  /bff/handshakes/:id/accept → PUT /api/handshakes/:id/accept
// PUT  /bff/handshakes/:id/reject → PUT /api/handshakes/:id/reject

router.all('*', kernelProxy('/api/handshakes'));

export { router as handshakesRouter };
```

```typescript
// spaceos-orchestrator/src/app.ts — regisztráció
app.use('/bff/handshakes', handshakesRouter);
```

### 6.3 Cabinet system prompt

```typescript
// spaceos-orchestrator/src/brands/cabinetmaker/systemPrompt.ts
export const cabinetmakerSystemPrompt = `
You are a professional cabinet making assistant.
- Help plan cutting lists (lapszabászat) for corpus construction
- Calculate material requirements (anyagszükséglet)
- Assist with ordering doors and hardware from partner manufacturers
- Track production FSM states for cabinet manufacturing
- All measurements in mm; default material: 18mm MDF
- Trade context: cabinet manufacturing (szekrénygyártás)
`;
```

---

## 7. Kernel változások

### 7.1 Migration 0025 — EnabledModules

```sql
-- Migration 0025: EnabledModules per Tenant
ALTER TABLE "Tenants"
  ADD COLUMN "EnabledModules" varchar(32)[] NOT NULL DEFAULT '{}';

-- Meglévő Doorstar tenant frissítése
UPDATE "Tenants"
  SET "EnabledModules" = ARRAY['door']
  WHERE "BrandSkinId" = 'doorstar';

-- CHECK constraint: csak ismert modulok
ALTER TABLE "Tenants"
  ADD CONSTRAINT "CK_Tenants_EnabledModules_Valid"
  CHECK (
    "EnabledModules" <@ ARRAY['door','cabinet','window']::varchar(32)[]
  );
```

### 7.2 Migration 0026 — TenantHandshakeAllowlist (D-06)

**Döntés: Opció A — join tábla.** Indok: trade-specifikus szűrés jövőbeli igénye
(pl. Asztalos csak `door` modult rendelhet Doorstartól, `window`-t nem) join tábla nélkül
nem modellezhető. `uuid[]` array ezt nem tudná hordozni.

```sql
-- Migration 0026: TenantHandshakeAllowlist
CREATE TABLE "TenantHandshakeAllowlist" (
  "GuestTenantId"     uuid         NOT NULL,
  "HostTenantId"      uuid         NOT NULL,
  "AllowedTradeTypes" varchar(32)[] NOT NULL DEFAULT '{}',
  "CreatedAt"         timestamptz  NOT NULL DEFAULT now(),
  CONSTRAINT "PK_TenantHandshakeAllowlist"
    PRIMARY KEY ("GuestTenantId", "HostTenantId"),
  CONSTRAINT "FK_TenantHandshakeAllowlist_Guest"
    FOREIGN KEY ("GuestTenantId") REFERENCES "Tenants"("Id") ON DELETE CASCADE,
  CONSTRAINT "FK_TenantHandshakeAllowlist_Host"
    FOREIGN KEY ("HostTenantId")  REFERENCES "Tenants"("Id") ON DELETE CASCADE,
  CONSTRAINT "CK_TenantHandshakeAllowlist_NoSelfLink"
    CHECK ("GuestTenantId" <> "HostTenantId"),
  CONSTRAINT "CK_TenantHandshakeAllowlist_TradeTypes"
    CHECK ("AllowedTradeTypes" <@ ARRAY['door','cabinet','window']::varchar(32)[]),
  CONSTRAINT "CK_AllowedTradeTypes_NotEmpty"
    CHECK (cardinality("AllowedTradeTypes") > 0)              -- SEC-P3CP-07
);

CREATE INDEX "IX_TenantHandshakeAllowlist_Guest"
  ON "TenantHandshakeAllowlist" ("GuestTenantId");

-- RLS: guest csak saját sorait látja
ALTER TABLE "TenantHandshakeAllowlist" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "TenantHandshakeAllowlist" FORCE ROW LEVEL SECURITY;

CREATE POLICY "TenantHandshakeAllowlist_TenantIsolation"
  ON "TenantHandshakeAllowlist"
  USING (
    "GuestTenantId" = current_setting('app.current_tenant_id')::uuid
    OR "HostTenantId" = current_setting('app.current_tenant_id')::uuid
  );

-- Seed: Asztalos Demo → Doorstar (minden trade)
INSERT INTO "TenantHandshakeAllowlist" ("GuestTenantId","HostTenantId","AllowedTradeTypes")
  SELECT g."Id", h."Id", ARRAY['door']
  FROM "Tenants" g, "Tenants" h
  WHERE g."BrandSkinId" = 'cabinetmaker'
    AND h."BrandSkinId" = 'doorstar'
ON CONFLICT ("GuestTenantId","HostTenantId") DO NOTHING;  -- SEC-P3CP-09
```

### 7.3 Domain + Repository — AllowedHostsAsync

```csharp
// SpaceOS.Kernel.Domain / Entities / TenantHandshakeAllowlist.cs (ÚJ)
public class TenantHandshakeAllowlist : TenantScopedEntity
{
    public Guid GuestTenantId    { get; private set; }
    public Guid HostTenantId     { get; private set; }
    public IReadOnlyList<string> AllowedTradeTypes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private TenantHandshakeAllowlist() { }  // EF

    public static TenantHandshakeAllowlist Create(
        Guid guestTenantId,
        Guid hostTenantId,
        IEnumerable<string> allowedTradeTypes)
    {
        // validáció: noSelfLink, known trade types
        return new TenantHandshakeAllowlist { ... };
    }
}

// SpaceOS.Kernel.Domain / Interfaces / ITenantHandshakeAllowlistRepository.cs (ÚJ)
public interface ITenantHandshakeAllowlistRepository
{
    Task<IReadOnlyList<AllowedHostDto>> GetAllowedHostsAsync(
        Guid guestTenantId,
        CancellationToken ct = default);
}

// AllowedHostDto
public record AllowedHostDto(
    Guid   TenantId,
    string TenantName,
    IReadOnlyList<string> AllowedTradeTypes
);
```

### 7.4 Kernel API — token claims bővítése

```csharp
// CreateTokenCommandHandler.cs — meglévő claims mellé (módosítás)

var enabledModulesJson = JsonSerializer.Serialize(
    tenant.EnabledModules ?? Array.Empty<string>());
claims.Add(new Claim("enabled_modules", enabledModulesJson));

var allowedHosts = await _allowlistRepo.GetAllowedHostsAsync(
    tenant.Id, cancellationToken).ConfigureAwait(false);
var allowedHostsJson = JsonSerializer.Serialize(allowedHosts);
claims.Add(new Claim("allowed_hosts", allowedHostsJson));
```

### 7.5 B2BHandshake API endpoint — státusz

Sprint C Phase 2-ben implementálva: `B2BHandshake` domain entity + repository.  
`POST /api/handshakes` Minimal API endpoint: **ellenőrizni kell a Kernel repóban.**  
Ha nincs → Nap 8 kiterjeszt (+1-2 nap).

> ⚠ **Ellenőrizni kell Phase 3C+ indítása előtt:**  
> `curl -X POST https://joinerytech.hu/bff/handshakes` → nem 404, nem 405

---

## 8. Finding-ek (kumulált review — v3 final)

### v1/v2 pre-emptive findings (BE-P3CP-01..05, SEC-P3CP-01..04)

| ID | Súly | Terület | Probléma | Javítás |
|----|------|---------|----------|---------|
| BE-P3CP-01 | 🔴 CRITICAL | Circular dep | `@spaceos/joinery-ui` importál `apps/*`-ból | `CuttingListBase` nem exportált; lazy import csak `apps/*`-ban |
| BE-P3CP-02 | 🟠 HIGH | moduleRouter | `MODULE_ROUTES` dict statikus → új modul hozzáadása kód-módosítást igényel | Elfogadott tradeoff — Horizon 3-ig nincs 4. ág |
| BE-P3CP-03 | 🟠 HIGH | enabledModules flash | `useAuthStore(s => s.enabledModules)` default `[]` → üres UI flash | Skeleton loader a ModuleRouter-ben amíg auth betölt |
| BE-P3CP-04 | 🟡 MEDIUM | Tailwind path | `tailwind.config.ts` nem tartalmazza `@spaceos/joinery-ui` path-t | Explicit content: `../../packages/@spaceos/joinery-ui/src/**/*.{ts,tsx}` |
| BE-P3CP-05 | 🟡 MEDIUM | Vitest workspace | Új package nincs `vitest.workspace.ts`-ben | Frissíteni kell minden új package hozzáadásakor |
| SEC-P3CP-01 | 🔴 CRITICAL | Module spoofing | `enabledModules` localStorage-ból override-olható | `enabledModules` kizárólag Orchestrator response; `useAuthStore` setter privát |
| SEC-P3CP-02 | 🔴 CRITICAL | Host spoofing | `allowedHosts` URL param / localStorage-ból | `allowedHosts` kizárólag Orchestrator response-ból; Kernel guard is véd |
| SEC-P3CP-03 | 🟠 HIGH | featureFlags bypass | `enabledModules` UX-only → API nem blokkolható | RBAC Orchestrator JWT middleware-ben |
| SEC-P3CP-04 | 🟡 MEDIUM | Window placeholder | `WindowCuttingList` stub dobhat runtime error | `return null` — nem crashel |

### v3 findings — `/senior-frontend` + `/senior-security` review (+10 finding)

| ID | Súly | Terület | Probléma | Javítás |
|----|------|---------|----------|---------|
| BE-P3CP-06 | 🔴 CRITICAL | Suspense boundary | `moduleRouter.tsx` lazy komponensek `<Suspense>` nélkül → slow network / chunk load delay = blank screen | `<Suspense fallback={<PageSkeleton />}>` wrapper a `ModuleRouter` minden lazy ágán |
| BE-P3CP-07 | 🟠 HIGH | HandshakeOrderPanel trade filter | `allowedHosts` nincs szűrve aktív modul trade type alapján → `/cabinet/orders` oldalon `door`-only host is megjelenik | `HandshakeOrderPanel` kap `activeTrade: string` propot; belül `hosts.filter(h => h.allowedTradeTypes.includes(activeTrade))` |
| BE-P3CP-08 | 🟠 HIGH | enabledModules nullish | `useAuthStore(s => s.enabledModules)` auth betöltés előtt `undefined` → `undefined.map()` crash | `enabledModules ?? []` minden használati helyen; store initial state `enabledModules: []` |
| BE-P3CP-09 | 🟡 MEDIUM | ErrorBoundary hiányzik | Chunk load failure (stale deploy) → unhandled error → teljes app crash | `<ErrorBoundary fallback={<ModuleLoadError />}>` a `Suspense` körül |
| BE-P3CP-10 | 🟡 MEDIUM | Auth race condition | Direkt URL navigáció `/cabinet/cutting`-ra auth töltés közben → `enabledModules = []` → redirect `/`-re; betöltés után elveszett URL | `ModuleRouter` auth loading state: spinner amíg `isAuthLoading`, csak utána renderel vagy redirect |
| SEC-P3CP-05 | 🔴 CRITICAL | Zustand persist szivárgás | `useAuthStore` Zustand `persist` middleware-rel `enabledModules` / `allowedHosts` localStorage-ba kerülhet → attacker módosíthatja | `persist` `partialize` opció: `enabledModules`, `allowedHosts` **kizárva** a persistált state-ből; vagy persist teljes eltávolítása a auth store-ból |
| SEC-P3CP-06 | 🟠 HIGH | Kernel handshake validáció hiányzik | `POST /api/handshakes` nem validálja `hostTenantId` a `TenantHandshakeAllowlist`-en → bypass lehetséges közvetlen API hívással | Kernel `CreateHandshakeCommandHandler`: `_allowlistRepo.GetAllowedHostsAsync(requestingTenantId)` → ha `hostTenantId` nem szerepel → `Result.Forbidden` |
| SEC-P3CP-07 | 🟠 HIGH | Üres AllowedTradeTypes | `AllowedTradeTypes = '{}'` átmegy a CHECK constraint-en → szemantikailag ambiguous ("minden" vagy "semmi"?) | `ALTER TABLE "TenantHandshakeAllowlist" ADD CONSTRAINT "CK_AllowedTradeTypes_NotEmpty" CHECK (cardinality("AllowedTradeTypes") > 0)` |
| SEC-P3CP-08 | 🟡 MEDIUM | JWT claim méret | `allowed_hosts` claim mérete nincs limitálva → nagy tenant hálózatban JWT > 8KB → nginx buffer overflow | Limit: max 20 allowed host per tenant claim-ben; ha több: on-demand `GET /bff/handshakes/allowed-hosts` fetch; limit dokumentálandó ADR-ban |
| SEC-P3CP-09 | 🟡 MEDIUM | Migration seed nem idempotent | `INSERT ... WHERE BrandSkinId = 'cabinetmaker'` több tenant-ra illeszkedhet → PRIMARY KEY violation | `ON CONFLICT ("GuestTenantId","HostTenantId") DO NOTHING` a seed INSERT-en |

---

## 9. Implementációs sorrend (15 nap)

```
Nap  1:   @spaceos/joinery-ui scaffold
          → package.json + workspace:* dep-ek
          → üres index.ts + src/ struktúra (base/, door/, cabinet/, window/)
          → vitest.workspace.ts frissítve
          → ELLENŐRZÉS: pnpm turbo build 0 error, 256 teszt zöld

Nap  2:   CuttingListBase + ProductionKanban
          → base/CuttingListBase/ (specSlot, actionSlot pattern)
          → base/ProductionKanban/ átemeléssel apps/joinerytech-ből (ha ott van)
          → unit tesztek: render, slot injection, empty state, error boundary

Nap  3:   MaterialPanel + HandshakeHostPanel
          → base/MaterialPanel/ (TradeType-agnosztikus inventory display)
          → base/HandshakeHostPanel/ (host oldal: kiad másnak; allowedGuests prop)
          → unit tesztek

Nap  4:   door/ branch
          → DoorSpecPanel: vasalat, élzárás, nyílásirány paraméterei
          → DoorCuttingList: CuttingListBase + DoorSpecPanel compose
          → DoorCuttingList unit tesztek: 6 teszt
          → apps/joinerytech: importálja @spaceos/joinery-ui-ból
          → ELLENŐRZÉS: apps/joinerytech 256 teszt zöld

Nap  5:   cabinet/ branch + window/ placeholder
          → CabinetSpecPanel: korpuszméret, fiók, polc, anyagvastagság
          → CabinetCuttingList: CuttingListBase + CabinetSpecPanel
          → WindowCuttingList: stub (return null)
          → CabinetCuttingList unit tesztek: 6 teszt

Nap  6:   HandshakeOrderPanel (UI, allowedHosts prop, form)
          → base/HandshakeOrderPanel/ (allowedHosts Orchestrator response-ból)
          → Form: host selector + rendelési adatok
          → unit tesztek: render, host select, form submit, error (6 teszt)
          → SEC-P3CP-01/02 gate: localStorage nem írható felül

Nap  7:   moduleRouter.tsx + module route struktúra
          → ModuleRouter: enabledModules lazy-load
          → apps/joinerytech/src/modules/door/routes.tsx
          → apps/joinerytech/src/modules/cabinet/routes.tsx (CabinetOrdersPage stub)
          → apps/joinerytech/src/modules/window/routes.tsx (placeholder)
          → ELLENŐRZÉS: enabledModules: ['door'] → csak /door/* látható

Nap  8:   Kernel — Migration 0025 + token claims
          → EnabledModules varchar[] column + CHECK constraint
          → CreateTokenCommandHandler: enabled_modules + allowed_hosts claim
          → B2BHandshake API endpoint meglét ellenőrzése
            → Ha nincs: Nap 8-9 Kernel endpoint implementálás (+1 nap)
          → Migration 0025 unit tesztek + integration teszt

Nap  9:   Orchestrator — /bff/handshakes proxy (D-03 Live)
          → src/routes/handshakes.ts: kernelProxy('/api/handshakes')
          → app.ts: router regisztráció
          → auth response bővítése: enabledModules + allowedHosts mezők
          → useAuthStore bővítése: enabledModules + allowedHosts tárolás
          → Orchestrator tesztek: /bff/handshakes proxy 4 teszt

Nap 10:   @spaceos/api-client — handshakes API hookup
          → src/hooks/useHandshakes.ts (useCreateHandshake, useGetHandshakes)
          → src/lib/kernelClient.ts: handshakes endpoint hozzáadása
          → HandshakeOrderPanel: useCreateHandshake integrálva (live API)
          → ELLENŐRZÉS: POST /bff/handshakes → Kernel → 201 Created

Nap 11:   CabinetOrdersPage — live B2BHandshake integrálva
          → apps/joinerytech/src/modules/cabinet/CabinetOrdersPage.tsx
          → HandshakeOrderPanel: allowedHosts useAuthStore-ból
          → HandshakeHostPanel: door modulos cégeknek (kap megrendelést)
          → Tesztek: CabinetOrdersPage 5 teszt (render, create, list, error, empty)

Nap 12:   cabinet system prompt + Orchestrator brand routing
          → brands/cabinetmaker/systemPrompt.ts
          → brandSystemPrompts: 'cabinetmaker' ág
          → Orchestrator tesztek: prompt selection 2 teszt

Nap 13:   Tesztek — @spaceos/joinery-ui teljes lefedettség
          → CuttingListBase: 8 teszt
          → DoorCuttingList: 6, CabinetCuttingList: 6, WindowCuttingList: 2 (stub)
          → HandshakeOrderPanel: 8 teszt (allowedHosts enforced)
          → madge --circular packages/ → 0
          → grep "apps/" packages/@spaceos/joinery-ui/src → 0

Nap 14:   Tesztek — moduleRouter + integration
          → ModuleRouter: enabledModules=[] → redirect; ['door'] → door only; ['door','cabinet'] → mind
          → SEC-P3CP-01: localStorage.setItem('enabledModules',...) → nincs hatás
          → SEC-P3CP-02: URL ?host=xyz → HandshakeOrderPanel nem fogadja el
          → E2E: handshake create flow (ha Kernel endpoint él)

Nap 15:   DoD checklist + buffer / hotfix
          → Migration 0025 alkalmazva VPS-en
          → pnpm turbo test: minden teszt zöld
          → bundle size check: modules lazy-load igazolva (Network tab)
```

---

## 10. Kritikus anti-pattern-ek

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `@spaceos/joinery-ui` importál `apps/*`-ból | Composition: base + specSlot prop |
| `enabledModules` localStorage-ból vagy URL-ből | Kizárólag Orchestrator response (`useAuthStore`) |
| `allowedHosts` URL param / localStorage-ból | Kizárólag Orchestrator response |
| `if (!enabledModules.includes('cabinet')) return 403` | UX-only visibility; RBAC Orchestrator middleware-ben |
| `CuttingListBase` közvetlen importálása app-ból | Csak `CabinetCuttingList` / `DoorCuttingList` publikus |
| `windowRoutes` runtime error (stub) | `return null` — nem crashel |
| `allowedHosts` hardkódolva az app-ban | Orchestrator `allowed_hosts` claim → response body |

---

## 11. Definition of Done

### Package gate-ek
- [ ] `pnpm turbo build` — 0 error, 0 warning
- [ ] `pnpm turbo typecheck` — 0 TS error
- [ ] `madge --circular packages/` → 0 találat
- [ ] `grep -r "apps/" packages/@spaceos/joinery-ui/src` → 0
- [ ] `grep -r "api-client\|fetch\|axios" packages/@spaceos/ui/src` → 0

### Module system gate-ek
- [ ] `enabledModules: []` → ModuleRouter nem renderel route-ot, redirect `/`
- [ ] `enabledModules: ['door']` → csak `/door/*` elérhető, `/cabinet/*` → 404
- [ ] `enabledModules: ['door', 'cabinet']` → mindkét route-készlet elérhető
- [ ] `localStorage.setItem('enabledModules', '["window"]')` → nincs hatás (page reload után is)
- [ ] Bundle: `cabinet/routes.tsx` csak ha `cabinet` enabled (`React.lazy` chunk külön)
- [ ] `useAuthStore` persist `partialize`: `enabledModules` és `allowedHosts` nem szerepel localStorage-ban (SEC-P3CP-05)
- [ ] Slow network: lazy module chunk load → `<Suspense>` skeleton látható, nem blank screen (BE-P3CP-06)
- [ ] Chunk load failure: `<ErrorBoundary>` fallback látható, nem crash (BE-P3CP-09)
- [ ] `/cabinet/orders`: `HandshakeOrderPanel` csak `cabinet` AllowedTradeTypes-ú host-okat mutat (BE-P3CP-07)

### B2BHandshake gate-ek
- [ ] `GET /bff/handshakes` → Kernel → 200 (nem 404, nem 502)
- [ ] `POST /bff/handshakes` valid body → Kernel → 201 Created
- [ ] `HandshakeOrderPanel`: `allowedHosts` URL param-ból nem override-olható
- [ ] `allowedHosts: []` → HandshakeOrderPanel üres állapot, nem crashel
- [ ] Kernel: cross-tenant B2BHandshake guard → 403 ha TenantId nem egyezik

### Kernel gate-ek
- [ ] Migration 0025 alkalmazva: `SELECT "EnabledModules" FROM "Tenants"` → `{door}` Doorstar-nál
- [ ] `CK_Tenants_EnabledModules_Valid` CHECK: `{door,invalid}` → INSERT rejected
- [ ] Migration 0026 alkalmazva: `SELECT * FROM "TenantHandshakeAllowlist"` → seed sor létezik
- [ ] `CK_TenantHandshakeAllowlist_NoSelfLink`: `GuestTenantId = HostTenantId` → INSERT rejected
- [ ] `CK_AllowedTradeTypes_NotEmpty`: `AllowedTradeTypes = '{}'` → INSERT rejected (SEC-P3CP-07)
- [ ] RLS: Asztalos tenant csak saját allowlist sorait látja (Doorstar sorait nem)
- [ ] Token claim: `enabled_modules` és `allowed_hosts` jelen van a JWT-ben
- [ ] `allowed_hosts` tartalmazza `AllowedTradeTypes` mezőt (nem csak TenantId)
- [ ] Kernel `CreateHandshakeCommandHandler`: `hostTenantId` validálva az allowlist-en (SEC-P3CP-06)
- [ ] `allowed_hosts` claim: max 20 host per token; ha több → 400 Bad Request + ADR (SEC-P3CP-08)

### Component gate-ek
- [ ] `DoorCuttingList` renders without `CabinetSpecPanel`
- [ ] `CabinetCuttingList` renders without `DoorSpecPanel`
- [ ] `WindowCuttingList` stub: nem dob runtime error, `return null`
- [ ] `CuttingListBase` nem importálható kívülről (`index.ts`-ben nincs)

### Teszt gate-ek
- [ ] `pnpm turbo test` — 256 meglévő + ≥ 52 új teszt zöld
- [ ] `@spaceos/joinery-ui` coverage: minden publikus komponens tesztelve
- [ ] Orchestrator: `/bff/handshakes` proxy tesztek (4 eset)
- [ ] ModuleRouter: legalább 5 kombinációs teszt

---

## 12. Nyitott kérdés — Kernel ellenőrzés szükséges

| Kérdés | Hatás |
|--------|-------|
| Van-e `POST /api/handshakes` Minimal API endpoint a Kernel-ben? | Ha nincs: +1-2 nap Kernel scope (Nap 8 megnyúlik) |

`GetAllowedHostsAsync()` → **új** `ITenantHandshakeAllowlistRepository` interfész, Migration 0026 részeként implementálandó — nem meglévő kód.

---

## 13. Mi jön utána

| Fázis | Tartalom | Blokkoló feltétel |
|-------|----------|-------------------|
| **Phase 3D** | GDPR pseudonymizáció, PII szeparáció, audit alerting | Phase 3B kész ✅ |
| **Horizon 2** | S3 Object Lock, RFC 3161 TSA, Escrow feature flag ON | Phase 3B kész ✅ |
| **Horizon 3 — Modules.Joinery** | C# cutting list kalkulátor, `Modules.Joinery.Cabinet` live API | Phase 3C+ kész |
| **Horizon 3 — window/ branch** | `WindowCuttingList`, `WindowSpecPanel`, ablakgyártó workflow | Phase 3C+ kész |

---

*SpaceOS · Phase 3C+ Architecture v3.0 · 2026-04-07*  
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — minden döntés lezárva*  
*Kumulált review: `/senior-frontend` + `/senior-security` → 20 finding (4C+8H+8M)*  
*Blokkoló: Phase 3C DoD teljes · Kernel B2BHandshake endpoint ellenőrzés szükséges*
