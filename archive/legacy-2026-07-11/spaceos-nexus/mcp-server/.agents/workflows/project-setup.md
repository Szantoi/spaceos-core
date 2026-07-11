---
description: how to initialize a new Project with the correct folder structure (Program → Project → Milestone → Epic)
---

# Project Setup – Új Projekt Inicializálása

**Szerepkör:** Orchestrator / Tech Lead
**Trigger:** Új fejlesztési projekt indítása (pl. Discovery `validated` eredménye után, `goal.md` megvan)
**Forrás:** `Project_Folder_Structure_Standard.md`

> **Aranyszabály:** Csak annyi szintet hozz létre, amennyit a komplexitás indokol. A hierarchia "Epic-First" szemléletű: az Epic az atomi egység, a Milestone/Projekt/Program pedig opcionális konténerek (Trigger-alapú aktiválás).

---

## Lépések

### 1. Program szint döntés

Kérdezd meg: **Tartozik ez a projekt valamelyik meglévő Program alá?**

| Feltétel | Teendő |
|:---------|:-------|
| 2+ projekt logikailag összetartozik, közös állapottábla kell | → Program mappa szükséges |
| Önálló, független fejlesztés | → Kihagyható a Program szint |

**Ha Program szint kell:**

```
docs/<program-name>/
├── _program.md         ← Program Landing Page (Mi ez? Mely projektek tartoznak ide?)
└── program-state.md    ← Cross-projekt aggregált dashboard tábla
```

**`_program.md` kötelező tartalma:**
- Mi ez a Program és miért tartoznak össze a projektek?
- Melyik `docs/<project>/` mappák tartoznak bele?
- Közös függőségek / blocker-ek listája

**`program-state.md` kötelező tartalma:**

```markdown
| Sub-Project | Aktív Milestone | Státusz | Felelős |
|:------------|:----------------|:--------|:--------|
| `<project>` | M01: ...        | 📋 Planned | Architect |
```

### 2. Projekt alap struktúra létrehozása

```
docs/<project-name>/
├── _readme.md       ← Landing Page (összefoglaló, szabályok, hivatkozások)
├── goal.md          ← Célkitűzés, Scope (In/Out), Sikerkritériumok
├── state.md         ← Globális Dashboard (Milestone térkép, Epic Progress)
└── milestones/      ← Fejlesztési fázisok
```

**`_readme.md` tartalma:** Projekt neve, főbb fájlok listája, alapvető szabályok, link a `Plans/` kutatási dokumentumokra.

**`goal.md` kötelező elemei:**
- Üzleti és technológiai célok
- **In Scope** (mit csinálunk)
- **Out of Scope** (mit NEM csinálunk)
- Mérhető Sikerkritériumok

**`state.md` kötelező elemei:**
```markdown
# Project State

## Project Overview
| Total Milestones | Total Epics | Aktív Epic | Felelős |
|:--|:--|:--|:--|
| X | Y | EPIC-XX | Tech Lead |

## Milestone Map
| ID | Cím | Státusz |
|:---|:----|:--------|
| M01 | ... | 📋 Planned |

## Epic State Map
| ID | Title | State | Felelős |
|:---|:------|:------|:--------|
| EPIC-01 | ... | BACKLOG_READY | Backend Dev |
```

Ha Program-hoz tartozik, a `state.md` front-matter-be:
```yaml
program: <program-name>
program_state: docs/<program-name>/program-state.md
```

### 3. Milestone-ok megtervezése

A `goal.md` alapján bontsd fel a projektet Milestone-okra. Minden Milestone egy tesztelhető inkrementum.

**Sub-Milestone szükséges, ha:**
- A Milestone > 6 Epic-et tartalmaz
- A fázisok egymástól függetlenül szállíthatók

```
milestones/
├── milestone_01/
│   ├── plan.md               ← Milestone célja, fókusza, "kész" feltétele, Epic lista
│   ├── epic_01/              ← → lásd: epic-setup.md workflow
│   └── epic_02/
└── milestone_02/
    └── ...
```

**`milestone_X/plan.md` kötelező tartalma:**
- Milestone pontos célja és fókuszpontjai
- "Kész" feltétele (mikor tekinthető lezártnak?)
- Az alá tartozó Epic-ek listája

**Sub-Milestone esetén:**
```
milestone_01/
├── plan.md                    ← Fő terv (felsorolja a sub-milestone-okat)
├── sub_milestone_01a/
│   ├── sub-plan.md
│   └── epic_01/
└── sub_milestone_01b/
    ├── sub-plan.md
    └── epic_02/
```

### 4. Első Epic-ek létrehozása

Az aktuális Milestone alatt hozd létre az érintett Epic mappákat:
→ Menj az **`epic-setup.md`** workflow-ba

### 5. `state.md` frissítése

Minden létrehozott Milestone és Epic után frissítsd a projekt `state.md`-jét és a Program `program-state.md`-jét (ha van).

### 6. Definition of Done

- [ ] `docs/<project>/` mappa létrehozva
- [ ] `_readme.md`, `goal.md`, `state.md` kitöltve
- [ ] Program szint (ha szükséges): `_program.md`, `program-state.md` létrehozva
- [ ] Legalább 1 Milestone `plan.md`-del létrehozva
- [ ] `state.md` Milestone Map és Epic State Map kitöltve

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Landing Page | `docs/<project>/_readme.md` |
| Célkitűzés | `docs/<project>/goal.md` |
| Dashboard | `docs/<project>/state.md` |
| Milestone terv | `docs/<project>/milestones/milestone_XX/plan.md` |
| Program Landing (opcionális) | `docs/<program>/_program.md` |

## Következő lépés

→ **Epic létrehozáshoz:** `epic-setup.md`
→ **Fejlesztés indításához:** `orchestrator-epic-execution.md`
