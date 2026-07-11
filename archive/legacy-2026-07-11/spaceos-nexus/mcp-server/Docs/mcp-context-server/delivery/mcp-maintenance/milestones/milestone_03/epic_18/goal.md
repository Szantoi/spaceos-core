---
id: goal-epic-18
title: "EPIC-18 Goal: Self-Reflection & Memory Quality (Episodic Highlights)"
type: goal
epic: EPIC-18
sphere: mcp-context-server
milestone: M03
created: 2026-03-04
---

# EPIC-18 Goal: Self-Reflection & Memory Quality (Episodic Highlights)

## Executive Summary

Build the **closed-loop learning system**: after each agent session (M02 EPIC-12, which stores episodes),
automatically generate **episode highlights** (key decisions, lessons learned, next steps) using LLM reflection.
Highlight summaries are indexed in ChromaDB + FTS5 for semantic + keyword search. Future agents can learn from
these highlights via the `reflect_session()` tool.

**This completes Vision Goal #5: Self-Improving Memory — agents become smarter with each session.**

---

## Strategic Context (Vision Goals Addressed)

- **Goal #5:** Self-improving memory — Sessions generate learnings → indexed → reused
- **Goal #3 foundation:** Two-track reflection — Highlights include DWI phases + FSM stages
- **Broader goal:** Continuous improvement — System improves without manual intervention

---

## Key Principles

1. **Auto-reflection** — Post-session, LLM generates highlights automatically
2. **Dual-indexing** — Highlights in both FTS5 (keyword) + ChromaDB (semantic)
3. **Quality feedback loop** — Agents (or admins) rate highlight quality → system learns
4. **Non-invasive** — Reflection doesn't interfere with live session; happens after completion

---

## Success Criteria

### Episode Highlights Generation
- [ ] `generate_episode_highlights(session_id)` MCP tool (called post-session)
- [ ] LLM delegated via sampling (EPIC-14)
- [ ] Highlights include: key_decisions[], lessons[], next_steps[], quality_score
- [ ] Generated highlights stored in `episode_highlights` table
- [ ] Generation latency: < 2 seconds (async acceptable)

### Highlight Storage
- [ ] `episode_highlights` table: id, episode_id, key_decisions, lessons, next_steps, quality_score, ai_generated, created_at
- [ ] Unique constraint: 1 highlight row per episode
- [ ] Linked to episodes table via FK
- [ ] Manual feedback: `highlight_feedback` table for quality ratings

### FTS5 Indexing
- [ ] `highlights_fts` virtual table on highlights.lessons + highlights.next_steps
- [ ] Query example: `SELECT * FROM highlights_fts WHERE content MATCH 'validate idea'`
- [ ] Performance: < 100ms for 1000+ highlights

### ChromaDB Indexing
- [ ] ChromaDB collection: `episode_highlights` (distinct from EPIC-12 `episodes`)
- [ ] Embedding: (key_decisions + lessons + next_steps) concatenated → embedding
- [ ] Search: `search_highlights(query)` returns semantically similar highlights
- [ ] Performance: < 200ms for 1000+ highlights

### MCP Tools
- [ ] `generate_episode_highlights(session_id)` — LLM-assisted reflection
- [ ] `reflect_session(session_id)` — Query prior highlights + summarize learnings
- [ ] `tag_episode_quality(highlight_id, quality_score, comment)` — Manual feedback
- [ ] `get_session_summary(session_id)` — Plain English session recap

### Session Learning Loop
- [ ] Session #1: agent works → stores episode (EPIC-12) → `generate_episode_highlights()` creates highlight
- [ ] Session #2: agent bootstraps → prior highlights returned as context → agent references "Last time we learned..."
- [ ] Session #3+: memory grows; agent has increasing historical context

### Quality Assurance
- [ ] Highlight quality score: (ai_generation_confidence + manual_feedback_average) / 2
- [ ] Low-quality highlights (< 0.5) can be filtered or regenerated
- [ ] Audit: LLM cost tracking (tokens used) + ratios

### Testing
- [ ] Unit test: highlight generation produces correct structure
- [ ] Unit test: FTS5 search finds highlights by keyword
- [ ] Unit test: ChromaDB search returns semantically similar highlights
- [ ] E2E test: session → highlight generation → searchable
- [ ] E2E test: reflect_session() includes prior highlights + lessons
- [ ] E2E test: quality feedback loop works

---

## Deliverables

| Deliverable | Type | Location |
|:-----------|:-----|:---------|
| Highlights Schema | Code | `src/metadata/episodeSchema.ts` (extend) |
| Highlight Generator Tool | Code | `src/mcp/tools/memory.ts` (generate_episode_highlights) |
| Reflect Session Tool | Code | `src/mcp/tools/memory.ts` (reflect_session) |
| Quality Feedback Tool | Code | `src/mcp/tools/memory.ts` (tag_episode_quality) |
| ChromaDB Highlights Collection | Code | `src/rag/episodicMemory.ts` (highlights collection) |
| FTS5 Highlight Index | Code | `src/metadata/initAgentDb.ts` (highlights_fts table) |
| LLM Reflection Prompts | Code | `src/mcp/sampling/reflectionPrompts.ts` |
| Quality Scoring | Code | `src/metadata/qualityScoring.ts` |
| E2E Test Suite | Tests | `src/tests/e2e/epic-18-self-reflection.spec.ts` |
| Reflection Architecture Docs | Docs | `Docs/mcp-context-server/architecture/self-reflection.md` |
| Implementation Summary | Report | `implementation-summary/EPIC-18-<date>.md` |

---

## Database Schema Additions

```sql
-- Episode highlights (generated from episodes)
CREATE TABLE episode_highlights (
  id TEXT PRIMARY KEY,
  episode_id TEXT UNIQUE,
  key_decisions TEXT, -- JSON array: ["decision 1", "decision 2", ...]
  lessons TEXT, -- JSON array: ["lesson 1", "lesson 2", ...]
  next_steps TEXT, -- JSON array: ["step 1", "step 2", ...]
  quality_score FLOAT(0.0, 1.0), -- Average of ai_confidence + manual feedback
  ai_generated BOOL DEFAULT TRUE,
  ai_model TEXT, -- e.g., "gpt-4", "claude"
  ai_tokens_used INTEGER,
  created_at TIMESTAMP,
  FOREIGN KEY (episode_id) REFERENCES episodes(id) ON DELETE CASCADE
);

-- Quality feedback (manual ratings of highlights)
CREATE TABLE highlight_feedback (
  id TEXT PRIMARY KEY,
  highlight_id TEXT NOT NULL,
  rater_agent_id TEXT, -- Who rated (agent or human)
  quality_score FLOAT(0.0, 1.0),
  comment TEXT,
  rated_at TIMESTAMP,
  FOREIGN KEY (highlight_id) REFERENCES episode_highlights(id) ON DELETE CASCADE
);

-- FTS5 virtual table for highlight search
CREATE VIRTUAL TABLE highlights_fts USING fts5(
  key_decisions,
  lessons,
  next_steps,
  content=episode_highlights,
  content_rowid=id
);

-- ChromaDB metadata sync
CREATE TABLE highlights_chromadb_sync (
  highlight_id TEXT PRIMARY KEY,
  vector_id TEXT,
  embedding_model TEXT,
  last_synced TIMESTAMP,
  FOREIGN KEY (highlight_id) REFERENCES episode_highlights(id)
);
```

---

## Tool API

```typescript
// generate_episode_highlights
request: {
  session_id: string
}
response: {
  highlight_id: string,
  episode_id: string,
  key_decisions: string[],
  lessons: string[],
  next_steps: string[],
  quality_score: number,
  generated_at: ISO8601
}

// reflect_session
request: {
  session_id: string,
  include_prior_highlights: boolean (default: true)
}
response: {
  session_summary: string, -- Plain English recap
  current_highlights: {...}, -- This session's highlights
  prior_highlights: [{...}], -- Related highlights from past sessions
  learnings_applied: string[], -- "We applied X learning from session #N"
  reflection_at: ISO8601
}

// tag_episode_quality
request: {
  highlight_id: string,
  quality_score: 0.0..1.0,
  comment?: string
}
response: {
  highlight_id: string,
  new_quality_score: number,
  feedback_recorded_at: ISO8601
}

// get_session_summary
request: {
  session_id: string
}
response: {
  summary: string,
  breakdown: {
    phase: string,
    achievements: string[],
    blockers: string[],
    next_recommended_phase: string
  }
}
```

---

## LLM Reflection Prompt (Example)

```
System: You are a session reflection engine. Given an episode (session history),
        generate structured highlights for future learning.
        Focus on: decisions made, lessons learned, and recommended next steps.

User: Episode #xyz (discovery/architect, ideation phase)
      Tool calls: [create_artifact x5, submit_discovery_outcome x2]
      Artifacts: [idea_list.json, validation_report.md]
      Phase duration: 45 minutes

      Generate response JSON:
      {
        "key_decisions": [
          "Decided to focus on 3 core ideas instead of 10",
          "Rejected idea #X due to cost constraints"
        ],
        "lessons": [
          "Brainstorm divergence first (ideate 10+), then converge to 3-5",
          "Cost validation early prevents rework"
        ],
        "next_steps": [
          "Move top 3 ideas to validation phase",
          "Request stakeholder feedback on cost assumptions"
        ]
      }
```

---

## Task Breakdown

- **TASK-18-01:** Episode highlights schema design
- **TASK-18-02:** `generate_episode_highlights()` tool (LLM-assisted)
- **TASK-18-03:** `reflect_session()` tool (search + summarization)
- **TASK-18-04:** `tag_episode_quality()` tool + feedback loop
- **TASK-18-05:** ChromaDB highlights collection setup
- **TASK-18-06:** FTS5 highlights indexing
- **TASK-18-07:** Quality score calculation
- **TASK-18-08:** LLM sampling integration
- **TASK-18-09:** E2E test: full reflection cycle
- **TASK-18-10:** Cost tracking + LLM audit log
- **TASK-18-11:** Documentation + examples

---

## Blocks/Enablers

| Dependency | Status | Impact |
|:-----------|:-------|:--------|
| EPIC-12 (episodic memory) | ✅ M02 EPIC-12 | ✅ Episodes table + storage |
| EPIC-09 (SQLite) | ✅ M02 EPIC-09 | ✅ Base DB ready |
| EPIC-14 (sampling) | ✅ M02 EPIC-14 | ✅ LLM delegation available |
| ChromaDB | ✅ M01 | ✅ Ready to use |

---

## Design Decisions

| Decision | Rationale |
|:---------|:----------|
| Post-session reflection | Non-blocking; doesn't interfere with live agent work |
| LLM-assisted generation | Automatic highlights better than manual; interpretable |
| Dual-indexing (FTS5 + ChromaDB) | Keyword + semantic coverage; flexibility |
| Quality feedback loop | Manual correction improves future generations |
| Immutable highlights | Audit trail; no deletion (retention policy M05) |

---

## Success Metrics (M03 Post-Launch)

- ✅ EPIC-18 tasks complete + all tests green
- ✅ Highlight generation latency < 2 seconds
- ✅ reflect_session() returns related highlights 80%+ accuracy (manual eval)
- ✅ Session #2+ agents reference prior session learnings in 50%+ of interactions
- ✅ Manual quality ratings average ≥ 0.75 (scale 0-1)
- ✅ E2E test: full reflection cycle (session → highlight → search → reuse) < 5 seconds
- ✅ M03 demo: agent session #2 references session #1 learnings explicitly

---

## Metrics for Continuous Improvement (Post-M03)

- Quality score trend: should increase as feedback loop matures (M04+)
- Highlight reuse rate: % of highlights referenced in future sessions
- Agent decision quality: subjective eval (did highlights help?)
- LLM cost per session: tokens used vs. value generated

---

## Related Documentation

- `Docs/goal.md` § Vision Goal #5 — Self-improving memory
- EPIC-12 goal.md — Episodic memory storage foundation
- EPIC-14 goal.md — LLM sampling delegation
- `database/standards/00-foundation/two-track.meta-framework.md` — Learning loops
- MCP sampling docs: `get-library-docs mcp-sampling`
- ChromaDB embedding guide: `https://docs.trychroma.com/embeddings`
