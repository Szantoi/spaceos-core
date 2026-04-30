---
id: MSG-PORTAL-010
from: root
to: portal
type: task
priority: high
status: READ
ref: MSG-TESTER-020
created: 2026-04-19
---

# PORTAL-010 — Inventory Bevételezés UI Refactor (TESTER-020)

## Helyzet

TESTER-020 report elemzése (BUG-003b funkcionális teszt) feltárt **3 kritikus UI/UX probléma** a Bevételezés (inbound movements) modalban.

**Backend fix** (`9b170a7`) már LIVE, de frontend UX zavarba ejti a felhasználókat.

---

## Három UI fejlesztés szükséges

### 1. **Anyag típus + Vastagság szeparáció**

**Aktuális:** 
```
Anyag típus: [MDF 18mm ▼]  (név + vastagság együtt)
Vastagság (mm): [22 ▼]     (redundáns!)
```

**Probléma:** Felhasználó nem érti, hogy miért van már vastagság az anyag névben, és akkor mi ez a mező? → konfúzió, UI zavar

**Javaslat:**
```
Anyag típus:  [MDF ▼]       (csak az anyag: MDF, Forgácslap, ABS él)
Vastagság:    [18mm ▼]      (külön: 18mm, 16mm, 22mm stb.)
```

**Implementáció:** 
- Backend már támogatja: materialType normalizáció (MDF18mm → MDF, vastagság külön)
- Frontend: dropdown split (Material enum + Thickness enum)
- POST payload ugyanaz marad

---

### 2. **Méretek: Hossz × Szélesség + Auto m² kalkuláció**

**Aktuális:**
```
Lapszám (db):     [10]
Terület (m²):     [5.78]    (kézi kitöltés!)
```

**Probléma:** Felhasználó nem tudja fejből a terülteteket; nincs auto-kalkuláció

**Javaslat:**
```
Hossz (mm):     [2500 ▼]  (standard: 800, 1000, 1200, 1500, 2000, 2500, 3000)
Szélesség (mm): [1000 ▼]  (standard: 300, 400, 500, 600, 750)
Lapszám (db):   [10]

→ Terület auto: [2.5 m²]   (= 10 lap × 2.5m × 1.0m)
```

**Implementáció:**
- Hossz/szélesség → onChange → `areaM2 = (hossz/1000) × (szélesség/1000) × lapszám` auto-fill
- Backend: `areaM2` validáció (ha manuálisan szerkesztik, max ±5% eltérés)

---

### 3. **Dátum validáció: jövő dátum nem engedett**

**Aktuális:** "2026-04-19" OK, "2026-12-31" is OK (jövő!) ❌

**Javaslat:**
```
<input type="date" max={new Date().toISOString().split('T')[0]} />
```

**Backend:** `occurredAt <= now()` validáció + hibaüzenet: *"Bevételezés dátuma nem lehet a jövőben"*

---

## Szükséges sub-taskok

- [ ] `Anyag típus + Vastagság dropdown split` — Frontend form refactor
- [ ] `Hossz × Szélesség standardok + Auto m²` — Constants + onChange handler
- [ ] `Dátum input max validáció` — Frontend + Backend
- [ ] **TESTER funkcionális teszt** (POST /bff/inventory/movements/inbound szabad) — MDF18mm, 10 lap, auto m², mai dátum

---

## Priority

**HIGH** — Doorstar Soft Launch UX kritikus (végfelhasználó-szintű zavar)

Szükséges: `/spaceos-portal` skill + PORTAL terminál (nincs kód írás, csak integráció)

---

Status: **UNREAD** — PORTAL terminál kezeli
