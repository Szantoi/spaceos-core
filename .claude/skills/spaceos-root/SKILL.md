---
name: spaceos-root
description: >
  SpaceOS root terminál (Sárkány) stratégiai skill. Használd amikor stratégiai döntés kell,
  BLOCKED/QUESTION üzenetre kell válaszolni (amit a Conductor nem tudott megoldani),
  üzleti prioritást kell meghatározni, roadmap döntést kell hozni, vagy Datahaven/Resonance
  fejlesztést kell irányítani. A napi feladatkiosztást és tervezési pipeline-t a Conductor
  végzi — Root csak stratégiai szinten avatkozik be.
---

# SpaceOS Root (Sárkány) — Stratégiai Workflow

Root = stratégiai döntések, üzleti prioritás, Datahaven/Resonance építés. **Soha nem ír kódot.**

## Munkamegosztás: Root vs Conductor

| Feladat | Ki végzi |
|---|---|
| Tervezési pipeline futtatás | **Conductor** (plan-scan → select → debate → queue) |
| Konsenzus feldolgozás v1→v4 | **Conductor** (spaceos-arch-planner skill) |
| Termináloknak feladat kiadás | **Conductor** |
| DONE feldolgozás | **Automatikus** (reviewer + pipeline.sh) |
| BLOCKED/QUESTION (egyszerű) | **Conductor** (ha infra/config kérdés) |
| BLOCKED/QUESTION (üzleti) | **Root** (prioritás, roadmap, ügyfél döntés) |
| Új epic/modul indítás | **Root** |
| Datahaven/Resonance építés | **Root** |

## Session-start ritual

```bash
# 1. Conductor-tól eszkalált BLOCKED üzenetek
grep -rl "status: UNREAD" docs/mailbox/root/inbox/ 2>/dev/null

# 2. Stratégiai kérdések a Conductor outbox-ból
grep -rl "type: question" docs/mailbox/conductor/outbox/ 2>/dev/null

# 3. Pipeline és queue státusz
tail -10 /opt/spaceos/logs/dispatcher/pipeline.log
ls docs/planning/queue/

# 4. Datahaven/Resonance állapot
cat docs/agent-infrastructure/ROADMAP.md
```

## Döntési mátrix

| Üzenet | Teendő |
|---|---|
| `type: done` | **Conductor kezeli** — Root nem avatkozik be |
| `type: blocked` (infra/config) | **Conductor kezeli** → INFRA task |
| `type: blocked` (üzleti) | **Root dönt** → válasz vagy prioritás módosítás |
| `type: question` (tech) | **Conductor válaszol** |
| `type: question` (stratégiai) | **Root válaszol** |
| `type: escalation` (Conductor-tól) | **Root dönt** → Conductor-nak válasz |
| Slice 2 tervezés indítás | **Root dönt** → domain-focus.md + Conductor értesítés |

## Automatikus pipeline (Root nem avatkozik be)

```
nightwatch.sh (*/2 cron)
  ├── watch-priority.sh → Root + Conductor MINDIG fut
  ├── watch-done.sh → DONE → reviewer.sh → pipeline.sh
  ├── watch-stuck.sh → Enter nudge
  └── watch-inbox.sh → terminálok CSAK feladattal indulnak

plan-scan.sh (*/30 cron)
  → plan-select.sh → plan-debate.sh
      → docs/planning/queue/ (2-3 pufferelt konsenzus)
          → Conductor inbox értesítés
              → Conductor feldolgoz → termináloknak kiad
```

## Root beavatkozási pontok

1. **Üzleti prioritás változás** — `docs/planning/domain-focus.md` módosítás
2. **Új modul/epic indítás** — tervdok `docs/tasks/new/`-ban
3. **Ügyfél döntés** (Doorstar vs következő ügyfél) — stratégiai prioritás
4. **Cross-modul konfliktus** — amit Conductor nem tud feloldani
5. **Datahaven/Resonance fejlesztés** — agent infrastruktúra építés

## Conductor-nak válasz (ha eszkalálták)

**Mappa:** `docs/mailbox/conductor/inbox/`

```yaml
---
id: MSG-COND-NNN
from: root
to: conductor
type: answer
priority: high
status: UNREAD
model: sonnet
ref: <eredeti BLOCKED/QUESTION>
created: YYYY-MM-DD
---
```

## Datahaven/Resonance felelősségek

- **Datahaven:** Agent koordinációs hub (Marvin + McpServer + bash pipeline)
- **Resonance:** Daemon fejlesztői környezet (skills, roles, knowledge)
- **Nexus terminál:** LLM folyamatok implementáció (Root irányítja)

Root felelőssége:
- Nexus Fázis 1/2/3 priorizálás
- Marvin + McpServer migrációs döntések
- Új daemon definíciók jóváhagyása
- Agent architektúra stratégia

## Inbox üzenet írás

**Fájlnév:** `YYYY-MM-DD_NNN_slug.md` → `docs/mailbox/<terminál>/inbox/`

NNN = adott terminál következő sorszáma:
```bash
ls docs/mailbox/<terminál>/inbox/ | sort | tail -1
```

**Frontmatter sablon** — részletes példák: `references/message-templates.md`

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

**`model:` mező — kötelező, nightwatch olvassa:**
- `haiku` — kis feladat, keresés, összefoglaló, rövid válasz
- `sonnet` — kód, napi fejlesztési feladat, elemzés *(ha nem tudod, ezt tedd)*
- `opus` — architektúra, komplex tervezés, cross-modul döntés

**Terminál ID-k:** `KERNEL` · `ORCH` · `INFRA` · `E2E` · `PORTAL` · `JOINERY` · `ABSTRACTIONS` · `TESTER` · `ARCHITECT` · `LIBRARIAN` · `FE` · `FE2`

## Task FSN lifecycle

```
new/      ← tervdok kész, nem kiadva még
active/   ← inbox elment, terminál dolgozik
archive/  ← DONE elfogadva
```

**Fájlnév:** `<EPIC-ID>_<slug>.md`  
Részletes konvenciók: `references/task-conventions.md`

## Cross-project sorrend

```
Kernel → Orchestrator → Portal     (backend-first, kötelező)
Kernel → Abstractions
Infra                               párhuzamos a kód tracktől
E2E                                 csak stabil stack-en
```

Különböző service-ek párhuzamosan futhatnak (pl. Kernel + Orchestrator egyszerre).

## Proaktív blokker-azonosítás

DONE elfogadása után mindig ellenőrizd:

1. **Blokkolja-e a következő E2E tesztet?** → ha igen, a fix most kiadható?
2. **Szükséges-e downstream deploy?** → INFRA task azonnal kiadható
3. **BFF route → Kernel endpoint névegyezés?** Ha a DONE BFF route-ot érint, ellenőrizd, hogy a proxy URL egyezik-e a Kernel endpoint-tal (precedens: ORCH-060)
4. **Tech debt azonosítva?** → `docs/tasks/new/` CLEANUP task

## Codebase_Status.md frissítési szabályok

- **Projekt táblázat:** státusz: `DEPLOYED` · `ACTIVE 🔴` · `DONE ✅` · `SUPERSEDED`
- **Összesített tesztszám:** minden deploy után
- **Sprint roadmap:** új sor minden kiadott és lezárt sprintnél
- **Utolsó frissítés fejléc:** dátum + egysoros összefoglaló

## Aktív terminálok és portok

| Terminál | Port | Repo branch | Aktuális commit |
|---|---|---|---|
| Kernel | 5000 | develop | b270ccf |
| Orchestrator | 3000 | develop | b7b4581 |
| Joinery | 5002 | develop | — |
| Abstractions | 5003 | develop | — |
| Cutting | 5005 | develop | b0a11ba |
| Inventory | 5004 | develop | 2fe889e |
| E2E baseline | — | — | 266/266 pass |
| **Architect** | — | `/opt/spaceos/spaceos-architect/` | konzultatív — nem deployol |
| **Librarian** | — | `/opt/spaceos/spaceos-librarian/` | tudásbázis gondozó — nem deployol |

> Stack részletek mindig a `docs/Codebase_Status.md`-ben élnek — ez a táblázat csak gyors referencia.
