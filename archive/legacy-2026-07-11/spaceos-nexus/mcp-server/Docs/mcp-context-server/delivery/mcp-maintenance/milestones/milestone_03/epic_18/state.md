---
id: epic-mcp-maintenance-18
title: "Epic 18: Self-Reflection & Memory Quality (Episodic Highlights)"
type: epic
milestone: M03
project: mcp-maintenance
project_id: mcp-context-server
status: in_progress
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "IN_PROGRESS"
fsm_retry_count: 0
created: 2026-03-04
assignee: backend_developer
depends_on: EPIC-12
---

# 🧠 EPIC-18: Self-Reflection & Memory Quality (Episodic Highlights)

## Célkitűzés

Az EPIC-12 episodic memory alapján EPIC-18 feladata az **MCP szerver "tanulása"** megvalósítása:
az agent session-ek után automatikus **"highlights" generálása** (mit tanultunk, mi működött, mi nem),
majd ezek **ChromaDB-ba** indexelődnek semantic search-hez. Későbbi sessions tudnak ezekből tanulni.

**Végállapot:** Az agent 2. session-ben képes a korábbi session tanulságaira hivatkozni;
a memory quality folyamatosan javul.

---

## Kontextus és Motiváció

M02-ben az EPIC-12 bevezetett episodic memory storage-t (SQLite FTS5 + ChromaDB). De az
"milyenbe tanultunk" kérdés még nyitott:

```
Session #1: agent-discovery → ideation → 3 ideas generated
Session #2: agent-discovery → validation → "Did I have prior discoveries to validate against?"
           → search_experience("discovery") → Session #1 3 ideas returned!
           → Agent reflects: "These were good. Let me validate idea #2 deeper."
```

EPIC-18 feladata: **automatikus highlight-generálás** az EPIC-12 session-tekből, hogy
Session #2 szeminálisan searchable tanulságokat találjon.

---

## Érintett MCP Tool-ok (New/Modified)

| Tool | Purpose |
|:-----|:--------|
| `store_experience(episode)` | EPIC-12 — session befejeztél után episode tárolása |
| `search_experience(query, track)` | E2E — older episodes szeminálisan keresése |
| `generate_episode_highlights()` | NEW — session highlights generálása (ai-assisted) |
| `reflect_session(session_id)` | NEW — utolsó session reflexiós kör ("mit tanultam?") |
| `tag_episode_quality(episode_id, quality_score)` | NEW — manual feedback loop |

---

## Workflow: Self-Reflection Loop

```
┌─ Session Start ─────────────────────────────────────────┐
│ agent("discovery/architect") → bootstrap + auth        │
│ request_context("ideation") → workflow + tools         │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
         [IDEATION PHASE]
       - brain-storm ideas
       - submit_artifact() calls with ideas
       - tool_calls tracked
┌─────────────────────────────────────────────────────────┐
│ Session End → store_experience(episode)               │
│ episode = {session_id, tool_calls[], artifacts[],      │
│             phase, domain, role, timestamp}             │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
       [LLM REFLECTION - NEW]
    generate_episode_highlights()
    - "Key decisions: 3 ideas generated, 1 dismissed"
    - "Lessons: Brainstorm divergence > premature filtering"
    - "Next steps: validation phase for ideas #1, #2"
    - highlight = {episode_id, key_decisions[], lessons[],
                   next_steps[], quality_score}
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
    [STORE HIGHLIGHTS + INDEX]
    - SQLite: INSERT episode_highlights
    - ChromaDB: embed highlights → search
    - FTS5: index lesson strings
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
   [FUTURE SESSION - LEARNING]
   search_experience("ideation in product domain")
   → 5 prior episode_highlights returned
   → agent uses as context for current ideation
```

---

## Key Tables (EPIC-12 + EPIC-18 combined)

```sql
-- Episodes (from EPIC-12)
CREATE TABLE episodes (
  id TEXT PRIMARY KEY,
  session_id TEXT,
  domain TEXT,
  track TEXT, -- "discovery" | "delivery"
  phase TEXT, -- "ideation" | "validation" | ...
  tool_calls_json TEXT,
  artifacts_json TEXT,
  created_at TIMESTAMP
);

-- Episode Highlights (NEW - EPIC-18)
CREATE TABLE episode_highlights (
  id TEXT PRIMARY KEY,
  episode_id TEXT UNIQUE,
  key_decisions TEXT[], -- JSON array
  lessons TEXT[],       -- JSON array
  next_steps TEXT[],    -- JSON array
  quality_score FLOAT (0.0-1.0),
  ai_generated BOOL,
  created_at TIMESTAMP,
  FOREIGN KEY (episode_id) REFERENCES episodes(id)
);

-- Quality feedback (manual)
CREATE TABLE highlight_feedback (
  id TEXT PRIMARY KEY,
  highlight_id TEXT,
  agent_id TEXT,
  quality_score FLOAT,
  comment TEXT,
  created_at TIMESTAMP,
  FOREIGN KEY (highlight_id) REFERENCES episode_highlights(id)
);
```

---

## Sikerkritérium (Definition of Done)

- [ ] `generate_episode_highlights()` MCP tool elérhetővé (calls LLM for reflection)
- [ ] `reflect_session(session_id)` tool: 5-10 prior highlights keresése + summary generálása
- [ ] `episode_highlights` SQLite table, `highlight_feedback` table létrehozva
- [ ] ChromaDB collection: "episode_highlights" vectorized, searchable
- [ ] FTS5 index: highlights.lessons + highlights.next_steps
- [ ] Quality score formula: (manual_feedback + semantic_similarity_to_similar_episodes) / 2
- [ ] E2E teszt:
  - [ ] Session #1: ideation → store_experience() + generate_highlights()
  - [ ] Session #2: search_experience() → prior highlights returned
  - [ ] Session #2: reflect_session() → summary includes prior lessons
- [ ] Zero breaking changes to existing E2E tests
- [ ] Reflection LLM cost tracked (log to audit_log)

---

## LLM Reflection Prompting (Draft)

```
System: You are a self-reflection engine for an AI agent.
        Given a session's tool calls and artifacts, generate structured highlights.

User: Session #{id} (discovery/architect, ideation phase)
      Tool calls: {5 create_artifact calls with brainstorm ideas}
      Artifacts: {3 ideas JSON}

      Generate highlights in JSON format:
      {
        "key_decisions": [...],
        "lessons": ["Divergence before convergence = better ideas"],
        "next_steps": ["Validate idea #1 with stakeholders"],
        "quality_score": 0.8
      }
```

---

## Nem Scope (EPIC-18-ben)

- Agent-to-agent knowledge sharing (M04 collaboration)
- Permanent memory pruning / forgetting (M05 data hygiene)
- Multi-session coherence scoring (future ML model)
- Reflection loop failure recovery (M04 resilience)

---

## Task Breakdown

- [x] TASK-18-01: Episode + highlights schema design (✅ implemented)
- [x] TASK-18-02: `generate_episode_highlights()` tool (LLM-assisted) (✅ implemented, runtime-verified)
- [x] TASK-18-03: `reflect_session()` tool (search + summarization) (✅ implemented, runtime-verified)
- [x] TASK-18-04: ChromaDB episode_highlights collection setup (✅ implemented, runtime-verified)
- [x] TASK-18-05: FTS5 indexing on highlights (✅ implemented, 15/15 tests green)
- [x] TASK-18-06: Quality score computation (implemented, unit tests green)
- [ ] TASK-18-07: `tag_episode_quality()` feedback loop (task file ready, pending implementation)
- [ ] TASK-18-08: E2E test: full self-reflection cycle (task file ready, pending implementation)
- [ ] TASK-18-09: LLM cost logging + audit (task file ready, pending implementation)

**Task file status / planning readiness:**

- [x] Decomposition complete (9/9 tasks)
- [x] Dependencies mapped (schema -> generation/indexing -> reflection -> e2e)
- [x] Acceptance criteria in Given/When/Then format
- [x] Definition of Done defined per task
- [x] Affected files, implementation hints, and risks documented

**Current implementation status:**

- [x] TASK-18-01 implemented: migration `009_epic18_highlights_schema.sql`, AgentDb integration, unit tests
- [x] TASK-18-02 implemented: tool handler, latest-episode lookup, reflection prompt, focused unit tests, regression fixes
- [x] TASK-18-02 runtime verification complete (3/3 tests green)
- [x] TASK-18-03 implemented: read-only retrieval bridge, `reflect_session()` tool, unit tests
- [x] TASK-18-03 runtime verification complete (3/3 tests green)
- [x] TASK-18-04 implemented: dedicated highlight collection sync/search, sync metadata persistence, unit tests
- [x] TASK-18-04 runtime verification complete (4/4 tests green)
- [x] TASK-18-05 implemented: **FTS5 migration, AgentDb `searchHighlightsFts()`, 9 comprehensive unit tests**
- [x] TASK-18-05 runtime verification complete: **9/9 tests green, performance benchmark 1ms <100ms threshold**
- [x] Consolidated EPIC-18 test suite: **15/15 tests green across tasks 18-02/03/05**
- [ ] TASK-18-06..TASK-18-09 implementation pending

---

## Függőségek

| Függőség | Állapot | Hatás |
|:---------|:--------|:------|
| EPIC-12: Episodic Memory | ✅ M02 | episode storage + ChromaDB basis |
| EPIC-09: SQLite schema | ✅ M02 | episode_highlights table |
| LLM sampling | ✅ EPIC-14 M02 | Reflection LLM delegation |
| ChromaDB | ✅ M01 | Highlights vectorization |

---

## Success Metrics (M03 Post-Launch)

- 🎯 Agent session #2+ references prior learnings 50%+ of time
- 🎯 highlight_feedback quality_score avg ≥ 0.75/1.0 (good reflection quality)
- 🎯 E2E test session-cycle time ≤ 20s (reflection+storage included)
- 🎯 ChromaDB semantic search relevance ≥ 0.8 (cosine similarity threshold)

---

## Kapcsolódó Dokumentáció

- Vision Goal #5: "Self-Improving Memory"
- EPIC-12: Episodic Memory Layer
- `database/standards/00-foundation/two-track.meta-framework.md` — learning loops
- MCP sampling docs: `get-library-docs mcp-sampling`
