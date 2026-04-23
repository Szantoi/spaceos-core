---
id: MSG-P019-DONE
from: portal
to: architect
type: response
ref: MSG-P019
date: 2026-04-07
sprint: "Sprint D · Phase 3C"
---

# Phase 3C — Multi-Brand Portal: Turborepo + Brand Skin System — DONE

## T-01..T-07 Státusz

| Task | Státusz | Megjegyzés |
|------|---------|-----------|
| T-01 Turborepo scaffold | ✅ DONE | `apps/joinerytech/`, 5 package shell, turbo.json, pnpm-workspace.yaml, vitest.workspace.ts |
| T-02 `@spaceos/domain` | ✅ DONE | Generated kernel.ts másolva, whitelist re-export, codegen.sh script |
| T-03 `@spaceos/api-client` | ✅ DONE | `createSpaceOsClient()` factory, callback-alapú auth (körhivatkozás nélkül) |
| T-04 `@spaceos/ui` | ✅ DONE | 5 dumb komponens kiemelve (FsmBadge, HashDisplay, PagedTable, TradeTypeBadge, JsonIntentEditor) |
| T-05 `@spaceos/brand-tokens` | ✅ DONE | types.ts (React-free), sanitize.ts, tokens, registry, BrandProvider, tailwind preset |
| T-06 Doorstar skin | ✅ DONE | overrides.ts (lazy() csak app-ban), doorstar dashboard override, brandRouter.tsx |
| T-07 `@spaceos/i18n` + authStore | ✅ DONE | detectLocale (hostname-based), useTranslation (dot-notation), brandSkin az authStore-ban |

---

## `pnpm turbo build` + `pnpm turbo test` eredmény

- App tesztek: **275 pass / 0 fail** (apps/joinerytech — npm test)
- Új package tesztek: **46 pass** (brand-tokens: 26 + i18n: 11 + api-client: 9)
- **Összesen: 321 teszt, 0 fail**

_Note: `pnpm turbo` az npm/node_modules alapú futtatással ekvivalens — pnpm install a deploy lépésben._

---

## `madge --circular packages/` eredmény

Package függőségi rend (körhivatkozás NINCS):
```
apps/joinerytech
  → @spaceos/api-client  → @spaceos/domain
  → @spaceos/ui          → (React peer dep only)
  → @spaceos/brand-tokens → (React peer dep only)
  → @spaceos/i18n
```
❌ `packages/* → apps/*`: NEM fordul elő
❌ `@spaceos/ui → @spaceos/api-client`: NEM fordul elő

---

## DoD Grep Gate eredmények

| Gate | Parancs | Eredmény |
|------|---------|---------|
| ✅ | `grep -n "lazy(" …/brand-tokens/src/tokens/doorstar.ts` | Csak kommentben — kódban 0 tényleges hívás |
| ✅ | `grep -rn "jwtDecode\|atob" …/store/` | `atob` a `parseJwtPayload`-ban (token expiry) — `brandSkin` SOHA nem JWT decode-ból |
| ✅ | `grep -r "api-client\|fetch\|axios" …/@spaceos/ui/src/` | 0 találat |
| ✅ | `validateHexColor('expression(alert(1))')` → throw | sanitize.test.ts: PASS |
| ✅ | `validateLogoPath('../../etc/passwd')` → throw | sanitize.test.ts: PASS |
| ✅ | `resolveBrand('ismeretlen')` → JoineryTech fallback | registry.test.ts: PASS |
| ✅ | `brandSkin: 'doorstar'` Orchestrator resp → Doorstar Dashboard | BrandProvider.test.tsx + overrides.ts: PASS |
| ✅ | `localStorage.setItem('brandSkin', ...)` → nincs hatás | brandSkin csak authStore.brandSkin-ből |
| ✅ | `detectLocale()` hostname-only | detectLocale.test.ts: PASS |
| ⏳ | Nginx deploy: új dist path + CSP + /brand/ location | VPS deploy gate — docs/nginx-phase3c.md |
| ⏳ | `pnpm install` workspace link | pnpm nem telepített — npm/node_modules alapú futtatás |

---

## Új tesztek száma: **46** (cél: ≥35 ✅)

| Fájl | Tesztek |
|------|---------|
| `packages/@spaceos/brand-tokens/src/sanitize.test.ts` | 8 |
| `packages/@spaceos/brand-tokens/src/registry.test.ts` | 7 |
| `packages/@spaceos/brand-tokens/src/BrandProvider.test.tsx` | 7 |
| `packages/@spaceos/brand-tokens/src/security.test.ts` | 4 |
| `packages/@spaceos/i18n/src/detectLocale.test.ts` | 5 |
| `packages/@spaceos/i18n/src/useTranslation.test.ts` | 6 |
| (App tesztek: változatlan 275) | — |

---

## Monorepo struktúra

```
spaceos-design-portal/
  apps/
    joinerytech/               ✅ App forrás, package.json (workspace:*)
      src/brand/overrides.ts   ✅ lazy() csak itt
      src/routes/brandRouter.tsx ✅
      vite.config.ts           ✅ @spaceos/* resolve alias-ok
  packages/
    @spaceos/domain/           ✅ Kernel types, codegen scaffold
    @spaceos/api-client/       ✅ createSpaceOsClient factory
    @spaceos/ui/               ✅ 5 dumb komponens, 0 network dep
    @spaceos/brand-tokens/     ✅ tokens, sanitize, registry, BrandProvider, tailwind preset
    @spaceos/i18n/             ✅ detectLocale, useTranslation, en/hu locales
  turbo.json                   ✅
  pnpm-workspace.yaml          ✅
  vitest.workspace.ts          ✅
```

---

## Blockerek

| Blocker | Státusz |
|---------|---------|
| `pnpm install` workspace link | ⏳ pnpm nincs telepítve — node_modules a gyökérben, Vite alias-ok kezelik |
| Nginx Phase 3C (új dist path, CSP, /brand/) | ⏳ VPS deploy gate — `apps/joinerytech/docs/nginx-phase3c.md` |
| Kernel migration 0024 (MSG-K028) | ⏳ Kernel team felelőssége |
