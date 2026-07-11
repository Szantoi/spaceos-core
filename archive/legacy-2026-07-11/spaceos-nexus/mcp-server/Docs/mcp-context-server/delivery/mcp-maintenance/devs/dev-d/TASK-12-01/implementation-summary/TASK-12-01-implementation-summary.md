### TASK-12-01 Implementation Summary

Date: 2026-03-12
Status: Completed

Implemented:

- Added and stabilized `src/episodic/EpisodeStore.ts` as the core storage service.
- Ensured schema bootstrap is idempotent and migration-driven (`003_episodes.sql`).
- Enforced 5MB payload limit with explicit `episode_size_exceeded` error code.
- Implemented retrieval methods: `getEpisode`, `getEpisodesBySession`, `getEpisodesByDomainTrackSession`.
- Added structured JSON round-trip handling for `tool_calls_json` and `artifacts_json`.

Acceptance criteria coverage:

- AC-1: `episodes` table exists with required columns and constraints.
- AC-2: Composite and session indexes verified via schema and performance tests.
- AC-3: Oversized payload rejection validated.
- AC-4: CRUD and serialization behaviors validated.

Verification:

- `npx vitest run src/tests/unit/episode.schema.test.ts`
- Result: 17/17 tests passed.
