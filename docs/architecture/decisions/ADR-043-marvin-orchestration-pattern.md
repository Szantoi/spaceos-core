# ADR-043: Marvin Orchestration Pattern

> **Státusz:** PROPOSED
> **Dátum:** 2026-06-17
> **Forrás:** MSG-ARCH-010 (Root konzultáció)
> **Tervdokumentum:** `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md`

---

## Kontextus

A SpaceOS bash planning pipeline (7 script, ~1200 sor) törékeny:
- Ha egy script meghal, az egész ciklus elvész
- Nincs resumability — crash után újra kell kezdeni
- Nincs strukturált state tracking
- Nincs audit trail a planning döntésekről

**Jelenlegi architektúra:**
```
Cron 2h → plan-scan.sh (Haiku, 9 segment)
            → plan-select.sh (Sonnet, web research)
                → plan-debate.sh (2× Sonnet + synthesis)
                    → Architect inbox

Cron 2min → nightwatch.sh
              → reviewer.sh (2× Haiku) → pipeline.sh (Sonnet)
```

---

## Döntés

**Marvin (Python) veszi át a planning pipeline-t.**

Tasks + Agents + Threads abstrakcióval. SQLite thread history resumability-t biztosít.

**Cél architektúra:**
```
Marvin Scheduler → PlanningThread (resumable)
                    → ScanTask (Haiku, MCP knowledge_search)
                    → SelectTask (Sonnet, MCP search_knowledge)
                    → DebateTask (2× Sonnet parallel + synthesis)
                    → Architect inbox

Marvin Scheduler → NightwatchThread
                    → ReviewTask (2× Haiku + GuardrailService)
                    → PipelineTask (Sonnet, docs update)
```

---

## Alternatívák értékelése

| Alternatíva | Értékelés | Miért nem |
|---|---|---|
| **LangChain/LangGraph** | ❌ ELVETETT | Python, de túl komplex, dependency hell (MSG-ARCH-007 tapasztalat). 50+ transitive dependency, verzió konfliktusok. |
| **Claude Code sub-agent-ek** | ❌ ELVETETT | Nem scheduled, nem resumable, nincs persistent state. Minden session újrakezdés. |
| **Bash pipeline javítása** | ❌ ELVETETT | A törékeny alapokat nem érdemes foltozni. Nincs native error recovery. |
| **Custom Python orchestrator** | ❌ ELVETETT | Marvin már megoldja a Thread/Task/Agent abstrakciókat. Ne találjuk fel újra. |
| **Marvin** | ✅ VÁLASZTOTT | Resumable, multi-agent, provider-agnostic, structured output. |

---

## Indoklás

### 1. Resumability (Thread history)
```python
with marvin.Thread(name=f"planning-{date}") as thread:
    # Ha crash → Thread SQLite-ben marad
    # Következő futás: Thread.resume(thread_id)
```
Bash pipeline: crash = teljes adatvesztés. Marvin: crash = folytatás a bukás pontjától.

### 2. Multi-agent explicit definíció
```python
scanner = marvin.Agent(name="Scanner", model="claude-haiku-4-5-20251001", ...)
selector = marvin.Agent(name="Selector", model="claude-sonnet-4-20250514", ...)
debater_a = marvin.Agent(name="Debater-A", ...)
debater_b = marvin.Agent(name="Debater-B", ...)
synthesizer = marvin.Agent(name="Synthesizer", ...)
```
5 agent explicit instrukcióval — nem ad-hoc prompt-ok bash script-ekben.

### 3. Structured output (Pydantic models)
```python
result = await marvin.run(
    "Válaszd ki a TOP 3 ötletet",
    result_type=list[SelectedIdea]  # Type-safe
)
```
Bash: regex parsing, törékeny. Marvin: Pydantic validáció, type-safe.

### 4. Parallel execution
```python
plan_a, plan_b = await asyncio.gather(
    marvin.run("Konzervatív terv", agent=debater_a),
    marvin.run("Merész terv", agent=debater_b)
)
```
Bash: szekvenciális. Marvin: async parallel → gyorsabb debate cycle.

---

## Golden Rule ellenőrzés

| Szabály | Ellenőrzés | Státusz |
|---|---|---|
| **Data → Rules → Geometry** | Marvin NEM számol geometriát — csak planning paramétereket kezel | ✅ OK |
| **Modular Monolith** | Agent-ek decoupled (Scanner, Selector, Debater, Synthesizer) | ✅ OK |
| **Immutability & Trust** | Thread history SQLite immutable append-only log | ✅ OK |
| **Need-to-Know RBAC** | RbacFilter Fázis 3-ban kerül bekötésre (tool visibility) | ⚠️ PENDING |
| **Walking Skeleton First** | Fázis 1-2 párhuzamos a Slice 1-gyel, élő pipeline érintetlen | ✅ OK |

---

## Kritikus függőségek

| Függőség | Blokkoló? | Megoldás |
|---|---|---|
| VPS memória (8GB → 16GB) | **HIGH** | Marvin ~200MB + ChromaDB ~400MB. VPS bővítés ígérve. |
| Marvin Anthropic provider érettség | MEDIUM | Pydantic AI backend. Fallback: bash pipeline visszakapcsolható. |
| Thread SQLite corruption | LOW | WAL mode + backup. Thread elvesztés = újraindítás, nem katasztrófa. |

---

## Fázis lebontás

### Fázis 1: McpServer Knowledge Service (COMPLETE ✅)
- Knowledge Service operational (port 3456)
- Systemd deployment, auto-restart
- Librarian cron reindex trigger

### Fázis 2: Marvin Planning Pipeline (~6-7 nap)
1. Marvin Python env setup (INFRA)
2. Agent definíciók: 5 agent (NEXUS)
3. PlanningThread lifecycle (NEXUS)
4. MCP tool wrappers (NEXUS)
5. Cron átállás: bash → Marvin scheduler (INFRA)
6. Parallel futtatás tesztelés (ROOT validation)
7. Bash cron kikapcsolás (INFRA)

### Fázis 3: Reviewer + Nightwatch (~8-10 nap, Slice 2 előtt)
- reviewer.sh → Marvin Task
- nightwatch.sh → Marvin Scheduler
- WorkflowStateTracker bekötés
- RbacFilter bekötés

---

## Rollback terv

```bash
# Marvin scheduler leállítása
systemctl stop spaceos-marvin

# Bash cron visszakapcsolása
crontab -e
# Uncomment: */30 * * * * /opt/spaceos/scripts/plan-scan.sh
# Uncomment: */2 * * * * /opt/spaceos/scripts/nightwatch.sh
```

Rollback idő: <5 perc. Élő pipeline megszakítás nélkül visszaállítható.

---

## Megvalósító terminál

**NEXUS** (spaceos-nexus/) — Agent infrastruktúra fejlesztés
- Marvin Python env
- Agent definíciók
- MCP tool integráció

**INFRA** — VPS konfiguráció
- Systemd service
- Cron átállás

---

## Referenciák

- Tervdokumentum: `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md`
- Agent Infrastructure Roadmap: `docs/agent-infrastructure/ROADMAP.md`
- JoineryTech.McpServer referencia: `spaceos-nexus/mcp-server/`
