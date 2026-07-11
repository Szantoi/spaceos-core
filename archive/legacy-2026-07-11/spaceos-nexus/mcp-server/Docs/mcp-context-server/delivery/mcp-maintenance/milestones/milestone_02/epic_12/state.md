---
id: epic-mcp-maintenance-12
title: "Epic 12: Episodic Memory Layer (Phase 1)"
type: epic
milestone: M02
project: mcp-maintenance
status: "COMPLETE — T12-01..04 ALL DONE"
fsm_state: "DONE"
fsm_retry_count: 0
updated: 2026-03-12
---

# EPIC-12 State Summary

## Hogyan lesz megoldva
- Az episodic adatok SQLite `episodes` + FTS5 indexbe kerulnek gyors keyword kereseshez.
- A szemantikus visszakereses ChromaDB `episodic` kollekcioval egeszul ki.
- MCP tool layer: `store_experience` es `search_experience` vegpontok.
- A keresesi pipeline ketfazisu: FTS5 eloszures, majd semantic relevance merge.
- Phase-1 scope szigoruan storage + retrieval; optimalizacios retegek kesobbre halasztva.

## Levont tapasztalatok (tervezesi)
- A szukitett Phase-1 scope (core only) csokkenti a scope creep kockazatat.
- A hybrid FTS5 + vector megkozelites jobb aranyt ad sebesseg es relevancia kozott.
- SLA-hoz kotott AC (latency celok) nelkul nehez objektiven lezarni.

## Celallapot
- AC target: 16/16 ✅
- Teszt target: 39+ ✅ (52 teszt: 17+7+2+26 zöld)
- Kimenet: Production-grade episodic storage és retrieval alap ✅

## Megvalósított fájlok
- `src/episodic/types.ts` — Episode, StoreExperienceParams, EpisodeDomain típusok
- `src/episodic/EpisodeStore.ts` — SQLite CRUD + FTS5 trigger + ChromaDB hybrid (TASK-12-01)
- `src/episodic/FtsSearch.ts` — FTS5 keyword search + SQL injection védelem (TASK-12-02)
- `src/episodic/EpisodicChromaClient.ts` — ChromaDB adapter + EmbeddingCache (TASK-12-03)
- `src/episodic/EmbeddingCache.ts` — Embedding cache (30-50% cost reduction)
- `src/episodic/EpisodeManager.ts` — High-level facade; session+store+search API (TASK-12-04)
- `src/mcp/tools/memory.ts` — MemoryPlugin: save_episode, query_memory, search_memory

## Teszt lefedettség
| Test file | Task | Tesztek |
|:----------|:-----|:--------|
| `episode.schema.test.ts` | T12-01 | 17 ✅ |
| `fts.search.test.ts` | T12-02 | 7 ✅ |
| `semantic.search.test.ts` | T12-03 | 2 ✅ |
| `episodic-e2e.test.ts` | T12-04 | 23 ✅ |
| `episodic-hybrid.test.ts` | T12-04 | 3 ✅ |
| **Összesen** | | **52 ✅** |
