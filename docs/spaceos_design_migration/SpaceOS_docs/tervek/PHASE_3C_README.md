# SpaceOS — Phase 3C: Claude Code Kontextus

> **Repo:** `spaceos-design-portal`
> **Státusz:** Implementációra kész — v2 architektúra véglegesítve
> **Teljes spec:** `SpaceOS_Phase3C_Architecture_v2.md`
> **Baseline tesztek:** 256 pass / 0 fail (ezek végig zöldek maradnak)
> **Becsült effort:** 16 fejlesztői nap

---

## Mi folyik itt?

A `spaceos-design-portal` jelenleg egy monolitikus React 18 + Vite alkalmazás, amelyet két domainen szolgál ki az nginx:

- `joinerytech.hu` → angol UI
- `asztalostech.hu` → magyar UI (ugyanaz az app, i18n)

**Phase 3C célja:** Ezt az egy alkalmazást átalakítani **Turborepo monorepouvá**, és bevezetni egy **brand skin rendszert**, amely lehetővé teszi, hogy különböző bérlők (pl. Doorstar Kft.) saját arculattal és testreszabott Dashboard-dal lássák az alkalmazást — anélkül, hogy külön appot kellene deployolni.

---

## A végállapot: monorepo struktúra

```
spaceos-design-portal/
  apps/
    joinerytech/              ← a jelenlegi app, ide kerül átköltöztetés után
      src/
        brand/
          overrides.ts        ← lazy page override-ok brand-ként (pl. Doorstar Dashboard)
        pages/
          Dashboard/
            index.tsx         ← default Dashboard
            __brand/
              doorstar.tsx    ← Doorstar-specifikus Dashboard (Medium override)
        routes/
          brandRouter.tsx     ← BrandContext alapján tölt be default vagy override page-et
        App.tsx               ← BrandProvider + LocaleProvider wrapper
      index.html              ← Google Fonts preconnect (Inter, JetBrains Mono)
      vite.config.ts          ← resolve.alias minden @spaceos/* csomagra
      tailwind.config.ts      ← extends @spaceos/brand-tokens/tailwind/preset
      package.json            ← workspace:* dependenciák
  packages/
    @spaceos/domain/          ← OpenAPI codegen → TypeScript típusok (DO NOT EDIT generated/)
    @spaceos/api-client/      ← typed HTTP wrapperek (Kernel + Orchestrator)
    @spaceos/ui/              ← dumb komponensek (Button, Card, Table, PageShell stb.)
    @spaceos/brand-tokens/    ← brand adat (szín, logo, flag) + BrandProvider React context
    @spaceos/i18n/            ← en.json / hu.json + useTranslation hook
  vitest.workspace.ts         ← root-szintű Vitest workspace config
  turbo.json
  pnpm-workspace.yaml
```

---

## Package függőségi rend (körhivatkozás tilos)

```
apps/joinerytech
  → @spaceos/api-client  → @spaceos/domain
  → @spaceos/ui          → @spaceos/brand-tokens
  → @spaceos/i18n
  → @spaceos/brand-tokens (direkt, BrandProvider-hez)

TILOS: packages/* → apps/*
TILOS: @spaceos/ui → @spaceos/api-client
```

---

## A brand skin rendszer logikája

### 3 réteg

| Réteg | Mit jelent | Phase 3C-ben |
|-------|-----------|-------------|
| **Shallow** | Szín, logo, tipográfia — CSS custom properties | ✅ Implementálva |
| **Medium** | Page-szintű override (pl. Doorstar Dashboard ≠ default Dashboard) | ✅ Doorstar Dashboard |
| **Deep** | Külön navigáció, feature set | 🔜 Architektúra kész, Phase 3C+ |

### Hogyan dől el, melyik skin töltődik be?

1. User belép → `POST /bff/api/auth/token`
2. **Orchestrator** RS256-verifikálja a JWT-t, visszaadja: `{ accessToken, brandSkin: 'doorstar', expiresAt }`
3. `useAuthStore` tárolja: `brandSkin = 'doorstar'`
4. `App.tsx`: `<BrandProvider skinId={brandSkin} pageOverrides={brandPageOverrides['doorstar']}>`
5. `BrandProvider`: CSS custom properties beállítva (`--color-primary` stb.), context kiosztva
6. `BrandedPage`: ha van override → lazy load; ha nincs → default page

### ⚠ Kritikus szabály — a JWT-t a frontend NEM dekódolja

`brandSkin` az Orchestrator response body-jából jön, NEM `localStorage`-ból vagy JWT decode-ból. Ez security finding volt (SEC-P3C-07).

---

## `@spaceos/brand-tokens` — a legfontosabb csomag

### Mit tartalmaz

- `types.ts` — `BrandTokens` interface: **React-mentes, csak adat** (szín, logo, tipográfia, featureFlags)
- `sanitize.ts` — per-type validátorok: `validateHexColor()`, `validateFontFamily()`, `validateLogoPath()`
- `tokens/joinerytech.ts` — default brand
- `tokens/doorstar.ts` — Doorstar skin (**nincs `lazy()` import ebben a fájlban!**)
- `registry.ts` — `resolveBrand(skinId)` + startup validáció
- `tailwind/preset.ts` — Tailwind theme extension CSS var-okra
- `BrandProvider.tsx` — React context; `pageOverrides` propot kap kívülről (az APP adja)

### Mit NEM tartalmaz

- `React.lazy()` hívásokat — ezek az **app** `src/brand/overrides.ts`-ben élnek
- API hívásokat, store referenciákat

### Doorstar token (adat, nem React)

```typescript
// packages/@spaceos/brand-tokens/src/tokens/doorstar.ts
export const doorstar: BrandTokens = {
  id: 'doorstar',
  colors: { primary: '#B45309', background: '#0C0A09', ... },
  logo: { src: '/brand/doorstar/logo.svg', ... },
  featureFlags: { showCncExport: true, showAuditPage: false, ... },
  // NINCS pageOverrides — az app/brand/overrides.ts-ben van
};
```

### Page override az APP-ban

```typescript
// apps/joinerytech/src/brand/overrides.ts
export const brandPageOverrides: Record<string, PageOverrideRegistry> = {
  doorstar: {
    dashboard: lazy(() => import('../pages/Dashboard/__brand/doorstar')),
  },
};
```

---

## `@spaceos/domain` — OpenAPI codegen

- Forrás: `openapi-snapshot/kernel.json` (commitolt snapshot a repóban)
- Generált: `src/generated/kernel.ts` — **DO NOT EDIT**
- Frissítés: `pnpm --filter @spaceos/domain codegen:fetch` → új snapshot → `codegen` → commit
- CI gate: `git diff --exit-code src/generated/kernel.ts` → ha eltér, build FAIL
- Csak publikus schema-k exportálva (`index.ts` whitelist re-export)

---

## Vite workspace resolution (dev HMR)

```typescript
// apps/joinerytech/vite.config.ts
resolve: {
  alias: {
    '@spaceos/api-client':   path.resolve(__dirname, '../../packages/@spaceos/api-client/src'),
    '@spaceos/brand-tokens': path.resolve(__dirname, '../../packages/@spaceos/brand-tokens/src'),
    '@spaceos/domain':       path.resolve(__dirname, '../../packages/@spaceos/domain/src'),
    '@spaceos/i18n':         path.resolve(__dirname, '../../packages/@spaceos/i18n/src'),
    '@spaceos/ui':           path.resolve(__dirname, '../../packages/@spaceos/ui/src'),
  },
},
```

---

## Locale detektálás

Két domain, egy build, i18n hostname alapján:

```typescript
// @spaceos/i18n/src/detectLocale.ts
export function detectLocale(): 'en' | 'hu' {
  return window.location.hostname.includes('asztalostech') ? 'hu' : 'en';
}
```

Nincs nginx `sub_filter` — hostname-only (stabil, nem törékeny).

---

## Nginx változás (deploy)

| Domain | Régi path | Új path |
|--------|-----------|---------|
| `joinerytech.hu` | `/opt/spaceos/design-portal/dist/` | `/opt/spaceos/frontend/apps/joinerytech/dist/` |
| `asztalostech.hu` | ugyanaz | ugyanaz (azonos build) |

Átmeneti szimlink: `ln -sf /opt/spaceos/frontend/apps/joinerytech/dist /opt/spaceos/design-portal/dist`

CSP frissítés: `img-src 'self' /brand/ data:` + `font-src https://fonts.gstatic.com`

---

## Kernel változás (migration 0024)

```sql
ALTER TABLE "Tenants" ADD COLUMN "BrandSkinId" varchar(64) NULL;
```

```csharp
// TokenCommandHandler
claims.Add(new Claim("brand_skin", tenant.BrandSkinId ?? "joinerytech"));
```

Az Orchestrator a `/bff/api/auth/token` response-ban adja vissza: `{ brandSkin: "doorstar" }`.

---

## Migrációs elv

Az átállás **in-place**, nem greenfield. 7 lépésben, minden lépés végén a **256 meglévő teszt zöld**:

1. Turborepo scaffold + `apps/joinerytech` átköltöztetés
2. `@spaceos/domain` codegen
3. `@spaceos/api-client` kiszervezés
4. `@spaceos/ui` kiszervezés
5. `@spaceos/brand-tokens` + BrandProvider + Tailwind preset
6. Doorstar skin + `brand/overrides.ts` + Kernel migration 0024
7. `@spaceos/i18n` + nginx frissítés

---

## Kritikus anti-pattern-ek (NE csináld)

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `lazy()` egy `packages/@spaceos/*` fájlban | `lazy()` csak `apps/*`-ban |
| `brandSkin` JWT decode-ból (`atob`, `jwtDecode`) | `brandSkin` Orchestrator response body-ból |
| `style={{ color: brand.colors.primary }}` | `className="text-primary"` (Tailwind CSS var) |
| `localStorage.getItem('skin')` | Orchestrator response state-ből |
| `@spaceos/ui` importálja `@spaceos/api-client`-et | `@spaceos/ui`-nak nulla hálózati dep |
| `featureFlags.showAuditPage === false` → API hívás blokkolva | `featureFlags` UX-only; RBAC Orchestrator-ban |
| Direkt `packages/@spaceos/domain/src/generated/` path import | Csak `@spaceos/domain` publikus re-export |

---

## featureFlags — ⚠ NEM biztonsági határ

```typescript
// ⚠ NOT A SECURITY BOUNDARY — UX-only
// Ez elrejti a gombot/menüpontot, de NEM blokkolja az API hívást.
// RBAC az Orchestrator JWT middleware-ben érvényesítve.
featureFlags: {
  showCncExport:    true,
  showAuditPage:    false,
}
```

---

## Definition of Done (rövid verzió)

- [ ] `pnpm turbo build` — 0 error
- [ ] `pnpm turbo typecheck` — 0 TS error
- [ ] `pnpm turbo test` — 256 meglévő + ≥ 35 új teszt zöld
- [ ] `madge --circular packages/` → 0
- [ ] `grep -r "lazy" packages/@spaceos/brand-tokens/src/tokens/` → 0
- [ ] `grep -rn "jwtDecode\|atob" apps/joinerytech/src/store` → 0
- [ ] `resolveBrand('ismeretlen')` → JoineryTech fallback (nem crash)
- [ ] `validateHexColor('expression(alert(1))')` → throw
- [ ] `validateLogoPath('../../etc/passwd')` → throw
- [ ] `joinerytech.hu` → `detectLocale() === 'en'`; `asztalostech.hu` → `'hu'`
- [ ] Doorstar JWT → Doorstar Dashboard render; `null` JWT → JoineryTech default
- [ ] Nginx deploy: új dist path + CSP frissítve
- [ ] Kernel migration 0024 alkalmazva

---

*SpaceOS Phase 3C — Claude Code context · 2026-04-07*
*Teljes spec: `SpaceOS_Phase3C_Architecture_v2.md`*
