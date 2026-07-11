import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { mkdtempSync, rmSync, existsSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { v4 as uuid } from 'uuid';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { EpisodeHighlightsIndex } from '../../rag/episodicMemory';

/**
 * Integration test for end-to-end reflection cycle.
 *
 * Verifies that:
 * - Session 1 generates a highlight.
 * - Session 2 reflects and retrieves session 1 highlight via keyword match.
 * - Highlights can be synced and searched semantically via the ChromaDB path.
 */
describe('EPIC-18: End-to-End reflection cycle', () => {
  let tempDir: string;
  let metadataDbPath: string;
  let episodicDbPath: string;
  let connManager: DatabaseConnectionManager;
  let agentDb: AgentDb;
  let memoryPlugin: MemoryPlugin;

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), 'epic18-reflection-'));
    metadataDbPath = join(tempDir, 'metadata.db');
    episodicDbPath = join(tempDir, 'episodic.db');

    // Ensure MemoryPlugin uses isolated episodic DB for this test
    process.env.EPISODIC_DB_PATH = episodicDbPath;

    connManager = new DatabaseConnectionManager(metadataDbPath);
    agentDb = new AgentDb(connManager);
    agentDb.initSchema();

    memoryPlugin = new MemoryPlugin({ agentDb } as any);
  });

  afterEach(() => {
    try {
      connManager.close();
    } catch {
      // ignore
    }
    delete process.env.EPISODIC_DB_PATH;

    if (existsSync(tempDir)) {
      rmSync(tempDir, { recursive: true, force: true });
    }
  });

  it('AC-1/AC-2/AC-4: Session 2 reflection finds Session 1 highlight; semantic search works', async () => {
    const session1 = `sess-${uuid()}`;
    const session2 = `sess-${uuid()}`;

    // Session 1: save episode + generate highlight
    const saveRes1 = await memoryPlugin.handlers['save_episode']?.(
      {
        agent_id: 'agent-1',
        episode_data: {
          thought_process: 'Recorded a deployment readiness review.',
          actions: ['run_checklist', 'validate_configs'],
          outcome: 'Deployment checklist completed successfully',
          reasoning: 'Ensures nothing is missed before release',
        },
        metadata: {
          domain: 'engineering',
          track: 'delivery',
        },
      },
      { session_id: session1 } as any
    );
    expect(saveRes1).toHaveProperty('status', 'success');

    const genRes1 = await memoryPlugin.handlers['generate_episode_highlights']?.(
      {
        session_id: session1,
        ai_model: 'test-model',
        ai_tokens_used: 123,
      },
      { session_id: session1 } as any
    );
    expect(genRes1).toHaveProperty('success', true);
    const session1HighlightId = genRes1.data.highlight_id as string;

    // Session 2: create a new episode whose content overlaps keywords (deploy/checklist)
    const saveRes2 = await memoryPlugin.handlers['save_episode']?.(
      {
        agent_id: 'agent-2',
        episode_data: {
          thought_process: 'Follow-up validation of deployment checklist.',
          actions: ['review_checklist', 'verify_deployment'],
          outcome: 'Validated checklist and confirmed deploy steps',
          reasoning: 'Reusing previous deployment learnings',
        },
        metadata: {
          domain: 'engineering',
          track: 'delivery',
        },
      },
      { session_id: session2 } as any
    );
    expect(saveRes2).toHaveProperty('status', 'success');

    const genRes2 = await memoryPlugin.handlers['generate_episode_highlights']?.(
      {
        session_id: session2,
        ai_model: 'test-model',
        ai_tokens_used: 88,
      },
      { session_id: session2 } as any
    );
    expect(genRes2).toHaveProperty('success', true);

    // Reflect in session 2, expecting prior highlight to be returned
    const reflectRes = await memoryPlugin.handlers['reflect_session']?.(
      { session_id: session2, include_prior_highlights: true, limit: 5 },
      { session_id: session2 } as any
    );

    expect(reflectRes).toHaveProperty('success', true);
    const priorHighlights = reflectRes.data.prior_highlights as any[];
    expect(Array.isArray(priorHighlights)).toBe(true);

    // Ensure the session1 highlight appears in prior highlights (keyword match)
    const found = priorHighlights.find((h) => h.highlight_id === session1HighlightId);
    expect(found).toBeDefined();
    expect(found?.retrieval_reason).toBe('keyword-match');

    // AC-3: Semantic (Chroma) search path — verify we can sync and query by embedding
    const index = new EpisodeHighlightsIndex(agentDb, {
      client: { heartbeat: async () => {}, getOrCreateCollection: async () => ({
        upsert: async () => {},
        query: async () => ({ ids: [[session1HighlightId]], distances: [[0.1]] })
      }) } as any,
      embeddings: {
        embedDocuments: async (texts: string[]) => texts.map(() => [1, 2, 3]),
        embedQuery: async (query: string) => [1, 2, 3],
      } as any,
      embeddingModel: 'fake-embedding',
    });

    const syncResult = await index.syncHighlights();
    expect(syncResult.indexed_count).toBeGreaterThanOrEqual(1);

    const searchResults = await index.searchHighlights('deployment checklist', 1);
    expect(searchResults.length).toBeGreaterThan(0);
    expect(searchResults[0].highlight_id).toBe(session1HighlightId);
  });
});
