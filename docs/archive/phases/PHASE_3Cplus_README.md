# SpaceOS — Phase 3C+: Claude Code Kontextus

> **Repo-k:** `spaceos-design-portal` · `spaceos-orchestrator` · `spaceos-kernel`  
> **Státusz:** Implementációra kész — v3 architektúra véglegesítve  
> **Teljes spec:** `SpaceOS_Phase3Cplus_Architecture_v3.md`  
> **Baseline tesztek:** ~1382 pass / 0 fail (ezek végig zöldek maradnak)  
> **Becsült effort:** 16 fejlesztői nap  
> **Blokkoló feltétel:** Phase 3C DoD teljes

---

## Mi folyik itt?

### A felismerés

Az asztalos iparban minden szereplő egyszerre **termelő és megrendelő** — kontextustól függően:

```
Doorstar Kft.        →  ajtókat GYÁRT  +  anyagot, vasalatot RENDEL
Nagy Asztalos Kft.   →  szekrényeket GYÁRT  +  ajtókat RENDEL Doorstartól
```

Phase 3C-ban egy app kapta meg a brand skin rendszert (Turborepo). Phase 3C+-ban ezt **két irányban bővítjük:**

1. **Modul-rendszer** — egy faiparos cég egyszerre lehet ajtógyártó és szekrénygyártó. A `apps/joinerytech` app tenant-onként konfigurálja, melyik modul-készlet látható (`enabledModules`).
2. **Shared joinery UI package** — ami közös az összes faiparosban (szabászat, kanban, anyagok, B2B rendelés), azt **egyszer írjuk meg** `@spaceos/joinery-ui`-ban. Minden ág csak a saját specialitását adja hozzá.
3. **Live B2BHandshake** — az asztalos a portálból közvetlenül tud ajtót rendelni Doorstartól. Ez valós Kernel API hívás, nem mock.

---

## Mit fejlesztünk — a 3 nagy terület

### 1. `@spaceos/joinery-ui` — ÚJ shared package

Jelenleg nincs ilyen csomag. A Phase 3C+ létrehozza.

**Mi kerül bele:**

| Komponens | Mi ez | Ki használja |
|---|---|---|
| `CuttingListBase` | Szabászat táblázat + slot-ok (belső, nem exportált) | Alap — mindenki |
| `DoorCuttingList` | Ajtó-specifikus szabászat (vasalat, élzárás, nyílásirány) | Doorstar (`door` modul) |
| `CabinetCuttingList` | Szekrény-specifikus szabászat (korpusz, fiók, polc) | Asztalos (`cabinet` modul) |
| `WindowCuttingList` | Ablak-specifikus (stub — Horizon 3) | Jövő |
| `ProductionKanban` | FSM Kanban — minden faiparosnak közös | Mindkét modul |
| `MaterialPanel` | Anyaggazdálkodás — trade-agnosztikus | Mindkét modul |
| `HandshakeOrderPanel` | B2B rendelési UI: asztalos rendel Doorstartól | `cabinet` modul Orders oldal |
| `HandshakeHostPanel` | B2B kiadási UI: host kap megrendelést | `door` modul (Doorstar fogadja) |

**Ami NEM kerül bele:** API hívások, store referenciák, `React.lazy()` hívások, `apps/*` importok.

### 2. Modul-rendszer — `apps/joinerytech` bővítése

Az app nem változik branden. Belülről egy új **modul-router réteg** épül rá:

```
apps/joinerytech/src/
  modules/
    door/         ← /door/cutting, /door/kanban (már volt, most strukturált)
    cabinet/      ← /cabinet/cutting, /cabinet/kanban, /cabinet/orders  ← ÚJ
    window/       ← placeholder, Horizon 3
  routes/
    moduleRouter.tsx   ← ÚJ: enabledModules alapján lazy-loadolja a route-okat
```

**Hogyan dől el, melyik modul látható:**

```
User belép → POST /bff/api/auth/token
  → Orchestrator: tenant.EnabledModules = ['door', 'cabinet']
  → response: { accessToken, brandSkin, enabledModules: ['door','cabinet'], allowedHosts: [...] }
  → useAuthStore.enabledModules = ['door', 'cabinet']
  → ModuleRouter: mindkét route-készlet lazy-loadolva
  → Sidebar: /door/* és /cabinet/* menüpontok láthatók
```

Egyetlen cég, egyetlen belépés — két szakági workflow egyszerre.

### 3. Live B2BHandshake integráció

Az asztalos a `/cabinet/orders` oldalon ajtót rendelhet Doorstartól. Ez **valós API hívás:**

```
Portal → POST /bff/handshakes → Orchestrator → POST /api/handshakes → Kernel
                                                      ↓
                                         TenantHandshakeAllowlist ellenőrzés
                                         (Asztalos engedélyezett-e Doorstartól rendelni?)
```

Ehhez három réteg kell:
- **Kernel:** `TenantHandshakeAllowlist` tábla (Migration 0026) + `CreateHandshakeCommandHandler` allowlist validáció
- **Orchestrator:** `/bff/handshakes` proxy route (jelenleg hiányzik)
- **Frontend:** `HandshakeOrderPanel` live API hookup, `allowedHosts` Orchestrator response-ból

---

## A végállapot: teljes struktúra

```
spaceos-design-portal/
  apps/
    joinerytech/
      src/
        modules/
          door/
            routes.tsx              ← /door/* lazy route-ok
            DoorDashboard.tsx
            DoorCuttingListPage.tsx ← DoorCuttingList komponens innen
            DoorKanbanPage.tsx
          cabinet/
            routes.tsx              ← /cabinet/* lazy route-ok
            CabinetDashboard.tsx
            CabinetCuttingListPage.tsx ← CabinetCuttingList komponens innen
            CabinetKanbanPage.tsx
            CabinetOrdersPage.tsx   ← HandshakeOrderPanel live API-val
          window/
            routes.tsx              ← placeholder stub
        brand/
          overrides.ts              ← Phase 3C-ból
        routes/
          moduleRouter.tsx          ← ÚJ: enabledModules lazy-load + Suspense + ErrorBoundary
          brandRouter.tsx           ← Phase 3C-ból
        App.tsx                     ← BrandProvider + ModuleProvider
  packages/
    @spaceos/joinery-ui/            ← ÚJ package
      src/
        base/
          CuttingListBase/          ← internal, nem exportált
          ProductionKanban/
          MaterialPanel/
          HandshakeOrderPanel/
          HandshakeHostPanel/
        door/
          DoorCuttingList.tsx
          DoorSpecPanel.tsx
        cabinet/
          CabinetCuttingList.tsx
          CabinetSpecPanel.tsx
        window/
          WindowCuttingList.tsx     ← stub (return null)
        index.ts                    ← whitelist re-export
    @spaceos/ui/                    ← Phase 3C-ból (változatlan)
    @spaceos/domain/                ← Phase 3C-ból (változatlan)
    @spaceos/api-client/            ← bővül: /handshakes endpoint
    @spaceos/brand-tokens/          ← Phase 3C-ból (változatlan)
    @spaceos/i18n/                  ← Phase 3C-ból (változatlan)
```

---

## Kernel változások

### Migration 0025 — EnabledModules
```sql
ALTER TABLE "Tenants"
  ADD COLUMN "EnabledModules" varchar(32)[] NOT NULL DEFAULT '{}';
-- CHECK: csak 'door', 'cabinet', 'window' értékek
```

### Migration 0026 — TenantHandshakeAllowlist
```sql
CREATE TABLE "TenantHandshakeAllowlist" (
  "GuestTenantId"     uuid          NOT NULL,  -- aki rendel (Asztalos)
  "HostTenantId"      uuid          NOT NULL,  -- akit megrendelhet (Doorstar)
  "AllowedTradeTypes" varchar(32)[] NOT NULL,  -- pl. ARRAY['door']
  PRIMARY KEY ("GuestTenantId", "HostTenantId"),
  CHECK ("GuestTenantId" <> "HostTenantId"),
  CHECK (cardinality("AllowedTradeTypes") > 0)
);
-- RLS: guest csak saját sorait látja
```

### CreateTokenCommandHandler módosítás
A JWT tokenbe két új claim kerül:
- `enabled_modules` → `["door","cabinet"]`
- `allowed_hosts` → `[{tenantId, tenantName, allowedTradeTypes}]` (max 20 host)

### CreateHandshakeCommandHandler módosítás
`POST /api/handshakes` előtt validálni kell: a kérő tenant szerepel-e az allowlist-en a megcélzott host-tal szemben. Ha nem → `Result.Forbidden`.

---

## Orchestrator változások

### Új route: `/bff/handshakes`
```typescript
// Jelenleg HIÁNYZIK — implementálandó
router.all('*', kernelProxy('/api/handshakes'));
app.use('/bff/handshakes', handshakesRouter);
```

### Auth response bővítése
```typescript
// Régi: { accessToken, brandSkin, expiresAt }
// Új:   { accessToken, brandSkin, expiresAt, enabledModules, allowedHosts }
```

### Cabinet system prompt
```
Persona: professional cabinet making assistant
Trade context: szekrénygyártás
Mértékegység: mm
Alapanyag default: 18mm MDF
```

---

## Package dependency szabályok (körhivatkozás TILOS)

```
apps/joinerytech
       ↓
@spaceos/joinery-ui  →  @spaceos/ui
                     →  @spaceos/brand-tokens
                     →  @spaceos/domain
                     →  @spaceos/api-client → @spaceos/domain

❌ TILOS: packages/* → apps/*
❌ TILOS: @spaceos/joinery-ui → apps/*
❌ TILOS: @spaceos/ui → @spaceos/joinery-ui
```

---

## Kritikus szabályok — NE csináld

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `enabledModules` localStorage-ból olvasni | Kizárólag `useAuthStore(s => s.enabledModules)` — Orchestrator response |
| `allowedHosts` URL param / localStorage | Kizárólag Orchestrator response `allowedHosts` mező |
| `useAuthStore` persist-be `enabledModules` / `allowedHosts` | `partialize`-ból kizárva — soha nem kerülhet localStorage-ba |
| `lazy()` egy `packages/@spaceos/*` fájlban | `lazy()` csak `apps/*`-ban |
| `CuttingListBase` direktben importálni app-ból | Csak `DoorCuttingList` / `CabinetCuttingList` publikus |
| `featureFlags` / `enabledModules` API-t blokkolni | UX-only — RBAC az Orchestrator JWT middleware-ben |
| `HandshakeOrderPanel`-ben az összes allowed host mutatása | `activeTrade` prop alapján szűrni |
| `Suspense` nélkül lazy komponenst renderelni | `<ErrorBoundary><Suspense fallback={<PageSkeleton/>}>` minden lazy route körül |

---

## Implementációs sorrend (16 nap)

| Nap | Task | Réteg | Ellenőrzés |
|-----|------|-------|------------|
| 1 | `@spaceos/joinery-ui` scaffold — package.json, üres src/, vitest.workspace.ts frissítés | Portal | `pnpm turbo build` 0 error |
| 2 | `CuttingListBase` + `ProductionKanban` | Portal | unit tesztek zöld |
| 3 | `MaterialPanel` + `HandshakeHostPanel` | Portal | unit tesztek zöld |
| 4 | `door/` branch — `DoorSpecPanel` + `DoorCuttingList`; apps/joinerytech átáll | Portal | 256 meglévő teszt zöld |
| 5 | `cabinet/` branch — `CabinetSpecPanel` + `CabinetCuttingList`; `window/` stub | Portal | unit tesztek zöld |
| 6 | `HandshakeOrderPanel` — form, `allowedHosts` prop, `activeTrade` szűrő | Portal | SEC-P3CP-01/02 gate |
| 7 | `moduleRouter.tsx` — `enabledModules` lazy-load, Suspense, ErrorBoundary, auth race fix | Portal | module isolation tesztek |
| 8 | Kernel — Migration 0025 + 0026; `ITenantHandshakeAllowlistRepository` + impl | Kernel | migration unit tesztek |
| 9 | Kernel — `CreateTokenCommandHandler` claims + `CreateHandshakeCommandHandler` allowlist validáció | Kernel | integration tesztek |
| 10 | Orchestrator — `/bff/handshakes` proxy + auth response bővítés (`enabledModules`, `allowedHosts`) | Orchestrator | proxy tesztek |
| 11 | `@spaceos/api-client` — `useCreateHandshake`, `useGetHandshakes` hookup | Portal | E2E handshake create |
| 12 | `CabinetOrdersPage` — live `HandshakeOrderPanel` + `useAuthStore.allowedHosts` | Portal | Orders page tesztek |
| 13 | Cabinet system prompt (Orchestrator) + `useAuthStore` partialize fix | Orchestrator + Portal | SEC-P3CP-05 gate |
| 14 | Tesztek — `@spaceos/joinery-ui` teljes lefedettség (≥52 új teszt) | Mind | `madge --circular` → 0 |
| 15 | DoD checklist, bundle size check, security gate-ek | Mind | minden gate zöld |
| 16 | Buffer / hotfix / VPS deploy | VPS | E2E zöld |

---

## Biztonsági finding-ek (v3 — beépítve)

| ID | Súly | Javítás |
|----|------|---------|
| SEC-P3CP-05 | 🔴 CRITICAL | Zustand `partialize`: `enabledModules`, `allowedHosts`, `brandSkin` kizárva localStorage-ból |
| SEC-P3CP-06 | 🟠 HIGH | Kernel `CreateHandshakeCommandHandler` allowlist validáció |
| SEC-P3CP-07 | 🟠 HIGH | `CK_AllowedTradeTypes_NotEmpty` CHECK constraint |
| SEC-P3CP-08 | 🟡 MEDIUM | Max 20 allowed host per JWT claim; ADR dokumentálva |
| SEC-P3CP-09 | 🟡 MEDIUM | Seed `ON CONFLICT DO NOTHING` — idempotent |
| BE-P3CP-06 | 🔴 CRITICAL | `<Suspense>` + `<ErrorBoundary>` minden lazy route körül |
| BE-P3CP-07 | 🟠 HIGH | `HandshakeOrderPanel` `activeTrade` szűrő |
| BE-P3CP-08 | 🟠 HIGH | `enabledModules ?? []` — nullish coalesce minden használati helyen |
| BE-P3CP-09 | 🟡 MEDIUM | `<ErrorBoundary>` chunk load failure fallback |
| BE-P3CP-10 | 🟡 MEDIUM | Auth race condition: `isAuthLoading` spinner a redirect előtt |

---

## Definition of Done (rövid verzió)

- [ ] `pnpm turbo build` — 0 error, 0 warning
- [ ] `pnpm turbo typecheck` — 0 TS error
- [ ] `pnpm turbo test` — meglévő + ≥ 52 új teszt zöld
- [ ] `madge --circular packages/` → 0
- [ ] `grep -r "apps/" packages/@spaceos/joinery-ui/src` → 0
- [ ] `enabledModules: ['door']` → `/cabinet/*` route nem létezik
- [ ] `enabledModules: ['door','cabinet']` → mindkét route-készlet él
- [ ] `localStorage.setItem('enabledModules',...)` → nincs hatás
- [ ] `localStorage.setItem('allowedHosts',...)` → nincs hatás
- [ ] `POST /bff/handshakes` invalid host → Kernel 403
- [ ] `/cabinet/orders` → csak `door` AllowedTradeTypes-ú host-ok látszanak
- [ ] Slow network → `<Suspense>` skeleton, nem blank screen
- [ ] Chunk load error → `<ErrorBoundary>` fallback, nem crash
- [ ] Kernel migration 0025 + 0026 VPS-en alkalmazva

---

## Nyitott kérdés (Phase 3C+ session elején ellenőrizni)

> Van-e `POST /api/handshakes` Minimal API endpoint a Kernel repóban?  
> Ha nincs → Nap 8-9 kiterjed +1-2 nappal.

```bash
curl -X POST https://joinerytech.hu/bff/handshakes \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"hostTenantId":"...","tradeType":"door"}'
# Elvárt: 400 (validation) vagy 201 (created) — NEM 404, NEM 405
```

---

*SpaceOS Phase 3C+ — Claude Code context · 2026-04-07*  
*Teljes spec: `SpaceOS_Phase3Cplus_Architecture_v3.md`*  
*Review: `/senior-frontend` + `/senior-security` → v3 · 21 finding beépítve*
