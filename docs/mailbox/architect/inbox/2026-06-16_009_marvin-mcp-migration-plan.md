---
id: MSG-ARCH-009
from: root
to: architect
type: task
priority: high
status: READ
model: opus
created: 2026-06-16
---

# Architect — Marvin + McpServer fokozatos migrációs terv

## Döntés

A SpaceOS átáll Marvin (Python orchestrátor) + JoineryTech.McpServer (MCP tool provider)
kombinációra. A migráció tudatos, apró lépésekkel zajlik — párhuzamosan a Slice 1
kódfejlesztéssel, az élő Doorstar pipeline megszakítása nélkül.

VPS kapacitásbővítés: Gábor gondoskodik róla, nem blokkol.

## Referencia projektek

- `https://github.com/Szantoi/JoineryTech.McpServer` — TypeScript MCP server (RBAC, RAG, FSM, artifacts)
- `https://github.com/PrefectHQ/marvin` — Python agent orchestrátor (Tasks, Agents, Threads, SQLite)

## Három fázis — amit meg kell tervezned

### Fázis 1 — McpServer knowledge service (izolált, nincs kockázat)

**Mit csináljunk:**
- JoineryTech.McpServer adaptálása SpaceOS-ra: `/opt/spaceos/spaceos-mcp/`
- `indexKnowledgeBase.ts` → `docs/knowledge/**/*.md` olvasás (`.knowledge.md` suffix eltávolítás)
- Gemini embedding → voyage-3-lite (Anthropic API, vendor egységesség)
- ChromaDB Docker service indítása
- Librarian cron (`cron-librarian.sh`) kiegészítése: knowledge sync után indexer hívás
- Haiku scanner (`plan-scan.sh`) kap egy `search_knowledge` MCP tool hívást

**Amit megold:** A scanner nem csak rotálva olvas szegmenseket, hanem szemantikusan kérdez rá korábbi döntésekre és mintákra.

**Párhuzamos futás:** A meglévő bash pipeline **érintetlen marad**.

---

### Fázis 2 — Marvin átveszi a planning pipeline-t

**Mit csináljunk:**
- `plan-scan.sh` + `plan-select.sh` + `plan-debate.sh` → Python Marvin Tasks
- Marvin Thread: egy tervezési ciklus összes fázisa egy Thread-ben él (resumable)
- McpServer `search_knowledge` → Marvin tool-ként bekötve
- McpServer `submitArtifact` → ötlet és konsenzus fájlok regisztrálva
- Exception handling: ha egy fázis meghal, a Thread-ből folytatható

**Amit megold:** A bash pipeline törékenysége — ha `plan-debate.sh` meghal, az egész ciklus elvész. Marvin-ban resumable.

**Párhuzamos futás:** Az új Marvin planning fut, a régi bash `plan-scan.sh` cron kikapcsol.

---

### Fázis 3 — Marvin reviewer + nightwatch (Slice 2 előtt)

**Mit csináljunk:**
- `reviewer.sh` → Marvin Task (2 párhuzamos Haiku review + GuardrailService compliance pass)
- `nightwatch.sh` → Marvin scheduled task (cron helyett Marvin Scheduler)
- McpServer WorkflowStateTracker → terminál session FSM (stuck helyett proper state)
- McpServer RbacFilter → tool kényszerítés (CLAUDE.md emberi szabályok helyett gépi)

**Amit megold:** Stuck session detekció, RBAC gépi kényszerítés, audit trail minden agent actionre.

**Párhuzamos futás:** Régi bash nightwatch leáll, Marvin veszi át — átállás pillanata egyetlen deploy.

---

## Elvárt output (spaceos-arch-planner pipeline, min. v2)

```
docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md
docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v2.md
```

A tervdokumentumnak tartalmaznia kell:

1. **Fázis 1 részletes spec**
   - McpServer adaptációs teendők (fájlonként)
   - voyage-3-lite integráció (API hívás formátum)
   - ChromaDB Docker compose snippet
   - Librarian cron kiegészítés

2. **Fázis 2 Marvin Task struktúra**
   - Agent definíciók (scanner, selector, debater_a, debater_b, synthesizer)
   - Thread lifecycle (mikor indul, mikor zárul)
   - McpServer tool hívások Marvin-ból

3. **Fázis 3 scope** (vázlat elég — Slice 2 előtt pontosítjuk)

4. **Megvalósító terminál minden fázishoz**
   - Fázis 1: ki csinálja? (ORCH? Új MCP terminál?)
   - Fázis 2: ki csinálja? (ROOT? Új MARVIN terminál?)

5. **Kockázat és rollback** minden fázishoz

6. **ADR bejegyzés** — `docs/knowledge/architecture/ADR_CATALOGUE.md`
   - ADR: Marvin mint orchestrátor réteg
   - ADR: JoineryTech.McpServer SpaceOS knowledge service-ként

DONE outbox mikor a v2 tervdok kész.
