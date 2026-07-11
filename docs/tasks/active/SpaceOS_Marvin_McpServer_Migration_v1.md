# SpaceOS Marvin + McpServer Fokozatos Migrációs Terv

> **Verzió:** v2 · **Dátum:** 2026-06-16
> **Státusz:** REVIEW — v2 infra review kész
> **Forrás:** MSG-ARCH-009 (Root kérés)
> **ADR:** ADR-041 (Marvin orchestrátor) + ADR-042 (McpServer knowledge service)

---

## 0. Executive Summary

A SpaceOS bash planning pipeline (7 script, ~1200 sor) átáll Marvin (Python) + JoineryTech.McpServer (TypeScript MCP) kombinációra. 3 fázisban, párhuzamosan a Slice 1 fejlesztéssel, az élő Doorstar pipeline megszakítása nélkül.

### Jelenlegi architektúra (bash)

```
Cron 2h → plan-scan.sh (Haiku, 9 segment)
            → ≥5 idea → plan-select.sh (Sonnet, web research)
                          → plan-debate.sh (2× Sonnet + synthesis)
                            → Architect inbox

Cron 2min → nightwatch.sh
              → watch-done.sh → reviewer.sh (2× Haiku) → pipeline.sh (Sonnet)
              → watch-stuck.sh (Enter nudge)
              → watch-inbox.sh (auto-start, inbox nudge)

Cron 5h → cron-librarian.sh → Librarian inbox
```

### Cél architektúra (Marvin + MCP)

```
Marvin Scheduler → PlanningThread (resumable)
                    → ScanTask (Haiku, 9 segment, MCP knowledge_search)
                    → SelectTask (Sonnet, MCP search_knowledge)
                    → DebateTask (2× Sonnet parallel + synthesis)
                    → Architect inbox

Marvin Scheduler → NightwatchThread
                    → ReviewTask (2× Haiku + GuardrailService compliance)
                    → PipelineTask (Sonnet, docs update)

McpServer (stdio) → search_knowledge (FTS/vector)
                  → submitArtifact (idea/consensus fájl regisztráció)
                  → getWorkflowState / updateWorkflowState (FSM tracking)
                  → RbacFilter (tool visibility per role)
```

---

## 1. Fázis 1 — McpServer Knowledge Service

### 1.1 Scope

Izolált, 0 kockázat. A meglévő bash pipeline **érintetlen marad**.

### 1.2 McpServer adaptáció — fájlonként

| Eredeti fájl | Adaptáció | SpaceOS változás |
|---|---|---|
| `src/rag/VectorStore.ts` | Embedding model csere | Gemini → voyage-3-lite (Anthropic API) |
| `src/rag/indexKnowledgeBase.ts` | Forrás path + suffix | `database/*.knowledge.md` → `docs/knowledge/**/*.md` (bármilyen `.md`) |
| `src/rag/episodicMemory.ts` | Forrás path | Session highlight → terminál memóriák (`/home/gabor/.claude/projects/*/memory/*.md`) |
| `src/rag/RetryableSeeder.ts` | Változatlan | Retry logika megmarad |
| `src/mcp/RbacFilter.ts` | Role mapping | JoineryTech role-ok → SpaceOS terminálok mapping |
| `src/metadata/WorkflowStateTracker.ts` | Változatlan | FSM 7-state lifecycle (Fázis 3-ban kerül használatba) |

### 1.3 voyage-3-lite integráció

```typescript
// VectorStore.ts adaptáció
import Anthropic from '@anthropic-ai/sdk';

const client = new Anthropic();

async function getEmbedding(text: string): Promise<number[]> {
  const response = await client.embeddings.create({
    model: 'voyage-3-lite',
    input: [text],
  });
  return response.data[0].embedding;  // 1024 dim
}

// Embedding dimension: 1024 (voyage-3-lite)
// ChromaDB collection config: distance = cosine
```

**API cost:** voyage-3-lite ~$0.02/1M tokens. 103 fájl × ~500 token átlag = ~50K token = ~$0.001/indexelés. Elhanyagolható.

### 1.4 RAG döntés — MSG-ARCH-005 felülvizsgálat

A MSG-ARCH-005-ben tsvector FTS-t választottam (Walking Skeleton). A MSG-ARCH-009 ChromaDB-t kéri.

**Módosított döntés:** ChromaDB elfogadva, mert:
1. A referencia implementáció (JoineryTech.McpServer) már production-tested ChromaDB-vel
2. VPS kapacitásbővítés ígérve → ChromaDB Docker container elfér
3. In-memory fallback megvan → ha ChromaDB leáll, a pipeline nem áll meg
4. A tsvector FTS (MSG-ARCH-005) marad **backup terv** ha a ChromaDB bővítés nem jön össze

**A MSG-ARCH-005 RAG spec státusza: SUPERSEDED by MSG-ARCH-009** — a tsvector FTS terv archiválandó.

### 1.5 ChromaDB Docker compose

```yaml
# /opt/spaceos/docker-compose.chromadb.yml
version: '3.8'
services:
  chromadb:
    image: chromadb/chroma:latest
    container_name: spaceos-chromadb
    ports:
      - "127.0.0.1:8000:8000"  # localhost only, nincs külső hozzáférés
    volumes:
      - chromadb-data:/chroma/chroma
    environment:
      - IS_PERSISTENT=TRUE
      - ANONYMIZED_TELEMETRY=FALSE
    restart: unless-stopped
    mem_limit: 512m  # korlátozott memória VPS-en

volumes:
  chromadb-data:
```

**Memória footprint:** ChromaDB ~200-400 MB idle, 512 MB limit. VPS bővítés után elfér.

### 1.6 Librarian cron kiegészítés

```bash
# cron-librarian.sh kiegészítés (a meglévő inbox létrehozás UTÁN):
# Indexer hívás ha McpServer fut
if curl -s http://localhost:3100/health > /dev/null 2>&1; then
  node /opt/spaceos/spaceos-mcp/dist/scripts/indexKnowledgeBase.js
  echo "$TIMESTAMP Knowledge index frissítve" >> "$LOG_DIR/librarian.log"
fi
```

### 1.7 Scanner MCP tool hívás

```bash
# plan-scan.sh kiegészítés:
# A claude -p hívásba MCP server hozzáadás
claude -p \
  --model haiku \
  --mcp-server spaceos-knowledge \
  "... meglévő prompt ... \
   Használd a search_knowledge toolt korábbi döntések és minták kereséséhez."
```

A `spaceos-knowledge` MCP server a Claude settings-ben regisztrálva:
```json
// ~/.claude/settings.json kiegészítés
{
  "mcpServers": {
    "spaceos-knowledge": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-mcp/dist/index.js"],
      "env": {
        "ANTHROPIC_API_KEY": "${ANTHROPIC_API_KEY}",
        "CHROMA_URL": "http://localhost:8000"
      }
    }
  }
}
```

### 1.8 Fázis 1 implementáció

| Sorrend | Feladat | Terminál | Becsült nap |
|---|---|---|---|
| 1.1 | McpServer repo klón + adaptálás | INFRA | 1 |
| 1.2 | voyage-3-lite integráció | INFRA | 0.5 |
| 1.3 | ChromaDB Docker compose + indítás | INFRA (VPS Operator) | 0.5 |
| 1.4 | indexKnowledgeBase.ts adaptálás (path-ok) | INFRA | 0.5 |
| 1.5 | MCP server regisztráció Claude settings | INFRA | 0.5 |
| 1.6 | cron-librarian.sh kiegészítés | INFRA | 0.5 |
| 1.7 | plan-scan.sh MCP tool hívás | INFRA | 0.5 |
| 1.8 | Teszt: scanner MCP query → releváns eredmény | ROOT | 0.5 |

**Fázis 1 összesen: ~4-5 nap · 0 kockázat az élő pipeline-ra**

### 1.9 Rollback

ChromaDB container leállítása + MCP server settings eltávolítása → bash pipeline változatlan.

---

## 2. Fázis 2 — Marvin Planning Pipeline

### 2.1 Scope

A bash planning pipeline (plan-scan → plan-select → plan-debate) átáll Marvin-ra. A nightwatch/reviewer/pipeline pipeline (Fázis 3) még bash marad.

### 2.2 Marvin alapkonfiguráció

```python
# /opt/spaceos/spaceos-marvin/config.py
import marvin

# Anthropic provider Pydantic AI-on keresztül
marvin.settings.provider = "anthropic"
marvin.settings.default_model = "claude-sonnet-4-20250514"

# SQLite thread history
# Default: ~/.marvin/threads.db
```

**Python környezet:**
```bash
cd /opt/spaceos/spaceos-marvin
python -m venv .venv
source .venv/bin/activate
pip install marvin anthropic
```

### 2.3 Agent definíciók

```python
# agents.py
import marvin

scanner = marvin.Agent(
    name="Scanner",
    model="claude-haiku-4-5-20251001",
    instructions="""
    Te a SpaceOS Haiku scanner vagy. Szegmensenként vizsgálod a kódbázist
    és 0-2 fejlesztési ötletet írsz. Használd a search_knowledge toolt
    korábbi döntések kereséséhez.
    """,
    tools=["search_knowledge"]  # McpServer MCP tool
)

selector = marvin.Agent(
    name="Selector",
    model="claude-sonnet-4-20250514",
    instructions="""
    Te a SpaceOS Sonnet szelektáló vagy. A gyűjtött ötletekből TOP 3-at
    választasz WSJF (Business Value / Time-to-Market) alapján.
    Web kutatás engedélyezett iparági minták kereséséhez.
    """,
    tools=["search_knowledge", "web_search"]
)

debater_a = marvin.Agent(
    name="Debater-A",
    model="claude-sonnet-4-20250514",
    instructions="""
    Te a konzervatív/inkrementális tervező vagy (Sonnet-A).
    A legmagasabb üzleti értékű feature-t teszed először.
    """
)

debater_b = marvin.Agent(
    name="Debater-B",
    model="claude-sonnet-4-20250514",
    instructions="""
    Te a merész/innovatív tervező vagy (Sonnet-B).
    A legkönnyebben indítható feature-t teszed először.
    """
)

synthesizer = marvin.Agent(
    name="Synthesizer",
    model="claude-sonnet-4-20250514",
    instructions="""
    Te a szintetizáló vagy. Két tervből és két cross-review-ból
    konszenzus tervet készítesz. Az eredmény Architect inbox üzenet lesz.
    """
)
```

### 2.4 Thread lifecycle — PlanningThread

```python
# planning_thread.py
import marvin
from pathlib import Path

async def run_planning_cycle(domain_focus: str | None = None):
    """Egy teljes tervezési ciklus — resumable Thread-ben."""
    
    with marvin.Thread(name=f"planning-{datetime.now():%Y%m%d}") as thread:
        
        # Fázis 1: IDEATION (9 szegmens, rotálva)
        ideas = []
        for segment in SEGMENTS:
            result = await marvin.run(
                f"Vizsgáld meg a {segment} szegmenst és adj 0-2 ötletet.",
                agent=scanner,
                context={"segment": segment, "domain_focus": domain_focus},
                result_type=list[Idea]
            )
            ideas.extend(result)
            save_ideas(result)  # docs/planning/ideas/
        
        if len(ideas) < 5:
            return {"status": "insufficient_ideas", "count": len(ideas)}
        
        # Fázis 2: VALIDATION
        selected = await marvin.run(
            "Válaszd ki a TOP 3 ötletet WSJF alapján.",
            agent=selector,
            context={"ideas": ideas},
            result_type=list[SelectedIdea]
        )
        save_selected(selected)  # docs/planning/selected/
        
        # Fázis 3: ITERATION (2× parallel)
        plan_a, plan_b = await asyncio.gather(
            marvin.run("Készíts konzervatív implementációs tervet.", 
                       agent=debater_a, context={"selected": selected}),
            marvin.run("Készíts merész implementációs tervet.",
                       agent=debater_b, context={"selected": selected})
        )
        
        # Cross-evaluation
        review_a, review_b = await asyncio.gather(
            marvin.run("Értékeld B tervét.", agent=debater_a, context={"plan": plan_b}),
            marvin.run("Értékeld A tervét.", agent=debater_b, context={"plan": plan_a})
        )
        
        # Synthesis
        consensus = await marvin.run(
            "Szintetizáld a két tervet és review-t konszenzusba.",
            agent=synthesizer,
            context={"plan_a": plan_a, "plan_b": plan_b, 
                     "review_a": review_a, "review_b": review_b}
        )
        
        save_consensus(consensus)  # docs/planning/consensus/
        create_architect_inbox(consensus)  # docs/mailbox/architect/inbox/
        
        return {"status": "completed", "consensus": consensus}
```

### 2.5 McpServer tool hívások Marvin-ból

Marvin a `tools` paraméterben kapja az MCP tool-okat. A McpServer stdio transport-on csatlakozik:

```python
# tools.py — MCP tool wrapper Marvin-hoz
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def search_knowledge(query: str, category: str = None) -> list[dict]:
    """SpaceOS tudásbázis keresés MCP-n keresztül."""
    server = StdioServerParameters(
        command="node",
        args=["/opt/spaceos/spaceos-mcp/dist/index.js"],
        env={"ANTHROPIC_API_KEY": os.environ["ANTHROPIC_API_KEY"]}
    )
    async with stdio_client(server) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()
            result = await session.call_tool("search_knowledge", {
                "query": query,
                "category": category
            })
            return result.content

async def submit_artifact(file_path: str, artifact_type: str) -> dict:
    """Ötlet vagy konsenzus fájl regisztrálása."""
    # ... hasonló MCP hívás ...
```

### 2.6 Exception handling — Thread resumability

```python
# Ha plan-debate meghal:
try:
    await run_planning_cycle()
except Exception as e:
    # Thread history SQLite-ben marad
    # Következő futásnál: Thread.resume(thread_id) → folytatás a bukás pontjától
    logger.error(f"Planning cycle failed: {e}")
    # Telegram alert
    notify_telegram(f"⚠️ Planning cycle failed: {e}")
```

A bash pipeline-ban ha `plan-debate.sh` meghal, az egész ciklus elvész. Marvin-ban a Thread SQLite history-ból **resumable**.

### 2.7 Fázis 2 implementáció

| Sorrend | Feladat | Terminál | Becsült nap |
|---|---|---|---|
| 2.1 | Marvin Python env setup | INFRA | 0.5 |
| 2.2 | Agent definíciók (5 agent) | ROOT | 1 |
| 2.3 | PlanningThread lifecycle | ROOT | 2 |
| 2.4 | MCP tool wrappers (search_knowledge, submitArtifact) | ROOT | 1 |
| 2.5 | Cron átállás: plan-scan.sh → Marvin scheduler | INFRA | 0.5 |
| 2.6 | Parallel futtatás tesztelés (Marvin + bash) | ROOT | 1 |
| 2.7 | Bash plan-scan/select/debate cron kikapcsolás | INFRA | 0.5 |

**Fázis 2 összesen: ~6-7 nap**

### 2.8 Rollback

Marvin cron kikapcsolása + bash plan-scan cron visszakapcsolása → <5 perc.

---

## 3. Fázis 3 — Marvin Reviewer + Nightwatch (vázlat)

> Fázis 3 Slice 2 előtt pontosítjuk. Jelenlegi scope: vázlat.

### 3.1 Reviewer → Marvin Task

```python
reviewer_a = marvin.Agent(name="Reviewer-A", model="claude-haiku-4-5-20251001", ...)
reviewer_b = marvin.Agent(name="Reviewer-B", model="claude-haiku-4-5-20251001", ...)

async def review_done(done_file: Path):
    verdict_a, verdict_b = await asyncio.gather(
        marvin.run("Review this DONE message.", agent=reviewer_a, result_type=ReviewVerdict),
        marvin.run("Review this DONE message.", agent=reviewer_b, result_type=ReviewVerdict)
    )
    if verdict_a.approved and verdict_b.approved:
        await run_pipeline(done_file)
    else:
        await create_reject_inbox(done_file, verdict_a, verdict_b)
```

### 3.2 Nightwatch → Marvin Scheduler

```python
# Marvin nem csinál cron-t natívan — systemd timer vagy APScheduler wrapper
from apscheduler.schedulers.asyncio import AsyncIOScheduler

scheduler = AsyncIOScheduler()
scheduler.add_job(nightwatch_cycle, 'interval', minutes=2)
scheduler.add_job(planning_cycle, 'interval', hours=2)
scheduler.add_job(librarian_trigger, 'interval', hours=5)
```

### 3.3 WorkflowStateTracker integráció

A McpServer `WorkflowStateTracker` (7-state FSM) átveszi a tmux session monitoring-ot:
- `initialized` → session létrehozva
- `briefed` → inbox olvasva
- `in_progress` → implementálás folyamatban
- `awaiting_input` → BLOCKED / QUESTION
- `ready_to_submit` → build+test zöld, DONE outbox írás előtt
- `submitted` → DONE outbox elküldve
- `failed` → session crash

### 3.4 RbacFilter integráció

A McpServer `RbacFilter` gépileg kényszeríti amit jelenleg a CLAUDE.md emberi szabályok próbálnak:
- Backend terminál NEM hívhat frontend tool-okat
- Tester NEM commitolhat kódot
- Root NEM írhat kódot

### 3.5 Fázis 3 becslés

~8-10 nap — de csak Slice 2 előtt indul, nem most.

### 3.6 Rollback

Marvin nightwatch kikapcsolása + bash nightwatch.sh cron visszakapcsolása.

---

## 4. Megvalósító terminálok

| Fázis | Feladat típus | Terminál | Indoklás |
|---|---|---|---|
| 1 | McpServer Node.js adaptálás | **INFRA** | Szerver config, Docker, service management |
| 1 | MCP regisztráció + cron | **INFRA** | VPS admin |
| 2 | Marvin Python agent definíciók | **ROOT** | Planning pipeline = Root felelősség |
| 2 | Thread lifecycle + MCP wrappers | **ROOT** | Üzleti logika az agent-ekben |
| 2 | Cron átállás | **INFRA** | Szerver config |
| 3 | Reviewer + nightwatch átírás | **ROOT** + **INFRA** | Vegyes: logika + infra |

**Nem kell új terminál** — a meglévő INFRA és ROOT elegendő.

---

## 5. Kockázatok és mitigation

| Kockázat | Fázis | Severity | Mitigation |
|---|---|---|---|
| VPS memória túlcsordulás (ChromaDB + Marvin + McpServer) | 1-2 | HIGH | VPS bővítés (Gábor ígérte). ChromaDB mem_limit: 512m. Marvin lightweight. |
| Marvin Anthropic provider érettség | 2 | MEDIUM | Pydantic AI backend — ha bug, fallback bash pipeline |
| ChromaDB adatvesztés upgrade-nél | 1 | LOW | Docker volume persistent. Teljes re-indexelés <1 perc (103 fájl) |
| Thread SQLite corruption | 2 | LOW | WAL mode + backup. Thread elvesztés = újraindítás, nem katasztrófa |
| voyage-3-lite API downtime | 1 | LOW | In-memory fallback a VectorStore-ban. Indexelés elhalasztható. |
| Párhuzamos futás interferencia (Fázis 2 átállás) | 2 | MEDIUM | Tesztperiódus: Marvin fut de NEM ír inbox-ot. Manuális összehasonlítás a bash output-tal. |

---

## 6. Architekturális döntések

### ADR-041 (proposed): Marvin mint Agent Orchestrátor

**Kontextus:** A bash planning pipeline (7 script, ~1200 sor) törékeny — ha egy script meghal, az egész ciklus elvész. Nincs resumability, nincs structured state, nincs audit trail.

**Döntés:** Marvin (Python) veszi át a planning pipeline-t. Tasks + Agents + Threads abstrakcióval. SQLite thread history resumability-t biztosít.

**Indoklás:**
- Resumable: Thread history SQLite-ben, crash után folytatható
- Multi-agent: 5 agent (Scanner, Selector, Debater A/B, Synthesizer) explicit definícióval
- Provider-agnostic: Pydantic AI backend, Anthropic modellekkel
- Structured output: Type-safe eredmények (Pydantic modellekkel)

**Elvetett alternatívák:**
| Alternatíva | Miért nem |
|---|---|
| LangChain/LangGraph | Python, de túl komplex, dependency hell (MSG-ARCH-007 tapasztalat) |
| Claude Code sub-agent-ek | Nem scheduled, nem resumable, nincs persistent state |
| Bash pipeline javítása | A törékeny alapokat nem érdemes foltozni |

### ADR-042 (proposed): JoineryTech.McpServer mint Knowledge Service

**Kontextus:** A SpaceOS terminálok és a planning pipeline szegmentáltan olvasnak dokumentációt. Nincs cross-szegmens szemantikus keresés.

**Döntés:** JoineryTech.McpServer adaptálva SpaceOS-ra. ChromaDB vector store + voyage-3-lite embedding + MCP stdio interface.

**Indoklás:**
- Production-tested: referencia implementáció már működik
- In-memory fallback: headless scanner futásoknál nincs ChromaDB dependency
- MCP natív: Claude Code session-ök tool-ként hívják
- RBAC: tool visibility gépi kényszerítéssel (Fázis 3)

**Supersedes:** MSG-ARCH-005 RAG spec (tsvector FTS) → archiválandó. A tsvector terv backup marad ha ChromaDB nem jön össze.

---

## 7. v2 Infra Review

| ID | Súly | Terület | Probléma | Javítás | Státusz |
|---|---|---|---|---|---|
| INF-P1 | HIGH | VPS memória | ChromaDB 512m + Marvin ~200m + McpServer ~100m = ~800m extra | VPS bővítés szükséges (8GB → 16GB). Gábor ígérte. **BLOKKOLÓ Fázis 1-re ha nincs bővítés.** | ⚠️ OPEN |
| INF-P2 | MEDIUM | Port foglalás | ChromaDB :8000 (localhost only) | OK — nincs konfliktus, Keycloak :8080, nincs más :8000 | ✅ RESOLVED |
| INF-P3 | MEDIUM | Python env | Marvin Python venv a VPS-en | `/opt/spaceos/spaceos-marvin/.venv/` — izolált, nem ütközik a rendszerrel | ✅ RESOLVED |
| INF-P4 | LOW | Docker | ChromaDB Docker container — Docker telepítve van a VPS-en? | Ellenőrizni: `docker --version`. Ha nincs → INFRA task. | ⚠️ CHECK |
| INF-P5 | LOW | Anthropic API cost | voyage-3-lite embedding + Marvin agent hívások | ~$0.001/indexelés + agent hívások a meglévő API key-en. Elhanyagolható. | ✅ ACCEPTED |

---

## 8. Teljes ütemterv

```
Fázis 1 (4-5 nap) — McpServer knowledge service
  ↓ VPS bővítés szükséges
  ↓ parallel a Slice 1 fejlesztéssel
Fázis 2 (6-7 nap) — Marvin planning pipeline
  ↓ Fázis 1 kész + tesztperiódus
  ↓ bash pipeline kikapcsolás utána
Fázis 3 (8-10 nap) — Marvin reviewer + nightwatch
  ↓ Slice 2 előtt
  ↓ teljes bash → Marvin átállás
```

**Teljes migráció:** ~18-22 nap (nem szekvenciális — Fázis 1-2 parallel a Slice 1-gyel)
