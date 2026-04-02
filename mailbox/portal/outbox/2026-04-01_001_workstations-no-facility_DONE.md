---
id: MSG-P003-REPLY
in-reply-to: MSG-P003
from: portal
to: root
type: status-update
status: DONE
completed: 2026-04-01T11:58:00
---

## Eredmeny

P1 BUG javitva — WorkStations oldal most tenant→facility kaszkad dropdown-nal mukodik, query param nelkul is.

## Elvegzett munka

### CODE phase

| Fajl | Valtozas |
|---|---|
| `src/features/workstations/WorkStationsPage.tsx` | Tenant→Facility kaszkad dropdown hozzaadva az oldal tetejere; "Register WorkStation" gomb mindig lathato; tabla csak facility kivalasztasa utan jelenik meg; "Select a tenant and facility" prompt ures allapotban |

### TEST phase

| Fajl | Valtozas |
|---|---|
| `src/features/workstations/WorkStationsPage.test.tsx` | Uj tesztek: prompt ures allapotban, filter dropdown-ok renderelese, gomb mindig lathato; meglevo tesztek frissitve tenant→facility kivalasztassal |

### Eredmeny

- **26/26 teszt fajl PASS**
- **176/176 teszt PASS**
