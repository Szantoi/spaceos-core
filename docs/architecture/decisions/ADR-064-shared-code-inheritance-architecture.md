# ADR-064: Megosztott Kód Öröklődési Architektúra

**Státusz:** ACCEPTED
**Dátum:** 2026-07-11
**Döntéshozó:** Root

---

## Kontextus

A SpaceOS fejlesztése során több általánosítható komponens alakult ki:
- **Nexus** — Agent orchestration platform (fejlesztési eszköz)
- **TaskOS** — Feladatmenedzsment
- **ERP** — Üzleti modulok
- **SpaceOS** — Építészeti/térkialakítási OS

**Pragmatikus megközelítés:** A termékek egymásból inspirálódhatnak, de az általánosítás mértékét a gyakorlat mutatja meg. Ne darabolj túl korán — hagyd, hogy a használat során derüljön ki, mi az amit általánosan kezelhetünk.

---

## Döntés

**Hierarchikus termékstruktúra** tiszta öröklődéssel:

```
┌─────────────────────────────────────────────────────────────────────┐
│                           NEXUS                                      │
│                (Agent Orchestration Platform)                        │
│                                                                      │
│   CLI Integration:                                                   │
│   ┌──────────────┬──────────────┬──────────────┬──────────────┐     │
│   │ Claude Code  │  Google ADK  │    Codex     │     ...      │     │
│   └──────────────┴──────────────┴──────────────┴──────────────┘     │
│                                                                      │
│   Agent Services:                                                    │
│   • Identity — Agent azonosság és jogosultságok                      │
│   • Memory — Hosszútávú kontextus megőrzés                           │
│   • Knowledge — Tudástár és RAG keresés                              │
│   • Goals — Célok definiálása és követése (goal drift prevention)    │
│   • Federation — Multi-agent koordináció                             │
│   • Pipeline — Automatizált munkafolyamatok                          │
│   • Session — CLI session menedzsment                                │
│                                                                      │
│                        @nexus/platform                               │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
    ┌─────────────────┼─────────────────┬─────────────────┐
    │                 │                 │                 │
    ▼                 ▼                 ▼                 ▼
┌────────┐      ┌──────────┐      ┌──────────┐      ┌──────────┐
│ TaskOS │      │   ERP    │      │ SpaceOS  │      │ [Más...] │
└───┬────┘      └────┬─────┘      └────┬─────┘      └──────────┘
    │                │                 │
    │                │                 │
    ▼                ▼                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                           TASKOS                                     │
│              (Általános Feladatmenedzsment Eszköz)                   │
│                                                                      │
│   • Tasks — Feladatok létrehozása, státusz, prioritás                │
│   • Workflow — Folyamat definíció, lépések, átmenetek                │
│   • Scheduling — Ütemezés, határidők, emlékeztetők                   │
│   • Tracking — Előrehaladás követés, riportok                        │
│   • Dependencies — Feladat függőségek kezelése                       │
│   • Notifications — Értesítések, eszkalációk                         │
│                                                                      │
│   Nexus-t használja agent infrastruktúrához                          │
│                                                                      │
│                        @taskos/core                                  │
└─────────────────────────────────────────────────────────────────────┘
    │
    │ TaskOS-t használják:
    ├───────────────────────────────────────┐
    │                                       │
    ▼                                       ▼
┌─────────────────────────────────────┐ ┌─────────────────────────────┐
│              ERP                     │ │          SpaceOS            │
│    (Általános Orbit Eszköztár)         │ │  (Építészeti/Térkialakítási │
│                                      │ │     Operációs Rendszer)     │
│ • CRM — Ügyfélkapcsolat-kezelés      │ │                             │
│ • HR — Humán erőforrás               │ │ • Térszervezés — helyiségek │
│ • Inventory — Készletkezelés         │ │ • BIM Management            │
│ • Orders — Rendeléskezelés           │ │ • Parametrikus tervezés     │
│ • Invoicing — Számlázás              │ │                             │
│ • Kontrolling — Pénzügyi kontroll    │ │ Használja: @taskos/core     │
│ • Maintenance — Karbantartás         │ │                             │
│ • QA — Minőségbiztosítás             │ │       @spaceos/core         │
│ • DMS — Dokumentumkezelés            │ └──────────────┬──────────────┘
│                                      │                │
│ Használja: @taskos/core              │                │
│                                      │                │
│           @orbit/core                  │                │
└──────────────────┬───────────────────┘                │
                   │                                    │
     ┌─────────────┼─────────────┐                      │
     │             │             │                      │
     ▼             ▼             ▼                      │
┌─────────┐ ┌─────────┐ ┌─────────┐                     │
│[Pékség] │ │[Vendég- │ │ [Más...}│                     │
│         │ │ látás]  │ │         │                     │
└─────────┘ └─────────┘ └─────────┘                     │
                                                        │
┌───────────────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────────────────────────────┐
│                        JOINERYTECH                                   │
│                    (Faipari SaaS Platform)                           │
│                                                                      │
│   Asztalos vertikum: bútorgyártás, ajtó/ablak, szekrény              │
│                                                                      │
│   Használja: @orbit/core + @spaceos/core (mindkettő @taskos/core-ral)  │
│                                                                      │
│                    @joinerytech/platform                             │
└─────────────────────────┬───────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         DOORSTAR                                     │
│                  (JoineryTech Ügyfél Instance)                       │
│                                                                      │
│   Doorstar Kft. specifikus konfiguráció és testreszabás              │
│   6-STAGE workflow, Cabinet-VPS kommunikáció                         │
│                                                                      │
│                      Doorstar deployment                             │
└─────────────────────────────────────────────────────────────────────┘
```

### Termék Hierarchia

| Termék | Típus | Használja | Leírás |
|--------|-------|-----------|--------|
| **Nexus** | Agent Orchestration Platform | — | CLI integráció (Claude Code, ADK, Codex), agent identity/memory/goals |
| **TaskOS** | Általános Feladatmenedzsment | Nexus | Tasks, Workflow, Scheduling, Tracking, Dependencies |
| **Orbit** | Általános Orbit Eszköztár | TaskOS | CRM, HR, Inventory, Orders, Invoicing, Kontrolling, stb. |
| **SpaceOS** | Építészeti/Térkialakítási OS | TaskOS | Térszervezés, BIM, parametrikus tervezés |
| **JoineryTech** | Faipari SaaS Platform | Orbit + SpaceOS | Asztalos vertikum |
| **Doorstar** | Ügyfél instance | JoineryTech | Doorstar Kft. — ajtógyártó |
| **[Pékség]** | Vertikális platform | Orbit | Pékség-specifikus workflow |
| **[Vendéglátás]** | Vertikális platform | Orbit | Vendéglátás-specifikus workflow |

### Nexus Szolgáltatások

| Szolgáltatás | Leírás |
|--------------|--------|
| **Identity** | Agent azonosság, szerepkörök, jogosultságok |
| **Memory** | Hosszútávú kontextus, session state, handoff |
| **Knowledge** | Tudástár, RAG keresés, embedding |
| **Goals** | Cél definiálás, tracking, goal drift prevention |
| **Federation** | Multi-agent koordináció, inbox/outbox |
| **Pipeline** | Automatizált workflow, nightwatch, review |
| **Session** | CLI session lifecycle, tmux management |

---

## Architektúra Részletek

### 1. Package Struktúra

```
/opt/nexus/                       # NEXUS — Önálló Agent Orchestration Platform
├── packages/
│   └── platform/                # @nexus/platform
│       ├── package.json
│       └── src/
│           ├── mcp/             # MCP tool framework
│           ├── federation/      # Federation protokoll
│           ├── knowledge/       # Tudástár kezelés
│           ├── cli/             # CLI menedzsment (Claude Code, ADK, Codex)
│           ├── identity/        # Agent azonosság
│           ├── memory/          # Hosszútávú kontextus
│           ├── goals/           # Goal tracking, drift prevention
│           └── pipeline/        # Pipeline infrastruktúra
│
/opt/taskos/                      # TASKOS — Általános Feladatmenedzsment
├── packages/
│   └── core/                    # @taskos/core
│       ├── package.json         # depends: @nexus/platform
│       └── src/
│           ├── tasks/           # Feladat létrehozás, státusz
│           ├── workflow/        # Folyamat definíció, lépések
│           ├── scheduling/      # Ütemezés, határidők
│           ├── tracking/        # Előrehaladás követés
│           ├── dependencies/    # Feladat függőségek
│           └── notifications/   # Értesítések, eszkalációk
│
/opt/orbit/                         # Orbit — Általános Orbit Eszköztár
├── packages/
│   └── core/                    # @orbit/core
│       ├── package.json         # depends: @taskos/core
│       └── src/
│           ├── crm/             # Ügyfélkapcsolat-kezelés
│           ├── hr/              # Humán erőforrás
│           ├── inventory/       # Készletkezelés
│           ├── orders/          # Rendeléskezelés
│           ├── invoicing/       # Számlázás
│           ├── kontrolling/     # Pénzügyi kontroll
│           ├── maintenance/     # Karbantartás
│           ├── qa/              # Minőségbiztosítás
│           └── dms/             # Dokumentumkezelés
│
/opt/spaceos/                     # SPACEOS — Építészeti/Térkialakítási OS
├── packages/
│   └── core/                    # @spaceos/core
│       ├── package.json         # depends: @taskos/core
│       └── src/
│           ├── space/           # Térszervezés, helyiségek
│           ├── bim/             # BIM management
│           ├── parametric/      # Parametrikus tervezés
│           └── integrations/    # CAD/BIM integrációk
│
/opt/joinerytech/                 # JOINERYTECH — Faipari Platform
├── packages/
│   └── platform/                # @joinerytech/platform
│       ├── package.json         # depends: @orbit/core + @spaceos/core
│       └── src/
│           ├── joinery/         # Asztalos-specifikus logika
│           ├── cutting/         # Lapszabászat
│           ├── assembly/        # Összeszerelés
│           └── production/      # Gyártás workflow
│
/opt/doorstar/                    # DOORSTAR — Ügyfél Instance
├── config/                      # Doorstar-specifikus konfiguráció
│   ├── 6-stage.yaml            # 6-STAGE workflow config
│   └── cabinet-vps.yaml        # Cabinet-VPS integráció
└── extensions/                  # Ügyfél-specifikus bővítmények (opcionális)
```

### Fejlesztési Felelősség

| Package | Fejlesztési hely | Felelősség |
|---------|------------------|------------|
| `@nexus/platform` | **/opt/nexus** | Agent orchestration, MCP, federation, CLI integration |
| `@taskos/core` | **/opt/taskos** | Feladatmenedzsment, workflow, scheduling |
| `@orbit/core` | **/opt/erp** | CRM, HR, Inventory, Orders, Kontrolling |
| `@spaceos/core` | **/opt/spaceos** | Térszervezés, BIM, parametrikus tervezés |
| `@joinerytech/platform` | **/opt/joinerytech** | Faipari modulok, production workflow |
| Doorstar config | **/opt/doorstar** | Ügyfél konfiguráció, 6-STAGE |

### 2. Dependency Lánc

```
@nexus/platform            ← Agent Orchestration (legalsó réteg)
       │
       │ npm dependency
       ▼
@taskos/core               ← Általános Feladatmenedzsment
       │
       ├───────────────────┬─────────────────────┐
       │                   │                     │
       ▼                   ▼                     ▼
@orbit/core             @spaceos/core         [más termékek]
       │                   │
       │                   │
       └─────────┬─────────┘
                 │
                 ▼
      @joinerytech/platform
                 │
                 │ deployment uses
                 ▼
             Doorstar
```

### 3. Verzió Kezelés

```json
// /opt/nexus/packages/platform/package.json
{
  "name": "@nexus/platform",
  "version": "1.0.0",
  "main": "dist/index.js",
  "description": "Agent Orchestration Platform - CLI integration, identity, memory, goals"
}

// /opt/taskos/packages/core/package.json
{
  "name": "@taskos/core",
  "version": "1.0.0",
  "dependencies": {
    "@nexus/platform": "^1.0.0"
  },
  "description": "General Task Management - workflow, scheduling, tracking"
}

// /opt/orbit/packages/core/package.json
{
  "name": "@orbit/core",
  "version": "1.0.0",
  "dependencies": {
    "@taskos/core": "^1.0.0"
  },
  "description": "General Orbit Toolkit - CRM, HR, Inventory, Orders, Kontrolling"
}

// /opt/spaceos/packages/core/package.json
{
  "name": "@spaceos/core",
  "version": "1.0.0",
  "dependencies": {
    "@taskos/core": "^1.0.0"
  },
  "description": "Architecture & Space Planning OS - BIM, parametric design"
}

// /opt/joinerytech/packages/platform/package.json
{
  "name": "@joinerytech/platform",
  "version": "1.0.0",
  "dependencies": {
    "@orbit/core": "^1.0.0",
    "@spaceos/core": "^1.0.0"
  },
  "description": "Woodworking SaaS Platform - joinery, cutting, assembly"
}
```

### 4. Frissítési Folyamat

```bash
# 1. Nexus platform frissítés (legalsó réteg)
cd /opt/nexus/packages/platform
npm version patch
npm publish --registry=local

# 2. TaskOS frissítése (Nexus-t használja)
cd /opt/taskos
npm update @nexus/platform
npm run build
npm version patch
npm publish --registry=local

# 3. ERP frissítése (TaskOS-t használja)
cd /opt/erp
npm update @taskos/core
npm run build
npm version patch
npm publish --registry=local

# 4. SpaceOS frissítése (TaskOS-t használja)
cd /opt/spaceos
npm update @taskos/core
npm run build
npm version patch
npm publish --registry=local
systemctl restart spaceos-knowledge

# 5. JoineryTech frissítése (ERP + SpaceOS-t használja)
cd /opt/joinerytech
npm update @orbit/core @spaceos/core
npm run build
systemctl restart joinerytech-knowledge

# 6. Doorstar frissítése (JoineryTech-et használja)
cd /opt/doorstar
npm update @joinerytech/platform
systemctl restart doorstar-knowledge
```

### 5. Frissítés Propagálás

```bash
#!/bin/bash
# /opt/nexus/scripts/propagate-update.sh
# Nexus frissítés propagálása a teljes láncon keresztül

echo "=== Full Product Chain Update Propagation ==="

# 1. TaskOS frissítése (Nexus → TaskOS)
echo "1. Updating TaskOS..."
cd /opt/taskos && npm update @nexus/platform && npm run build

# 2. ERP frissítése (TaskOS → Orbit)
echo "2. Updating ERP..."
cd /opt/erp && npm update @taskos/core && npm run build

# 3. SpaceOS frissítése (TaskOS → SpaceOS)
echo "3. Updating SpaceOS..."
cd /opt/spaceos && npm update @taskos/core && npm run build

# 4. JoineryTech frissítése (ERP + SpaceOS → JoineryTech)
echo "4. Updating JoineryTech..."
cd /opt/joinerytech && npm update @orbit/core @spaceos/core && npm run build

# 5. Doorstar frissítése (JoineryTech → Doorstar)
echo "5. Updating Doorstar..."
cd /opt/doorstar && npm update @joinerytech/platform

# 6. Szolgáltatások újraindítása
echo "6. Restarting services..."
systemctl restart spaceos-knowledge
systemctl restart joinerytech-knowledge
systemctl restart doorstar-knowledge

echo "=== Propagation Complete ==="
```

---

## Implementációs Terv

### Phase 1: Nexus Platform Kiválasztás

**Cél:** Nexus kiválasztása önálló Agent Orchestration Platform-ként

1. `/opt/nexus/packages/platform/` létrehozása
2. Agent-specifikus kód kiemelése:
   - `mcp/` → MCP tool framework
   - `federation/` → Federation protokoll
   - `knowledge/` → Tudástár kezelés
   - `cli/` → CLI menedzsment (Claude Code, ADK, Codex integráció)
   - `identity/` → Agent azonosság
   - `memory/` → Hosszútávú kontextus
   - `goals/` → Goal tracking, drift prevention
   - `pipeline/` → Pipeline infrastruktúra
3. `@nexus/platform` npm package
4. Local npm registry setup (Verdaccio)

### Phase 2: TaskOS Core Package

**Cél:** Általános feladatmenedzsment kiválasztása

1. `/opt/taskos/packages/core/` létrehozása
2. Feladatmenedzsment kód:
   - `tasks/` → Feladat CRUD, státusz
   - `workflow/` → Folyamat definíció
   - `scheduling/` → Ütemezés, határidők
   - `tracking/` → Előrehaladás követés
   - `dependencies/` → Függőségek kezelése
   - `notifications/` → Értesítések
3. `@nexus/platform` dependency hozzáadása
4. `@taskos/core` npm package

### Phase 3: ERP Core Package

**Cél:** Általános ERP eszköztár csomagolása

1. `/opt/orbit/packages/core/` létrehozása
2. ERP modulok:
   - CRM, HR, Inventory, Orders, Invoicing
   - Kontrolling, Maintenance, QA, DMS
3. `@taskos/core` dependency hozzáadása
4. `@orbit/core` npm package

### Phase 4: SpaceOS Core Package

**Cél:** Építészeti/Térkialakítási OS csomagolása

1. `/opt/spaceos/packages/core/` létrehozása
2. Építészeti modulok:
   - `space/` → Térszervezés
   - `bim/` → BIM management
   - `parametric/` → Parametrikus tervezés
3. `@taskos/core` dependency hozzáadása
4. `@spaceos/core` npm package

### Phase 5: JoineryTech Platform Package

**Cél:** Faipari platform csomagolása

1. `/opt/joinerytech/packages/platform/` létrehozása
2. Faipari modulok: joinery, cutting, assembly, production
3. `@orbit/core` + `@spaceos/core` dependency hozzáadása
4. `@joinerytech/platform` npm package

### Phase 6: Doorstar Konfiguráció

**Cél:** Ügyfél instance beállítása

1. `/opt/doorstar/config/` konfiguráció
2. 6-STAGE workflow config
3. Cabinet-VPS integráció config
4. `@joinerytech/platform` dependency

### Phase 7: CI/CD és Propagálás

1. `propagate-update.sh` szkript
2. Automatikus verziókezelés
3. Rollback mechanizmus
4. Health check minden szinten

---

## Alternatívák

### ALT-1: Git Submodules
- **Előny:** Egyszerű verziókezelés
- **Hátrány:** Bonyolult merge konfliktusok, nehéz dependency kezelés
- **Döntés:** Elvetva

### ALT-2: Monorepo (nx/turborepo)
- **Előny:** Modern tooling, gyors build
- **Hátrány:** Túl komplex a jelenlegi mérethez
- **Döntés:** Későbbre halasztva (ha nő a csapat)

### ALT-3: Symlinks
- **Előny:** Egyszerű, azonnali
- **Hátrány:** Nem verziókezelt, törékeny
- **Döntés:** Átmeneti megoldásként elfogadható

---

## Átmeneti Megoldás (Symlinks)

Amíg a teljes NPM package architektúra nincs kész, **symlink alapú megosztás**:

```bash
# Agent Core forrás: NEXUS
/opt/nexus/nexus-core/knowledge-service/src/  # Agent Management Tool

# Symlinked projektek — mindenki a Nexus agent-core-t használja
ln -s /opt/nexus/nexus-core/knowledge-service/src/mcp \
      /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp

ln -s /opt/nexus/nexus-core/knowledge-service/src/federation \
      /opt/spaceos/spaceos-nexus/knowledge-service/src/federation

ln -s /opt/nexus/nexus-core/knowledge-service/src/pipeline \
      /opt/spaceos/spaceos-nexus/knowledge-service/src/pipeline

# JoineryTech és Doorstar is a láncból örököl
# (SpaceOS-en keresztül vagy direktben a Nexus-ból)
```

**Frissítés:**
```bash
# Nexus agent-core fejlesztés
cd /opt/nexus/nexus-core/knowledge-service
npm run build

# Propagálás a láncon
systemctl restart spaceos-knowledge
systemctl restart joinerytech-knowledge
systemctl restart doorstar-knowledge
```

---

## Következmények

### Pozitív
- **Nexus önálló termék** — Agent Management Tool bárki számára elérhető
- **Tiszta hierarchia** — Nexus → SpaceOS → JoineryTech → Doorstar
- **Automatikus propagálás** — Frissítések végigmennek a láncon
- **Skálázható** — Új platform = SpaceOS-re épül, új ügyfél = JoineryTech-re épül
- **Újrafelhasználható** — Nexus más projektekben is használható

### Negatív
- **Kezdeti munka** — Nexus kiválasztása a SpaceOS-ből
- **Több package** — 3 szintű dependency lánc kezelése
- **Dependency hell kockázat** — Verzió konfliktusok
- **Local npm registry** — Verdaccio setup és karbantartás

### Semleges
- **Tanulási görbe** — NPM package pattern elsajátítása

---

## Termék Összefoglaló

| Termék | Típus | Dependency | Publikál |
|--------|-------|------------|----------|
| **Nexus** | Agent Orchestration Platform | — | `@nexus/platform` |
| **TaskOS** | Általános Feladatmenedzsment | `@nexus/platform` | `@taskos/core` |
| **ERP** | Általános Orbit Eszköztár | `@taskos/core` | `@orbit/core` |
| **SpaceOS** | Építészeti/Térkialakítási OS | `@taskos/core` | `@spaceos/core` |
| **JoineryTech** | Faipari SaaS Platform | `@orbit/core` + `@spaceos/core` | `@joinerytech/platform` |
| **Doorstar** | Ügyfél Instance | `@joinerytech/platform` | — (config only) |

---

## Döntés Státusz

**ACCEPTED** — Pragmatikus megközelítés elfogadva.

**Alapelv:**
- A termékek egymásból inspirálódnak
- Az általánosítás mértékét a gyakorlat mutatja meg
- Ne darabolj túl korán — a használat során derül ki, mi általánosítható

**Jelenlegi struktúra:**
- **3 core termék:** TaskOS, Orbit, SpaceOS
- **Vertikális platformok:** JoineryTech (Orbit + SpaceOS + gyártás)
- **Ügyfél instance-ok:** Doorstar
- **Fejlesztési eszköz:** Nexus (opcionálisan beépíthető)

---

_ADR-064 — Megosztott Kód Öröklődési Architektúra v1.4 — Pragmatikus határok — 2026-07-11_
