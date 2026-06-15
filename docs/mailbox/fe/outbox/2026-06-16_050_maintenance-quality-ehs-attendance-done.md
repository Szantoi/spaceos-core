---
id: 2026-06-16_050_maintenance-quality-ehs-attendance-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-16_050_maintenance-quality-ehs-attendance.md
date: 2026-06-16
---

# DONE — FE-050 Karbantartás + Minőség + EHS + Jelenléti világ implementálása

## Commit

`51a55c8` — feat: FE-050 Karbantartás + Minőség + EHS + Jelenléti világ implementálása

## Elvégzett munka

### MaintenancePage.tsx (új fájl)
- `AssetStatusPill` — státusz badge (ok/warning/down/maintenance, color-coded)
- `TicketTypeBadge` — jegy típus badge (preventív/korrekció/hibaelhárítás)
- `AssetDetailSlideOver` — gép részletek: utolsó karbantartás, következő ütemezett, nyitott jegyek
- `TicketDetailSlideOver` — jegy részletek: típus, prioritás, eszköz, határidő, leírás
- `AssetList` — gép/eszköz lista státusz badge + modell + helyszín
- `TicketsList` — karbantartási jegyek listája típus + státusz badge-ekkel
- `ScheduleView` — ütemezett karbantartások naptár-sáv (következő 7 nap)
- `MaintenanceDashboard` — KPI: Aktív gépek / Figyelmeztetés / Leállt / Nyitott jegyek
- `MaintenanceWorldPage` — router: dash / assets / tickets / schedule
- Stone + amber akcent, ikon: `wrench`, route: `/w/maintenance`

### QualityPage.tsx (új fájl)
- `NcrStatusPill` — NCR státusz badge (open/under-review/closed/rejected)
- `NcrSeverityBadge` — súlyosság badge (critical/major/minor)
- `NcrDetailSlideOver` — NCR részletek: leírás, érintett termékek, javítási terv, státusz
- `NcrList` — NCR lista státusz + súlyosság badge-ekkel
- `TemplatesList` — ellenőrzőlista sablonok termék-típusonként
- `AuditLog` — utolsó 6 ellenőrzés eredménye (pass/fail/conditional)
- `QualityDashboard` — KPI: Nyitott NCR / Átlagos zárási idő / Pass rate / Aktív auditok
- `QualityWorldPage` — router: dash / ncr / templates / audits
- Stone + emerald akcent, ikon: `check`, route: `/w/quality`

### EhsPage.tsx (új fájl)
- `IncidentTypeBadge` — esemény típus badge (baleset/közel-baleset/környezeti)
- `SeverityPill` — súlyosság badge (high/medium/low)
- `IncidentStatusPill` — státusz badge (bejelentett/kivizsgálás/intézkedés/lezárt)
- `IncidentDetailSlideOver` — esemény részletek: leírás, érintett személyek, kivizsgálás státusza
- `RiskMatrix` — 3×3 kockázat-mátrix (valószínűség × hatás, color-coded cellák)
- `IncidentList` — esemény lista típus + súlyosság badge-ekkel
- `ActionsList` — aktív intézkedések lista (ki, mit, mikor, státusz)
- `EhsDashboard` — KPI: Esemény YTD / Nyitott intézkedés / Kockázat szint / Incident-free napok
- `EhsWorldPage` — router: dash / incidents / risks / actions
- Stone + rose akcent, ikon: `shield`, route: `/w/ehs`

### AttendancePage.tsx (új fájl)
- `AttendanceStatusPill` — jelenlét státusz badge (bent/késő/hiányzó/jóváhagyott/függőben)
- `EmployeeSlideOver` — dolgozó részletek: heti összesítő, kivételek listája
- `TodayShift` — mai műszak nézet: ki van bent, mikor érkezett, várható távozás
- `AttendanceTable` — jelenléti lista táblázat (dátum × dolgozó, be/ki időbélyeg, ledolgozott órák)
- `ExceptionsPanel` — kivételek panel: késések, hiányzások, túlórák
- `AttendanceDashboard` — KPI: Ma bent / Késő / Hiányzó / Átlag ledolgozott óra
- `AttendanceWorldPage` — router: dash / today / history / exceptions
- Stone + sky akcent, ikon: `calendar`, route: `/w/attendance`

### Mock adatok
- `src/mocks/maintenance.ts`: 6 gép (Holzma HPP380, Biesse Rover CNC stb.), 5 jegy, 4 ütemezett
- `src/mocks/quality.ts`: 5 NCR, 3 ellenőrzőlista sablon, 6 audit bejegyzés
- `src/mocks/ehs.ts`: 4 esemény, 6 kockázati tétel (3×3 mátrixhoz), 5 intézkedés
- `src/mocks/attendance.ts`: 8 dolgozó, mai jelenlét (5 bent/1 késő/1 hiányzó/1 függő), 5 nap history

### Navigáció + routing
- `worlds.ts`: maintenance/quality/ehs/attendance hozzáadva, WORLD_ORDER 15→19
- `Icon.tsx`: `shield` és `clock` ikonok hozzáadva
- `WorldShell.tsx`: `sky` akcent hozzáadva az ACCENT_MAP-be
- `types/index.ts`: WorldKey `| 'maintenance' | 'quality' | 'ehs' | 'attendance'`
- `HomeScreen.tsx`: Admin mind a 19 világ; Designer + maintenance/quality/ehs
- `App.tsx`: /w/maintenance, /w/quality, /w/ehs, /w/attendance (8 új route)
- `data.test.ts`: 15→19 world count

## Tesztek

| Fájl | Tesztek |
|---|---|
| `MaintenancePage.test.tsx` | 15 teszt — dashboard, assets, tickets, schedule, SlideOver-ek |
| `QualityPage.test.tsx` | 15 teszt — dashboard, NCR, templates, audit log, SlideOver |
| `EhsPage.test.tsx` | 15 teszt — dashboard, incidents, risk matrix, actions, SlideOver |
| `AttendancePage.test.tsx` | 15 teszt — dashboard, today shift, history table, exceptions, SlideOver |
| **Összesen** | **596 teszt — mind zöld** |

```
Test Files  63 passed (63)
Tests       596 passed (596)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 596/596 zöld
- [x] Karbantartás világ megjelenik a Home-on
- [x] Minőség világ megjelenik a Home-on
- [x] EHS világ megjelenik a Home-on
- [x] Jelenléti világ megjelenik a Home-on
- [x] Gép/eszköz lista renderel mock adattal (6 gép, státusz badge-ekkel)
- [x] Karbantartási jegyek listája típus + státusz badge-ekkel
- [x] Ütemezett karbantartások naptár-sáv
- [x] NCR lista státusz + súlyosság badge-ekkel + detail SlideOver
- [x] Ellenőrzőlista sablonok termék-típusonként
- [x] Audit napló (pass/fail/conditional)
- [x] EHS esemény lista típus + súlyosság badge-ekkel + detail SlideOver
- [x] Kockázat-mátrix 3×3 color-coded
- [x] Aktív intézkedések lista
- [x] Mai műszak nézet (be/ki időbélyeg)
- [x] Jelenléti táblázat 5 nap history
- [x] Kivételek panel (késések, hiányzások, túlórák)
