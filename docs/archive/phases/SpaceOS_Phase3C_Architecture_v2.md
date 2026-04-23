# SpaceOS — Phase 3C Architecture
## Multi-Brand Portal: Turborepo + Brand Skin System + Doorstar Pilot UI

> Verzió: v2.0 — 2026-04-07
> Státusz: **IMPLEMENTÁCIÓRA KÉSZ — végleges tervdokumentum**
> Blokkoló feltétel: Phase 3B DoD teljes (párhuzamosan tervezhető)
> Referencia ügyfél: Doorstar Kft. — első tenant skin pilot
> Becsült időtartam: **16 fejlesztői nap** (v1: 14 nap → v2: +2 nap, 14 új finding)
> Érintett repo: `spaceos-design-portal` (in-place Turborepo migráció)
> Kumulált review: `/senior-frontend` + `/senior-security` → v2

---

## 1. Kumulált Finding Összesítő (v1 → v2)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|---------------------|-------------|
| v1 (pre-emptive) | 0C + 3H + 3M | CSS injection regex, lazy load crash, locale XSS, Turborepo cache | — |
| v1 → `/senior-frontend` + `/senior-security` → v2 | 2C + 4H + 8M | Circular dep package→app; JWT client-decode; Tailwind preset; Vite HMR; codegen CI; CSS regex split; logo path | +2 nap |
| **Összesen** | **2C + 7H + 11M = 20 finding** | | **16 nap végleges becslés** |

### Frontend findings — v1 → v2

| ID | Súly | Terület | Probléma | v2 javítás |
|----|------|---------|----------|-----------|
| BE-P3C-01 | 🔴 CRITICAL | Circular dep | `doorstar.ts` (`@spaceos/brand-tokens`) `lazy(() => import('../../apps/joinerytech/...'))` — package nem importálhat app-ból | `pageOverrides` szétválasztva: token package csak adatot (szín, logo, flag) tartalmaz; page override-ok az **app** `src/brand/overrides.ts`-ben regisztrálva; `BrandProvider` `pageOverrides` propot kap |
| BE-P3C-02 | 🟠 HIGH | Tailwind preset | Tailwind preset nincs specifikálva — `className="text-primary"` nem működik | Teljes `tailwind/preset.ts` specifikálva: `theme.extend.colors` CSS var-okra mutat |
| BE-P3C-03 | 🟠 HIGH | Vite dev HMR | Workspace package resolution nincs specifikálva | `vite.config.ts` `resolve.alias` minden `@spaceos/*` csomagra; prod-ban standard dist |
| BE-P3C-04 | 🟠 HIGH | Codegen CI | `KERNEL_OPENAPI_URL` CI-ban nem elérhető | `openapi.json` snapshot commitolva `spaceos-docs`-ba; codegen snapshot ellen fut; CI diff gate |
| BE-P3C-05 | 🟡 MEDIUM | Vitest workspace | `vitest.workspace.ts` root config hiányzik | Specifikálva: `vitest.workspace.ts` minden `apps/*` + `packages/@spaceos/*`-ra |
| BE-P3C-06 | 🟡 MEDIUM | workspace:* | `workspace:*` protocol nem megmutatva | App `package.json` teljes dependencia lista megmutatva |
| BE-P3C-07 | 🟡 MEDIUM | i18n interface | `useTranslation` hook interface nem specifikált | Teljes hook signature + nested key + type-safe keys |
| BE-P3C-08 | 🟡 MEDIUM | Font loading | Inter + JetBrains Mono nincs betöltve | `index.html`-be brand-agnosztikus Google Fonts preconnect; brand-specifikus font override `BrandProvider`-ben |

### Security findings — v1 → v2

| ID | Súly | Terület | Probléma | v2 javítás |
|----|------|---------|----------|-----------|
| SEC-P3C-07 | 🔴 CRITICAL | JWT spoofing | `brand_skin` client-side JWT decode → tamper lehetséges (JWT-t localStorage-ban tárolja, módosítható) | `brandSkin` az Orchestrator `/bff/api/auth/token` **response body**-jából jön — Orchestrator RS256-verifikált; a frontend nem dekódolja a JWT-t skin extraction céljára |
| SEC-P3C-08 | 🟠 HIGH | CSS sanitizer | Általános regex túl permisszív | Per-type sanitizer: `validateHexColor()` (hex/rgb/hsl only), `validateFontFamily()` (whitelist), `validateLogoPath()` (relative path regex) |
| SEC-P3C-09 | 🟠 HIGH | logo.src | Nincs path validáció | `validateLogoPath()`: csak `/brand/{alphanumeric}/logo.{svg\|png\|webp}` engedélyezett |
| SEC-P3C-10 | 🟡 MEDIUM | nginx sub_filter | Törékeny locale injektálás | Eltávolítva — `detectLocale()` hostname-alapú kizárólag; `sub_filter` dependency megszüntetve |
| SEC-P3C-11 | 🟡 MEDIUM | CSP | Nincs frissítve `/brand/*` és font forrásokra | CSP bővítve: `img-src 'self' /brand/`, `font-src https://fonts.gstatic.com` |
| SEC-P3C-12 | 🟡 MEDIUM | featureFlags | `featureFlags` nem dokumentált mint UX-only | Explicit `⚠ NOT A SECURITY BOUNDARY` jelölés minden featureFlag használati helyen; RBAC az Orchestrator-ban marad |

---

## 2. Architektúra áttekintés

### 2.1 Monorepo struktúra

```
spaceos-design-portal/              ← repo neve marad, belső struktúra változik
  apps/
    joinerytech/
      src/
        brand/
          overrides.ts              ← ÚJ (BE-P3C-01 fix): app regisztrálja az override-okat
        pages/
          Dashboard/
            index.tsx               ← default
            __brand/
              doorstar.tsx          ← Medium override
        routes/
          brandRouter.tsx
        App.tsx
        main.tsx
      index.html                    ← Google Fonts preconnect (BE-P3C-08)
      vite.config.ts                ← resolve.alias (BE-P3C-03)
      tailwind.config.ts
      vitest.config.ts
      package.json                  ← workspace:* deklarációk (BE-P3C-06)
  packages/
    @spaceos/ui/
    @spaceos/domain/
    @spaceos/api-client/
    @spaceos/brand-tokens/          ← React-mentes; csak adat + BrandProvider
    @spaceos/i18n/
  vitest.workspace.ts               ← ÚJ (BE-P3C-05)
  turbo.json
  package.json
  pnpm-workspace.yaml
```

### 2.2 Package dependency graph (kör nélkül)

```
apps/joinerytech
    ↓
@spaceos/api-client ──→ @spaceos/domain       (nulla internal dep)
@spaceos/ui         ──→ @spaceos/brand-tokens  (react peer dep, nincs app import)
@spaceos/i18n                                  (nulla dep)

❌ TILOS: packages/* → apps/*
❌ TILOS: @spaceos/ui → @spaceos/api-client
```

### 2.3 Rendszerszintű változások

```
Browser  https://joinerytech.hu  →  Nginx  →  /opt/spaceos/frontend/apps/joinerytech/dist/
Browser  https://asztalostech.hu →  Nginx  →  /opt/spaceos/frontend/apps/joinerytech/dist/

Nginx (joinerytech.hu): X-SpaceOS-Brand: joinerytech
Nginx (asztalostech.hu): X-SpaceOS-Brand: asztalostech
Locale: hostname-alapú detektálás az app-ban — nincs nginx sub_filter (SEC-P3C-10)

brand_skin flow (SEC-P3C-07 fix):
  POST /bff/api/auth/token
    → Orchestrator RS256-verifikál
    → response: { accessToken, brandSkin: 'doorstar', expiresAt }
    → App: useAuthStore.brandSkin = response.brandSkin
    → BrandProvider skinId={brandSkin}
```

---

## 3. Package specifikációk

### 3.1 `@spaceos/domain`

```
packages/@spaceos/domain/
  src/
    generated/
      kernel.ts          ← openapi-typescript kimenet — DO NOT EDIT
    index.ts             ← whitelist re-export (SEC-P3C-03)
  scripts/
    codegen.sh
  openapi-snapshot/
    kernel.json          ← commitolt snapshot (BE-P3C-04 fix)
  package.json
```

```json
// packages/@spaceos/domain/package.json
{
  "name": "@spaceos/domain",
  "private": true,
  "scripts": {
    "codegen": "openapi-typescript ./openapi-snapshot/kernel.json -o src/generated/kernel.ts",
    "codegen:fetch": "curl -sf $KERNEL_OPENAPI_URL -o openapi-snapshot/kernel.json",
    "build": "tsc --noEmit"
  },
  "devDependencies": {
    "openapi-typescript": "^7.0.0",
    "typescript": "^5.0.0"
  }
}
```

**Codegen CI flow (BE-P3C-04 fix):**
```
Dev workflow:
  1. Kernel API változik → developer futtatja: pnpm --filter @spaceos/domain codegen:fetch
  2. kernel.json snapshot frissítve → commitolva
  3. pnpm --filter @spaceos/domain codegen → kernel.ts újragenerálva → commitolva

CI gate:
  pnpm --filter @spaceos/domain codegen
  git diff --exit-code packages/@spaceos/domain/src/generated/kernel.ts
  → Ha eltér: build FAIL ("Generated types out of sync — run codegen locally")
```

**Whitelist re-export (SEC-P3C-03):**
```typescript
// src/index.ts
// ONLY public-facing schemas exported — internal SIP + audit admin schemas excluded
export type {
  components as KernelSchemas,
  paths     as KernelPaths,
} from './generated/kernel';

// Manuális branded types
export type TenantId    = string & { readonly __brand: 'TenantId' };
export type FlowEpicId  = string & { readonly __brand: 'FlowEpicId' };
```

---

### 3.2 `@spaceos/api-client`

```json
// packages/@spaceos/api-client/package.json (BE-P3C-06 minta)
{
  "name": "@spaceos/api-client",
  "private": true,
  "main": "./src/index.ts",
  "exports": { ".": "./src/index.ts" },
  "dependencies": {
    "@spaceos/domain": "workspace:*"
  }
}
```

```typescript
// src/createClient.ts
export interface ClientOptions {
  baseUrl: string;
  getToken: () => string | null;
}

// 401 → Zustand logout dispatch — nincs retry loop az api-client-ben.
// Token refresh az Orchestrator /bff/api/auth/refresh útján — api-client ezt nem kezeli.
```

---

### 3.3 `@spaceos/ui`

```json
// packages/@spaceos/ui/package.json
{
  "name": "@spaceos/ui",
  "private": true,
  "main": "./src/index.ts",
  "exports": { ".": "./src/index.ts" },
  "peerDependencies": { "react": "^18.0.0" },
  "dependencies": {
    "@spaceos/brand-tokens": "workspace:*"
  }
}
```

**Invariáns:** `grep -r "api-client\|fetch\|axios" packages/@spaceos/ui/src` → 0 találat.

---

### 3.4 `@spaceos/brand-tokens` (BE-P3C-01 fix)

**Kulcsváltozás v2-ben:** A csomag `BrandTokens` interfészből **eltávolítva** a `pageOverrides` (React típusok). A token csomag tisztán adatot tartalmaz — nincs `React` import a `types.ts`-ben és a token definíciókban.

```
packages/@spaceos/brand-tokens/
  src/
    types.ts             ← React-MENTES — csak adat (BE-P3C-01 fix)
    registry.ts
    sanitize.ts          ← per-type validátorok (SEC-P3C-08, SEC-P3C-09)
    tokens/
      joinerytech.ts
      doorstar.ts        ← NEM tartalmaz lazy() importot
    tailwind/
      preset.ts          ← teljes spec (BE-P3C-02 fix)
    BrandProvider.tsx    ← React context — pageOverrides prop-ot kap kívülről
    useBrand.ts
  package.json
```

#### BrandTokens interface — React-mentes (BE-P3C-01 fix)

```typescript
// src/types.ts

// ── SHALLOW ──────────────────────────────────────────────────────────
export interface ColorTokens {
  primary:    string;
  secondary:  string;
  accent:     string;
  surface:    string;
  background: string;
  text:       string;
  textMuted:  string;
  danger:     string;
  warning:    string;
  success:    string;
}

export interface LogoConfig {
  // Csak /brand/{alphanumeric}/logo.{svg|png|webp} engedélyezett (SEC-P3C-09)
  src:    string;
  alt:    string;
  width:  number;
  height: number;
}

export interface TypographyTokens {
  fontFamily:      string;
  fontFamilyMono:  string;
}

// ── DEEP ─────────────────────────────────────────────────────────────
export interface FeatureFlags {
  // ⚠ NOT A SECURITY BOUNDARY — UX-only (SEC-P3C-12)
  // RBAC az Orchestrator JWT middleware-ben érvényesítve.
  // Ezen flag-ek elrejtik az UI elemet, NEM blokkolják az API hívást.
  showCncExport:      boolean;
  showEscrowWidget:   boolean;
  showAuditPage:      boolean;
  showSpatialViewer:  boolean;
}

export interface NavItem {
  key:   string;
  label: string;   // i18n key
  icon:  string;
  path:  string;
  roles: string[];
}

// ── FULL TOKEN SET — NEM tartalmaz React típust ───────────────────────
export interface BrandTokens {
  id:          string;
  displayName: string;
  colors:      ColorTokens;
  logo:        LogoConfig;
  typography:  TypographyTokens;
  // Medium: pageOverrides NEM itt van — az app regisztrálja (BE-P3C-01)
  featureFlags?:    FeatureFlags;
  navigationItems?: NavItem[];
}

// ── MEDIUM: page override type — app-ban használatos ─────────────────
// Ez a type exportálva van, hogy az app type-safe legyen,
// de a konkrét React.lazy() hívások az APP-ban élnek.
export type RouteKey = 'dashboard' | 'flowEpicDetail' | 'workstationDetail';
// ComponentType: generikus — React-mentes, az app tölti fel konkrét típussal
export type PageOverrideRegistry = Partial<Record<RouteKey, unknown>>;
```

#### Per-type CSS sanitizers (SEC-P3C-08, SEC-P3C-09)

```typescript
// src/sanitize.ts

// Szín: csak hex (#RGB, #RRGGBB), rgb(), hsl() engedélyezett
const COLOR_RE = /^(#[0-9a-fA-F]{3,8}|rgb\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|hsl\(\s*\d+\s*,\s*\d+%\s*,\s*\d+%\s*\))$/;
export function validateHexColor(value: string): string {
  if (!COLOR_RE.test(value.trim()))
    throw new Error(`Invalid color token: "${value}"`);
  return value.trim();
}

// Font-family: csak betűk, számok, szóköz, vessző, aposztrof, kötőjel
const FONT_RE = /^[\w\s,"'\-]+$/;
export function validateFontFamily(value: string): string {
  if (!FONT_RE.test(value))
    throw new Error(`Invalid font-family token: "${value}"`);
  return value;
}

// Logo path: csak /brand/{alphanumeric}/{filename}.{svg|png|webp}
const LOGO_PATH_RE = /^\/brand\/[a-z0-9_-]+\/[a-z0-9_-]+\.(svg|png|webp)$/i;
export function validateLogoPath(src: string): string {
  if (!LOGO_PATH_RE.test(src))
    throw new Error(`Invalid logo path: "${src}" — must match /brand/{id}/filename.{svg|png|webp}`);
  return src;
}

// resolveBrand hívja validálás után
export function validateBrandTokens(tokens: BrandTokens): BrandTokens {
  Object.values(tokens.colors).forEach(v => validateHexColor(v));
  validateFontFamily(tokens.typography.fontFamily);
  validateFontFamily(tokens.typography.fontFamilyMono);
  validateLogoPath(tokens.logo.src);
  return tokens;
}
```

#### Registry

```typescript
// src/registry.ts
import { joinerytech } from './tokens/joinerytech';
import { doorstar }    from './tokens/doorstar';
import { validateBrandTokens } from './sanitize';

const RAW_REGISTRY: Record<string, BrandTokens> = { joinerytech, doorstar };

// Startup validation — minden token sanitizálva van registry build-kor
export const brandRegistry = Object.fromEntries(
  Object.entries(RAW_REGISTRY).map(([k, v]) => [k, validateBrandTokens(v)])
) as Record<string, BrandTokens>;

export function resolveBrand(skinId: string | null): BrandTokens {
  return brandRegistry[skinId ?? 'joinerytech'] ?? brandRegistry['joinerytech'];
}
```

#### BrandProvider — pageOverrides propot kap (BE-P3C-01 fix)

```typescript
// src/BrandProvider.tsx
import { createContext, useContext, useEffect, useMemo, type ReactNode } from 'react';
import { resolveBrand, type BrandTokens, type PageOverrideRegistry } from './index';

interface BrandContextValue extends BrandTokens {
  pageOverrides: PageOverrideRegistry;
}

const BrandContext = createContext<BrandContextValue>({
  ...resolveBrand('joinerytech'),
  pageOverrides: {},
});

export function BrandProvider({
  skinId,
  pageOverrides = {},
  children,
}: {
  skinId:          string | null;
  pageOverrides?:  PageOverrideRegistry;  // ← az APP adja át (BE-P3C-01)
  children:        ReactNode;
}) {
  const tokens = useMemo(() => resolveBrand(skinId), [skinId]);

  useEffect(() => {
    const root = document.documentElement;
    // Token értékek már validateBrandTokens-on átmentek registry-build-kor
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

    // Cleanup: skinId változáskor régi CSS var-ok törlése
    return () => {
      const vars = ['--color-primary','--color-secondary','--color-accent',
                    '--color-surface','--color-background','--color-text',
                    '--color-text-muted','--color-danger','--color-warning',
                    '--color-success','--font-family','--font-family-mono'];
      vars.forEach(v => root.style.removeProperty(v));
    };
  }, [tokens]);

  const value = useMemo(() => ({ ...tokens, pageOverrides }), [tokens, pageOverrides]);

  return <BrandContext.Provider value={value}>{children}</BrandContext.Provider>;
}

export const useBrand = () => useContext(BrandContext);
```

#### Tailwind preset (BE-P3C-02 fix)

```typescript
// packages/@spaceos/brand-tokens/src/tailwind/preset.ts
import type { Config } from 'tailwindcss';

// Tailwind CSS var-okra mutat — a szín értékek runtime-ban cserélhetők BrandProvider által
export const spaceosPreset: Config = {
  theme: {
    extend: {
      colors: {
        primary:    'var(--color-primary)',
        secondary:  'var(--color-secondary)',
        accent:     'var(--color-accent)',
        surface:    'var(--color-surface)',
        background: 'var(--color-background)',
        'text-base': 'var(--color-text)',
        'text-muted': 'var(--color-text-muted)',
        danger:     'var(--color-danger)',
        warning:    'var(--color-warning)',
        success:    'var(--color-success)',
      },
      fontFamily: {
        sans: ['var(--font-family)', 'system-ui', 'sans-serif'],
        mono: ['var(--font-family-mono)', 'monospace'],
      },
    },
  },
};
```

```typescript
// apps/joinerytech/tailwind.config.ts
import { spaceosPreset } from '@spaceos/brand-tokens/tailwind/preset';
import type { Config } from 'tailwindcss';

export default {
  presets: [spaceosPreset],
  content: ['./src/**/*.{ts,tsx}', '../../packages/@spaceos/ui/src/**/*.{ts,tsx}'],
  // App-specifikus extend ide kerül
} satisfies Config;
```

---

### 3.5 `@spaceos/i18n` (BE-P3C-07 fix)

```typescript
// packages/@spaceos/i18n/src/useTranslation.ts

// Type-safe translation keys (nested dot notation)
type TranslationKey =
  | 'common.save' | 'common.cancel' | 'common.loading' | 'common.error'
  | 'nav.dashboard' | 'nav.projects' | 'nav.audit' | 'nav.settings'
  | 'dashboard.title' | 'dashboard.activeOrders' | 'dashboard.milestones'
  | 'flowEpic.status.pending' | 'flowEpic.status.inProgress' | 'flowEpic.status.done'
  // ... stb.

// Hook interface
export function useTranslation(): {
  t: (key: TranslationKey, vars?: Record<string, string | number>) => string;
  locale: 'en' | 'hu';
} { ... }
```

```typescript
// packages/@spaceos/i18n/src/detectLocale.ts
// SEC-P3C-10 fix: nginx sub_filter eltávolítva — hostname-only
export function detectLocale(): 'en' | 'hu' {
  if (typeof window === 'undefined') return 'en';
  return window.location.hostname.includes('asztalostech') ? 'hu' : 'en';
}
```

---

## 4. Brand override router (Medium szint — BE-P3C-01 fix)

### 4.1 App-oldali override regisztráció

```typescript
// apps/joinerytech/src/brand/overrides.ts
// ← Ez az APP fájl — itt biztonságos lazy() importot használni
import { lazy } from 'react';
import type { PageOverrideRegistry } from '@spaceos/brand-tokens';

// Kulcs = brand skin ID, érték = route override-ok
export const brandPageOverrides: Record<string, PageOverrideRegistry> = {
  doorstar: {
    dashboard: lazy(() => import('../pages/Dashboard/__brand/doorstar')),
    // flowEpicDetail: nincs override — Doorstar a default-ot kapja
  },
  // joinerytech: nincs override bejegyzés — mindenhol default
};
```

### 4.2 App root — brand skin az Orchestrator response-ból (SEC-P3C-07 fix)

```typescript
// apps/joinerytech/src/App.tsx
import { BrandProvider }    from '@spaceos/brand-tokens';
import { LocaleProvider }   from '@spaceos/i18n';
import { detectLocale }     from '@spaceos/i18n';
import { brandPageOverrides } from './brand/overrides';
import { useAuthStore }     from './store/authStore';

export function App() {
  // brandSkin az Orchestrator token response body-jából jön — nem JWT decode
  const brandSkin = useAuthStore(s => s.brandSkin);

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
}
```

```typescript
// apps/joinerytech/src/store/authStore.ts
interface AuthState {
  accessToken:  string | null;
  brandSkin:    string | null;   // ← Orchestrator response-ból, NEM JWT decode
  // ...
}

// Login handler:
// const response = await orchestratorClient.login(credentials);
// set({ accessToken: response.accessToken, brandSkin: response.brandSkin });
```

### 4.3 BrandedPage router

```typescript
// apps/joinerytech/src/routes/brandRouter.tsx
import { lazy, Suspense } from 'react';
import { useBrand } from '@spaceos/brand-tokens';
import type { RouteKey } from '@spaceos/brand-tokens';

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
    // SEC-P3C-04: ismeretlen route → default fallback, nem crash
    console.warn(`[BrandedPage] Unknown route key: "${route}" — falling back to Dashboard`);
    return <Suspense fallback={<PageSkeleton />}><DefaultPages.dashboard /></Suspense>;
  }

  return (
    <Suspense fallback={<PageSkeleton />}>
      <Page />
    </Suspense>
  );
}
```

---

## 5. Doorstar tenant skin (v2 — React-mentes token definíció)

### 5.1 Token definíció — nincs `lazy()` import (BE-P3C-01 fix)

```typescript
// packages/@spaceos/brand-tokens/src/tokens/doorstar.ts
import type { BrandTokens } from '../types';
// ← NINCS React import, NINCS lazy() — ez csak adatstruktúra

export const doorstar: BrandTokens = {
  id:          'doorstar',
  displayName: 'Doorstar',

  colors: {
    primary:    '#B45309',   // amber-700
    secondary:  '#1C1917',   // stone-900
    accent:     '#D97706',   // amber-600
    surface:    '#292524',   // stone-800
    background: '#0C0A09',   // stone-950
    text:       '#FAFAF9',   // stone-50
    textMuted:  '#A8A29E',   // stone-400
    danger:     '#DC2626',
    warning:    '#F59E0B',
    success:    '#16A34A',
  },
  logo: {
    src:    '/brand/doorstar/logo.svg',   // validateLogoPath() ✓
    alt:    'Doorstar',
    width:  140,
    height: 36,
  },
  typography: {
    fontFamily:     '"Inter", system-ui, sans-serif',
    fontFamilyMono: '"JetBrains Mono", monospace',
  },

  // ⚠ NOT A SECURITY BOUNDARY — UX-only (SEC-P3C-12)
  // Az API hívások minden esetben Orchestrator RBAC-on mennek át.
  featureFlags: {
    showCncExport:      true,
    showEscrowWidget:   true,
    showAuditPage:      false,
    showSpatialViewer:  true,
  },
  // pageOverrides: NINCS ITT — apps/joinerytech/src/brand/overrides.ts-ben (BE-P3C-01)
};
```

### 5.2 Font loading (BE-P3C-08 fix)

```html
<!-- apps/joinerytech/index.html -->
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />

  <!-- Google Fonts preconnect (brand-agnosztikus — minden skin használja) -->
  <link rel="preconnect" href="https://fonts.googleapis.com" />
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
  <!-- Inter (Doorstar + JoineryTech default) -->
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet" />
  <!-- JetBrains Mono (monospace — kód megjelenítés) -->
  <link href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet" />

  <title>JoineryTech</title>
</head>
```

---

## 6. Orchestrator változás — brand_skin a token response-ban (SEC-P3C-07 fix)

```typescript
// spaceos-orchestrator: src/routes/auth.route.ts
// POST /bff/api/auth/token response kiegészítése brandSkin-nel

interface TokenResponse {
  accessToken: string;
  expiresAt:   string;
  brandSkin:   string;   // ← ÚJ: Orchestrator dekódolja a JWT-t, nem a frontend
}

// Orchestrator: RS256-verifikált JWT-ből kiolvassa a brand_skin claim-et
// és a response body-ban adja vissza — a frontend nem dekódol JWT-t
const decoded = jwtVerify(token, publicKey);   // Orchestrator-ban
return {
  accessToken: token,
  expiresAt:   new Date(decoded.exp * 1000).toISOString(),
  brandSkin:   (decoded['brand_skin'] as string) ?? 'joinerytech',
};
```

**Megjegyzés:** Ez az Orchestrator változás minimális — 1 extra claim visszaadása a meglévő token response-ban. Kernel migration 0024 (`BrandSkinId`) Phase 3C T-06 napján.

---

## 7. Turborepo + Vite konfiguráció

### 7.1 turbo.json

```json
{
  "$schema": "https://turbo.build/schema.json",
  "pipeline": {
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["dist/**"]
    },
    "codegen": {
      "cache": false,
      "outputs": ["src/generated/**"]
    },
    "test": {
      "dependsOn": ["^build"],
      "outputs": ["coverage/**"]
    },
    "typecheck": { "dependsOn": ["^build"], "outputs": [] },
    "lint":      { "outputs": [] }
  }
}
```

### 7.2 App package.json — workspace:* (BE-P3C-06 fix)

```json
// apps/joinerytech/package.json
{
  "name": "@spaceos/app-joinerytech",
  "private": true,
  "dependencies": {
    "@spaceos/api-client":   "workspace:*",
    "@spaceos/brand-tokens": "workspace:*",
    "@spaceos/domain":       "workspace:*",
    "@spaceos/i18n":         "workspace:*",
    "@spaceos/ui":           "workspace:*",
    "react":                 "^18.0.0",
    "react-dom":             "^18.0.0",
    "react-router-dom":      "^6.0.0",
    "zustand":               "^4.0.0"
  },
  "devDependencies": {
    "vite":      "^5.0.0",
    "vitest":    "^2.0.0",
    "tailwindcss": "^3.0.0"
  }
}
```

### 7.3 Vite workspace resolution (BE-P3C-03 fix)

```typescript
// apps/joinerytech/vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // Dev HMR: workspace csomagok src/-ből töltődnek (nem dist/)
      '@spaceos/api-client':   path.resolve(__dirname, '../../packages/@spaceos/api-client/src'),
      '@spaceos/brand-tokens': path.resolve(__dirname, '../../packages/@spaceos/brand-tokens/src'),
      '@spaceos/domain':       path.resolve(__dirname, '../../packages/@spaceos/domain/src'),
      '@spaceos/i18n':         path.resolve(__dirname, '../../packages/@spaceos/i18n/src'),
      '@spaceos/ui':           path.resolve(__dirname, '../../packages/@spaceos/ui/src'),
    },
  },
  // Prod build: standard node_modules resolution (pnpm symlinks)
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'spaceos-core': ['@spaceos/ui', '@spaceos/brand-tokens'],
        },
      },
    },
  },
});
```

### 7.4 Vitest workspace (BE-P3C-05 fix)

```typescript
// vitest.workspace.ts (repo root)
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

---

## 8. Nginx változások (SEC-P3C-10, SEC-P3C-11 fix)

```nginx
# /etc/nginx/sites-available/spaceos

# ── joinerytech.hu ───────────────────────────────────────────────────
server {
    listen 443 ssl http2;
    server_name joinerytech.hu;

    root /opt/spaceos/frontend/apps/joinerytech/dist;

    # Maradó security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;

    # CSP — frissítve Phase 3C-re (SEC-P3C-11)
    add_header Content-Security-Policy "
        default-src 'self';
        script-src  'self';
        style-src   'self' https://fonts.googleapis.com 'unsafe-inline';
        font-src    'self' https://fonts.gstatic.com;
        img-src     'self' /brand/ data:;
        connect-src 'self' https://joinerytech.hu https://asztalostech.hu;
    " always;

    add_header X-SpaceOS-Brand "joinerytech" always;
    # X-SpaceOS-Locale: nincs — hostname alapján detektálja az app (SEC-P3C-10)

    location / {
        try_files $uri $uri/ /index.html;
    }
    location /assets/ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
    location /brand/ {
        expires 7d;
        add_header Cache-Control "public";
    }
    location /bff/ {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header X-SpaceOS-Brand "joinerytech";
    }
}

# ── asztalostech.hu ──────────────────────────────────────────────────
server {
    listen 443 ssl http2;
    server_name asztalostech.hu;

    root /opt/spaceos/frontend/apps/joinerytech/dist;   # UGYANAZ a dist

    # CSP identikus — nincs sub_filter (SEC-P3C-10 fix)
    add_header Content-Security-Policy "..." always;   # ua. mint fent

    add_header X-SpaceOS-Brand "asztalostech" always;
    # Locale: az app detektálja a hostname-ből ('asztalostech' → 'hu')

    location / {
        try_files $uri $uri/ /index.html;
        # NINCS sub_filter (SEC-P3C-10) — hostname-based detection az app-ban
    }
    location /assets/ { expires 1y; add_header Cache-Control "public, immutable"; }
    location /brand/  { expires 7d; add_header Cache-Control "public"; }
    location /bff/ {
        proxy_pass http://127.0.0.1:3000;
        proxy_set_header X-SpaceOS-Brand "asztalostech";
    }
}
```

**Deploy path változás:**
```bash
# Régi: /opt/spaceos/design-portal/dist/
# Új:   /opt/spaceos/frontend/apps/joinerytech/dist/

# Átmeneti szimlink (deploy után eltávolítandó):
ln -sf /opt/spaceos/frontend/apps/joinerytech/dist /opt/spaceos/design-portal/dist
```

---

## 9. Migrációs stratégia (in-place, 7 lépés)

**Invariáns:** Minden lépés végén `pnpm turbo test` zöld. Vizuális regresszió: manuális snapshot minden lépés után.

```
Lépés 1: Turborepo scaffold (Nap 1)
  → package.json workspace root; pnpm-workspace.yaml; turbo.json
  → apps/joinerytech/ ← jelenlegi src/ + összes config
  → packages/@spaceos/* — üres shell-ek (index.ts = placeholder)
  → vitest.workspace.ts root config
  → ELLENŐRZÉS: pnpm turbo build ✓; 256 teszt zöld

Lépés 2: @spaceos/domain codegen (Nap 2)
  → openapi-typescript + openapi-snapshot/kernel.json commitolva
  → src/generated/kernel.ts generálva
  → Jelenlegi típusok cserélve → @spaceos/domain importok
  → ELLENŐRZÉS: 0 TS error; 256 teszt zöld

Lépés 3+4: @spaceos/api-client (Nap 3–4)
  → src/lib/api.ts + hooks API hívások → packages/@spaceos/api-client
  → workspace:* dependency az app package.json-ban
  → ELLENŐRZÉS: 256 teszt zöld

Lépés 4: @spaceos/ui (Nap 5)
  → Button, Card, Table, Modal, Badge, StatusBadge, PageShell átköltöznek
  → ELLENŐRZÉS: 256 teszt zöld

Lépés 5: @spaceos/brand-tokens + BrandProvider (Nap 6–7)
  → types.ts (React-mentes); joinerytech.ts; sanitize.ts; Tailwind preset
  → BrandProvider app root-ban; CSS custom properties váltás
  → vite.config.ts resolve.alias
  → ELLENŐRZÉS: 256 teszt + brand unit tesztek zöld

Lépés 6: Doorstar skin (Nap 8–9)
  → doorstar.ts tokens (nincs lazy())
  → apps/joinerytech/src/brand/overrides.ts — lazy override regisztrálva
  → Doorstar Dashboard override elkészítve
  → Kernel migration 0024 + Orchestrator brandSkin response
  → ELLENŐRZÉS: 256 + brand tesztek zöld

Lépés 7: @spaceos/i18n + nginx (Nap 10–11)
  → en.json/hu.json; useTranslation; detectLocale (hostname-only)
  → Nginx config frissítve (new paths, CSP, no sub_filter)
  → Deploy path + szimlink
  → ELLENŐRZÉS: 256 + i18n tesztek zöld; E2E zöld
```

---

## 10. Implementációs sorrend (16 nap)

```
Nap  1:   T-01  Turborepo scaffold — workspace root, apps/joinerytech, package shells, vitest.workspace.ts
Nap  2:   T-02  @spaceos/domain codegen — snapshot, CI gate, típus migráció
Nap  3:   T-03  @spaceos/api-client — KernelClient + OrchestratorClient kiszervezés
Nap  4:   T-03  @spaceos/api-client — hook-ok átköltöztetése + workspace:* app package.json
Nap  5:   T-04  @spaceos/ui — PageShell, Button, Card, Table, Badge, StatusBadge
Nap  6:   T-05  @spaceos/brand-tokens — types.ts (React-mentes), sanitize.ts, joinerytech tokens, Tailwind preset
Nap  7:   T-05  BrandProvider + CSS custom properties váltás + vite resolve.alias
Nap  8:   T-06  Doorstar tokens + apps/brand/overrides.ts + Doorstar Dashboard override
Nap  9:   T-06  Orchestrator brandSkin response + Kernel migration 0024 + BrandProvider integration
Nap 10:   T-07  @spaceos/i18n — en.json/hu.json, useTranslation, detectLocale
Nap 11:   T-07  Nginx: new paths + CSP frissítés + deploy path + szimlink
Nap 12:   Tesztek — brand sanitize unit, BrandProvider, override resolution, i18n
Nap 13:   Tesztek — brand E2E, locale E2E, security gate-ek (skinId spoofing tesztek)
Nap 14:   EXPLAIN ANALYZE (ha van új API hívás) + bundle size check
Nap 15:   DoD checklist final
Nap 16:   Buffer / hotfix
```

---

## 11. Kritikus anti-pattern-ek (v2 — frissítve)

| Anti-pattern | Helyes megoldás |
|---|---|
| `lazy(() => import(...))` egy `@spaceos/*` package fájlban | Lazy importok csak `apps/*`-ban engedélyeztek |
| `useAuthStore(s => s.accessToken)` → JWT decode → `brand_skin` | `useAuthStore(s => s.brandSkin)` — Orchestrator response-ból |
| `style={{ color: brand.colors.primary }}` | Csak `className="text-primary"` — Tailwind CSS var-on át |
| `localStorage.getItem('skin')` brand override | `skinId` kizárólag Orchestrator response-ból |
| `@spaceos/ui` importálja `@spaceos/api-client`-et | `@spaceos/ui` nulla hálózati dep — `madge` gate |
| `root.style.setProperty('--color', unsanitizedValue)` | Token értékek `validateBrandTokens()`-on átmentek registry-build-kor |
| `featureFlags.showAuditPage === false` → hozzáférés megtagadva | `featureFlags` UX-only; RBAC az Orchestrator JWT middleware-ben |
| Direkt `packages/@spaceos/domain/src/generated` path import | Csak `@spaceos/domain` publikus re-export |

---

## 12. Biztonsági finding-ek teljes lista (v2)

| ID | Súly | Terület | Státusz |
|----|------|---------|---------|
| SEC-P3C-01 | 🔴 CRITICAL | Brand spoofing (URL/localStorage) | ✅ v1-ben javítva |
| SEC-P3C-02 | 🟠 HIGH | CSS injection regex | ✅ SEC-P3C-08-ba bővítve |
| SEC-P3C-03 | 🟠 HIGH | OpenAPI internal endpoint szivárgás | ✅ v1-ben javítva (whitelist re-export) |
| SEC-P3C-04 | 🟠 HIGH | Lazy load undefined crash | ✅ v1-ben javítva (console.warn fallback) |
| SEC-P3C-05 | 🟡 MEDIUM | Locale XSS (meta tag) | ✅ SEC-P3C-10-ben megszüntetve |
| SEC-P3C-06 | 🟡 MEDIUM | Turborepo cache poisoning | ✅ v1-ben javítva (codegen no-cache) |
| SEC-P3C-07 | 🔴 CRITICAL | JWT client-decode brand_skin | ✅ v2-ben javítva (Orchestrator response) |
| SEC-P3C-08 | 🟠 HIGH | CSS sanitizer per-type | ✅ v2-ben javítva (validateHexColor etc.) |
| SEC-P3C-09 | 🟠 HIGH | logo.src path traversal | ✅ v2-ben javítva (validateLogoPath) |
| SEC-P3C-10 | 🟡 MEDIUM | nginx sub_filter fragile | ✅ v2-ben megszüntetve |
| SEC-P3C-11 | 🟡 MEDIUM | CSP hiányos | ✅ v2-ben javítva |
| SEC-P3C-12 | 🟡 MEDIUM | featureFlags nem dokumentált | ✅ v2-ben dokumentálva |

---

## 13. Definition of Done (v2)

### Migration gate-ek
- [ ] `pnpm turbo build` 0 error, minden package buildelve
- [ ] `pnpm turbo typecheck` 0 TS error
- [ ] `pnpm turbo test` — 256 meglévő teszt + ≥ 35 új teszt zöld
- [ ] `madge --circular packages/` → 0 találat (körhivatkozás nincs)
- [ ] `madge --no-color packages/@spaceos/ui/src` → `api-client` nem szerepel
- [ ] `grep -r "api-client\|fetch\|axios" packages/@spaceos/ui/src` → 0
- [ ] CI codegen diff: `git diff --exit-code packages/@spaceos/domain/src/generated/` → 0

### Brand gate-ek
- [ ] `validateBrandTokens(doorstar)` — 0 exception (minden token valid)
- [ ] `validateHexColor('#B45309')` → pass; `validateHexColor('expression(alert(1))')` → throw
- [ ] `validateLogoPath('/brand/doorstar/logo.svg')` → pass; `validateLogoPath('../../etc/passwd')` → throw
- [ ] `resolveBrand('doorstar')` → Doorstar tokens; `resolveBrand('ismeretlen')` → JoineryTech (nem crash)
- [ ] JWT `brandSkin: 'doorstar'` (Orchestrator response) → Doorstar Dashboard render
- [ ] JWT `brandSkin: null` → JoineryTech default Dashboard
- [ ] `doorstar.ts` fájlban nincs `import { lazy }` — `grep -n "lazy" packages/@spaceos/brand-tokens/src/tokens/doorstar.ts` → 0
- [ ] Doorstar `showCncExport: true` → CNC gomb látható; `showAuditPage: false` → Audit menüpont rejtett

### i18n gate-ek
- [ ] `joinerytech.hu` → `detectLocale() === 'en'`
- [ ] `asztalostech.hu` → `detectLocale() === 'hu'` (hostname-only, nincs nginx dependency)
- [ ] `grep -rn "sub_filter" /etc/nginx` → 0 (eltávolítva)
- [ ] Minden UI string `useTranslation()` hook-on át — `grep -rn '"[A-ZÁÉÍÓÖŐÚÜŰ]' apps/joinerytech/src` → 0

### Nginx / Deploy gate-ek
- [ ] `https://joinerytech.hu` → HTTP 200, `X-SpaceOS-Brand: joinerytech`
- [ ] `https://asztalostech.hu` → HTTP 200, ugyanaz az `index.html` (nincs `spaceos-locale` meta tag)
- [ ] `https://joinerytech.hu/brand/doorstar/logo.svg` → HTTP 200 (nem 404)
- [ ] CSP header: `img-src 'self' /brand/ data:` — curl -I ellenőrzés
- [ ] Kernel migration 0024: `SELECT "BrandSkinId" FROM "Tenants"` → NULL (default, nem hiba)

### Biztonsági gate-ek
- [ ] `localStorage.setItem('brandSkin', 'doorstar')` → page reload után nincs hatás
- [ ] URL `?skin=doorstar` → nincs hatás
- [ ] `grep -rn "localStorage" packages/@spaceos/brand-tokens` → 0
- [ ] `grep -rn "jwtDecode\|atob" apps/joinerytech/src/store` → 0 (JWT nem dekódolva kliens oldalon)
- [ ] `grep -rn "featureFlags" apps/joinerytech/src` → csak UI visibility logika (nem `if (!featureFlags.x) return 403`)
- [ ] Turborepo: `turbo.json` `codegen` pipeline: `"cache": false`

---

## 14. Mi jön utána

| Fázis | Tartalom | Blokkoló feltétel |
|-------|----------|-------------------|
| **Phase 3C+ (~3 nap)** | `apps/design-portal` megrendelői Portal first scaffold; Deep skin support (navigationItems override) | Phase 3C kész |
| **Phase 3D — GDPR + Alerting** | PII pseudonymizáció; audit alerting | Phase 3B kész |
| **Horizon 2 — Escrow GA** | S3 Object Lock COMPLIANCE; RFC 3161 TSA; Escrow feature flag ON | Phase 3B kész |
| **Horizon 3 — Modules.Joinery** | CNC export; vágáslista; `Modules.Joinery.Door` Doorstar-ra | Phase 3C kész |

---

*SpaceOS · Phase 3C Architecture v2.0*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 20 finding beépítve (2C + 7H + 11M), minden döntés lezárva*
*`/senior-frontend` + `/senior-security` → v2 · 2026-04-07*
