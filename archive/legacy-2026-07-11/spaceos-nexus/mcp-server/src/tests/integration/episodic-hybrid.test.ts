import { describe, it, expect, beforeEach, vi } from 'vitest';
import Database from 'better-sqlite3';
import { EpisodeStore } from '../../episodic/EpisodeStore';
import { Episode } from '../../episodic/types';

// Mock ChromaClient and Gemini Embeddings
vi.mock('chromadb', () => {
    return {
        ChromaClient: vi.fn().mockImplementation(() => ({
            getOrCreateCollection: vi.fn().mockResolvedValue({
                add: vi.fn().mockResolvedValue(undefined),
                query: vi.fn().mockResolvedValue({
                    ids: [['ep_hybrid_1']],
                    distances: [[0.1]], // Similarity 0.9
                }),
            }),
            heartbeat: vi.fn().mockResolvedValue(true),
        })),
    };
});

vi.mock('@langchain/google-genai', () => {
    return {
        GoogleGenerativeAIEmbeddings: vi.fn().mockImplementation(() => ({
            embedQuery: vi.fn().mockResolvedValue(new Array(768).fill(0.1)),
            embedDocuments: vi.fn().mockResolvedValue([new Array(768).fill(0.1)]),
        })),
    };
});

describe('Hybrid Search E2E Workflow (TASK-12-04)', () => {
    let db: Database.Database;
    let store: EpisodeStore;

    beforeEach(async () => {
        // Use a real in-memory SQLite DB
        db = new Database(':memory:');

        // Initialize schema (FTS5 triggers, etc.)
        // In a real scenario, we'd run the migration scripts.
        // For this test, we'll manually create the tables or just enough for EpisodeStore.
        db.exec(`
            CREATE TABLE episodes (
                id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL,
                domain TEXT NOT NULL,
                track TEXT NOT NULL,
                phase TEXT NOT NULL,
                tool_calls_json TEXT,
                artifacts_json TEXT,
                outcome_summary TEXT NOT NULL,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE VIRTUAL TABLE episodes_fts USING fts5(
                id UNINDEXED,
                outcome_summary,
                content='episodes',
                content_rowid='rowid'
            );

            CREATE TRIGGER episodes_ai AFTER INSERT ON episodes BEGIN
                INSERT INTO episodes_fts(rowid, id, outcome_summary)
                VALUES (new.rowid, new.id, new.outcome_summary);
            END;
        `);

        store = new EpisodeStore(db);
        await store.initialize();
    });

    it('should store 100 episodes and perform hybrid search', async () => {
        // 1. Store 100 experience episodes
        const episodes: any[] = [];
        for (let i = 0; i < 100; i++) {
            const ep = {
                sessionId: 'test-session-1',
                domain: 'engineering' as const,
                track: 'standard',
                phase: 'implementation' as const,
                outcomeSummary: `Implemented feature ${i}: database migration pattern`,
                toolCalls: [],
                artifacts: []
            };
            episodes.push(ep);
            await store.storeExperience(ep);
        }

        // 2. Perform FTS5 search (exact keyword)
        // Since we have "database migration pattern" in all of them, FTS5 should find them.

        // 3. Perform Hybrid search
        const results = await store.searchHybrid({
            query: 'database migration',
            limit: 10,
            threshold: 0.5
        });

        // 4. Verify results
        expect(results.length).toBe(10); // Limited by 10
        expect(results[0].outcomeSummary).toContain('database migration');

        // 5. Verify Cache Metrics
        // @ts-ignore - access private cache for verification
        const metrics = store['chromaClient'].getCacheMetrics();
        console.log('Cache Metrics:', metrics);

        // We stored 100 episodes + 1 search query = 101 embeddings
        // Total misses should be 101 if this is the first run.
        expect(metrics.misses).toBeGreaterThanOrEqual(101);
    });

    it('should deduplicate results between FTS5 and Semantic', async () => {
        // 1. Store one specific episode and get its generated ID
        const { episodeId } = await store.storeExperience({
            sessionId: 's1',
            domain: 'engineering' as const,
            track: 't',
            phase: 'implementation' as const,
            outcomeSummary: 'Unique keyword match for deduplication test',
            toolCalls: [],
            artifacts: []
        });

        // 2. Mock searchIds to return this specific ID
        // @ts-ignore
        const searchIdsSpy = vi.spyOn(store['chromaClient'], 'searchIds');
        searchIdsSpy.mockResolvedValue([episodeId]);

        // 3. Hybrid search for query that hits BOTH FTS5 and Semantic
        const results = await store.searchHybrid({
            query: 'Unique keyword',
            limit: 5,
            threshold: 0.1
        });

        // 4. Verify deduplication
        // It's found via FTS5 and should be found via mocked Semantic.
        // deduplication should result in only 1 result.
        const matchCount = results.filter(r => r.id === episodeId).length;
        expect(matchCount).toBe(1);
        expect(results.length).toBe(1);

        searchIdsSpy.mockRestore();
    });
});
