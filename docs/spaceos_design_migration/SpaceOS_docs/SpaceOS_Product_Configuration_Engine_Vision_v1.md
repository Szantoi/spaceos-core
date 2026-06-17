# SpaceOS — Product Configuration Engine
## Modules.Abstractions Vision & Architecture Foundation

> **Verzió:** v1.0 — 2026-04-09
> **Típus:** Vízió + Architekturális Alap — az arch-planner pipeline bemenete
> **Célközönség:** Fejlesztők, döntéshozók, és a SpaceOS ökoszisztéma jövőbeli partnerei
> **Nyelv:** Magyar (üzleti kontextus) + angol (technikai terminológia)

---

## 1. Mi a SpaceOS?

**SpaceOS nem CAD program.** SpaceOS egy **térbeli adatkezelő platform**, ami összeköti a tervező, gyártó, és kivitelező világokat.

```
A tervező a saját CAD-jét használja (Inventor, SolidWorks, Fusion, Revit).
A gyártó a saját gépét használja (CNC, élzáró, lapszabász).
A megrendelő a saját eszközét használja (telefon, email, Excel).

SpaceOS az ADAT RÉTEG közöttük:
  - fogadja a méreteket és modelleket bármilyen formából
  - alkalmazza a gyártási szabályokat
  - kiadja a szabászlistát, CNC programot, műhelyrajzot, BIM modellt
```

### Alapelv: Data → Rules → Geometry

| Réteg | Ki felelős | Példa |
|---|---|---|
| **Data** | Megrendelő / tervező / felmérő | "800×2040-as FAF_T ajtó" |
| **Rules** | Gyártó (konfigurált a SpaceOS-ban) | Tok offset, fúga, anyagvastagság, CNC paraméterek |
| **Geometry** | Automatikus (SpaceOS számol) | Szabászlista, CNC kód, 2D/3D modell |

---

## 2. A probléma amit megoldunk

### Ma (2026)

```
Megrendelő                         Gyártó (Doorstar)
    │                                  │
    ├── Viber üzenet: "800×2040 FAF_T" │
    │                                  │
    │                                  ├── Excel megnyitása
    │                                  ├── Falnyílás beírása
    │                                  ├── Offset-ek kézi alkalmazása
    │                                  ├── Szabászlista kézi másolása
    │                                  ├── CNC program kézi írása
    │                                  └── Kanban: post-it a táblán
    │                                  │
    └── "Mikor lesz kész?"             └── "Hétfőre" (talán)
```

### Holnap (SpaceOS-szal)

```
Megrendelő                         Gyártó (Doorstar)
    │                                  │
    ├── Portal: megrendelés rögzítés   │
    │   (vagy Viber → AI → Portal)     │
    │                                  │
    └── automatikus:                   ├── automatikus:
        - visszaigazolás                   - szabászlista generálás
        - státusz követés                  - CNC program deriválás
        - BIM modell elérés                - Kanban automatikus
                                           - határidő kalkuláció
```

---

## 3. A termék mint gráf — az alapötlet

### Miért gráf és nem tábla?

Egy ajtó (vagy szekrény, vagy ablak) alkatrészei **nem függetlenek** — egymásból következnek. Egy falnyílás mérete meghatározza a tok méretét, ami meghatározza a borítás méretét, ami meghatározza az ajtólap méretét, ami meghatározza a keret méretét.

**Ez egy fa (gráf), ahol a méretek propagálnak gyökértől a levelekig.**

```
Klasszikus megközelítés (Excel):     SpaceOS megközelítés (Gráf):

┌──────────────────────┐             ┌──────────────────────┐
│ DoorTypeRules tábla  │             │ [Falnyílás]          │
│ 22 sor × 12 oszlop   │             │     │                │
│ BkmWidthFixed = -3   │       →     │     ├─→ [Tok BM]     │
│ BkmHeightFixed = -1.5│             │     │     │           │
│ ...                  │             │     │     ├─→ [BBM]   │
└──────────────────────┘             │     │     └─→ [BKM]   │
┌──────────────────────┐             │     └─→ [Ajtólap]     │
│ PartDimensionRules   │             │           ├─→ [Keret] │
│ 200 sor              │             │           └─→ [Betét] │
│ WidthBase = -6       │             └──────────────────────┘
│ Multiplier = 1.0     │
└──────────────────────┘             Ugyanaz az információ,
                                     de STRUKTURÁLTAN.
Nehéz bővíteni.
Nehéz érteni.                        Könnyű bővíteni.
Termék-specifikus.                   Könnyű érteni.
                                     UNIVERZÁLIS.
```

### Egy gráf, három nézet

Ugyanabból a gráfból három különböző kimenet származtatható:

| Nézet | Mit mutat | Ki használja |
|---|---|---|
| **1 — Termék konfigurátor** | Alkatrészek + csatlakozások + anyagok | Tervező, gyártásvezető |
| **2 — CNC műveleti terv** | Vágások, fúrások, marások sorrendben | Gépkezelő, CNC programozó |
| **3 — Gyártási folyamat** | Fázisok (szabász → CNC → élzáró → festés → összeszer.) | Termelésirányító, Kanban |

---

## 4. A gráf elemei

### 4.1 ComponentSlot (alkatrész hely)

Egy "hely" a termékben, ahova egy alkatrész kerül. Lehet **fizikai** (valódi lap, keret, betét) vagy **virtuális** (számítási közbülső érték, mint a "Tok belméret").

| Tulajdonság | Leírás | Példa |
|---|---|---|
| Név | Emberi olvasható | "Tok keret felső" |
| Típus | Alkatrész kategória | Frame, Insert, Clad, Blende... |
| Anyag | Alapértelmezett anyag | MDF 18mm |
| Darabszám | Hány kell belőle | 2 (bal+jobb keret) |
| Virtuális? | Nem fizikai alkatrész | Tok BM, BBM (számítási csomópontok) |
| Gravitációs szerep | MFT osztály | Vertical (teherhordó) / Horizontal (teherfelvevő) |

### 4.2 SlotConnection (csatlakozás)

Két alkatrész közötti kapcsolat. Megmondja:
- **Hogyan számolódik** a gyermek mérete a szülőből (méretszabály)
- **Hogyan csatlakozik** fizikailag (illesztés típusa)
- **Milyen gépi műveletet** igényel (CNC, élzáró, vágás)
- **Melyik gyártási fázisban** készül

| Tulajdonság | Leírás | Példa |
|---|---|---|
| Szülő → Gyermek | Irány | Tok BM → Ajtólap |
| Tengely | Melyik irányban hat | Width / Height / Depth |
| Szabály | Matematikai művelet | Subtract (kivonás), Add (hozzáadás), Max... |
| Operandus | Fix érték (mm) | 18mm (anyagvastagság) |
| Illesztés típusa | Fizikai csatlakozás | Butt (tompa), Dado (hornyolt), Miter (gér)... |
| Gépi művelet | CNC/gép | Cut (vágás), Groove (hornyolás), Drill (fúrás)... |
| Gyártási fázis | Kanban oszlop | Cutting, CNC, EdgeBanding, Surface, Assembly... |

### 4.3 TemplateParameter (paraméter)

Termék-szintű beállítás, amit a felhasználó módosíthat:

| Paraméter | Érték | Leírás |
|---|---|---|
| FugaTop | 1.0 mm | Felső fúga (ajtólap és tok közötti rés) |
| FugaBottom | 3.0 mm | Alsó fúga (nagyobb — padló/küszöb) |
| FugaLeft | 2.0 mm | Bal fúga |
| FugaRight | 2.0 mm | Jobb fúga |
| CuttingOversize | 1.0 mm | Szabásnál ráhagyás |
| DefaultThickness | 18 mm | Alapértelmezett anyagvastagság |

---

## 5. Illesztés típusok — a gyártás nyelve

A csatlakozás típusa meghatározza a CNC műveletet. Ez a bridge a CAD és a gyártás között:

| Illesztés | Magyar | CAD feature | CNC művelet | Gép |
|---|---|---|---|---|
| **Butt** | Tompa illesztés | — | Egyenes vágás | Lapszabász |
| **Dado** | Hornyolt | Cut (slot) | Horonymarás | CNC |
| **Rabbet** | Falcolt | Cut (rabbet) | Falcmarás | CNC |
| **Miter** | Gérvágás | Cut (angled) | Szögben vágás | Gérvágó |
| **Pocket** | Zseb/rejtett csavar | Pocket | Zsebmarás | CNC |
| **Tongue & Groove** | Csap-horony | Groove ×2 | Hornyolás ×2 | CNC |
| **Dowel** | Tipli | Hole | Fúrás | CNC / fúrógép |
| **EdgeBand** | Élzárás | — | — | Élzáró gép |
| **Chamfer** | Letörés | Chamfer | Letörés-marás | CNC |
| **Round** | Kerekítés | Fillet | Sugár-marás | CNC |
| **Offset** | Virtuális | — | — | — (csak számítás) |

### Inventor / SolidWorks / Fusion megfeleltetés

```
CAD Feature Tree                SpaceOS SlotConnection
────────────────                ──────────────────────
Extrude (kihúzás)          →    ComponentSlot (alkatrész létrejötte)
Cut (kivágás)              →    Connection: JointType=Dado/Rabbet, MachiningOp=Groove
Fillet (kerekítés)         →    Connection: JointType=Round, MachiningOp=Round
Chamfer (letörés)          →    Connection: JointType=Chamfer, MachiningOp=Chamfer
Hole (furat)               →    Connection: JointType=Dowel, MachiningOp=Drill
Shell (héj)                →    Connection: JointType=Rabbet (hátfal horony)
Assembly Mate (kényszer)   →    Connection: DimensionRule (méretszámítás)
```

---

## 6. Geometria szintek — nem mindenki ad STEP fájlt

A SpaceOS-nak tudnia kell dolgozni bármilyen részletességű adattal:

| Szint | Mit kapunk | Honnan jön | Mire elég |
|---|---|---|---|
| **L0 — Paraméter** | Csak méretek (Sz × M × V) | Telefon, email, Excel, API | Szabászlista, BOM, árazás |
| **L1 — BoundingBox** | Befoglaló doboz (min/max pont) | Alaprajz, helyfoglalás | Ütközés vizsgálat, térkitöltés |
| **L2 — Skeleton** | Középvonalak + kényszerek | CAD skeleton, gépész terv | Top-down tervezés, csővezetés |
| **L3 — Surface** | Felületek (mesh) | Fusion, egyedi formák | Vizualizáció, felület-ütközés |
| **L4 — Solid** | Teljes test | Inventor, SolidWorks STEP/IFC | CNC, pontos gyártás, BIM |

**Egy projekten belül keveredhetnek:**
- Ajtó: L4 (Inventor STEP)
- Fal: L1 (építész alaprajz → bounding box)
- Csővezeték: L2 (középvonal)
- Bútor: L0 (csak méretek a megrendelésből)

**Az ütközés vizsgálat mindig a legalacsonyabb közös szinten működik (L1 — BoundingBox), de ha magasabb szintű adat elérhető, azt is használja.**

---

## 7. A klasszikus felhasználó élménye (Excel skin)

### A probléma

Nem mindenki gondolkodik gráfokban. Egy Doorstar műhelyvezető Excel-ben gondolkodik:

```
"Add meg az ajtó típust, írd be a falnyílás méretet,
 és kapd meg a szabászlistát. Ennyi."
```

### A megoldás: az Excel nézet egy skin a gráf felett

```
┌─────────────────────────────────────────────────────────────┐
│  FELHASZNÁLÓI FELÜLET — "Klasszikus" mód                    │
│                                                             │
│  Ajtó típus: [FAF_T          ▼]                             │
│                                                             │
│  Falnyílás szélesség:  [  800 ] mm                          │
│  Falnyílás magasság:   [ 2040 ] mm                          │
│  Falnyílás vastagság:  [  100 ] mm                          │
│                                                             │
│  Vasalat: [Standard pántok   ▼]  → fúga override: nincs     │
│                                                             │
│  [  SZÁMOL  ]                                               │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ Szabászlista                                          │  │
│  │ Alkatrész           Anyag     Sz    M    db   Fázis   │  │
│  │ Tok keret felső     MDF 18   800   18    1   Szabász  │  │
│  │ Tok keret alsó      MDF 20   800   20    1   Szabász  │  │
│  │ Tok keret bal       MDF 18    18  2040   1   Szabász  │  │
│  │ Tok keret jobb      MDF 18    18  2040   1   Szabász  │  │
│  │ Ajtólap keret H     MDF 18   756   80    2   Szabász  │  │
│  │ Ajtólap keret V     MDF 18    80  1980   2   Szabász  │  │
│  │ Betét               HDF 3    750  1976   1   Szabász  │  │
│  │ Élzárás             ABS 0.5  —     —     8   Élzáró   │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                             │
│  [ Excel export ]  [ Műhelyrajz PDF ]  [ CNC küldés ]      │
└─────────────────────────────────────────────────────────────┘
```

**Ami a háttérben történik:**

```
1. "FAF_T" kiválasztás → ProductTemplate betöltés (gráf)
2. Falnyílás méretek → DimensionInput (root node)
3. [SZÁMOL] → GraphCalculationEngine.Calculate()
4. Gráf bejárás: Falnyílás → Tok BM → BBM → BKM → Ajtólap → Alkatrészek
5. Szabászlista = a gráf fizikai leaf node-jai méretekkel
6. Excel export = CuttingList → XLSX
7. CNC küldés = MachiningOp-ok → G-code post-processor
```

**A felhasználó sosem látja a gráfot, ha nem akarja.**

### Konstans tábla nézet (admin)

A "klasszikus" admin felület is megtartható — de egy tábla szerkesztése valójában a gráf módosítása:

```
┌─────────────────────────────────────────────────────────────┐
│  ADMIN FELÜLET — "Konstans táblák" mód                      │
│                                                             │
│  Termék: FAF_T ajtó                                         │
│                                                             │
│  Tok offset-ek:                                             │
│  ┌────────────────────────────────────────────┐             │
│  │ Irány        Offset (mm)                   │             │
│  │ Szélesség    10.0                          │             │
│  │ Magasság     5.0                           │             │
│  └────────────────────────────────────────────┘             │
│  → háttérben: SlotConnection(Root→TokBM, Subtract, 10.0)   │
│                                                             │
│  Tok keret vastagságok:                                     │
│  ┌────────────────────────────────────────────┐             │
│  │ Pozíció      Vastagság (mm)                │             │
│  │ Bal          18.0                          │             │
│  │ Jobb         18.0                          │             │
│  │ Felső        18.0                          │             │
│  │ Alsó         20.0                          │             │
│  └────────────────────────────────────────────┘             │
│  → háttérben: SlotConnection(TokBM→TokKM, Add, 18.0, ×1)  │
│                                                             │
│  Fúga értékek:                                              │
│  ┌────────────────────────────────────────────┐             │
│  │ Pozíció      Méret (mm)                    │             │
│  │ Felső        1.0                           │             │
│  │ Alsó         3.0                           │             │
│  │ Bal          2.0                           │             │
│  │ Jobb         2.0                           │             │
│  └────────────────────────────────────────────┘             │
│  → háttérben: TemplateParameter(FugaTop, 1.0)              │
│                                                             │
│  [ Mentés ]  [ Teszt számítás ]  [ Gráf nézet → ]          │
└─────────────────────────────────────────────────────────────┘
```

**"Gráf nézet →" gombbal átválthat a haladó nézetbe, de nem kötelező.**

---

## 8. Hogyan illeszkedik a meglévő SpaceOS-hoz

```
┌─────────────────────────────────────────────────────────────┐
│  L4 — Design Portal (React + Turborepo)                     │
│  ├── Klasszikus nézet (Excel-like UI)                       │
│  ├── Gráf editor (haladó)                                   │
│  ├── 3D megjelenítő (Three.js — L1/L3 geometry)            │
│  └── Kanban (ProcessPhase nézet)                            │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────────┐
│  L3 — Orchestrator (Node.js BFF)                            │
│  ├── Mediation: Portal → Kernel + Modules                   │
│  ├── AI: természetes nyelv → DimensionInput                 │
│  └── Import: IFC/STEP parser → Graph                        │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────────┐
│  L2 — Modules                                               │
│  ├── Modules.Abstractions (ÚJ)                              │
│  │   ├── ProductTemplate + ComponentSlot + SlotConnection   │
│  │   ├── GraphCalculationEngine                             │
│  │   ├── ManufacturingDerivation (CNC, Process)             │
│  │   ├── GeometryAttachment (L0-L4 multi-fidelity)          │
│  │   └── IFC/STEP bridge (import/export)                    │
│  ├── Modules.Joinery (faipar-specifikus)                    │
│  │   ├── Joinery JointType-ok (Dado, Rabbet, EdgeBand...)  │
│  │   ├── Faipar ProcessPhase sorrend                        │
│  │   └── Anyag katalógus (MDF, HDF, ABS, stb.)             │
│  ├── Modules.Door (ajtó branch)                             │
│  │   └── Ajtó ProductTemplate seed-ek (FAF_T, Sikban...)   │
│  ├── Modules.Cabinet (szekrény branch)                      │
│  │   └── Szekrény ProductTemplate seed-ek                   │
│  └── Modules.Window (ablak branch — Horizon 3)              │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────┴─────────────────────────────────┐
│  L1 — Kernel (C# .NET 8)                                    │
│  ├── SpatialElement + BVH (Phase 3A — KÉSZ ✅)              │
│  ├── FlowEpic + FSM (KÉSZ ✅)                               │
│  ├── AggregateSnapshot + ProofHash (Phase 3B — KÉSZ ✅)     │
│  └── TenantHandshakeAllowlist (Phase 3C+ — FOLYAMATBAN)    │
└─────────────────────────────────────────────────────────────┘
```

---

## 9. Import / Export — a CAD világ felé

### Import (külső → SpaceOS)

| Forrás | Formátum | SpaceOS szint | Hogyan |
|---|---|---|---|
| Megrendelő telefon/email | Szöveg → AI → paraméterek | L0 | Orchestrator AI parse |
| Megrendelő Excel | CSV/XLSX | L0 | Seed Data Template |
| Tervező Inventor | STEP AP214 | L4 | STEP parser → Part-onként ComponentSlot |
| Tervező SolidWorks | STEP AP203 | L4 | Ugyanaz |
| Tervező Fusion 360 | STEP / OBJ | L3-L4 | STEP vagy mesh import |
| Építész Revit | IFC 2×3 / 4 | L1-L4 | IFC parser → BIM elemek |
| Gépész | DXF | L2 | 2D kontúr → skeleton |
| Más SpaceOS node | JSON API | L0-L1 | SyncSignal + BoundingBox |

### Export (SpaceOS → külső)

| Cél | Formátum | Tartalom |
|---|---|---|
| **Revit / BIM** | IFC | Elemek + anyagok + kapcsolatok + méretek |
| **Inventor / SW** | STEP | Solid test (ha L4 elérhető) |
| **AutoCAD** | DXF | 2D lapkontúr + kótázás |
| **CNC gép** | G-code / DSTV | Vágás, marás, fúrás (post-processor specifikus) |
| **Lapszabász** | Optimalizált DXF | Nesting (lapkiosztás) |
| **ERP / MES** | CSV / JSON | Szabászlista, BOM, anyagrendelés |
| **Műhely** | PDF | Műhelyrajz (2D nézetek + méretezés) |
| **Portál** | JSON + SVG | Interaktív megjelenítés |

---

## 10. Fázisterv

| Fázis | Tartalom | Effort | Prioritás |
|---|---|---|---|
| **A — Abstractions Core** | ProductTemplate, ComponentSlot, SlotConnection, GraphCalculationEngine, TemplateParameter, RuleOperator | ~15 nap | P0 — Soft Launch alap |
| **B — Manufacturing Derivation** | JointType→MachiningOp mapping, CncOperation deriválás, ProcessPhase→ProductionStep, CuttingList generálás | ~8 nap | P0 — Soft Launch |
| **C — Geometry Attachment** | GeometryAttachment entity, L0-L4 szintek, SpatialElement FK, SkeletonJson | ~5 nap | P1 — Soft Launch után |
| **D — IFC Bridge** | IFC import/export (xbim .NET), Revit kompatibilitás, BIM metadata mapping | ~10 nap | P1 — Horizon 2 |
| **E — STEP Bridge** | STEP import (Open CASCADE .NET), Part-szintű bontás, Feature tree mapping | ~10 nap | P1 — Horizon 2 |
| **F — CNC Post-processor** | G-code generálás (gépspecifikus), nesting integráció, DXF export | ~8 nap | P2 — Horizon 2 |
| **G — Portal UI** | Klasszikus nézet (táblázat), Gráf editor (haladó), Template admin | ~12 nap | P1 — Soft Launch-szal párhuzamos |

**Doorstar Soft Launch: A + B = ~23 nap** (Track 3 új scope)

---

## 11. Összefoglaló — miben más ez mint egy CAD program

| CAD program | SpaceOS |
|---|---|
| Geometriát szerkeszt | **Adatot és szabályokat** kezel |
| Egy felhasználó, egy gép | **Hálózat**: tervező + gyártó + megrendelő |
| Fájl-alapú (.sldprt, .ipt) | **Adatbázis-alapú** (PostgreSQL, verziókezelt) |
| CNC programot kézzel írnak | **CNC automatikusan** deriválódik a gráfból |
| BOM kézzel készül | **BOM automatikusan** a leaf node-okból |
| Kanban külön rendszerben | **Kanban a gráf ProcessPhase** nézetéből |
| Export: STEP/DXF egyenként | **Export: IFC + STEP + DXF + G-code + CSV egyszerre** |
| Licenc: drága, géphez kötött | **SpaceOS: az adat nyílt, a CAD bármi lehet** |

**SpaceOS nem helyettesíti az Inventort. SpaceOS az, amit az Inventor nem tud: hálózatban gondolkodni, automatikusan gyártani, és mindenkinek a saját nyelvén beszélni.**

---

*SpaceOS — Product Configuration Engine Vision v1.0 · 2026-04-09*
*A Modules.Abstractions arch-planner pipeline bemenete*
