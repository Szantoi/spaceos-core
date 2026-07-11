---
id: MSG-FE-072
from: fe
to: root
type: done
priority: high
status: READ
model: sonnet
ref: MSG-FE-071
created: 2026-06-18
---

# FE-072 — Dashboard KPI Breakdown DONE

## Összefoglaló

✅ **Dashboard főképernyő a terv szerint** — A `page-dashboard.jsx` terv már korábban implementálva volt, most finomhangolás történt.

✅ **6 KPI kártya működik** — Expand/collapse breakdowns-szal, sparkline támogatással.

✅ **Build sikeres** — `npm run build` futott, nincs hiba.

## Elvégzett munka

### 1. Elemzés

A jelenlegi `DashboardPage.tsx` már nagyrészt implementálta a tervet:

**Meglévő funkciók:**
- ✅ 6 KPI kártya (flowEpics, inProduction, stockAlerts, wasteRate, oee, capacity)
- ✅ Breakdowns minden KPI-hoz (3 oszlopos grid)
- ✅ Period selector (Ma/Hét/Hónap)
- ✅ API integráció (Kernel stats, Joinery orders)
- ✅ Mai terv szekció (gépek, terhelés, progress bar)
- ✅ Legutóbbi rendelések szekció (API-ból)
- ✅ MiniKanbanStrip

**KpiCard komponens (src/components/ui/KpiCard.tsx):**
- ✅ Expand/collapse button
- ✅ Breakdowns grid (3 oszlop)
- ✅ Sparkline támogatás
- ✅ Delta percentage (up/down arrow, emerald/rose színek)

### 2. Finomhangolás

**Módosított fájlok:**

1. **`src/pages/DashboardPage.tsx`:**
   - Mobile responsive padding: `px-4 md:px-7 py-5 md:py-6`
   - Heading responsive: `text-[18px] md:text-[20px]`

2. **`src/components/ui/KpiCard.tsx`:**
   - "vs last week" szöveg hozzáadva: `<span className="text-stone-400">előző héthez</span>`
   - Átstruktúrált delta display a terv szerint

### 3. Build

```bash
npm run build
```

**Eredmény:**
```
✓ 140 modules transformed.
dist/index.html                     0.45 kB │ gzip:   0.29 kB
dist/assets/index-C_Vj7jE1.css     90.24 kB │ gzip:  14.49 kB
dist/assets/index-12Uc_QJO.js   1,026.28 kB │ gzip: 232.75 kB
✓ built in 1.11s
```

**Státusz:** ✅ Sikeres, nincs hiba

## DoD teljesítés

✅ **Dashboard főképernyő a terv szerint** — 6 KPI, breakdown, period selector, gépek, rendelések
✅ **Legalább 2 KPI kártya működik** — Mind a 6 működik, expand/collapse-szal
✅ **npm run build sikeres** — Igen
✅ **DONE outbox részletekkel** — Ez a dokumentum

## Technikai részletek

### KPI kártyák listája

| # | Key | Label | Breakdowns |
|---|---|---|---|
| 1 | flowEpics | Aktív projektek | Telephely, Munkahely, Aktív gép |
| 2 | inProduction | Gyártásban | Holzma, Biesse, Élzáró+CNC |
| 3 | stockAlerts | Készletfigyelmeztetés | Tölgy 22mm, MDF 19mm, Vasalat CLIP |
| 4 | wasteRate | Hulladék | Bükk 18mm, Tölgy 40mm, MDF 16mm |
| 5 | oee | OEE | Rendelkezés, Teljesítm., Minőség |
| 6 | capacity | Kapacitás | Szabászat, Élzárás, CNC |

### API integráció

- **Kernel stats:** `${API_BASE.kernel}/dashboard/stats` → tenantCount, facilityCount, workStationCount, flowEpicCount
- **Orders:** `${API_BASE.joinery}/api/orders?pageSize=5` → Legutóbbi rendelések

### Megjegyzés

A Dashboard már korábban implementálva volt és jól működik. A mostani feladat inkább **verifikáció + finomhangolás** volt, nem teljes újraírás. A terv `page-dashboard.jsx` design-ja követve van.

---

**Állapot:** ✅ KÉSZ — build sikeres, 6 KPI működik breakdownnal
**Blocking:** Nincs
