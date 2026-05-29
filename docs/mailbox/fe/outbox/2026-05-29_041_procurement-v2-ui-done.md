---
id: MSG-FE-041-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-29
ref: MSG-FE-041
---

# FE-041 DONE — Procurement v2 UI (1a–1d)

## Elvégzett munkák

### Új fájlok

| Fájl | Leírás |
|---|---|
| `src/data/data-procurement-v2.ts` | V2 DTO-k (Requisition, Invoice, PriceList) + status map-ek + fallback mock adatok |
| `src/components/procurement/RequisitionPanel.tsx` | 1a: Igénylés lista + CreateRequisitionDrawer + RequisitionDetailSlideOver (FSM+SoD) |
| `src/components/procurement/InvoicePanel.tsx` | 1b+1c: Számla lista + RecordInvoiceDrawer + InvoiceDetailSlideOver (3WM, ApproveWithVariance, Vita) |
| `src/components/procurement/PriceListPanel.tsx` | 1d: Árlista kártyák (best-price kiemelés) + NewPriceListDrawer + PriceListDetailSlideOver |

### Módosított fájlok

- **`src/pages/ProcurementPage.tsx`** — 4 tab hozzáadva: Megrendelések | Igénylések | Számlák | Árlisták

### Tesztek

| Fájl | Tesztek |
|---|---|
| `src/components/procurement/__tests__/RequisitionPanel.test.tsx` | 10 test |
| `src/components/procurement/__tests__/InvoicePanel.test.tsx` | 10 test |
| `src/components/procurement/__tests__/PriceListPanel.test.tsx` | 10 test |

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 360 passed, 0 failed  (+30 új teszt, előtte 330)
```

## Funkcionális összefoglaló

### 1a. Igénylések
- Lista táblázat: reqNumber, materialCode, materialName, qty, benyújtó, dátum, státusz pill
- Létrehozás drawer: anyagkód+név, mennyiség/egység, preferált szállító (opcionális), megjegyzés, `POST /api/v2/requisitions`
- Részletek SlideOver: adatlap + FSM (Approve/Reject only for Draft), Reject inline textarea
- **SoD jelzés:** ha benyújtó = jelenlegi felhasználó → amber figyelmeztetés, akciók tiltva
- FSM: `Draft → Approved → Rejected / ConvertedToPO`

### 1b. Számlák + 1c. Three-Way Match
- Lista táblázat: számlaszám, szállító, PO hivatkozás, nettó, bruttó, dátum, státusz
- Rögzítés drawer: szállító + számlaszám + dátum + PO ref + tételsorok (anyag/db/egységár/ÁFA auto), Nettó/ÁFA/Bruttó összesítő
- Részletek SlideOver + **3WM táblázat**: PO db | Szállított db | Számlázott db | Match status pill (OK/Warning/Exception) + variancia %
- ApproveWithVariance gomb ha Exception found, Vita jelölése inline form
- **SoD tiltás:** variance jóváhagyó ≠ rögzítő
- FSM: `Received → Matched/Exception → Approved/Disputed`

### 1d. Árlisták
- Kártyás layout szállítónként: státusz pill, érvényességi időszak, tételsorok beágyazva
- **Best-price kiemelés:** azonos anyagkódon legolcsóbb ár → zöld szín + `★ Legjobb` badge
- Részletek SlideOver: tételek táblázat + Draft esetén Aktiválás FSM akció
- Új árlista drawer: szállító dropdown, érvényesség, tételek (anyagkód+név+ár+deviza), `POST /api/v2/pricelists` + `/entries`
- FSM: `Draft → Active → Expired`
