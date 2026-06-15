---
id: MSG-FE2-002
from: root
to: fe2
type: task
priority: high
status: UNREAD
ref: MSG-FE2-001
created: 2026-06-16
---

# FE2-002 — Raktár (bővített) + Reklamáció/Szerviz világ

## Kontextus

FE2-001 (HR + Kontrolling) fut — ezt a feladatot az FE2-001 DONE után kezdd el.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett

## Prototípus fájlok

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-warehouse-2.jsx    — Raktár bővített: lot-kezelés, zónák, mozgások
  data-warehouse.js       — Raktár store (lot, zone, movement mock adatok)
  page-service.jsx        — Szerviz/Reklamáció: jegyek, ütemezés, garanciák
  page-service-2.jsx      — Service SlideOver-ek: jegy detail, látogatás form
  data-service.js         — Service store (tickets, visits, warranties)
```

## Amit implementálni kell

### Raktár bővített világ (`WarehousePage.tsx` — BŐVÍTÉS)

A meglévő `WarehousePage.tsx`-t **egészítsd ki** (ne töröld):
- Lot-kezelés tab: lot lista (LOT-ID, anyag, mennyiség, lejárat, zóna)
- Zóna-térkép panel: zónák listája kapacitás sávokkal
- Mozgások napló: BE/KI mozgások táblázata (dátum, lot, zóna, mennyiség, ok)
- Új KPI kártyák: Aktív lot-ok / Kritikus készlet / Lejáró 30 napon belül

### Reklamáció/Szerviz világ (`ServicePage.tsx`)
- Jegy lista (státusz: open/in-progress/resolved/closed, prioritás: high/medium/low)
- Garanciák panel: aktív garanciák (ügyfél, termék, lejárat, fedezeti típus)
- Látogatás ütemező: közelgő szerviz látogatások naptár nézettel (heti rács)
- Dashboard KPI: Nyitott jegyek / SLA-ban / Garanciás / Mai látogatás
- Stone + orange akcent, ikon: `wrench`
- Router: `/w/service`
- SlideOver: JegyDetail (leírás, fotók placeholder, státusz FSM, látogatás rögzítés)

## Mock adatok
- `src/mocks/warehouse.ts` — bővítsd: 8 lot, 4 zóna, 12 mozgás (`data-warehouse.js` alapján)
- `src/mocks/service.ts` — 6 jegy, 4 garancia, 5 látogatás (`data-service.js` alapján)

## Tesztek
- `WarehousePage.test.tsx` — bővítsd a meglévő teszteket (lot, zóna, mozgások)
- `ServicePage.test.tsx` — új tesztek
- `pnpm test` zöld

## DONE feltételek
- [ ] Raktár bővítve (lot + zóna + mozgások tab)
- [ ] Szerviz világ megjelenik a Home-on
- [ ] `pnpm build` + `pnpm test` zöld

## Koordináció
Ne módosítsd: FE-A fájljait (MasterdataPage, TradePage, InteriorPage)
