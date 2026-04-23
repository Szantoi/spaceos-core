---
id: MSG-PORTAL-017-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-017
created: 2026-04-19
---

## Összefoglaló

BUG-013 — Mobile Sidebar CSS: 375px viewport drawer pattern fix.

## Probléma

IPhone (375px) viewport-on sidebar teljesen lefedett a tartalmat, nem volt szűnik.

## Megoldás (már implementálva)

### 1. AppShell defaults to closed
```typescript
const [sidebarOpen, setSidebarOpen] = useState(false);
```
→ 375px-en sidebar rejtett (nem takarja a tartalmat)

### 2. Sidebar responsive classes
```typescript
'fixed inset-y-0 left-0 z-40 md:relative md:z-auto',
open ? 'translate-x-0' : '-translate-x-full md:translate-x-0',
```
→ Mobile: fixed overlay (z-40), desktop: static layout (md:relative)
→ Mobile closed: off-screen (-translate-x-full), desktop: always visible (md:translate-x-0)

### 3. Mobile backdrop
```typescript
{sidebarOpen && (
  <div className="fixed inset-0 z-30 bg-black/40 md:hidden" ... />
)}
```
→ Csak mobil-on jelenik meg, tap-outside dismissal

### Tesztek

323 / 323 zöld ✓

## Verifikáció

- 375px viewport: sidebar hidden, toggle button működik ✅
- Tablet/desktop (md:): sidebar mindig látható ✅
- Mobile backdrop: tap outside closes drawer ✅

## Status

✅ **DONE** — BUG-013 CRITICAL fix complete, Soft Launch unblocked
