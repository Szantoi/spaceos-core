import Database from 'better-sqlite3';
import path from 'path';
import { readFileSync, rmSync } from 'fs';
import { describe, test, expect, beforeAll, afterAll } from 'vitest';
import { EpisodeStore } from '../../episodic/EpisodeStore';
import { searchExperience } from '../../episodic/FtsSearch';

const DB_PATH = path.join(__dirname, 'test_fts.db');

function createTestDb() {
    // Delete old test DB if exists
    try { rmSync(DB_PATH); } catch (e) { }

    const db = new Database(DB_PATH);

    // Create base tables (AgentDb logic mockup)
    const episodicMigrationPath = path.join(__dirname, '../../episodic/migrations/003_episodes.sql');
    db.exec(readFileSync(episodicMigrationPath, { encoding: 'utf-8' }));

    const ftsMigrationPath = path.join(__dirname, '../../episodic/migrations/004_episodes_fts5.sql');
    db.exec(readFileSync(ftsMigrationPath, { encoding: 'utf-8' }));

    const store = new EpisodeStore(db);
    return { db, store };
}

describe('TASK-12-02: FTS5 Full-Text Search', () => {
    let db: Database.Database;
    let store: EpisodeStore;

    beforeAll(() => {
        const testEnv = createTestDb();
        db = testEnv.db;
        store = testEnv.store;
    });

    afterAll(() => {
        db.close();
        try { rmSync(DB_PATH); } catch (e) { }
    });

    describe('AC-1 & AC-2: Schema & Triggers', () => {
        test('episodes_fts table exists and has identical row count after insert', () => {
            store.storeExperience({
                sessionId: 'session-123',
                domain: 'engineering',
                track: 'standard',
                phase: 'ideation',
                outcomeSummary: 'First test outcome',
            });

            const rawCount = db.prepare('SELECT count(*) as c FROM episodes').get() as { c: number };
            const ftsCount = db.prepare('SELECT count(*) as c FROM episodes_fts').get() as { c: number };

            expect(rawCount.c).toBe(1);
            expect(ftsCount.c).toBe(1);
        });

        test('trigger auto-syncs DELETE appropriately', async () => {
            // storeExperience returns a promise; await it to get the correct id
            const { episodeId } = await store.storeExperience({
                sessionId: 'session-del', domain: 'testing', track: 'fast_track', phase: 'review', outcomeSummary: 'To be deleted'
            });

            // sanity check: the episode should be findable before deletion
            const beforeResults = searchExperience(db, 'To be deleted');
            expect(beforeResults.length).toBeGreaterThan(0);

            db.prepare('DELETE FROM episodes WHERE id = ?').run(episodeId);

            // after deletion the keyword should no longer be returned
            const afterResults = searchExperience(db, 'To be deleted');
            expect(afterResults.length).toBe(0);
        });

        test('trigger auto-syncs UPDATE appropriately', async () => {
            const { episodeId } = await store.storeExperience({
                sessionId: 'session-upd', domain: 'deployment', track: 'standard', phase: 'refinement', outcomeSummary: 'Old outcome'
            });

            db.prepare('UPDATE episodes SET outcome_summary = ? WHERE id = ?').run('Updated new outcome', episodeId);

            const result = searchExperience(db, 'Updated');
            expect(result.length).toBeGreaterThan(0);
            expect(result[0].outcomeSummary).toBe('Updated new outcome');

            const oldResult = searchExperience(db, 'Old');
            expect(oldResult.length).toBe(0);
        });
    });

    describe('AC-4: Keyword Search Functionality', () => {
        test('can search by basic keyword', () => {
            store.storeExperience({
                sessionId: 'session-kw', domain: 'engineering', track: 'standard', phase: 'ideation', outcomeSummary: 'ideation and engineering combined'
            });

            const results = searchExperience(db, 'ideation AND engineering');
            expect(results.length).toBeGreaterThan(0);
        });

        test('domain filter works correctly', () => {
            store.storeExperience({
                sessionId: 'session-domain', domain: 'discovery', track: 'standard', phase: 'ideation', outcomeSummary: 'special unique word'
            });

            store.storeExperience({
                sessionId: 'session-domain2', domain: 'testing', track: 'standard', phase: 'ideation', outcomeSummary: 'special unique word'
            });

            const resultsDiscovery = searchExperience(db, 'special', 'discovery');
            expect(resultsDiscovery.length).toBe(1);
            expect(resultsDiscovery[0].domain).toBe('discovery');
        });

        test('protects against SQL injection by sanitizing double quotes and hazardous chars', () => {
            store.storeExperience({
                sessionId: 'session-inj', domain: 'engineering', track: 'standard', phase: 'ideation', outcomeSummary: 'injection safe'
            });

            // This query contains multiple words after sanitization ("injection", "DROP", "TABLE", "episodes")
            // It won't match "injection safe" because of implicit AND, but it MUST NOT crash or drop the table.
            const results = searchExperience(db, 'injection"; DROP TABLE episodes; --');

            // Check that the table still exists
            const tableCheck = db.prepare("SELECT name FROM sqlite_master WHERE type='table' AND name='episodes'").get();
            expect(tableCheck).toBeDefined();

            // It should be safe and return 0 (as it's looking for "DROP TABLE episodes" which don't exist in the outcome)
            expect(results.length).toBe(0);

            // A simple search should still work
            const simpleResults = searchExperience(db, 'injection');
            expect(simpleResults.length).toBe(1);
        });
    });

    describe('AC-3: Performance (<50ms for 1000 items)', () => {
        test('bulk insert 1000 episodes and search', () => {
            const startInsert = performance.now();

            // Do a bulk insert utilizing a transaction for speed in setup
            const insert = db.prepare('INSERT INTO episodes (id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary, created_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)');

            const insertMany = db.transaction((count: number) => {
                for (let i = 0; i < count; i++) {
                    const outcome = i === 500 ? 'the unique target needle' : `Common outcome ${i} for load testing`;
                    insert.run(
                        `ep_perf_${i}`,
                        `sess_${i % 10}`,
                        'engineering',
                        'standard',
                        'implementation',
                        '[]',
                        '[]',
                        outcome,
                        new Date().toISOString()
                    );
                }
            });

            insertMany(1000);

            const startSearch = performance.now();
            const results = searchExperience(db, 'target');
            const endSearch = performance.now();

            expect(results.length).toBe(1);
            expect(results[0].outcomeSummary).toBe('the unique target needle');

            const executionMs = endSearch - startSearch;
            // Less than 50ms requirement (typically ~1-5ms on local SSD)
            expect(executionMs).toBeLessThan(50);
        });
    });
});
