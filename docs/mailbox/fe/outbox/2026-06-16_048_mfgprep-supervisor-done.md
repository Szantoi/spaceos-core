---
id: 2026-06-16_048_mfgprep-supervisor-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-15_048_mfgprep-supervisor.md
date: 2026-06-16
---

# DONE — FE-048 Gyártás-előkészítés (MfgPrep) + Supervisor világ implementálása

## Commit

`e86bede` — feat: FE-048 Gyártás-előkészítés (MfgPrep) + Supervisor világ implementálása

## Elvégzett munka

### MfgPrepPage.tsx (új fájl)
- `ReleaseStatusPill` — státusz badge (pending/approved/in_production/ready/blocked)
- `PriorityBadge` — prioritás badge (Sürgős/Közepes/Alacsony)
- `ReleaseDetailSlideOver` — release részletek: státusz + prioritás + határidő, ellenőrzőlista progress-sávval, anyagjegyzet badge, jóváhagyás gomb (pending állapotnál)
- `DatasheetSlideOver` — munkalap részletek: státusz, dátumok, műveletek chip-ek, megjegyzés
- `ReleaseQueue` — release queue lista státusz szűrő chip-ekkel (6 filter), koppintással SlideOver
- `DatasheetList` — munkalapok listája, státusz badge, koppintással SlideOver
- `MfgPrepDashboard` — KPI kártyák (kiadásra vár/gyártásban/kész/blokkolt), sürgős panel, munkalapok panel
- `MfgPrepWorldPage` — router: dash / queue / datasheets screen-ek

### SupervisorPage.tsx (új fájl)
- `WsStatePill` — munkaállomás státusz badge (idle/working/blocked/break)
- `AlertIcon` — alert típus ikon (blocked/late/material/quality)
- `WorkstationDetailSlideOver` — állomás részletek: operator, aktuális feladat, blokkolt ok (rose), kihasználtsági progress sáv (emerald/amber/rose a %-tól függően)
- `DayPlanSlideOver` — napi terv összes tétele státusz badge-ekkel (Kész/Folyamatban/Blokkolt/Késő)
- `FloorView` — műhely-floor: minden állomás kártya kihasználtság sávval
- `DayPlanPage` — napi terv áttekintés: összesítő progress sáv, tételek terv vs. tény
- `SupervisorDashboard` — KPI kártyák (dolgozik/blokkolt/napi terv/riasztás), alert panel (high=rose/medium=amber), workstation mini-kártyák
- `SupervisorWorldPage` — router: dash / floor / dayplan screen-ek

### Mock adatok
- `mfgprep.ts`: 5 release item (in_production/approved/ready/pending/blocked), 4 datasheet (active/draft/completed/on_hold), RELEASE_STATUS_META, DATASHEET_STATUS_META
- `supervisor.ts`: 6 workstation (working×2/blocked×1/break×1/idle×1), 8 day plan item, 4 alert (2 high/2 medium), WS_STATE_META

### Navigáció + routing
- `worlds.ts`: mfgprep (orange) + supervisor (rose) világ, WORLD_ORDER 10→12
- `Icon.tsx`: clipboard, eye ikonok
- `WorldShell.tsx`: orange + rose accent hozzáadva
- `types/index.ts`: WorldKey `| 'mfgprep' | 'supervisor'`
- `HomeScreen.tsx`: Admin role-hoz mfgprep + supervisor világ
- `App.tsx`: /w/mfgprep, /w/mfgprep/:screen, /w/supervisor, /w/supervisor/:screen

## Tesztek

| Fájl | Tesztek |
|---|---|
| `MfgPrepPage.test.tsx` | 17 teszt — dashboard, queue szűrők, SlideOver-ek, datasheets |
| `SupervisorPage.test.tsx` | 17 teszt — dashboard, alert panel, workstations, floor, dayplan |
| **Összesen** | **489 teszt — mind zöld** |

```
Test Files  56 passed (56)
Tests       489 passed (489)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 489/489 zöld
- [x] MfgPrep + Supervisor világ megjelenik a Home-on (Admin role)
- [x] Release queue renderel mock adattal (5 item, 6 státusz szűrő)
- [x] Workstation lista renderel státuszokkal (6 állomás, kihasználtság sávok)
- [x] ReleaseDetailSlideOver: ellenőrzőlista, jóváhagyás gomb
- [x] WorkstationDetailSlideOver: operator, feladat, blokkoló ok, utilization
- [x] Alert panel: high/medium severity színkódolással
- [x] Napi terv: terv vs. tény, progress sáv, késő tételek jelölve
