---
id: MSG-FE-049
from: root
to: fe
type: task
priority: high
status: UNREAD
ref: MSG-FE-048
created: 2026-06-16
---

# FE-049 — Törzsadatok + Kereskedelem + Belső tér világ

## Kontextus

FE-048 (MfgPrep + Supervisor) elfogadva ✅ — 489 teszt, commit `e86bede`. Folytatás.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett

## Prototípus fájlok

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-masterdata.jsx     — Törzsadatok: termékek, anyagok, szállítók, árlista
  page-trade.jsx          — Kereskedelem: ajánlatok, megrendelések, partnerek
  page-trade-2.jsx        — Trade SlideOver-ek: ajánlat detail, PO detail
  data-trade.js           — Trade store (mock adatok)
  page-interior.jsx       — Belső tér konfigurátor (bútor-elrendezés)
  page-interior-2.jsx     — Interior detail nézetek
  page-interior-3.jsx     — Interior 3D/2D preview panel
```

## Amit implementálni kell

### Törzsadatok világ (`MasterdataPage.tsx`)
- Termék-törzs lista (kód, megnevezés, kategória, ár, egység)
- Anyag-törzs (lapanyag, vasalat, festék — típus badge-ekkel)
- Szállító-lista (név, fizetési feltétel, leadtime)
- Dashboard KPI: Aktív termékek / Anyag cikkszámok / Aktív szállítók
- Stone + slate akcent, ikon: `database`
- Router: `/w/masterdata`

### Kereskedelem világ (`TradePage.tsx`)
- Árajánlat lista (státusz: draft/sent/accepted/rejected)
- Vevői megrendelés lista (PO státusz pill-ekkel)
- Partner lista (vevők + szállítók, forgalom összesítővel)
- Dashboard KPI: Nyitott ajánlatok / Aktív PO / Havi forgalom
- Stone + teal akcent, ikon: `briefcase`
- Router: `/w/trade`
- SlideOver: AjánlatDetail (tételek, összeg, státusz FSM) + PODetail

### Belső tér világ (`InteriorPage.tsx`)
- Szoba-konfigurátor lista: projektek/szobák (méret, bútor darabszám, státusz)
- Design panel: bútor kártyák (típus, méret, anyag, szín)
- Dashboard KPI: Aktív konfiguráció / Véglegesített / Összesített bútor érték
- Stone + purple akcent, ikon: `home`
- Router: `/w/interior`
- SlideOver: RoomDetail (bútorlista, méretek, anyag összesítő)

## Mock adatok
- `src/mocks/masterdata.ts` — 8 termék, 6 anyag, 4 szállító
- `src/mocks/trade.ts` — 5 ajánlat, 4 PO, 6 partner (`data-trade.js` alapján)
- `src/mocks/interior.ts` — 4 szoba-konfiguráció, 10 bútor elem

## Tesztek
- `MasterdataPage.test.tsx`, `TradePage.test.tsx`, `InteriorPage.test.tsx`
- `pnpm test` 489 → ~530 (minimum +1 teszt/komponens)

## DONE feltételek
- [ ] 3 új világ a Home-on
- [ ] Minden lista renderel mock adattal
- [ ] `pnpm build` + `pnpm test` zöld

## Koordináció
Ne módosítsd: `HrPage`, `ControllingPage`, `WarehousePage` (bővített), `ServicePage`
