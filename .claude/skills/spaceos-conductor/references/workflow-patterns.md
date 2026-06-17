# SpaceOS Workflow Patterns

> Reference fájl a `spaceos-conductor` skill számára. Olvasd be amikor workflow-méretű kérdés merül fel — orchestration, párhuzamosítás, repo-szervezés, agent-koordináció.

## A multi-agent orchestration modellje

A SpaceOS fejlesztési folyamatában **több AI-agent dolgozik egyszerre**, eltérő szinten:

```
┌──────────────────────────────────────────────────────────────┐
│  HUMÁN ARCHITEKT (Szántói Gábor)                             │
│  - Stratégiai döntések                                       │
│  - Prioritás                                                 │
│  - End-to-end review                                         │
└─────┬──────────────────┬──────────────────┬─────────────────┘
      │                  │                  │
      ▼                  ▼                  ▼
┌──────────┐      ┌──────────┐      ┌──────────────┐
│ Strategic│      │  Spec    │      │  Spec        │
│ Conductor│      │  Chat 1  │      │  Chat 2      │
│ (THIS)   │      │ (Cabinet │      │ (Cutting     │
│          │      │  0.2)    │      │  Phase 4)    │
└─────┬────┘      └────┬─────┘      └──────┬───────┘
      │ generálja      │ kiad v4-et         │ kiad v4-et
      │ promptokat     │                    │
      │                ▼                    ▼
      │           project knowledge (claude.ai)
      │                │                    │
      │                └────────┬───────────┘
      │                         │
      ▼                         ▼
┌────────────────────────────────────────────┐
│  Claude Code dispatcher (VPS, tmux)        │
│                                            │
│  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │
│  │Track │ │Track │ │Track │ │Track │      │
│  │  A   │ │  B   │ │  C   │ │  D   │ ...  │
│  └──────┘ └──────┘ └──────┘ └──────┘      │
│                                            │
│  Implementáció — kód, teszt, NuGet         │
└────────────────────────────────────────────┘
```

A SpaceOS-tempó (4023 teszt, Cabinet 0.1 1 óra alatt) ennek az **agent-orchestration** miatt lehet ilyen gyors. Egy ember **közvetlenül** nem tudna ilyen tempón dolgozni — de **strategic conductor** szerepben igen.

## Tervezés-párhuzamosítás 3 szintje

### 1. szint — Független modulok párhuzamosan

**Biztonságos.** Két különböző modul (pl. Cabinet 0.2 és Cutting Phase 4) tervezése **függetlenül** futhat, mert:
- Külön repo
- Külön NuGet csomag-család
- Külön DB schema
- A keresztmetszet a Kernel API surface, ami stabil

**Javasolt: max 2 párhuzamos**, mert a humán review-terhe ennyi.

### 2. szint — Egy modul több review-fázisa párhuzamosan

**Veszélyes.** A v1→v2→v3→v4 pipeline azért szekvenciális, mert minden review az előzőre épül. Ha v2 (persistence) és v3 (security) ugyanarra a v1-re épülne, a finding-ek konfliktusosak lehetnek.

**NEM javasolt** kivéve ha a modul kicsi és a review-területek diszjunktak.

### 3. szint — Tervezés és implementáció párhuzamosan **különböző** modulokon

**Legerősebb gyorsítás.** Példa:
- Cabinet 0.1 implementáció **fut** (Claude Code, VPS)
- Közben Cabinet 0.2 **arch-planner session** fut (claude.ai, Spec Chat 1)
- Közben Cutting Phase 4 **arch-planner session** fut (claude.ai, Spec Chat 2)

Mindhárom egyszerre, mert **független context**.

## A két párhuzamos limit

A felhasználó egy ember. Reálisan:

- **2 párhuzamos tervező-session**: kezelhető (napi 1500 sor markdown × 2 review)
- **3 párhuzamos**: nehéz, de lehetséges
- **4+**: minőség-vesztés, fáradt review, hibák becsúsznak

A **strategic conductor** segít abban, hogy a felhasználó **ne lépje túl** ezt a határt.

## A "1 órás Cabinet 0.1 implementáció" jelensége

A v4 dokumentumban a §16.1 track-bontás **27 napos sorozat**-ként szerepel. A valós implementáció **1 óra** volt.

Az **16-szoros gyorsítás** oka:
- A track-ek (A-H) **diszjunktak** voltak (track-bontás design pont ezt célozta)
- A Claude Code agent **párhuzamosan** futott minden track-en (tmux dispatcher)
- A teszt-első approach (TDD-szerű) miatt a verifikáció is automatikus volt

**Ez NEM minden modulra ilyen drámi gyorsítás.** A Cabinet 0.1 specifikusan ezt célozta meg. Egy bonyolultabb, sok cross-module függőséggel rendelkező modul nem fog ennyire skálázódni.

A **becslés-skálázás szabálya:**
- Track-bontható, diszjunkt scope: **8-16x gyorsítás** lehetséges
- Mérsékelt cross-track függőség: **3-5x**
- Sok keresztmetszet: **1-2x** (alig gyorsítás)

## Repo-szervezés és authoritative-rule

Két authoritative gép, **soha nem keveredhet**:

```
VPS (Linux, headless)              Windows asztal
─────────────────────              ──────────────
spaceos-kernel        AUTH         spaceos-kernel        mirror (read-only)
spaceos-orchestrator  AUTH         spaceos-orchestrator  mirror (read-only)
spaceos-portal        AUTH         spaceos-portal        mirror (read-only)
spaceos-modules-*     AUTH         spaceos-modules-*     mirror (read-only)
spaceos-docs          AUTH         spaceos-docs          mirror (read-only)
cabinetbilder-autocad mirror       cabinetbilder-autocad AUTH
                      (read-only)
```

**Read-only mirror védelem:** pre-commit hook minden mirror-on, ami `exit 1`-gyel lerombolja a véletlen commit-ot.

**Single source of truth elv:** minden fájlnak egy authoritative helye van. **Soha nem írunk ugyanabba a fájlba két különböző gépről.**

## A NuGet feed mint közlekedési tengely

A két authoritative gép közötti **kommunikáció** NuGet csomagon át megy:

```
VPS-en build: SpaceOS.Cabinet.Domain 0.1.0 .nupkg
       ↓
GitHub Packages (publish)
       ↓
Windows-on consume: dotnet add package SpaceOS.Cabinet.Domain --version 0.1.0
```

Ez azt is jelenti, hogy a **Modules.Cabinet kódot SOHA nem szerkesztjük Windows-ról**. Ha bug van, a VPS-en javítjuk, új NuGet csomagot publikálunk, és Windows-on `dotnet add package` újra.

## Tmux dispatcher orchestration mintája

A felhasználó VPS-én egy `tmux` szervezi az agent-eket:

```
tmux session: spaceos-development
├── window 0: dispatcher (root terminal)
│   - feladat-kiosztás a track-eknek
│   - status monitoring
│
├── window 1-N: track terminálok
│   ├── track-A: infra setup
│   ├── track-B: Geometry namespace
│   ├── track-C: Abstractions
│   ├── track-D: Domain
│   └── ...
│
└── window status: aggregált log + git status
```

A felhasználó a **dispatcher window-ban** ad új feladatot, és a track-terminálok parallel dolgoznak.

## Project knowledge méret-stratégia

A claude.ai project knowledge-be **kb. 25 MB** fér. Az aktuális SpaceOS knowledge ~8-12 MB (becsült). **Search precision degradation** már tapasztalható.

**Stratégia:**
- A project knowledge-ben **csak az aktuálisan releváns** dokumentumokat tartani
- Régebbi v1, v2, v3 verziókat **kivenni** ha van végleges v4
- **2-3 hónapnál régebbi** Codebase Status-okat archiválni (vagy git-be tenni)
- Az authoritative `spaceos-docs/` repo a hosszú-távú tárolóhely

## Single Source of Truth a dokumentumokra

```
spaceos-docs/                  ← AUTHORITATIVE (git, VPS-en)
    ├── manifesto/
    ├── adr/
    ├── vision/
    ├── architecture/
    └── README.md
         │
         ↓ (manuális szelekció)
project knowledge (claude.ai)  ← AKTÍV CACHE (legrelevánsabb 5-10 doc)
```

A claude.ai project knowledge **nem source-of-truth**, hanem **gyors-elérési cache**. Ha eltér, a `spaceos-docs/` repo nyer.

## Status-tracking ritmusa

Egy egészséges fejlesztési ritmus:

| Frekvencia | Tevékenység |
|---|---|
| **Naponta** | Codebase Status frissítés (új tesztszám, deployolt komponensek) |
| **Heti** | Strategic review (mit zártunk le, mit indítunk) |
| **Sprint-szint (~2 hét)** | Modul-szintű v4 dokumentum + implementáció |
| **Havi** | Project knowledge audit (mi elavult, mit kell archívba) |
| **Negyedéves** | Vision review (T1-T6 tenetek aktualitása, hosszú-távú prioritás) |

A `spaceos-conductor` skill főként a **napi/heti** szinten működik. A **havi/negyedéves** szintet a felhasználó kezdeményezi.
