/**
 * Episode Schema Tests — TASK-12-01 (Episodic Memory Foundation)
 *
 * Validates:
 *  - AC-1: Schema exists (episodes table + 9 columns)
 *  - AC-2: Index creation + performance targets (<10ms for domain+track+session, <5ms for session_id)
 *  - AC-3: Size enforcement (5MB limit)
 *  - AC-4: CRUD operations, UUID generation, data integrity
 *
 * Test count: 14 (exceeds AC-4 requirement of 10+)
 *
 * Uses an in-memory SQLite database for isolation (no side-effects on agent.db)
 */

import Database from 'better-sqlite3';
import { describe, test, expect } from 'vitest';
import { EpisodeStore } from '../../episodic/EpisodeStore';
import { EPISODE_MAX_SIZE_BYTES, StoreExperienceParams } from '../../episodic/types';

// ─────────────────────────────────────────────────────────────────────────────
// HELPERS
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Create an in-memory SQLite database and apply the episodes migration.
 * Returns both the db and a fresh EpisodeStore for each test.
 */
function createTestDb(): { db: Database.Database; store: EpisodeStore } {
    const db = new Database(':memory:');
    db.pragma('foreign_keys = ON');
    const store = new EpisodeStore(db);
    return { db, store };
}

/**
 * Build a minimal valid StoreExperienceParams object.
 */
function makeParams(overrides: Partial<StoreExperienceParams> = {}): StoreExperienceParams {
    return {
        sessionId: 'sess-test-001',
        domain: 'engineering',
        track: 'user-story-42',
        phase: 'implementation',
        outcomeSummary: 'Implemented the widget factory pattern.',
        ...overrides,
    };
}

/**
 * Seed `n` episodes into the store for performance benchmarking.
 */
async function seedEpisodes(store: EpisodeStore, n: number, sessionId = 'sess-bench'): Promise<void> {
    for (let i = 0; i < n; i++) {
        await store.storeExperience(makeParams({
            sessionId,
            domain: 'engineering',
            track: `track-${i % 10}`,
            outcomeSummary: `Episode ${i}`,
        }));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TEST SUITE
// ─────────────────────────────────────────────────────────────────────────────

describe('TASK-12-01: Episode Schema & Storage', () => {

    // ── AC-1: Schema Created ────────────────────────────────────────────────

    describe('AC-1: Schema Created', () => {
        test('episodes table exists after EpisodeStore construction', () => {
            const { db } = createTestDb();
            const row = db.prepare(
                "SELECT name FROM sqlite_master WHERE type='table' AND name='episodes'"
            ).get();
            expect(row).toBeDefined();
        });

        test('episodes table has all 9 required columns', () => {
            const { db } = createTestDb();
            const columns = db.prepare("PRAGMA table_info(episodes)").all() as Array<{ name: string }>;
            const names = columns.map(c => c.name);

            expect(names).toContain('id');
            expect(names).toContain('session_id');
            expect(names).toContain('domain');
            expect(names).toContain('track');
            expect(names).toContain('phase');
            expect(names).toContain('tool_calls_json');
            expect(names).toContain('artifacts_json');
            expect(names).toContain('outcome_summary');
            expect(names).toContain('created_at');
            expect(names).toHaveLength(9);
        });

        test('schema is idempotent — second EpisodeStore construction does not throw', () => {
            const { db } = createTestDb();
            expect(() => new EpisodeStore(db)).not.toThrow();
        });
    });

    // ── AC-2: Index Creation ─────────────────────────────────────────────────

    describe('AC-2: Index Creation', () => {
        test('idx_episodes_domain_track_session index exists', () => {
            const { db } = createTestDb();
            const idx = db.prepare(
                "SELECT name FROM sqlite_master WHERE type='index' AND name='idx_episodes_domain_track_session'"
            ).get();
            expect(idx).toBeDefined();
        });

        test('idx_episodes_session index exists', () => {
            const { db } = createTestDb();
            const idx = db.prepare(
                "SELECT name FROM sqlite_master WHERE type='index' AND name='idx_episodes_session'"
            ).get();
            expect(idx).toBeDefined();
        });

        test('domain+track+session query on 1000 episodes completes in <10ms (AC-2)', async () => {
            const { store, db } = createTestDb();
            await seedEpisodes(store, 1000, 'sess-perf-a');

            const start = performance.now();
            db.prepare(
                'SELECT id FROM episodes WHERE domain = ? AND track = ? AND session_id = ?'
            ).all('engineering', 'track-5', 'sess-perf-a');
            const elapsed = performance.now() - start;

            expect(elapsed).toBeLessThan(10);
        });

        test('session_id query on 1000 episodes completes in <5ms (AC-2)', async () => {
            const { store, db } = createTestDb();
            await seedEpisodes(store, 1000, 'sess-perf-b');

            const start = performance.now();
            db.prepare('SELECT id FROM episodes WHERE session_id = ?').all('sess-perf-b');
            const elapsed = performance.now() - start;

            expect(elapsed).toBeLessThan(5);
        });
    });

    // ── AC-3: Size Enforcement ────────────────────────────────────────────────

    describe('AC-3: Size Enforcement (5MB limit)', () => {
        test('stores episode at exactly 5MB boundary (pass)', async () => {
            const { store } = createTestDb();
            // Build a summary that keeps the whole params just under 5MB
            const padding = 'x'.repeat(EPISODE_MAX_SIZE_BYTES - 500);
            const params = makeParams({ outcomeSummary: padding });

            // The JSON.stringify of params must not exceed 5MB
            expect(JSON.stringify(params).length).toBeLessThanOrEqual(EPISODE_MAX_SIZE_BYTES);
            await expect(store.storeExperience(params)).resolves.toBeDefined();
        });

        test('rejects episode payload exceeding 5MB (fail)', async () => {
            const { store } = createTestDb();
            // Build a payload guaranteed to exceed 5MB
            const oversized = 'x'.repeat(EPISODE_MAX_SIZE_BYTES + 1);
            const params = makeParams({ outcomeSummary: oversized });

            await expect(store.storeExperience(params)).rejects.toThrow(/exceeds 5MB limit/);
        });

        test('error code is episode_size_exceeded on oversized payload', async () => {
            const { store } = createTestDb();
            const oversized = 'x'.repeat(EPISODE_MAX_SIZE_BYTES + 1);
            const params = makeParams({ outcomeSummary: oversized });

            try {
                await store.storeExperience(params);
                throw new Error('Expected error to be thrown');
            } catch (err: any) {
                expect(err.code).toBe('episode_size_exceeded');
            }
        });
    });

    // ── AC-4: CRUD & MCP Tool Readiness ─────────────────────────────────────

    describe('AC-4: CRUD Functionality & Episode Interface', () => {
        test('storeExperience returns episodeId and createdAt', async () => {
            const { store } = createTestDb();
            const result = await store.storeExperience(makeParams());

            expect(result.episodeId).toBeDefined();
            expect(result.episodeId).toMatch(/^ep_/);
            expect(result.createdAt).toBeInstanceOf(Date);
        });

        test('getEpisode retrieves a stored episode by ID', async () => {
            const { store } = createTestDb();
            const { episodeId } = await store.storeExperience(makeParams());
            const episode = store.getEpisode(episodeId);

            expect(episode).toBeDefined();
            expect(episode!.id).toBe(episodeId);
            expect(episode!.sessionId).toBe('sess-test-001');
            expect(episode!.domain).toBe('engineering');
            expect(episode!.track).toBe('user-story-42');
            expect(episode!.phase).toBe('implementation');
            expect(episode!.outcomeSummary).toBe('Implemented the widget factory pattern.');
        });

        test('getEpisode returns undefined for unknown ID', () => {
            const { store } = createTestDb();
            expect(store.getEpisode('ep_nonexistent')).toBeUndefined();
        });

        test('getEpisodesBySession returns all episodes for a session', async () => {
            const { store } = createTestDb();
            await store.storeExperience(makeParams({ sessionId: 'sess-A' }));
            await store.storeExperience(makeParams({ sessionId: 'sess-A', outcomeSummary: 'Second episode' }));
            await store.storeExperience(makeParams({ sessionId: 'sess-B' }));

            const results = store.getEpisodesBySession('sess-A');
            expect(results).toHaveLength(2);
            results.forEach(ep => expect(ep.sessionId).toBe('sess-A'));
        });

        test('getEpisodesBySession returns empty array for unknown session', () => {
            const { store } = createTestDb();
            expect(store.getEpisodesBySession('sess-none')).toHaveLength(0);
        });

        test('toolCalls and artifacts round-trip correctly through JSON serialization', async () => {
            const { store } = createTestDb();
            const toolCalls = [
                { tool: 'search_knowledge', args: { query: 'widget' }, result: 'OK' }
            ];
            const artifacts = [{ type: 'document', path: '/docs/widget.md', hash: 'abc123' }];

            const { episodeId } = await store.storeExperience(makeParams({ toolCalls, artifacts }));
            const ep = store.getEpisode(episodeId)!;

            expect(ep.toolCalls).toEqual(toolCalls);
            expect(ep.artifacts).toEqual(artifacts);
        });

        test('storeExperience handles optional toolCalls and artifacts as empty arrays', async () => {
            const { store } = createTestDb();
            const { episodeId } = await store.storeExperience(makeParams());
            const ep = store.getEpisode(episodeId)!;

            expect(ep.toolCalls).toEqual([]);
            expect(ep.artifacts).toEqual([]);
        });
    });
});
