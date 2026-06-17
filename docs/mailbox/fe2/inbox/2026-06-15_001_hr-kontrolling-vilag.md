---
id: MSG-FE2-001
from: root
to: fe2
type: task
priority: high
status: READ
ref: FE-Design-Requirements
created: 2026-06-15
---

# FE2-001 — HR + Kontrolling világ implementálása

## Kontextus

Te vagy a **FE-B terminál** — párhuzamosan dolgozol az FE-A terminállal (ő CRM + Finance világon dolgozik). A JoineryTech prototípus (https://joinerytech.hu/proto/) a design spec. A React production app: `/opt/spaceos/frontend/joinerytech-portal/`.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett (verifier agent futtatáshoz)

## Prototípus fájlok (referencia)

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-hr.jsx          — HR Dashboard + Dolgozók + Kapacitás-naptár
  page-hr-2.jsx        — EmployeeDetail SlideOver + Távollét + Személyes adatok
  data-hr.js           — HR store (mock: employees, absences, assignments)
  page-controlling.jsx — Kontrolling Dashboard + Portfólió + Eltérés-elemzés  
  page-controlling-2.jsx — UtóKalkuláció screen
  page-execbi.jsx      — Vezetői BI cockpit (4 tab-os exec dashboard)
  data-controlling.js  — Kontrolling store + engine
  data-execbi.js       — ExecBI trend seed + engine
```

## Amit implementálni kell

### HR világ

A React app-ba (`/opt/spaceos/frontend/joinerytech-portal/src/`) add hozzá:

**1. HR Page (`src/pages/HrPage.tsx`)**
- Dolgozó lista kártyás nézettel (Avatar + Név + Beosztás + Részleg + Kapacitás-sáv)
- HR Dashboard KPI: Összlétszám / Aktív / Szabadságon / Kapacitás kihasználtság
- Kapacitás-naptár (heti rács: dolgozó-sorok × nap-oszlopok)
- A prototípus design language-t kövesd (stone + amber akcent)

**2. Navigáció**
- `hr` világ, amber akcent, ikon: `user`
- Az App.tsx router-be bekötni (`/w/hr`)
- A HomeScreen-en megjeleníteni

**3. Mock adatok**
- A prototípus `data-hr.js` seed alapján: ~8-10 dolgozó, különböző részlegekkel, beosztásokkal
- Saját mock fájl: `src/mocks/hr.ts`

**4. SlideOver-ek**
- EmployeeDetail SlideOver (Alap adatok + Kapacitás heti nézet + Távollét FSM)
- NewEmployeeSheet drawer (Név, Beosztás, Részleg, Heti óra, Bér-kategória)
- AbsenceSheet (Típus: szabadság/táppénz/egyéb, Dátumtartomány, Jóváhagyás gomb)

### Kontrolling világ

**1. Kontrolling Page (`src/pages/ControllingPage.tsx`)**
- Portfólió tábla: Projekt × Terv/Tény/EAC fedezet (KPI sávval)
- Projekt-fedezet SlideOver (kategória bontás: anyag/munka/szállítás + margin%)
- Dashboard KPI: Szabad portfólió / Átlagos fedezet / Eltérő projektek száma

**2. Navigáció**
- `kontrolling` világ, slate akcent, ikon: `analytics`
- Router + HomeScreen kártya

**3. Mock adatok**
- A prototípus `data-controlling.js` alapján: 3-5 projekt, terv/tény adatokkal
- Saját mock: `src/mocks/controlling.ts`

**4. SlideOver-ek**
- ProjectControllingDetail (kategória bontás tábla + EAC + kézi korrekciók)

## Tesztek

- Minden új komponenshez legalább 1 unit test
- `pnpm test` zöld kell
- HrPage test + ControllingPage test (alapvető render)

## DONE feltételek

- HR + Kontrolling világ megjelenik a Home-on
- Dolgozó lista renderel (mock adattal)
- Portfólió tábla renderel (mock adattal)
- `pnpm build` hiba nélkül lefut
- `pnpm test` zöld

## Koordináció

Az FE-A terminál a CRM + Finance világon dolgozik **ugyanabban a git branch-ben**. Ütközések elkerülése:
- **Ne módosítsd:** `src/pages/CrmPage.tsx`, `src/pages/FinancePage.tsx`, `src/mocks/crm.ts`, `src/mocks/finance.ts`
- **Közösen érintheted:** `src/App.tsx` (router bővítés), `src/mocks/worlds.ts` (új worldök), `src/components/layout/HomeScreen.tsx`
- Ha ütközés van a WorldShell-ben: kommunikálj a root-tal
