# CLAUDE.md — SpaceOS Root terminál

> A root terminál tervez, koordinál és ellenőriz.
> **Soha nem ír kódot.** Kódot csak a projekt terminálok írnak.

---

## PROJEKT VÍZIÓ — ÖSSZEFOGLALÓ

> A SpaceOS a **magyar faipar digitális gerince** — egy iparspecifikus SaaS platform, amely az
> ajtógyártókat, szekrénygyártókat, lapszabászokat, kereskedőket és beszerelőket egyetlen
> összekapcsolt ökoszisztémába szervezi.

### Miért épül?

A faiparos KKV-k 90%+ ma Viber + Excel + telefon alapon koordinál. Nincs rájuk szabott,
megfizethető digitális megoldás. A SpaceOS ezt az űrt tölti be.

### Első éles ügyfél

**Doorstar Kft.** (ajtógyártó) — Soft Launch: **2026 Q2**

### Rendszer felépítése (4 réteg)

```
L4  Design Portal / JoineryTech   React 18 — brand-specifikus UI-k
L3  Orchestrator (BFF)            Node.js 22 — LLM Tool Calling, API gateway
L2  Modules (Drivers)             .NET 8 — iparági üzleti logika (Joinery, MEP, Pricing)
L1  Kernel                        .NET 8 + PostgreSQL — auth, audit, FSM, escrow
```

### 5 Golden Rule (minden döntésnél kötelező)

| # | Szabály |
|---|---|
| 1 | **Data → Rules → Geometry** — frontend rajzol, C# Driver számol, LLM csak paramétereket ad |
| 2 | **Modular Monolith** — Kernel `IParametricProduct` interfészen dolgozik, nem tudja mi az asztalos |
| 3 | **Immutability & Trust** — nincs UPDATE CAD adatokon, minden SHA-256 hashed audit eventtel |
| 4 | **Need-to-Know RBAC** — megrendelő nem látja a gyártó belső anyaglistáját |
| 5 | **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb mélyül |

### Célpiac és roadmap

- **HU célpiac:** 1300–2500 cég (ajtó, szekrény, lapszabász, ablak, kereskedő)
- **2026 Q2:** Doorstar Soft Launch · **2026 Q3:** Szabászat modul + 2. ügyfél
- **2027:** 5+ éles ügyfél, DACH belépés

> Teljes vízió: `docs/vision/SpaceOS_Vision_Results_20260413.md` | Technikai master: `docs/vision/SpaceOS_Vision_Master.md`

---

## TERMINÁL ARCHITEKTÚRA

```
ROOT  /opt/spaceos/                          ← ez a terminál
  ├── KERNEL       /SpaceOS.Kernel/
  ├── ORCH         /spaceos-orchestrator/
  ├── PORTAL       /design-portal/           (Turborepo monorepo)
  ├── FE           /spaceos-doorstar-portal/  (Doorstar brand portal)
  ├── JOINERY      /spaceos-modules-joinery/
  ├── ABSTRACTIONS /spaceos-modules-abstractions/
  ├── CUTTING      /spaceos-modules-cutting/
  ├── INVENTORY    /spaceos-modules-inventory/
  ├── PROCUREMENT  /spaceos-modules-procurement/
  ├── E2E          /e2e/
  ├── INFRA        /infra/
  ├── LIBRARIAN    /spaceos-librarian/        (tudásbázis gondozó, nem ír kódot)
  ├── TESTER       /tester/                   (manuális tesztelés, nem ír kódot)
  └── ARCHITECT    /spaceos-architect/        (konzultatív arch partner, nem ír kódot)
```

Minden terminálnak saját CLAUDE.md-je van. Teljes workflow: `/opt/spaceos/docs/WORKFLOW.md`

---

## KÖTELEZŐ PIPELINE — ROOT FELADATOKRA

⚠️ Minden lépés kötelező. Kihagyni TILOS.

```
OUTBOX OLVASÁS → DÖNTÉS → INBOX ÍRÁS → CODEBASE_STATUS FRISSÍTÉS
```

### 1. OUTBOX OLVASÁS (session elején)
```bash
# UNREAD keresés (minden terminál egyszerre)
grep -rl "status: UNREAD" docs/mailbox/*/outbox/ 2>/dev/null

# Manuális ellenőrzés ha szükséges
ls docs/mailbox/kernel/outbox/
ls docs/mailbox/orchestrator/outbox/
ls docs/mailbox/portal/outbox/
ls docs/mailbox/joinery/outbox/
ls docs/mailbox/abstractions/outbox/
ls docs/mailbox/e2e/outbox/
ls docs/mailbox/infra/outbox/
ls docs/mailbox/tester/outbox/
```

### 2. DÖNTÉS
- `DONE` üzenet → elfogadás vagy visszadobás
- `BLOCKED` üzenet → válasz a kérdésre, new inbox üzenet
- `QUESTION` üzenet → döntés + válasz

### 3. INBOX ÍRÁS (ha új feladat kell)
- Fájlnév: `YYYY-MM-DD_NNN_[slug].md`
- Mappa: `docs/mailbox/<projekt>/inbox/`
- Frontmatter kötelező (id, from, to, type, priority, status: UNREAD)

### 4. CODEBASE_STATUS.MD FRISSÍTÉS
- Minden elfogadott DONE után frissítsd: `docs/Codebase_Status.md`
- Teszt számok, sprint státusz, deployment státusz

---

## FELADAT TÍPUSOK ÉS TEENDŐK

| Beérkező üzenet | Root teendő |
|---|---|
| `status: DONE` — minden OK | Elfogadás, Codebase_Status.md frissítés, következő feladat |
| `status: DONE` — de hiányos | Visszadobás: új inbox üzenet konkrét hiánylistával |
| `status: BLOCKED` | Döntés/válasz: új inbox üzenet a blokkolt terminálnak |
| `status: QUESTION` | Válasz: új inbox üzenet `type: answer`-rel |
| `type: report` (TESTER outbox) | Bug feldolgozás → prioritás → PORTAL/INFRA task kiadás |
| `type: done` (TESTER outbox) | Teszt session lezárult → nyitott bugokat task-ként kiadja |
| `type: response` (ARCHITECT outbox) | Spec beépítése a következő terminál inbox üzenetébe |

---

## ARCHITECT TERMINÁL — MIKOR HÍVD

Az Architect konzultatív partner. Root **opcionálisan** hívhatja mielőtt komplex inbox üzenetet ír:

```bash
# Architect inbox: következő sorszám lekérdezése
ls docs/mailbox/architect/inbox/ | sort | tail -1
```

**Mikor érdemes Architectet bevonni:**
- Új cross-module interfész definiálásakor (pl. event bus, provider contract)
- Ha Root nem biztos a meglévő kódbázis mintájában
- Komplex domain döntésnél (aggregate root vs. value object, FSM tervezés)
- >5 napos implementációs feladat spec-je előtt

**Mikor NEM szükséges:**
- Egyszerű bugfix, kis feature
- A spec már kész és egyértelmű
- Gyors koordinációs döntések

**Architect mailbox:** `docs/mailbox/architect/inbox/` és `.../outbox/`

---

## CROSS-PROJECT SORREND

Ha egy epic több projektet érint:

```
Kernel → Orchestrator → Portal    (backend → middleware → frontend)
Kernel → Abstractions             (core domain first)
Infra  párhuzamosan fut a kód tracktől
```

Következő projektet csak akkor kiosztani, ha az előző DONE.

---

## FONTOS SZABÁLYOK

1. **Root soha nem ír kódot** — tervez, koordinál, ellenőriz
2. **Minden döntés dokumentált** — inbox/outbox üzenetek az audit trail
3. **BLOCKED üzenet 24 órán belül választ kap** — ne hagyd függőben
4. **Codebase_Status.md mindig naprakész** — ez az egyetlen igazságforrás
5. **Archive**: lezárt üzeneteket `archive/` mappába mozgatni

---

## FELADAT STÁTUSZ (FSN — docs/tasks/)

A `docs/tasks/` mappa a root feladatnézete. Minden tervdokumentum és kiadott feladat itt van nyilvántartva státusz szerint.

```
docs/tasks/
  README.md       ← dashboard (mindig naprakész)
  new/            ← tervdok kész, terminálnak még nem kiadva
  active/         ← inbox elment, terminál vagy operátor dolgozik rajta
  archive/        ← DONE + elfogadott, lezárt
```

### FSN munkafolyamat

| Esemény | Teendő |
|---|---|
| Új tervdokumentum készül (`docs/`) | Task fájl létrehozása `new/`-ban |
| Root kiadja terminálnak (inbox üzenet) | Task fájl mozgatása `new/` → `active/` |
| Terminál DONE outbox-a elfogadva | Task fájl mozgatása `active/` → `archive/` |
| Visszadobás (hiányos DONE) | Task fájl marad `active/`-ban, megjegyzés hozzáadva |

### Fájlnév konvenció
```
<EPIC-ID>_<slug>.md
pl: JOINERY-V2_pdf-gyartasilap.md
    INFRA-KC01_keycloak-vps-setup.md
```

### Task fájl frontmatter
```yaml
---
id: EPIC-ID
title: Feladat neve
status: new | active | archive
priority: high | medium | low
assignee: terminál neve vagy "VPS Operator"
epic: epic-slug
blocked_by: mi blokkolja (ha van)
created: YYYY-MM-DD
updated: YYYY-MM-DD
docs:
  - docs/relevant-file.md
---
```

### Session elején: FSN ellenőrzés
```bash
ls docs/tasks/new/
ls docs/tasks/active/
```

---

## KÖZÖS ERŐFORRÁSOK

| Fájl | Tartalom |
|---|---|
| `docs/Codebase_Status.md` | Minden modul státusza, teszt számok, sprint roadmap |
| `docs/tasks/README.md` | Feladatok dashboard (new / active / archive) |
| `docs/WORKFLOW.md` | Teljes munka módszertan, pipeline definíciók |
| `docs/mailbox/` | Minden terminál inbox/outbox/archive |
| `infra/CLAUDE.md` | Infra terminál szabályai |
| `docs/vision/SpaceOS_Vision_Results_20260413.md` | **Projekt vízió** — üzleti kontextus, Doorstar first customer, célpiac, roadmap |
| `docs/vision/SpaceOS_Vision_Master.md` | **Technikai master overview** — 4 réteg, 5 Golden Rule, domain modell, döntési fa |
| `.claude/skills/spaceos-root/` | **`/spaceos-root` skill** — root session ritual: outbox olvasás, döntési mátrix, inbox írás, task lifecycle |
| `.claude/skills/spaceos-terminal/` | **`/spaceos-terminal` skill** — terminál session ritual: inbox olvasás, build/test gate, DONE/BLOCKED outbox |
| `.claude/skills/spaceos-librarian/` | **`/spaceos-librarian` skill** — tudásbázis gondozó ritual: feldolgozási napló, knowledge doc írás |

---

## TUDÁSBÁZIS (`docs/knowledge/`)

A Librarian terminál által karbantartott, szintetizált tudás. **Minden terminál használhatja hideg indításhoz és kontextus építéshez.**

```
docs/knowledge/
  INDEX.md                              ← ELSŐ olvasnivaló: minden doc összefoglalója
  security/
    SECURITY_PATTERNS.md                ← JWT/RBAC, RLS, SSRF, CVE minták
    SECURITY_DECISIONS.md               ← Sprint 6 review döntései indoklással
  deployment/
    DEPLOYMENT_RUNBOOK.md               ← VPS deploy lépésről lépésre, env fájlok, portok
    KNOWN_GOTCHAS.md                    ← 15 csapda amit átéltünk (MapInboundClaims, GUC, stb.)
  patterns/
    DEV_DIFFICULTIES.md                 ← Visszatérő problémák és megoldásaik
    DATABASE_PATTERNS.md                ← RLS SQL, DbConnectionInterceptor, migration, Testcontainers
    TESTING_PATTERNS.md                 ← E2E struktúra, probe-and-skip, 401/200 minta
  architecture/
    ADR_CATALOGUE.md                    ← Architekturális döntések gyűjteménye
    API_CONTRACT_CATALOGUE.md           ← Minden endpoint (7 service)
    MODULE_BOUNDARIES.md                ← Provider interfészek, Contracts NuGet, DB szeparáció
  context/
    KERNEL_CONTEXT.md                   ← Kernel terminál kontextusa
    ORCH_CONTEXT.md                     ← Orchestrator terminál kontextusa
    PORTAL_CONTEXT.md                   ← Portal terminál kontextusa
    JOINERY_CONTEXT.md                  ← Joinery terminál kontextusa
    CUTTING_CONTEXT.md                  ← Cutting terminál kontextusa
    INFRA_CONTEXT.md                    ← Infra terminál kontextusa
    E2E_CONTEXT.md                      ← E2E terminál kontextusa
```

**Használat termináloknak:** Session indításakor olvasd el a saját `context/<TERMINÁL>_CONTEXT.md` fájlodat + az `INDEX.md`-t.

**Feldolgozási napló:** `docs/mailbox/librarian/PROCESSED_LOG.md` — ami itt szerepel, az már elemezve van.
