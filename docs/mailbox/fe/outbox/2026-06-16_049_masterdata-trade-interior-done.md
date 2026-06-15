---
id: 2026-06-16_049_masterdata-trade-interior-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-16_049_masterdata-trade-interior.md
date: 2026-06-16
---

# DONE — FE-049 Törzsadatok + Kereskedelem + Belső tér világ implementálása

## Commit

`4dea708` — feat: FE-049 Törzsadatok + Kereskedelem + Belső tér világ implementálása

## Elvégzett munka

### MasterdataPage.tsx (új fájl)
- `ProductStatusPill` — státusz badge (active/inactive/review/discontinued)
- `SupplierStatusPill` — szállító státusz badge
- `ProductDetailSlideOver` — termék részletek: ár, kategória, készlet vs. min
- `SupplierDetailSlideOver` — szállító részletek: fizetési határidő, átfutás, kapcsolattartó
- `MasterdataDashboard` — KPI kártyák (aktív termékek/anyag cikkszámok/aktív szállítók/alacsony készlet), low-stock panel
- `ProductsList` — termék-törzs lista státusz badge + ár + készlet
- `MaterialsList` — anyag-törzs lista típus badge-ekkel (Lapanyag/Vasalat/Festék/Élzáró)
- `SuppliersList` — szállítók listája + SlideOver
- `MasterdataWorldPage` — router: dash / products / materials / suppliers

### TradePage.tsx (új fájl)
- `QuoteStatusPill` — árajánlat státusz (draft/sent/accepted/rejected)
- `PoStatusPill` — PO státusz (pending/confirmed/delivered/invoiced/cancelled)
- `QuoteDetailSlideOver` — ajánlat részletek: összeg, tételszám, megjegyzés
- `PoDetailSlideOver` — PO részletek: összeg, szállítási dátum
- `TradeDashboard` — KPI kártyák + nyitott ajánlatok panel
- `QuotesList` — 5 ajánlat státusz badge-ekkel + SlideOver
- `POList` — 4 PO státusz badge-ekkel + SlideOver
- `PartnersList` — 6 partner (Vevő/Szállító/Mindkettő badge, YTD forgalom)
- `TradeWorldPage` — router: dash / quotes / pos / partners

### InteriorPage.tsx (új fájl)
- `RoomStatusPill` — szoba státusz (draft/designing/approved/finalized)
- `RoomDetailSlideOver` — bútorlista, anyag összesítő, összérték
- `InteriorDashboard` — KPI kártyák (aktív konfiguráció/véglegesített/összérték/bútor elemek), szoba-konfig lista
- `FurnitureCards` — bútor kártyák rács nézetben (típus badge, méretek, anyag, ár)
- `RoomList` — szoba lista teljes nézetben
- `InteriorWorldPage` — router: dash / rooms / furniture

### Mock adatok
- `masterdata.ts`: már létező fájl (8 termék, 6 anyag, 4 szállító) — felhasználva
- `trade.ts`: 5 ajánlat, 4 PO, 6 partner + status meta objektumok
- `interior.ts`: 4 szoba-konfiguráció, 10 bútor elem (linter által generált struktúra)

### Navigáció + routing
- `worlds.ts`: masterdata (stone/database, badge "8 termék") + trade (teal/briefcase, badge "2 nyitott") + interior (violet/home, badge "4 szoba"), WORLD_ORDER 12→15
- `Icon.tsx`: database, home ikonok hozzáadva
- `WorldShell.tsx`: slate + purple accent hozzáadva
- `types/index.ts`: WorldKey `| 'masterdata' | 'trade' | 'interior'`
- `HomeScreen.tsx`: Admin + Designer role-hoz mind a 3 világ
- `App.tsx`: /w/masterdata, /w/trade, /w/interior (6 új route)

## Tesztek

| Fájl | Tesztek |
|---|---|
| `MasterdataPage.test.tsx` | 17 teszt — dashboard, products, materials, suppliers, SlideOver-ek |
| `TradePage.test.tsx` | 18 teszt — dashboard, quotes, POs, partners, SlideOver-ek |
| `InteriorPage.test.tsx` | 17 teszt — dashboard, rooms, furniture cards, SlideOver-ek |
| **Összesen** | **539 teszt — mind zöld** |

```
Test Files  59 passed (59)
Tests       539 passed (539)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 539/539 zöld
- [x] Törzsadatok világ megjelenik a Home-on (Admin + Designer role)
- [x] Kereskedelem világ megjelenik a Home-on (Admin + Designer role)
- [x] Belső tér világ megjelenik a Home-on (Admin + Designer role)
- [x] Termék lista renderel mock adattal (8 termék, státusz badge-ekkel)
- [x] Anyag lista típus badge-ekkel (Lapanyag/Vasalat/Festék/Élzáró)
- [x] Szállítók listája + SlideOver (fizetési határidő, átfutás, kapcsolattartó)
- [x] Árajánlat lista státusz szűrőkkel + detail SlideOver
- [x] PO lista státusz badge-ekkel + detail SlideOver
- [x] Partnerek lista YTD forgalommal + típus badge (Vevő/Szállító/Mindkettő)
- [x] Szoba-konfiguráció lista + RoomDetail SlideOver (bútorlista + anyag összesítő)
- [x] Bútor kártyák rács nézetben (típus, méretek, anyag, ár)
