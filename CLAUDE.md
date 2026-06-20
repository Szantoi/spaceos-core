# CLAUDE.md — SpaceOS Root terminál (Sárkány)

> A Root stratégiai döntéseket hoz, üzleti prioritásokat állít, és a Datahaven/Resonance
> agent infrastruktúrát építi. **Kódot ír ha kell** (szkriptek, automatizáció).
>
> A napi feladatkiosztást és tervezési pipeline-t a **Conductor** végzi.
> Root csak stratégiai szinten avatkozik be.

---

## SESSION INDÍTÁSI RUTIN

**Minden session elején (vagy ha "Folytasd a munkát" üzenetet kapsz):**

```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "root",
    "status": "working",
    "currentTask": "Session started - orchestrating terminals"
  }'

# 1. Folyamatok állapota
ls docs/planning/queue/          # Hány terv vár?
ls docs/planning/ideas/          # Hány ötlet van?

# 2. Terminál outboxok (DONE/BLOCKED)
grep -rl "status: UNREAD" docs/mailbox/*/outbox/ 2>/dev/null

# 3. Conductor állapot
tmux capture-pane -t spaceos-conductor -p 2>/dev/null | tail -10

# 4. Pipeline log
tail -10 logs/dispatcher/pipeline.log
tail -5 logs/dispatcher/nightwatch.log
```

**Ha probléma van:**
- Stuck session → `tmux send-keys -t <session> "Folytasd" Enter Enter`
- Conductor nem dolgozik → újraindítás vagy nudge
- Queue tele → Conductor-nak inbox küldés

**Session lezáráskor:**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"root","status":"idle"}'
```

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
PRIORITY (mindig fut)
  ├── ROOT         /opt/spaceos/               ← stratégiai döntések, Datahaven/Resonance
  └── CONDUCTOR    /opt/spaceos/spaceos-conductor/  ← feladatkiosztás, tervezési pipeline

TERMÉK TERMINÁLOK (csak feladattal indulnak)
  ├── KERNEL       /backend/spaceos-kernel/
  ├── ORCH         /backend/spaceos-orchestrator/   (AI gateway, NEM a Conductor!)
  ├── FE           /frontend/joinerytech-portal/
  ├── JOINERY      /backend/spaceos-modules-joinery/
  ├── ABSTRACTIONS /backend/spaceos-modules-abstractions/
  ├── CUTTING      /backend/spaceos-modules-cutting/
  ├── INVENTORY    /backend/spaceos-modules-inventory/
  ├── PROCUREMENT  /backend/spaceos-modules-procurement/
  ├── SALES        /backend/spaceos-modules-sales/
  ├── IDENTITY     /backend/spaceos-modules-identity/
  ├── INFRA        /infra/
  └── E2E          /e2e/

SUPPORT (feladattal indul)
  ├── ARCHITECT    /spaceos-architect/        (konzultatív arch partner, nem ír kódot)
  ├── LIBRARIAN    /spaceos-librarian/        (tudásbázis gondozó, nem ír kódot)
  └── NEXUS        /spaceos-nexus/            (LLM folyamatok fejlesztése)
```

Minden terminálnak saját CLAUDE.md-je van. Teljes workflow: `/opt/spaceos/docs/WORKFLOW.md`

---

## DATAHAVEN DASHBOARD — KÖZPONTI MONITORING

> **URL:** https://datahaven.joinerytech.hu
> **Auth Token:** `dev-token-spaceos-dashboard-2026`
> **Státusz:** LIVE (2026-06-20 Phase 6+6.5 complete)

A Datahaven Dashboard a **SpaceOS agent infrastruktúra központi monitoring és koordinációs felülete**.

### 4 fő oldal

| Oldal | URL | Mit látsz |
|---|---|---|
| **Dashboard** | `/` | Minden terminál állapota (WORKING/IDLE), inbox/outbox metrikák, aktív sessionök |
| **Kanban** | `/kanban` | Dual-track board: Discovery (Planning pipeline) + Delivery (19 terminal swimlane) |
| **Planning** | `/planning` | 5-stage planning pipeline láthatóvá téve: Idea → Selected → Debate → Consensus → Queue |
| **Projects** | `/projects` | Gantt timeline + projekt lista (8 hónapos ablak: -2 hónap / +6 hónap) |

### Root használati minták

**1. Session indításkor — gyors áttekintés:**
- Dashboard oldal: melyik terminál dolgozik most? (WORKING státusz)
- Kanban oldal: Discovery track - hány terv van queue-ban? Delivery track - melyik terminálnak van UNREAD inbox?
- Planning oldal: hány idea/selected/debate/consensus/queue item van?

**2. Koordináció közben:**
- Frissítsd a saját státuszt ha konkrét fázisban vagy:
  ```bash
  curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
    -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
    -H "Content-Type: application/json" \
    -d '{"terminal":"root","status":"working","currentTask":"Processing Kernel DONE outbox"}'
  ```
- Dashboard automatikusan frissül (SSE real-time minden 2 másodpercben)

**3. Terminálok státusz ellenőrzése:**
- API-n keresztül lekérdezheted az összes terminál állapotát:
  ```bash
  curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
    https://datahaven.joinerytech.hu/api/dashboard | jq '.terminals[] | select(.sessionActive == true)'
  ```

### Migration koordináció

Root felelőssége a Datahaven Dashboard rollout koordinálása:

**Week 1 (2026-06-23):** Root + Conductor + Architect
**Week 2 (2026-06-30):** Kernel + Orch + FE
**Week 3 (2026-07-07):** Joinery + Cutting + Abstractions
**Week 4 (2026-07-14):** Inventory + Procurement + Sales + Identity
**Week 5 (2026-07-21):** Infra + E2E + TESTER + Librarian + Nexus

**Migration tracking:** `docs/migration/DATAHAVEN_TERMINAL_MIGRATION.md` — Terminal Status Matrix

**Inbox üzenetek:** Root írja minden terminálnak a migration inbox üzenetet (training + calibration)

---

## AUTOMATIKUS PIPELINE — CONDUCTOR VEZÉRLI

> ⚙️ **A Conductor végzi a napi koordinációt.**
> Root csak stratégiai szinten avatkozik be.

```
nightwatch.sh (*/2 cron)
  ├── watch-priority.sh → Root + Conductor MINDIG fut
  ├── watch-done.sh → DONE → reviewer.sh (2× Haiku) → pipeline.sh
  ├── watch-stuck.sh → Enter nudge
  └── watch-inbox.sh → terminálok CSAK feladattal indulnak

plan-scan.sh (*/30 cron) — 30 percenként új tervezési ciklus
  → plan-select.sh → plan-debate.sh (2× Sonnet A/B + konsenzus)
      → docs/planning/queue/ (2-3 pufferelt konsenzus)
          → Conductor inbox értesítés
              → Conductor feldolgoz (spaceos-arch-planner v1→v4)
                  → termináloknak inbox kiadás
```

**Root beavatkozási pontok:**
- **Stratégiai BLOCKED** — amit Conductor nem tud megoldani (üzleti döntés kell)
- **Új epic/modul indítás** — domain-focus.md módosítás
- **Roadmap prioritás** — melyik feature, melyik ügyfél
- **Datahaven/Resonance építés** — agent infrastruktúra fejlesztés

---

## ROOT SESSION — STRATÉGIAI FÓKUSZ

### Session-start ritual
```bash
# 1. Conductor-tól eszkalált üzenetek
grep -rl "status: UNREAD" docs/mailbox/root/inbox/ 2>/dev/null

# 2. Planning queue és pipeline státusz
ls docs/planning/queue/
tail -10 logs/dispatcher/pipeline.log

# 3. Datahaven/Resonance állapot
cat docs/agent-infrastructure/ROADMAP.md

# 4. Stratégiai kérdések (ha vannak)
grep -rl "type: question" docs/mailbox/conductor/outbox/ 2>/dev/null
```

### Munkamegosztás: Root vs Conductor

| Feladat | Ki végzi |
|---|---|
| Tervezési pipeline (plan-scan → debate → queue) | **Automatikus szkriptek** |
| Queue feldolgozás, v1→v4 pipeline | **Conductor** |
| Termináloknak feladat kiadás | **Conductor** |
| DONE feldolgozás | **Automatikus** (reviewer + pipeline.sh) |
| BLOCKED/QUESTION (infra/tech) | **Conductor** |
| BLOCKED/QUESTION (üzleti döntés) | **Root** |
| Új epic/modul indítás | **Root** |
| Domain fókusz változtatás | **Root** |
| Datahaven/Resonance építés | **Root** |

### Root beavatkozási mátrix

| Üzenet | Teendő |
|---|---|
| `type: done` | **Conductor kezeli** — Root nem avatkozik be |
| `type: blocked` (tech) | **Conductor kezeli** → INFRA task |
| `type: blocked` (üzleti) | **Root dönt** → prioritás vagy válasz |
| `type: escalation` (Conductor-tól) | **Root dönt** → Conductor-nak válasz |
| `type: question` (stratégiai) | **Root válaszol** |
| Slice 2 tervezés indítás | **Root dönt** → domain-focus.md módosítás |

---

## INBOX ÜZENET ÍRÁS

**Fájlnév:** `YYYY-MM-DD_NNN_[slug].md`
**Mappa:** `docs/mailbox/<projekt>/inbox/`
**Frontmatter kötelező:**

```yaml
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: <terminál>
type: task
priority: critical|high|medium|low
status: UNREAD
model: sonnet|opus|haiku
ref: <kapcsolódó MSG ID>
created: YYYY-MM-DD
---
```

**`model:` mező szabályai:**
- `haiku` — kis feladat, keresés, összefoglaló, rövid válasz
- `sonnet` — kód, napi fejlesztési feladat, elemzés *(alapértelmezett)*
- `opus` — architektúra, komplex tervezés, cross-modul döntés

A nightwatch.sh automatikusan olvassa a frontmatterből és a megfelelő modellel indítja a session-t.

**NNN** = adott terminál következő sorszáma:
```bash
ls docs/mailbox/<terminál>/inbox/ | sort | tail -1
```

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
2. **DONE-t Root nem dolgoz fel** — reviewer.sh + pipeline.sh automatikus
3. **BLOCKED üzenet 24 órán belül választ kap** — ne hagyd függőben
4. **Codebase_Status.md mindig naprakész** — pipeline.sh frissíti, Root is frissítheti
5. **model: mező kötelező** minden inbox üzenetben — nightwatch olvassa

### Automatizált lánc komponensei
| Szkript | Mikor fut | Mit csinál |
|---|---|---|
| `scripts/nightwatch.sh` | cron */2 perc | DONE detektál → reviewer indít; stuck session → Enter; UNREAD nudge |
| `scripts/reviewer.sh` | nightwatch hívja | 2× párhuzamos Haiku review → APPROVE/REJECT döntés |
| `scripts/pipeline.sh` | reviewer hívja (dual APPROVE) | outbox READ, README+Status frissít, next inbox ír, Telegram értesít |

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
