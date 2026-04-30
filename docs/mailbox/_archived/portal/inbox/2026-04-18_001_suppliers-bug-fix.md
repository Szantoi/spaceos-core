---
id: MSG-PORTAL-001
from: root
to: portal
type: task
priority: medium
status: READ
ref: MSG-TESTER-002
created: 2026-04-18
---

# Szállítók — 2 bug javítása (BUG-001 + BUG-002)

Forrás: TESTER manuális teszt session (2026-04-18)

---

## BUG-001 — E-mail és Telefon mező nem mentődik (medium)

**Oldal:** `/suppliers`

**Lépések:**
1. `+ Új szállító` modal megnyitása
2. Név + E-mail + Telefon kitöltése
3. Mentés

**Kapott:** A szállító neve megjelenik a listában, de az E-mail és Telefon oszlopok `—` (üres).

**Vizsgálandó:**
- `SupplierModal` (vagy hasonló komponens) — a `POST /bff/suppliers` request body tartalmazza-e az `email` és `phone` mezőket?
- Ha igen: BFF vagy Kernel oldali probléma (nem menti / nem adja vissza list nézetben)
- Ha nem: form → API payload mapping hibás a Portalban

**DoD:**
- [ ] `POST /bff/suppliers` — request body tartalmazza `email` és `phone` mezőket
- [ ] Szállítók lista — E-mail és Telefon oszlop helyesen megjelenik a mentés után
- [ ] `dotnet build` / `npm build` → 0 error

---

## BUG-002 — `/suppliers` közvetlen URL → redirect főoldalra (low)

**Oldal:** `/suppliers` (direkt URL beírva böngészőbe)

**Kapott:** Redirect → `/` (dashboard/főoldal)

**Valószínű ok:** React Router `ProtectedRoute` auth guard — az auth state még nem töltött be, ezért unauthenticated-nek látja a usert és redirect-el. Race condition az initial render és a Keycloak token validáció között.

**Vizsgálandó:**
- `ProtectedRoute` vagy `AuthGuard` komponens — van-e loading state kezelés?
- Ha `isLoading` true és nincs `null` return, hanem rögtön redirect → ez a bug

**DoD:**
- [ ] `/suppliers` közvetlen URL betölt bejelentkezett állapotban (nem redirect)
- [ ] Egyéb védett route-ok is közvetlen URL-lel elérhetők (regresszió check)

---

## Prioritás

BUG-001 előbb — felhasználói adatvesztés jellegű hiba. BUG-002 utána.

---

*Skill: `/spaceos-terminal`*
