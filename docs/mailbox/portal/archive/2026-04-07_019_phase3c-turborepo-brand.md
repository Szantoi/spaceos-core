---
id: MSG-P019
from: architect
to: portal
type: task
status: UNREAD
priority: P0
sprint: "Sprint D · Phase 3C"
---

# Phase 3C — Multi-Brand Portal: Turborepo + Brand Skin System

**Tervdokumentum:** `docs/SpaceOS_Phase3C_Architecture_v2.md`
**README:** `docs/PHASE_3C_README.md`
**Becsült effort:** 16 fejlesztői nap
**Test baseline:** 256 pass / 0 fail (végig zöldek maradnak)
**Cél:** ≥ 35 új teszt · 0 TS error · 0 körhivatkozás

---

## Összefoglalás

A jelenlegi monolitikus `spaceos-design-portal` React app átalakítása **Turborepo monorepo**-vá, és egy **brand skin rendszer** bevezetése, amely lehetővé teszi, hogy Doorstar Kft. (és jövőbeli bérlők) saját arculattal lássák a portált — anélkül, hogy külön appot kell deployolni.

**20 finding beépítve** (2 CRITICAL + 7 HIGH + 11 MEDIUM) — ezeket a v2 architektúra már tartalmazza.

---

## Végső monorepo struktúra

```
spaceos-design-portal/              ← repo neve marad
  apps/
    joinerytech/                    ← jelenlegi app, ide kerül
      src/
        brand/
          overrides.ts              ← ÚJ: lazy() page override-ok (BE-P3C-01)
        pages/
          Dashboard/
            index.tsx               ← default
            __brand/
              doorstar.tsx          ← Doorstar Medium override
        routes/
          brandRouter.tsx
        App.tsx                     ← BrandProvider + LocaleProvider
      index.html                    ← Google Fonts preconnect (BE-P3C-08)
      vite.config.ts                ← resolve.alias (BE-P3C-03)
      tailwind.config.ts            ← extends @spaceos/brand-tokens preset
      package.json                  ← workspace:* deklarációk
  packages/
    @spaceos/domain/                ← OpenAPI codegen → TS típusok
    @spaceos/api-client/            ← typed HTTP wrapper-ek
    @spaceos/ui/                    ← dumb komponensek
    @spaceos/brand-tokens/          ← adat + BrandProvider (React-mentes tokens)
    @spaceos/i18n/                  ← en.json / hu.json + useTranslation
  vitest.workspace.ts               ← root Vitest workspace (BE-P3C-05)
  turbo.json
  pnpm-workspace.yaml
```

**Package függőségi rend (körhivatkozás tilos):**
```
apps/joinerytech
  → @spaceos/api-client  → @spaceos/domain
  → @spaceos/ui          → @spaceos/brand-tokens
  → @spaceos/i18n
  → @spaceos/brand-tokens (direkt, BrandProvider-hez)

❌ TILOS: packages/* → apps/*
❌ TILOS: @spaceos/ui → @spaceos/api-client
```

---

## T-01 — Turborepo Scaffold (Nap 1)

### Fájlok

**`pnpm-workspace.yaml`** (repo root):
```yaml
packages:
  - 'apps/*'
  - 'packages/@spaceos/*'
```

**`turbo.json`** (repo root):
```json
{
  "$schema": "https://turbo.build/schema.json",
  "pipeline": {
    "build":      { "dependsOn": ["^build"], "outputs": ["dist/**"] },
    "codegen":    { "cache": false, "outputs": ["src/generated/**"] },
    "test":       { "dependsOn": ["^build"], "outputs": ["coverage/**"] },
    "typecheck":  { "dependsOn": ["^build"], "outputs": [] },
    "lint":       { "outputs": [] }
  }
}
```

**`vitest.workspace.ts`** (repo root — BE-P3C-05):
```typescript
import { defineWorkspace } from 'vitest/config';
export default defineWorkspace([
  'apps/joinerytech/vitest.config.ts',
  'packages/@spaceos/ui/vitest.config.ts',
  'packages/@spaceos/api-client/vitest.config.ts',
  'packages/@spaceos/brand-tokens/vitest.config.ts',
  'packages/@spaceos/i18n/vitest.config.ts',
  // @spaceos/domain: tsc --noEmit + codegen diff — nem Vitest
]);
```

### Teendők

1. A jelenlegi `src/` és konfigurációs fájlok átköltöztetése `apps/joinerytech/`-ba
2. `packages/@spaceos/*` — üres shell-ek (`index.ts = export {}` placeholder-rel)
3. Root `package.json` — `"private": true, "workspaces": ["apps/*", "packages/@spaceos/*"]`
4. `pnpm install` — workspace-ek összekötése

**Ellenőrzés:**
```bash
pnpm turbo build    # 0 error
pnpm turbo test     # 256 pass, 0 fail
```

---

## T-02 — `@spaceos/domain` codegen (Nap 2)

```
packages/@spaceos/domain/
  src/
    generated/
      kernel.ts       ← openapi-typescript kimenet — DO NOT EDIT
    index.ts          ← whitelist re-export
  scripts/
    codegen.sh
  openapi-snapshot/
    kernel.json       ← commitolt snapshot (BE-P3C-04)
  package.json
```

**`packages/@spaceos/domain/package.json`:**
```json
{
  "name": "@spaceos/domain",
  "private": true,
  "scripts": {
    "codegen":       "openapi-typescript ./openapi-snapshot/kernel.json -o src/generated/kernel.ts",
    "codegen:fetch": "curl -sf $KERNEL_OPENAPI_URL -o openapi-snapshot/kernel.json",
    "build":         "tsc --noEmit"
  },
  "devDependencies": {
    "openapi-typescript": "^7.0.0",
    "typescript": "^5.0.0"
  }
}
```

**`src/index.ts`** — whitelist re-export (SEC-P3C-03 — belső SIP + audit admin sémák kizárva):
```typescript
// ONLY public-facing schemas exported
export type {
  components as KernelSchemas,
  paths     as KernelPaths,
} from './generated/kernel';

export type TenantId   = string & { readonly __brand: 'TenantId' };
export type FlowEpicId = string & { readonly __brand: 'FlowEpicId' };
```

**CI gate** (BE-P3C-04):
```bash
pnpm --filter @spaceos/domain codegen
git diff --exit-code packages/@spaceos/domain/src/generated/kernel.ts
# → Ha eltér: "Generated types out of sync — run codegen locally"
```

Snapshot forrása: a meglévő `npm run sync-types` futtatásával kinyert JSON Kernel OpenAPI endpoint-ról.

**Ellenőrzés:** 0 TS error; 256 teszt zöld

---

## T-03 — `@spaceos/api-client` (Nap 3–4)

**`packages/@spaceos/api-client/package.json`:**
```json
{
  "name": "@spaceos/api-client",
  "private": true,
  "main": "./src/index.ts",
  "exports": { ".": "./src/index.ts" },
  "dependencies": { "@spaceos/domain": "workspace:*" }
}
```

Költöztess ki a jelenlegi appból:
- `src/lib/api.ts` → `packages/@spaceos/api-client/src/createClient.ts`
- BFF fetch wrapper-ek (auth, spaces, snapshots, proof stb.)
- Hook-ok **NEM** kerülnek ide — azok az app-ban maradnak, az api-client csak a fetch logikát tartalmazza

**Kritikus invariáns:**
```bash
grep -r "api-client\|fetch\|axios" packages/@spaceos/ui/src
# → 0 találat (ui-nak nulla hálózati dep)
```

App `package.json`-ban:
```json
"@spaceos/api-client": "workspace:*"
```

---

## T-04 — `@spaceos/ui` (Nap 5)

**`packages/@spaceos/ui/package.json`:**
```json
{
  "name": "@spaceos/ui",
  "private": true,
  "main": "./src/index.ts",
  "exports": { ".": "./src/index.ts" },
  "peerDependencies": { "react": "^18.0.0" },
  "dependencies": { "@spaceos/brand-tokens": "workspace:*" }
}
```

Költ**öztess ki** az appból (csak dumb komponensek — nincs API call, nincs store):
- `Button`, `Card`, `Table`, `Modal`, `Badge`, `StatusBadge`, `PageShell`

A komponensek `className="text-primary"` Tailwind CSS var-on át kapják a brandszínt — **nincs** `style={{ color: brand.colors.primary }}`.

---

## T-05 — `@spaceos/brand-tokens` + BrandProvider (Nap 6–7)

### `src/types.ts` — React-MENTES (BE-P3C-01 kritikus fix)

```typescript
// ❌ NEM: import { lazy } from 'react' — ez a package React-mentes
// ✅ Csak adatstruktúrák

export interface ColorTokens { primary: string; secondary: string; accent: string;
  surface: string; background: string; text: string; textMuted: string;
  danger: string; warning: string; success: string; }

export interface LogoConfig { src: string; alt: string; width: number; height: number; }

export interface TypographyTokens { fontFamily: string; fontFamilyMono: string; }

export interface FeatureFlags {
  // ⚠ NOT A SECURITY BOUNDARY — UX-only (SEC-P3C-12)
  showCncExport: boolean; showEscrowWidget: boolean;
  showAuditPage: boolean; showSpatialViewer: boolean;
}

export interface BrandTokens {
  id: string; displayName: string;
  colors: ColorTokens; logo: LogoConfig; typography: TypographyTokens;
  featureFlags?: FeatureFlags;
  // pageOverrides NINCS ITT — az app adja át BrandProvider propként (BE-P3C-01)
}

export type RouteKey = 'dashboard' | 'flowEpicDetail' | 'workstationDetail';
export type PageOverrideRegistry = Partial<Record<RouteKey, unknown>>;
```

### `src/sanitize.ts` (SEC-P3C-08, SEC-P3C-09)

```typescript
const COLOR_RE = /^(#[0-9a-fA-F]{3,8}|rgb\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|hsl\(\s*\d+\s*,\s*\d+%\s*,\s*\d+%\s*\))$/;
export function validateHexColor(value: string): string {
  if (!COLOR_RE.test(value.trim())) throw new Error(`Invalid color token: "${value}"`);
  return value.trim();
}

const FONT_RE = /^[\w\s,"'\-]+$/;
export function validateFontFamily(value: string): string {
  if (!FONT_RE.test(value)) throw new Error(`Invalid font-family token: "${value}"`);
  return value;
}

// SEC-P3C-09: csak /brand/{alphanumeric}/filename.{svg|png|webp}
const LOGO_PATH_RE = /^\/brand\/[a-z0-9_-]+\/[a-z0-9_-]+\.(svg|png|webp)$/i;
export function validateLogoPath(src: string): string {
  if (!LOGO_PATH_RE.test(src)) throw new Error(`Invalid logo path: "${src}"`);
  return src;
}

export function validateBrandTokens(tokens: BrandTokens): BrandTokens {
  Object.values(tokens.colors).forEach(v => validateHexColor(v));
  validateFontFamily(tokens.typography.fontFamily);
  validateFontFamily(tokens.typography.fontFamilyMono);
  validateLogoPath(tokens.logo.src);
  return tokens;
}
```

### `src/tokens/joinerytech.ts`

JoineryTech default brand tokenek — valós szín paletta, logo path: `/brand/joinerytech/logo.svg`

### `src/registry.ts`

```typescript
import { joinerytech } from './tokens/joinerytech';
import { doorstar }    from './tokens/doorstar';
import { validateBrandTokens } from './sanitize';

const RAW_REGISTRY: Record<string, BrandTokens> = { joinerytech, doorstar };

// Startup validation — minden token sanitizálva registry-build-kor
export const brandRegistry = Object.fromEntries(
  Object.entries(RAW_REGISTRY).map(([k, v]) => [k, validateBrandTokens(v)])
) as Record<string, BrandTokens>;

export function resolveBrand(skinId: string | null): BrandTokens {
  // ismeretlen skinId → JoineryTech fallback, nem crash (SEC-P3C-04)
  return brandRegistry[skinId ?? 'joinerytech'] ?? brandRegistry['joinerytech'];
}
```

### `src/BrandProvider.tsx`

```typescript
export function BrandProvider({
  skinId, pageOverrides = {}, children,
}: { skinId: string | null; pageOverrides?: PageOverrideRegistry; children: ReactNode }) {
  const tokens = useMemo(() => resolveBrand(skinId), [skinId]);

  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty('--color-primary',    tokens.colors.primary);
    root.style.setProperty('--color-secondary',  tokens.colors.secondary);
    root.style.setProperty('--color-accent',     tokens.colors.accent);
    root.style.setProperty('--color-surface',    tokens.colors.surface);
    root.style.setProperty('--color-background', tokens.colors.background);
    root.style.setProperty('--color-text',       tokens.colors.text);
    root.style.setProperty('--color-text-muted', tokens.colors.textMuted);
    root.style.setProperty('--color-danger',     tokens.colors.danger);
    root.style.setProperty('--color-warning',    tokens.colors.warning);
    root.style.setProperty('--color-success',    tokens.colors.success);
    root.style.setProperty('--font-family',      tokens.typography.fontFamily);
    root.style.setProperty('--font-family-mono', tokens.typography.fontFamilyMono);
    return () => { /* cleanup: CSS var-ok törlése skinId változáskor */ };
  }, [tokens]);

  const value = useMemo(() => ({ ...tokens, pageOverrides }), [tokens, pageOverrides]);
  return <BrandContext.Provider value={value}>{children}</BrandContext.Provider>;
}
```

### `src/tailwind/preset.ts` (BE-P3C-02)

```typescript
import type { Config } from 'tailwindcss';
export const spaceosPreset: Config = {
  theme: {
    extend: {
      colors: {
        primary:      'var(--color-primary)',
        secondary:    'var(--color-secondary)',
        accent:       'var(--color-accent)',
        surface:      'var(--color-surface)',
        background:   'var(--color-background)',
        'text-base':  'var(--color-text)',
        'text-muted': 'var(--color-text-muted)',
        danger:       'var(--color-danger)',
        warning:      'var(--color-warning)',
        success:      'var(--color-success)',
      },
      fontFamily: {
        sans: ['var(--font-family)', 'system-ui', 'sans-serif'],
        mono: ['var(--font-family-mono)', 'monospace'],
      },
    },
  },
};
```

`apps/joinerytech/tailwind.config.ts`:
```typescript
import { spaceosPreset } from '@spaceos/brand-tokens/tailwind/preset';
export default {
  presets: [spaceosPreset],
  content: ['./src/**/*.{ts,tsx}', '../../packages/@spaceos/ui/src/**/*.{ts,tsx}'],
} satisfies Config;
```

### Vite config (BE-P3C-03)

`apps/joinerytech/vite.config.ts`:
```typescript
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

## T-06 — Doorstar Skin + Override Router (Nap 8–9)

### `src/tokens/doorstar.ts` — NINCS `lazy()` import (BE-P3C-01)

```typescript
import type { BrandTokens } from '../types';
// ← NINCS React import, NINCS lazy() — csak adatstruktúra

export const doorstar: BrandTokens = {
  id: 'doorstar', displayName: 'Doorstar',
  colors: {
    primary: '#B45309', secondary: '#1C1917', accent: '#D97706',
    surface: '#292524', background: '#0C0A09',
    text: '#FAFAF9', textMuted: '#A8A29E',
    danger: '#DC2626', warning: '#F59E0B', success: '#16A34A',
  },
  logo: { src: '/brand/doorstar/logo.svg', alt: 'Doorstar', width: 140, height: 36 },
  typography: {
    fontFamily:     '"Inter", system-ui, sans-serif',
    fontFamilyMono: '"JetBrains Mono", monospace',
  },
  // ⚠ NOT A SECURITY BOUNDARY — UX-only (SEC-P3C-12)
  featureFlags: {
    showCncExport: true, showEscrowWidget: true,
    showAuditPage: false, showSpatialViewer: true,
  },
  // pageOverrides NINCS ITT — apps/joinerytech/src/brand/overrides.ts-ben
};
```

### `apps/joinerytech/src/brand/overrides.ts`

```typescript
import { lazy } from 'react';  // ← lazy() csak az APP-ban!
import type { PageOverrideRegistry } from '@spaceos/brand-tokens';

export const brandPageOverrides: Record<string, PageOverrideRegistry> = {
  doorstar: {
    dashboard: lazy(() => import('../pages/Dashboard/__brand/doorstar')),
  },
};
```

### `apps/joinerytech/src/routes/brandRouter.tsx`

```typescript
const DefaultPages: Record<RouteKey, React.LazyExoticComponent<React.ComponentType>> = {
  dashboard:         lazy(() => import('../pages/Dashboard')),
  flowEpicDetail:    lazy(() => import('../pages/FlowEpicDetail')),
  workstationDetail: lazy(() => import('../pages/WorkstationDetail')),
};

export function BrandedPage({ route }: { route: RouteKey }) {
  const { pageOverrides } = useBrand();
  const Override = pageOverrides[route] as React.ComponentType | undefined;
  const Page = Override ?? DefaultPages[route];

  if (!Page) {
    console.warn(`[BrandedPage] Unknown route key: "${route}" — falling back`);
    return <Suspense fallback={<PageSkeleton />}><DefaultPages.dashboard /></Suspense>;
  }
  return <Suspense fallback={<PageSkeleton />}><Page /></Suspense>;
}
```

### `apps/joinerytech/src/App.tsx`

```typescript
const brandSkin = useAuthStore(s => s.brandSkin);
// brandSkin az Orchestrator token response body-jából jön — NEM JWT decode (SEC-P3C-07)

return (
  <BrandProvider
    skinId={brandSkin}
    pageOverrides={brandPageOverrides[brandSkin ?? 'joinerytech'] ?? {}}
  >
    <LocaleProvider locale={detectLocale()}>
      <RouterProvider router={router} />
    </LocaleProvider>
  </BrandProvider>
);
```

### `authStore.ts` bővítés

```typescript
interface AuthState {
  accessToken: string | null;
  brandSkin:   string | null;  // ← Orchestrator response-ból, NEM JWT decode
}
// Login: set({ accessToken: response.accessToken, brandSkin: response.brandSkin });
```

### Doorstar Dashboard override

`apps/joinerytech/src/pages/Dashboard/__brand/doorstar.tsx`:
- Doorstar-specifikus dashboard: Escrow widget, CNC export gomb, Spatial viewer link
- `showAuditPage: false` → Audit menüpont elrejtve az override-ban
- `// ⚠ NOT A SECURITY BOUNDARY — UX-only` megjegyzés minden featureFlag checkre

### Font loading (BE-P3C-08)

`apps/joinerytech/index.html`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
<link href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet" />
```

---

## T-07 — `@spaceos/i18n` + Nginx (Nap 10–11)

### `@spaceos/i18n`

`src/detectLocale.ts` (SEC-P3C-10 — nincs nginx sub_filter):
```typescript
export function detectLocale(): 'en' | 'hu' {
  if (typeof window === 'undefined') return 'en';
  return window.location.hostname.includes('asztalostech') ? 'hu' : 'en';
}
```

`src/useTranslation.ts` — type-safe translation keys nested dot notation-nel:
```typescript
type TranslationKey =
  | 'common.save' | 'common.cancel' | 'common.loading' | 'common.error'
  | 'nav.dashboard' | 'nav.projects' | 'nav.audit' | 'nav.settings'
  | 'dashboard.title' | 'dashboard.activeOrders' | 'dashboard.milestones'
  | 'flowEpic.status.pending' | 'flowEpic.status.inProgress' | 'flowEpic.status.done'
  // ...
```

### Nginx frissítés (SEC-P3C-10, SEC-P3C-11)

```nginx
# Régi: root /opt/spaceos/design-portal/dist/
# Új:   root /opt/spaceos/frontend/apps/joinerytech/dist/

# CSP bővítés:
add_header Content-Security-Policy "
  default-src 'self';
  script-src  'self';
  style-src   'self' https://fonts.googleapis.com 'unsafe-inline';
  font-src    'self' https://fonts.gstatic.com;
  img-src     'self' /brand/ data:;
  connect-src 'self';
" always;

# Nincs sub_filter (SEC-P3C-10 — locale az app-ban detektálva)

# /brand/ statikus logókhoz:
location /brand/ { expires 7d; add_header Cache-Control "public"; }
```

Átmeneti szimlink (deploy után eltávolítandó):
```bash
ln -sf /opt/spaceos/frontend/apps/joinerytech/dist /opt/spaceos/design-portal/dist
```

---

## Tesztek (Nap 12–13, ≥ 35 új)

### Unit tesztek — `@spaceos/brand-tokens`

```
brandSanitize.test.ts:
  validateHexColor('#B45309')                   → pass
  validateHexColor('expression(alert(1))')      → throw
  validateHexColor('rgb(255,0,0)')              → pass
  validateLogoPath('/brand/doorstar/logo.svg')  → pass
  validateLogoPath('../../etc/passwd')          → throw
  validateLogoPath('/brand/x/logo.exe')         → throw
  validateBrandTokens(doorstar)                 → 0 exception (valid token)

brandRegistry.test.ts:
  resolveBrand('doorstar')     → Doorstar tokens
  resolveBrand('joinerytech')  → JoineryTech tokens
  resolveBrand('ismeretlen')   → JoineryTech fallback (nem crash)
  resolveBrand(null)           → JoineryTech fallback
```

### Unit tesztek — `BrandProvider`

```
BrandProvider.test.tsx:
  CSS var-ok beállítva mount-kor
  CSS var-ok törölve unmount-kor
  skinId='doorstar' → Doorstar primary color CSS var
  pageOverrides context elérhető useBrand()-ban

BrandedPage.test.tsx:
  Override nincs → DefaultPages[route] töltődik
  Override van → override komponens töltődik
  Ismeretlen route → Dashboard fallback + console.warn
```

### Security tesztek

```
brandSkinSecurity.test.ts:
  localStorage.setItem('brandSkin', 'doorstar') → nincs hatás App mountkor
  URL ?skin=doorstar → skinId nem változik
  brandSkin csak authStore.brandSkin-ből olvasódik

authStore.test.ts:
  login response.brandSkin tárolva
  grep grepek (CI gate):
    grep -rn "jwtDecode|atob" apps/joinerytech/src/store → 0
    grep -rn "lazy" packages/@spaceos/brand-tokens/src/tokens/doorstar.ts → 0
    grep -r "api-client|fetch|axios" packages/@spaceos/ui/src → 0
```

### i18n tesztek

```
detectLocale.test.ts:
  window.location.hostname = 'joinerytech.hu'  → 'en'
  window.location.hostname = 'asztalostech.hu' → 'hu'
  window.location.hostname = 'localhost'       → 'en' (default)

useTranslation.test.ts:
  locale 'en' → angol szöveg
  locale 'hu' → magyar szöveg
  ismeretlen key → key visszaadva (nem crash)
```

---

## DoD Checklist

| Gate | Ellenőrzés |
|------|-----------|
| ✅ | `pnpm turbo build` 0 error |
| ✅ | `pnpm turbo typecheck` 0 TS error |
| ✅ | `pnpm turbo test` 256 meglévő + ≥ 35 új zöld |
| ✅ | `madge --circular packages/` → 0 körhivatkozás |
| ✅ | `grep -r "api-client\|fetch\|axios" packages/@spaceos/ui/src` → 0 |
| ✅ | `grep -rn "lazy" packages/@spaceos/brand-tokens/src/tokens/doorstar.ts` → 0 |
| ✅ | `grep -rn "jwtDecode\|atob" apps/joinerytech/src/store` → 0 |
| ✅ | `validateHexColor('expression(alert(1))')` → throw |
| ✅ | `validateLogoPath('../../etc/passwd')` → throw |
| ✅ | `resolveBrand('ismeretlen')` → JoineryTech fallback |
| ✅ | `brandSkin: 'doorstar'` (Orchestrator resp) → Doorstar Dashboard |
| ✅ | `localStorage.setItem('brandSkin', 'doorstar')` → nincs hatás |
| ✅ | `detectLocale()` hostname-only, nincs nginx dependency |
| ⏳ | Nginx deploy: új dist path + CSP + /brand/ location |
| ⏳ | `https://joinerytech.hu/brand/doorstar/logo.svg` → 200 |
| ⏳ | CSP `img-src 'self' /brand/ data:` — curl -I ellenőrzés |
| ⏳ | Kernel migration 0024 alkalmazva (MSG-K028) |

---

## Kritikus anti-pattern-ek — NE csináld

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `lazy()` egy `packages/@spaceos/*` fájlban | `lazy()` csak `apps/*`-ban |
| `brandSkin` JWT decode-ból (`atob`, `jwtDecode`) | `brandSkin` Orchestrator response body-ból |
| `style={{ color: brand.colors.primary }}` | `className="text-primary"` (Tailwind CSS var) |
| `localStorage.getItem('skin')` | Orchestrator response state-ből |
| `@spaceos/ui` importálja `@spaceos/api-client`-et | `@spaceos/ui`-nak nulla hálózati dep |
| Direkt `packages/@spaceos/domain/src/generated/` path | Csak `@spaceos/domain` publikus re-export |
| `featureFlags.showAuditPage === false` → API hívás blokkolva | `featureFlags` UX-only; RBAC Orchestrator-ban |

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-P019`:
- T-01..T-07 státusz (✅ DONE / ⚠️ eltérés)
- `pnpm turbo build` + `pnpm turbo test` eredmény
- `madge --circular packages/` eredmény
- Összes DoD grep gate eredménye
- Új tesztek száma (cél: ≥ 35)
- Nginx változás elvégezve? (VPS deploy gate)
- Bármilyen blocker
