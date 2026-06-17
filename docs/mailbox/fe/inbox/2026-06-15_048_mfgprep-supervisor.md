---
id: MSG-FE-048
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-047
created: 2026-06-15
---

# FE-048 — Gyártás-előkészítés (mfgprep) + Supervisor világ implementálása

## Kontextus

FE-047 (Projektek + Logisztika) elfogadva ✅ — 455 teszt, commit `89da031`. Folytatás.

**Skill:** `/spaceos-terminal`  
**Sub-agent:** engedélyezett (verifier agent futtatáshoz)

## Prototípus fájlok (referencia)

```
/opt/spaceos/docs/tasks/new/joinerytech/
  page-mfg-prep.jsx         — MfgPrep főoldal: release queue + munkalapok
  page-mfg-prep-flow.jsx    — Gyártási folyamat: fázisok, blokkolt tételek
  page-mfg-prep-release.jsx — Release workflow: ellenőrzőlista + jóváhagyás
  page-mfg-datasheet.jsx    — Gyártási munkalap (datasheet) nézet
  page-mfg-datasheet-2.jsx  — Munkalap részletek: anyaglista, vágáslista
  data-mfgprep.js           — MfgPrep store (release queue, munkalapok, fázisok)
  mfg-prep-engine.js        — Release engine logika
  page-supervisor.jsx       — Supervisor műszakvezető nézet
```

## Amit implementálni kell

### Gyártás-előkészítés világ (MfgPrep)

**1. MfgPrepPage (`src/pages/MfgPrepPage.tsx`)**
- Release queue: tételek listája amelyek gyártásra várnak (ügyfél, projekt, termékek száma, státusz)
- Munkalapok (datasheets) lista: gyártó csapat, státusz, boundary dátumok
- Dashboard KPI: Kiadásra vár / Gyártásban / Beépítésre kész / Késő
- Stone + orange akcent — prototípus design language

**2. Navigáció**
- `mfgprep` világ, orange akcent, ikon: `clipboard`
- Router: `/w/mfgprep`, `/w/mfgprep/:screen`
- HomeScreen kártya (Production Manager és Admin role)

**3. Mock adatok (`src/mocks/mfgprep.ts`)**
- 5-6 release item különböző státuszokkal (pending/approved/in-production/ready)
- 4-5 munkalap (datasheet) hozzárendelt gyártó csapattal

**4. SlideOver-ek**
- ReleaseDetailSlideOver — ellenőrzőlista, jóváhagyás gomb, anyaglista összesítő
- DatasheetSlideOver — termékek listája, vágáslista link, státusz FSM

### Supervisor világ

**1. SupervisorPage (`src/pages/SupervisorPage.tsx`)**
- Műszakvezető nézet: aktív munkaállomások státusza (ki mit csinál most)
- Napi terv vs. tény: mi volt a plan, mennyi készült el
- Alert panel: blokkolók, késő tételek, anyaghiány jelzések
- Stone + red/rose akcent

**2. Navigáció**
- `supervisor` világ, red akcent, ikon: `eye`
- Router + HomeScreen kártya (Production Manager és Admin)

**3. Mock adatok (`src/mocks/supervisor.ts`)**
- 5-6 workstation aktuális állapottal (idle/working/blocked/break)
- Napi terv: 8-10 tétel, tény: részleges teljesítés

**4. SlideOver-ek**
- WorkstationDetail (aktuális feladat, operátor, blokkoló ok)
- DayPlanSlideOver (teljes napi terv tételei, tény jelölésekkel)

## Tesztek

- `MfgPrepPage.test.tsx` — release queue render, SlideOver, KPI kártyák
- `SupervisorPage.test.tsx` — workstation lista, alert panel, SlideOver
- `pnpm test` 455 → ~490 (minimum +1 teszt/komponens)

## DONE feltételek

- [ ] MfgPrep + Supervisor világ megjelenik a Home-on
- [ ] Release queue renderel mock adattal
- [ ] Workstation lista renderel státuszokkal
- [ ] `pnpm build` hiba nélkül
- [ ] `pnpm test` zöld (455-nél több)

## Koordináció

FE-B (fe2) terminál HR + Kontrolling világon dolgozik.
- **Ne módosítsd:** `HrPage.tsx`, `ControllingPage.tsx`, `hr.ts`, `controlling.ts`
- **Közösen érintheted:** `App.tsx`, `worlds.ts`, `HomeScreen.tsx`, `WorldShell.tsx` (accent map)
