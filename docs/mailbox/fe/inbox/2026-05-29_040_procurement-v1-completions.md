---
id: MSG-FE-040
from: root
to: fe
type: task
priority: high
status: READ
ref: FE_Design_Requirements_2026
created: 2026-05-29
---

# FE-040 — Procurement v1 kiegészítések (PO detail + Delivery + Supplier SlideOver + New PO)

Design sprint lezárult. A Procurement v1 mock-only területek design-kész, implementálható.

**Skill:** `/spaceos-terminal`  
**Backend állapot:** Procurement API él (port 5006), v1 endpointok aktívak

---

## Scope

### 1. PO detail SlideOver (kattints bármely PO sorra)

- FSM timeline: `Leadva → Visszaigazolva → Szállítás alatt → Megérkezett`
- Tételsorok táblázat: anyag + kód, db, egységár, érték, nettó összesen
- Nyomkövetési szám (ahol van)
- Visszaigazolás / szállítás dátuma
- Kontakt info + megjegyzés
- "Szállítás rögzítése" gomb (ha nem Delivered státusz)

**API:** `GET /procurement/api/orders/{id}` → PO detail

### 2. Szállítás rögzítő drawer (PO SlideOver-ből nyílik)

- Per-tétel mennyiség stepper (−/érték/+ gombok)
- Hiány / többlet figyelmeztetés ha eltér a rendelt mennyiségtől
- Szállítási dátum picker + megjegyzés
- Submit → `POST /procurement/api/deliveries`
- Sikeres → PO státusz frissítés + toast

**API:** `POST /procurement/api/deliveries`

### 3. Szállítói adatlap SlideOver (kattints bármely szállítóra)

- 3 KPI kártya: rating, megbízhatóság %, átlag lead time
- 7-hetes megbízhatóság trend (bar chart)
- Kapcsolattartó neve, e-mail, telefon
- Aktív megrendelések listája státusz pillel
- Utolsó rendelés dátuma

**API:** `GET /procurement/api/suppliers` (meglévő), supplier detail mezők kell

### 4. Új PO drawer (+ gomb a ProcurementPage-en)

- Szállító dropdown (`GET /procurement/api/suppliers` → lista)
- Anyag autocomplete (szabad szöveg)
- Mennyiség + egység
- Határidő datepicker
- Megjegyzés
- Submit → `POST /procurement/api/orders`

**API:** `POST /procurement/api/orders`

---

## Komponens struktúra (javaslat)

```
ProcurementPage
├── PODetailSlideOver (meglévő mock → real)
│   └── DeliveryRecordDrawer (új, SlideOver-ből nyílik)
├── SupplierSlideOver (új)
└── NewPODrawer (új, + gomb)
```

---

## Mock adatok eltávolítása

A `ProcurementPage`-en jelenleg:
- `ACTIVE_PO` mock → `GET /procurement/api/orders` (már bekötve?) vagy PO detail
- Supplier lista mock → `GET /procurement/api/suppliers` (bekötve)

Ellenőrizd, hogy a lista már API-ból jön-e, és ha igen, csak a SlideOver/Drawer részek hiányoznak.

---

## Tesztek

- Új komponensek unit tesztje (legalább smoke)
- Happy path: PO detail megjelenik, delivery rögzítve, toast látható
- `pnpm test` zöld

---

## Kimenet

Outbox: `MSG-FE-040-DONE` — teszt count + rövid összefoglaló
