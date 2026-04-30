---
id: MSG-FE-023-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-023
created: 2026-04-29
---

# FE-023 DONE — Login redirect + dupla fejléc + legacy route fix

## Commit

`84f00d7` — fix: FE-023 — Login redirect /w + legacy route redirects + dupla fejléc fix

---

## Definition of Done — teljesítés

- [x] **Login → `/w`** (World Home) ✅
- [x] **`/` → redirect `/w`** (nem DashboardPage, nincs dupla fejléc) ✅
- [x] **Nincs dupla fejléc** — DashboardPage import eltávolítva App.tsx-ből ✅
- [x] **Legacy route-ok redirect-elnek** ✅
- [x] **`pnpm build`** — 0 error ✅
- [x] **`pnpm test`** — **251/251 pass** ✅

---

## Változások

### `src/pages/CallbackPage.tsx`
```typescript
// VOLT:
navigate('/orders', { replace: true });
// LETT:
navigate('/w', { replace: true });
```

### `src/App.tsx`
- `/` route: `DashboardPage` → `<Navigate to="/w" replace />`
- Legacy redirects (dinamikus helper komponensekkel):
  - `/orders` → `/w/sales/orders`
  - `/orders/:id` → `/w/sales/orders/:id` (RedirectOrderDetail)
  - `/orders/:id/cutting-list` → `/w/sales/orders/:id/cutting-list` (RedirectCuttingList)
  - `/profile` → `/w/settings/tenant`
- `DashboardPage` + `ProfilePage` unused import eltávolítva

### `src/pages/NotFoundPage.tsx`
- "Vissza" link: `to="/orders"` → `to="/w"`

### `src/test/NotFoundPage.test.tsx`
- Teszt frissítve: `href="/orders"` → `href="/w"`

---

## Tesztek

**251/251 pass** ✅ — összes meglévő teszt átment.

---

## Security review

- Nincs auth state változás ✅
- Redirectek server-side-t nem érintenek ✅
- sourcemap: false ✅

---

## Kockázatok / kérdések

Nincsenek.
