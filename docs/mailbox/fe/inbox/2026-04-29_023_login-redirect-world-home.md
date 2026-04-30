---
id: MSG-FE-023
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-29
---

# FE-023 — Login redirect + dupla fejléc fix

> **3 bug egyszerre:**
> 1. Belépés után `/orders`-ra dob (legacy) — `/w`-re kellene
> 2. Dupla fejléc: a legacy DashboardPage saját header-e + WorldShell AppHeader
> 3. A `/` (root) a régi DashboardPage-t rendereli, nem a World Home-ot

## Fix

### 1. Login callback redirect: `/orders` → `/w`

**Fájl:** `src/auth/AuthProvider.tsx` (vagy `CallbackPage.tsx`)

A `onSigninCallback` vagy a `CallbackPage useEffect` navigate target:
```typescript
// VOLT:
navigate('/orders', { replace: true });
// KELL:
navigate('/w', { replace: true });
```

### 2. Root route `/` → redirect `/w`

**Fájl:** `src/App.tsx`

```typescript
// VOLT:
<Route path="/" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />

// KELL:
<Route path="/" element={<Navigate to="/w" replace />} />
```

A legacy `DashboardPage` törölhető vagy megtartható backup-nak — de a `/` route NEM renderelheti (dupla fejléc).

### 3. Legacy route-ok `/orders` → `/w/sales/orders` redirect

```typescript
<Route path="/orders" element={<Navigate to="/w/sales/orders" replace />} />
<Route path="/orders/:id" element={<Navigate to="/w/sales/orders/:id" replace />} />
<Route path="/profile" element={<Navigate to="/w/settings/tenant" replace />} />
```

## Definition of Done

- [ ] Login → `/w` (World Home, nem `/orders`)
- [ ] `/` → redirect `/w`
- [ ] Nincs dupla fejléc
- [ ] Legacy route-ok redirect-elnek a world route-okra
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 251 pass
- [ ] Outbox DONE
