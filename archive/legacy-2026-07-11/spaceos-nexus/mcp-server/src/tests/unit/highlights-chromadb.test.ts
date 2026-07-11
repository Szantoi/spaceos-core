import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { EpisodeHighlightsIndex } from '../../rag/episodicMemory';

describe('EPIC-18 TASK-18-04: highlights ChromaDB integration', () => {
    let tempDir: string;
    let dbPath: string;
    let connectionManager: DatabaseConnectionManager;
    let agentDb: AgentDb;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'ep18-highlights-chroma-'));
        dbPath = join(tempDir, 'metadata.db');
        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();
    });

    afterEach(() => {
        try {
            connectionManager.close();
        } catch {
            // Ignore close errors.
        }

        try {
            if (existsSync(tempDir)) {
                rmSync(tempDir, { recursive: true, force: true });
            }
        } catch {
            // Ignore cleanup errors.
        }
    });

    it('AC-1/2/4: creates or reuses episode_highlights collection and persists sync metadata', async () => {
        seedHighlight({
            episodeId: 'ep-1',
            sessionId: 'sess-1',
            highlightId: 'hl-1',
            keyDecisions: ['Validate deployment readiness'],
            lessons: ['Deployment validation reduces regressions'],
            nextSteps: ['Reuse the validation checklist'],
        });

        const fakeClient = new FakeChromaClient();
        const index = new EpisodeHighlightsIndex(agentDb, {
            client: fakeClient,
            embeddings: new FakeEmbeddings(),
            embeddingModel: 'test-embedding',
        });

        const result = await index.syncHighlights();

        expect(result.collection_name).toBe('episode_highlights');
        expect(result.indexed_count).toBe(1);
        expect(fakeClient.collectionNames).toEqual(['episode_highlights']);
        expect(fakeClient.documents[0]).toContain('Key decisions: Validate deployment readiness');
        expect(fakeClient.documents[0]).toContain('Lessons: Deployment validation reduces regressions');
        expect(fakeClient.documents[0]).toContain('Next steps: Reuse the validation checklist');

        const syncRow = agentDb.getHighlightChromaSync('hl-1');
        expect(syncRow).not.toBeNull();
        expect(syncRow?.vector_id).toBe('highlight:hl-1');
        expect(syncRow?.embedding_model).toBe('test-embedding');
    });

    it('AC-3: search_highlights returns semantically relevant highlights', async () => {
        seedHighlight({
            episodeId: 'ep-2',
            sessionId: 'sess-2',
            highlightId: 'hl-2',
            keyDecisions: ['Validate deployment readiness'],
            lessons: ['Deployment validation reduces regressions'],
            nextSteps: ['Run the release checklist'],
        });
        seedHighlight({
            episodeId: 'ep-3',
            sessionId: 'sess-3',
            highlightId: 'hl-3',
            keyDecisions: ['Refine backlog estimates'],
            lessons: ['Smaller stories improve flow'],
            nextSteps: ['Split oversized backlog items'],
        });

        const index = new EpisodeHighlightsIndex(agentDb, {
            client: new FailingChromaClient(),
            embeddings: new FakeEmbeddings(),
            embeddingModel: 'test-embedding',
        });

        await index.syncHighlights();
        const results = await index.searchHighlights('deployment validation checklist', 1);

        expect(results).toHaveLength(1);
        expect(results[0].highlight_id).toBe('hl-2');
        expect(results[0].similarity_score).toBeGreaterThan(0);
    });

    it('AC-5: missing ChromaDB degrades gracefully with explicit warning', async () => {
        seedHighlight({
            episodeId: 'ep-4',
            sessionId: 'sess-4',
            highlightId: 'hl-4',
            keyDecisions: ['Capture reflection warnings'],
            lessons: ['Fallback paths must stay explicit'],
            nextSteps: ['Surface degraded-mode warning'],
        });

        const index = new EpisodeHighlightsIndex(agentDb, {
            client: new FailingChromaClient(),
            embeddings: new FakeEmbeddings(),
            embeddingModel: 'test-embedding',
        });

        const syncResult = await index.syncHighlights();
        const searchResult = await index.searchHighlights('fallback warning', 1);

        expect(syncResult.warnings).toContain('CHROMADB_UNAVAILABLE');
        expect(index.getWarnings()).toContain('CHROMADB_UNAVAILABLE');
        expect(searchResult[0].highlight_id).toBe('hl-4');
        expect(agentDb.getHighlightChromaSync('hl-4')?.embedding_model).toBe('memory-fallback:test-embedding');
    });

    function seedHighlight(params: {
        episodeId: string;
        sessionId: string;
        highlightId: string;
        keyDecisions: string[];
        lessons: string[];
        nextSteps: string[];
    }): void {
        connectionManager.getAdminPool().prepare(`
            INSERT INTO episodes (
                id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        `).run(
            params.episodeId,
            params.sessionId,
            'engineering',
            'delivery',
            'implementation',
            '[]',
            '[]',
            params.lessons.join(' ')
        );

        agentDb.upsertEpisodeHighlight({
            id: params.highlightId,
            episode_id: params.episodeId,
            key_decisions: JSON.stringify(params.keyDecisions),
            lessons: JSON.stringify(params.lessons),
            next_steps: JSON.stringify(params.nextSteps),
            quality_score: 0.8,
            ai_generated: false,
        });
    }
});

class FakeEmbeddings {
    async embedDocuments(texts: string[]): Promise<number[][]> {
        return texts.map((text) => this.embed(text));
    }

    async embedQuery(text: string): Promise<number[]> {
        return this.embed(text);
    }

    private embed(text: string): number[] {
        const normalized = text.toLowerCase();
        return [
            countToken(normalized, 'deployment') + countToken(normalized, 'release'),
            countToken(normalized, 'valid') + countToken(normalized, 'checklist'),
            countToken(normalized, 'backlog') + countToken(normalized, 'story'),
            countToken(normalized, 'fallback') + countToken(normalized, 'warning'),
        ];
    }
}

class FakeChromaClient {
    public readonly collectionNames: string[] = [];
    public readonly documents: string[] = [];
    private readonly stored = new Map<string, { embedding: number[] }>();

    async heartbeat(): Promise<void> {
        return;
    }

    async getOrCreateCollection(params: { name: string }): Promise<{
        upsert: (payload: {
            ids: string[];
            documents: string[];
            embeddings: number[][];
            metadatas: Record<string, unknown>[];
        }) => Promise<void>;
        query: (payload: {
            queryEmbeddings: number[][];
            nResults: number;
            where?: Record<string, unknown>;
        }) => Promise<{ ids: string[][]; distances?: number[][] }>;
    }> {
        this.collectionNames.push(params.name);

        return {
            upsert: async (payload) => {
                payload.ids.forEach((id, index) => {
                    this.documents.push(payload.documents[index]);
                    this.stored.set(id, { embedding: payload.embeddings[index] });
                });
            },
            query: async (payload) => {
                const queryEmbedding = payload.queryEmbeddings[0];
                const ranked = Array.from(this.stored.entries())
                    .map(([id, stored]) => ({
                        id,
                        score: cosineSimilarityForTest(queryEmbedding, stored.embedding),
                    }))
                    .sort((left, right) => right.score - left.score)
                    .slice(0, payload.nResults);

                return {
                    ids: [ranked.map((entry) => entry.id)],
                    distances: [ranked.map((entry) => 1 - entry.score)],
                };
            },
        };
    }
}

class FailingChromaClient {
    async heartbeat(): Promise<void> {
        throw new Error('Chroma unavailable');
    }

    async getOrCreateCollection(): Promise<never> {
        throw new Error('Chroma unavailable');
    }
}

function countToken(text: string, token: string): number {
    return text.includes(token) ? 1 : 0;
}

function cosineSimilarityForTest(left: number[], right: number[]): number {
    let dotProduct = 0;
    let leftMagnitude = 0;
    let rightMagnitude = 0;

    for (let index = 0; index < Math.max(left.length, right.length); index += 1) {
        const leftValue = left[index] ?? 0;
        const rightValue = right[index] ?? 0;
        dotProduct += leftValue * rightValue;
        leftMagnitude += leftValue * leftValue;
        rightMagnitude += rightValue * rightValue;
    }

    if (leftMagnitude === 0 || rightMagnitude === 0) {
        return 0;
    }

    return dotProduct / (Math.sqrt(leftMagnitude) * Math.sqrt(rightMagnitude));
}