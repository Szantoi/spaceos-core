---
id: SPEC-007
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_009_marvin-migration-done.md
type: Architecture spec & Implementation plan (Infrastructure)
scope: [INFRA, ROOT, NEXUS]
priority: high
complexity: 4
dependencies: [INF-P1 (VPS memory), INF-P4 (Docker availability)]
status: NEW
created: 2026-06-17
adrs: [ADR-041, ADR-042]
---

# Marvin + McpServer Migration Plan (v2)

## Összefoglaló

Agent orchestration és knowledge service infrastruktúra migrációs terv. Marvin (Anthropic ágense) 5 agent-es planning pipeline + ChromaDB-alapú knowledge service (McpServer). 3 fázis, 18-22 nap, **infra függőséggel blokkolva** (VPS 8GB → 16GB).

## Scope

- **INFRA:** VPS bővítés (memória), Docker telepítés/validáció
- **ROOT:** Marvin koordináció, Agent prompt validation, Anthropic API rate limiting
- **NEXUS:** McpServer knowledge service integráció, voyage-3-lite embeddings, ChromaDB maintenance

## Fázisok

### Fázis 1 — McpServer Knowledge Service (4-5 nap)
**Kockázat: 0 (izolált)**

- ChromaDB állomány (in-memory fallback + persistent PostgreSQL sync)
- voyage-3-lite embeddings (Anthropic vendor lock-in, ~$0.001/indexálás)
- MCP (Model Context Protocol) server (/opt/spaceos/spaceos-nexus/knowledge-service/)
- Dokumentumok betöltése: ADRs, CLAUDE.md-k, Vision docs, Module boundaries
- Haiku review validáció (semantic search QA)

**Output:** JoineryTech.McpServer NuGet package + knowledge index base

### Fázis 2 — Marvin Planning Pipeline (6-7 nap)
**Kockázat: LOW (parallel testing)**

- 5 agent orchestration:
  - **Scanner (Haiku):** Knowledge base Query → 5 ide ötlet (RAG)
  - **Selector (Sonnet):** Consensus filtering → 3 top idea
  - **Debater A (Sonnet):** Pro arguments
  - **Debater B (Sonnet):** Contra arguments
  - **Synthesizer (Sonnet):** Final proposal (ADR draft)

- Thread resumability: SQLite history store
  - Crash után folytatható (bash-ban elveszne az állapot)
  - Agent state logging, token accounting

- Integration: `scripts/plan-scan.sh` → `plan-select.sh` → Marvin pipeline

**Output:** Marvin CLI tool, agent thread history DB, consensus JSON

### Fázis 3 — Marvin Reviewer + Nightwatch Integration (8-10 nap)
**Kockázat: MEDIUM**

- Marvin-based `reviewer.sh` replacement (1-2 detailed review agent helyett)
- nightwatch.sh integrációk (DONE detection, stuck session nudge)
- Vázlat ~ Slice 2 előtti finalizálás

**Output:** Production-ready reviewer pipeline, logging/monitoring

---

## Kulcs Döntések

| # | Döntés | Indoklás |
|---|--------|---------|
| 1 | **ChromaDB** | MSG-ARCH-005 tsvector terv superseded; production-tested, Chroma API stabil, in-memory fallback |
| 2 | **voyage-3-lite** | Anthropic vendor consistency, cost-effective (~$0.001/indexálás), multi-language |
| 3 | **5 agent** | Scanner → Selector → Debater A/B → Synthesizer; Sonnet für complex logic, Haiku scan |
| 4 | **Thread resumability** | SQLite history; Marvin SDK built-in, bash pipeline-ben elveszne state |
| 5 | **Nem új terminál** | INFRA (server) + ROOT (orchestration) elegendő; NEXUS advisory |

---

## ADRs

- **ADR-041** (PROPOSED): Marvin mint Agent Orchestrátor
  - Scope: planning pipeline, reviewer, night watch
  - Replaces: bash pipeline state management
  - Trade-off: Python dependency (Marvin, Anthropic SDK)

- **ADR-042** (PROPOSED): JoineryTech.McpServer mint Knowledge Service
  - Scope: centralized documentation, ADR, architecture knowledge
  - Replaces: MSG-ARCH-005 (tsvector FTS) → ChromaDB
  - Trade-off: new NuGet package, chromadb.so dependency, embeddings cost

---

## Kockázatok & Blockerek

### BLOCKER: INF-P1 — VPS Memory Expansion
- **Status:** Awaiting Gábor confirmation
- **Requirement:** 8GB → 16GB RAM
- **Why:** ChromaDB (~300MB) + Marvin processes (~400MB) + McpServer (~100MB) = ~800MB extra
- **Impact:** Fázis 1 nem indítható nélküle
- **Action:** ROOT ellenőrizze, INFRA scheduling

### BLOCKER: INF-P4 — Docker on VPS
- **Question:** Docker telepítve / elérhető?
- **Requirement:** ChromaDB container + optional Marvin container orchestration
- **Action:** INFRA validate + setup ha szükséges

### RISK: Marvin Anthropic Provider Maturity
- **Mitigation:** Pydantic AI backend fallback; ha Marvin bug, bash pipeline continue
- **Token counting:** Marvin SDK provides token tracking per agent

### RISK: Knowledge Index Maintenance
- **Mitigation:** Automation: librarian.sh trigger on docs/* change
- **Window:** ~500ms re-index (single-doc delta)

---

## Implementációs Javaslat

### 1. VPS Setup (INFRA)
```bash
# INF-P1: Memory check & expansion approval
# INF-P4: Docker validation
sudo docker --version
docker run --rm hello-world
```

### 2. McpServer Build (ROOT + NEXUS)
```
/opt/spaceos/spaceos-nexus/knowledge-service/
  ├── Dockerfile
  ├── chromadb-config.json
  ├── voyage-loader.ts
  └── mcp-server.ts
```

### 3. Marvin Pipeline (ROOT)
```
/opt/spaceos/scripts/
  ├── marvin-pipeline.py      (orchestrator)
  ├── agents/
  │   ├── scanner.py           (Haiku RAG)
  │   ├── selector.py          (Sonnet consensus)
  │   ├── debater-pro.py       (Sonnet A)
  │   ├── debater-contra.py    (Sonnet B)
  │   └── synthesizer.py       (Sonnet ADR)
  └── storage/
      └── thread-history.db    (SQLite)
```

### 4. Integration (nightwatch.sh)
- Marvin scoring DONE detection
- Agent thread logging to logs/marvin/
- Anthropic rate limit monitoring

---

## Eredeti Dokumentum

[2026-06-16_009_marvin-migration-done.md](/opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_009_marvin-migration-done.md)

## Kapcsolódó Dokumentumok

- `docs/vision/SpaceOS_Vision_Master.md` — Datahaven/Resonance agent infrastructure context
- `docs/agent-infrastructure/ROADMAP.md` — Agent system evolution
- `MSG-ARCH-005` — tsvector FTS terv (superseded by this spec)
- `ADR-041`, `ADR-042` — Decision records (to be created)

---

## Next Steps

1. **INFRA ellenőrzés:** INF-P1 + INF-P4 status
2. **ADR formal submission:** ADR-041, ADR-042 draft
3. **Fázis 1 greenlight:** Ha infra ready → NEXUS knowledge-service build start
4. **Timeline tracking:** Sprint planning, 3 fázis parallelize ahol lehetséges
