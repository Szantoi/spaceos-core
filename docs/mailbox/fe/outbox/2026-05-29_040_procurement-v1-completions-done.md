---
id: MSG-FE-040-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-29
ref: MSG-FE-040
---

# FE-040 DONE — Procurement v1 kiegészítések

## Elvégzett munkák

### Új fájlok

| Fájl | Leírás |
|---|---|
| `src/data/data-procurement.ts` | DTO-k, status map, mock generátorok (PODetail, SupplierDetail) |
| `src/components/procurement/PODetailSlideOver.tsx` | PO részletező (640px): FSM timeline, tételek táblázat, inline szállítás rögzítő form |
| `src/components/procurement/SupplierSlideOver.tsx` | Szállítói adatlap (500px): 3 KPI kártya, 7-hetes trend bars, kapcsolat, aktív rendelések |
| `src/components/procurement/NewPODrawer.tsx` | Új megrendelés (480px): szállító dropdown, anyag, mennyiség/egység, határidő, megjegyzés |

### Módosított fájlok

- **`src/pages/ProcurementPage.tsx`** — PO sorok és szállítók kattinthatók, "Új megrendelés" gomb aktív, `rawId` megőrzése a display transform-nál, 3 SlideOver bekötve

### Tesztek

| Fájl | Tesztek |
|---|---|
| `src/components/procurement/__tests__/PODetailSlideOver.test.tsx` | 9 test |
| `src/components/procurement/__tests__/SupplierSlideOver.test.tsx` | 8 test |
| `src/components/procurement/__tests__/NewPODrawer.test.tsx` | 9 test |

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 330 passed, 0 failed  (+26 új teszt, előtte 304)
```

## Funkcionális összefoglaló

- **PODetailSlideOver**: FSM timeline (Leadva→Visszaigazolva→Szállítás alatt→Megérkezett), tételsorok anyag+kód+db+ár+összesen, Nettó összesen tfoot, nyomkövetési szám ahol van, kapcsolat szekció, inline "Szállítás rögzítése" form per-tétel qty stepperekkel, hiány/többlet jelzéssel, `POST /procurement/api/deliveries`
- **SupplierSlideOver**: 3 KPI kártya (rating/megbízhatóság/lead time), 7-bar trend chart (amber ha <85%), kapcsolattartó adatok, aktív megrendelések listája státusz pill-el
- **NewPODrawer**: szállító dropdown (API + SUPPLIERS_FALLBACK), anyag szabad szöveg, menny+egység, határidő datepicker (min=holnap), megjegyzés textarea, teljes form validáció, `POST /procurement/api/orders` → sikeresen létrehozva esetén automatikusan megnyílik a PODetailSlideOver
- **Mock fallback**: minden SlideOver azonnal megjelenik mock adattal, API válasz esetén frissül
