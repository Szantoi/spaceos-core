---
id: MSG-FE-050
from: root
to: fe
type: task
priority: high
status: UNREAD
ref: MSG-FE-049
created: 2026-06-16
---

# FE-050 — Karbantartás + Minőség + EHS + Jelenléti világ

## Kontextus

FE-049 (Törzsadatok+Kereskedelem+Belső tér) elfogadva ✅ — 539 teszt, commit `4dea708`. Folytatás — ez a sprint visz 15→19 világra.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett

## Prototípus fájlok

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-maintenance.jsx    — Karbantartás: gépek, hibajegyek, ütemezés
  page-maintenance-2.jsx  — Karbantartás detail SlideOver-ek
  data-maintenance.js     — Maintenance store (mock)
  page-quality.jsx        — Minőség-ellenőrzés: ellenőrzőlisták, NCR-ek
  data-quality.js         — Quality store (mock)
  page-ehs.jsx            — EHS: munkabiztonsági események, kockázatok
  page-ehs-2.jsx          — EHS detail nézetek
  data-ehs.js             — EHS store (mock)
  page-attendance.jsx     — Jelenléti rendszer: be/kilépések, műszakok
  data-attendance.js      — Attendance store (mock)
```

## Amit implementálni kell

### Karbantartás világ (`MaintenancePage.tsx`)
- Gép/eszköz lista (státusz: ok/warning/down/maintenance)
- Karbantartási jegy lista (típus: preventív/korrekció/hibaelhárítás)
- Ütemezett karbantartások naptár-sáv (következő 7 nap)
- Dashboard KPI: Aktív gépek / Figyelmeztetés / Leállt / Nyitott jegyek
- Stone + yellow akcent, ikon: `wrench`
- Router: `/w/maintenance`
- SlideOver: GépDetail (utolsó karbantartás, következő ütemezett, nyitott jegyek) + JegyDetail

### Minőség világ (`QualityPage.tsx`)
- NCR (Non-Conformance Report) lista (státusz: open/under-review/closed/rejected)
- Ellenőrzőlista sablonok (termék-típusonként)
- Audit napló (utolsó 10 ellenőrzés eredménye)
- Dashboard KPI: Nyitott NCR / Átlagos zárási idő / Pass rate / Aktív auditok
- Stone + green akcent, ikon: `check`
- Router: `/w/quality`
- SlideOver: NCRDetail (leírás, érintett termékek, javítási terv, státusz FSM)

### EHS világ (`EhsPage.tsx`)
- Esemény lista (típus: baleset/közel-baleset/veszélyhelyzet, súlyosság: high/medium/low)
- Kockázat-mátrix: 3×3 táblázat (valószínűség × hatás, color-coded)
- Aktív intézkedések lista (ki, mit, mikor)
- Dashboard KPI: Esemény YTD / Nyitott intézkedés / Kockázat szint / Incident-free napok
- Stone + red akcent, ikon: `shield`
- Router: `/w/ehs`
- SlideOver: EseményDetail (leírás, érintett személyek, kivizsgálás státusza)

### Jelenléti világ (`AttendancePage.tsx`)
- Mai műszak nézet: ki van bent, mikor érkezett, várható távozás
- Jelenléti lista táblázat (dátum × dolgozó, be/ki időbélyeg, ledolgozott órák)
- Kivételek panel: késések, hiányzások, túlórák
- Dashboard KPI: Ma bent / Késő / Hiányzó / Átlag ledolgozott óra
- Stone + blue akcent, ikon: `calendar`
- Router: `/w/attendance`
- SlideOver: DolgozóJelenléti (heti összesítő, kivételek listája)

## Mock adatok
- `src/mocks/maintenance.ts` — 6 gép, 5 jegy, 4 ütemezett (`data-maintenance.js` alapján)
- `src/mocks/quality.ts` — 5 NCR, 3 sablon, 6 audit bejegyzés (`data-quality.js` alapján)
- `src/mocks/ehs.ts` — 4 esemény, 6 kockázat, 5 intézkedés (`data-ehs.js` alapján)
- `src/mocks/attendance.ts` — 8 dolgozó mai jelenléte, 5 nap history (`data-attendance.js` alapján)

## Tesztek
- `MaintenancePage.test.tsx`, `QualityPage.test.tsx`, `EhsPage.test.tsx`, `AttendancePage.test.tsx`
- `pnpm test` 539 → ~595 (+14/világ minimum)

## DONE feltételek
- [ ] 4 új világ a Home-on
- [ ] Minden lista renderel mock adattal
- [ ] `pnpm build` + `pnpm test` zöld

## Koordináció
Ne módosítsd: FE-B fájljait (HrPage, ControllingPage, WarehousePage bővített, ServicePage)
