# Project Knowledge Management

> Reference fájl a `spaceos-conductor` skill számára. Olvasd be amikor a felhasználó project knowledge audit-ot, szinkronizációt, vagy "mit kell feltölteni" kérdést tesz fel.

## A project knowledge szerepe

A claude.ai **project knowledge** egy **aktív cache** a SpaceOS legfontosabb dokumentumaiból. **Nem authoritative source** — a `spaceos-docs/` repo a hosszú-távú tároló.

A project knowledge célja:
- **Gyors elérés** a tervező-session-ek számára
- **Common context** több párhuzamos chat-nél
- **Audit trail** — egy adott időpontban mit tudott a system

A project knowledge **nem célja**:
- Teljes történelmi archív tárolása
- Részletes implementációs naplók
- Bug-tracking

## Mit tartson a project knowledge

A jelenlegi SpaceOS-helyzetre adaptált prioritás-lista:

### TIER 1 — Mindig fent (kötelező)

| Fájl | Méret | Miért kell |
|---|---|---|
| **Master Manifesto** (`SpaceOS_Master_Manifesto.docx`) | ~30 KB | T1-T6 tenetek, alapfilozófia |
| **Vision Master** (`SpaceOS_Vision_Master.md` ha van) | ~50 KB | Hosszú-távú irány |
| **Mathematical Furniture Theory** (`Mathematical_Furniture_Theory.docx`) | ~80 KB | Cabinet-vertikum matematikai alapja |
| **Aktuális Codebase Status** (`Codebase_Status_YYYYMMDD.md`) | ~20-50 KB | Jelenlegi LIVE állapot |
| **Design Pipeline Strategy** (`SpaceOS_Design_Pipeline_Strategy_v1.md`) | ~30 KB | Munkamódszertan szabályrendszer |

### TIER 2 — Aktív munkára (1-3 hónap-os relevancia)

| Fájl | Méret | Mikor |
|---|---|---|
| **Aktuális v4 architektúra-dokumentumok** (max 4-5 db) | ~50-100 KB / db | Aktív fejlesztés alatt álló modulok |
| **Aktuális design session record-ok** (max 3-5 db) | ~30 KB / db | Aktív tervezés axiómái |
| **Aktuális ADR-ek** (legutóbbi 5-10) | ~10-30 KB / db | Friss architektúra-döntések |

### TIER 3 — Háttér-reference (ritkán használt)

| Fájl | Méret | Mikor |
|---|---|---|
| **Vision dokumentumok** (Modules.Joinery, Modules.Cutting, Modules.Cabinet) | ~50-80 KB / db | Új tervezés indításakor referencia |
| **Domain Glossary** | ~40 KB | Specifikus terminológia |
| **Distributed Network**, **Federated Data Schema** | ~50 KB / db | Cross-tenant kérdéseknél |

### Mit NE tarts a project knowledge-ben

- **Deprecated megközelítések** (`SpaceOS_Deprecated_Approaches_v1.md` típus) — git-ben elég
- **Régi v1, v2, v3** dokumentumok ha v4 elkészült — git-ben elég
- **Régi Codebase Status** (3+ hónap) — archív git-ben
- **Deployment runbook-ok** (SETUP_VPS, SETUP_WINDOWS) — egyszer használatos, git-ben jó
- **Részletes integration log-ok** — git-ben elég

## A "tier rotation" minta

A project knowledge **dinamikus** — a tartalma változik a fejlesztési fázis szerint:

```
Cabinet 0.1 fejlesztés alatt:
  TIER 2: Cabinet 0.1 v4 architektúra ← AKTÍV

Cabinet 0.1 LIVE, Cabinet 0.2 design alatt:
  TIER 2: Cabinet 0.1 v4 architektúra ← MÉG MARAD (precedens-példa)
  TIER 2: Cabinet 0.2 v1-v4 architektúra ← AKTÍV
  TIER 3: Cabinet Core Session record ← MARAD (axiómák miatt)

Cabinet 0.2 LIVE, Cabinet 0.3 design alatt:
  TIER 3: Cabinet 0.1 v4 ← LE LEHET VENNI (git-ben elég)
  TIER 2: Cabinet 0.2 v4 ← AKTÍV PRECEDENS
  TIER 2: Cabinet 0.3 v1-v4 ← AKTÍV
```

A **lerakás-szabály:**
- Egy v4 dokumentum **akkor vehető le**, ha:
  1. Az alapján implementált modul **stabil LIVE**
  2. **Új** v4 dokumentum készül a következő verzióhoz
  3. **Két verzióval később** már nem precedens-példa

## Search precision tünetek

A project knowledge keresése **pontosság-vesztést** szenvedhet:

| Tünet | Mit jelent | Mit tegyél |
|---|---|---|
| Friss fájl helyett régi formátumút talál | Indexálási késedelem vagy duplikáció | Kérdezz a felhasználótól megerősítést |
| Egy terminus többször visszajön irreleváns kontextusban | A terminus túl gyakori az egyéb dokumentumokban | Specifikusabb kérdést tegyél fel |
| Nincs találat egy tudott fájlra | A fájl **nincs** feltöltve, vagy hibás indexel | Listázd a `<project_files>`-ban szereplő nevet |

A `spaceos-conductor` skill **mindig kétszer keres** ha az első találat nem egyértelmű — egyszer specifikus query-vel, egyszer alternatív megfogalmazással.

## Project knowledge audit checklist

Amikor a felhasználó kéri ("mit kell feltölteni / levenni?"), kövesd ezt:

### 1. Kategorizáld a meglévő fájlokat

A `<project_files>` blokk mutatja a jelenlegi tartalmat. Minden fájlt hozzárendelsz egy tier-hez:

```
TIER 1 (mindig fent):
  ✓ SpaceOS_Master_Manifesto.docx
  ✓ Mathematical_Furniture_Theory.docx
  ✓ Codebase_Status_20260426.md (de friss-e?)

TIER 2 (aktív):
  ✓ SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md
  ✓ SpaceOS_Cabinet_Core_Session_20260425.docx
  ? SpaceOS_Phase3Cplus_Architecture_v3.md (még aktív?)

TIER 3 (háttér):
  ✓ SpaceOS_Modules_Cutting_Vision_v1.md
  ✓ SpaceOS_Domain_Glossary.docx

LEHET LEVENNI:
  - SpaceOS_Phase3A_Architecture_Draft.docx (régi, v3 fent van)
  - SpaceOS_Sprint_D_Phase1_5_v4.md (lezárt sprint?)
```

### 2. Azonosítsd a hiányzó fájlokat

Vesd össze a **aktív tervezésekkel** + **friss codebase-eseményekkel**:

- Aktív tervezés Cabinet 0.2-re? → kell **`SpaceOS_Cabinet_0.2_v4.md`** ha létezik
- Cutting Phase 4 indul? → kell **`SpaceOS_Cutting_Phase4_v4.md`**
- Új Codebase Status szülein? → friss **`Codebase_Status_YYYYMMDD.md`**

### 3. Javasolj konkrét akciót

Listázd:
- **TÖLTSD FEL**: [fájl] — indok
- **VEDD LE**: [fájl] — indok
- **FRISSÍTSD**: [fájl] — friss verzióval cseréld

A felhasználó manuálisan végzi a műveleteket — te csak a listát adod.

## Az "aktuális-e" gyors-teszt

Ha a felhasználó kérdezi "Aktuális a project knowledge?", kövesd ezt:

1. Olvass be egy specifikus értéket a friss Codebase Status-ból (pl. tesztszám)
2. Ha **nem találsz** ilyen friss értéket → "A project knowledge a régi formátumú Status-t mutatja, frissítsd"
3. Ha **találsz** egy friss számot → "A project knowledge tartalmazza a [tesztszám] friss állapotot, aktuális"

A felhasználó **rövid igen/nem** válaszra számít. Ne adj hosszú listát, csak azt mondd: **aktuális** vagy **nem aktuális, ezt frissítsd**.

## Project knowledge méret-monitoring

A claude.ai 25 MB project knowledge limit közelében:

- **0-15 MB**: Komfortzóna, search jó
- **15-22 MB**: Sárga zóna, search-precision csökken
- **22+ MB**: Piros zóna, fel kell takarítani

A `spaceos-conductor` skill **proaktívan** jelzi ha a fájllistából látható, hogy közeledünk a piros zónához:

> "A project knowledge ~16 MB-on van (becsült), kezd közeledni a search-precision degradation-hoz. Javasolt: vedd le a [régi fájl] és [másik régi fájl] dokumentumokat — git-ben elérhetőek maradnak."

## Mikor migrálni a `spaceos-docs/` repo-ra

A migration **nem most** szükséges, de érdemes előkészíteni:

**Indikátorok hogy ideje:**
- Search-precision már zavarja a munkát (gyakran kéri a `spaceos-conductor` az "ezt erősítsd meg" kérdést)
- Project knowledge 18+ MB-on van
- 3-nál több párhuzamos modul fut

**Migration lépések** (akkor írd le, ha aktuálissá válik):
1. `spaceos-docs/` repo létrehozása a VPS-en
2. Project knowledge tartalom export → repo
3. Claude Code agent CLAUDE.md frissítése: "spaceos-docs/ a primary forrás"
4. Project knowledge **csak** a TIER 1-re csökkentve
5. Tervező-session-ekhez **link** a spaceos-docs/ relevant fájlhoz az induló prompt-ban

A `spaceos-conductor` ezt a migration-t **nem kezdeményezi**, csak akkor javasolja, ha a search-degradation elviselhetetlen.
