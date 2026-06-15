---
id: 2026-06-15_046_crm-vilag-done
from: fe
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-06-15_046_crm-vilag.md
date: 2026-06-15
---

# DONE — FE-046 CRM és Finance világ implementálása

## Commit

`9070dc0` — feat: FE-046 CRM és Finance világ implementálása

## Elvégzett munka

### CrmPage.tsx (új fájl)
- `LeadStatusPill`, `OppStatusPill`, `SourcePill` — státusz badge komponensek
- `LeadDetailSlideOver` — lead részletek: státusz, forrás, tevékenységnapló, LEAD-ID
- `OppDetailSlideOver` — opportunity részletek: értéke, fázis, OPP-ID, tételek
- `CrmDashboard` — KPI kártyák (pipeline érték, win rate, lead konverzió), lead pipeline mini, nyitott lehetőségek, nyitott feladatok
- `PipelineKanban` — Kanban oszlopok lead kártyákkal (Kapcsolat, Minősítés, Nurturing, Ajánlat, Megnyert, Elvetve)
- `LeadList` — lead lista szűrővel (státusz chip-ek), keresőmező, SlideOver megnyitás kattintásra
- `OppList` — opportunity lista státusz pill-ekkel, SlideOver megnyitás kattintásra
- `CrmForecast` — pipeline KPI-k + forecast tábla fázis szerint (valószínűség, súlyozott érték)
- `CrmWorldPage` — router: dash / pipeline / leads / opps / forecast screen-ek

### FinancePage.tsx (új fájl)
- `FinStatusPill`, `FinKindBadge`, `FinMethodBadge` — badge komponensek
- `InvoiceDetailSlideOver` — számla részletek: tételek, ÁFA-bontás (kulcsonként), kifizetések, hátralék, sztornó indok, benyújtó megjegyzés
- `InvoiceRow` — kompakt számla sor (partner, összeg, esedékesség, státusz, irány)
- `FinanceDashboard` — mini stat kártyák (kintlévőség, fizetendő, lejárt stb.), legutóbbi kimenő + bejövő számlák
- `OutgoingInvoices` — kimenő számlák 5 szűrő chip-pel (mind / nyitott / lejárt / piszkozat / fizetve)
- `IncomingInvoices` — bejövő számlák, „Portálon benyújtott" badge, benyújtó megjegyzés SlideOver-ben
- `PaymentsPage` — minden kifizetés táblázatban (dátum + ref, számla, partner, mód, összeg, megjegyzés)
- `FinanceWorldPage` — router: dash / outgoing / incoming / payments screen-ek

### Navigáció + routing
- `worlds.ts`: `crm` és `finance` világ hozzáadva WORLDS-hoz, WORLD_ORDER 8 elemre bővítve
- `HomeScreen.tsx`: Admin és Designer role-okhoz CRM + Finance kártya megjelenik
- `App.tsx`: `/w/crm`, `/w/crm/:screen`, `/w/finance`, `/w/finance/:screen` útvonalak

### Egyéb
- `Icon.tsx`: phone, calendar, receipt, file, lock, storefront ikonok
- `types/index.ts`: WorldKey union `| 'crm' | 'finance'` kiegészítve
- `data.test.ts`: WORLDS count assertion 6 → 8

## Tesztek

| Fájl | Tesztek |
|---|---|
| `CrmPage.test.tsx` | 16 teszt — dashboard, pipeline, leads, opps, forecast, SlideOver-ek |
| `FinancePage.test.tsx` | 15 teszt — dashboard, outgoing/incoming listák, payments, SlideOver részletek |
| **Összesen** | **422 teszt — mind zöld** |

```
Test Files  52 passed (52)
Tests       422 passed (422)
```

## Elfogadási kritériumok

- [x] `pnpm build` — 0 TS hiba
- [x] `pnpm test` — 422/422 zöld
- [x] CRM + Finance világ megjelenik a Home-on (Admin/Designer role)
- [x] Lead pipeline kanban renderel mock adattal (6 lead, 4 Kanban oszlop)
- [x] Finance számla lista renderel mock adattal (9 kimenő, 5 bejövő)
- [x] SlideOver-ek: LeadDetail, OppDetail, InvoiceDetail mind működnek
- [x] Kifizetések képernyő: 6 payment sor, GIRO ref megjelenítve
- [x] ÁFA-bontás szekció az InvoiceDetail-ben
