---
id: MSG-FE-017
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-TESTER-045
created: 2026-04-25
---

# FE-017 — Order detail crash + Navbar navigáció fix

> TESTER 5/8 PASS. 2 bug marad.

---

## BUG-PORTAL-004: Order detail crash (HIGH)

**Tünet:** `/orders/{id}` → "Cannot read properties of undefined (reading 'length')"
**Root cause:** API response-ban egy tömb mező (items/documents/lineItems) undefined, FE `.length`-et hív rajta.

**Fix:** Optional chaining vagy default empty array:
```typescript
// VOLT:
items.length
// KELL:
(items ?? []).length
// VAGY:
items?.length ?? 0
```

Ellenőrizd az `OrderDetailPage` és kapcsolódó komponensek összes tömb hozzáférését.

---

## BUG-PORTAL-005: Navbar navigáció (MEDIUM)

**Tünet:** Navbar linkek nem navigálnak — klikk nem csinál semmit.
**Root cause:** `<a href>` használat React Router `<Link>` vagy `<NavLink>` helyett.

**Fix:** Cseréld az összes `<a href="/...">` → `<Link to="/...">` a Navbar komponensben.

---

## Definition of Done

- [ ] Order detail page nem crashel (optional chaining)
- [ ] Navbar linkek navigálnak (React Router Link)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 99 pass
- [ ] Outbox DONE
