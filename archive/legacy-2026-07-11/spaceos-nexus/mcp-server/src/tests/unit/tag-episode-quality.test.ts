import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { mkdtempSync, rmSync, existsSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { v4 as uuid } from 'uuid';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { computeQualityScore } from '../../metadata/qualityScoring';
import { ErrorResponses } from '../../mcp/ErrorResponses';

describe('TASK-18-07: tag_episode_quality tool', () => {
  let tempDir: string;
  let dbPath: string;
  let connManager: DatabaseConnectionManager;
  let agentDb: AgentDb;
  let memoryPlugin: MemoryPlugin;
  const mockContext = {
    session_id: 'test-session',
    user_id: 'rater-123',
  } as any;

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), 'tag-episode-quality-'));
    dbPath = join(tempDir, 'metadata.db');
    connManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connManager);
    agentDb.initSchema();

    // Provide minimal system context (agentDb required for tool)
    memoryPlugin = new MemoryPlugin({ agentDb } as any);
  });

  afterEach(() => {
    try {
      connManager.close();
    } catch {
      // ignore
    }

    if (existsSync(tempDir)) {
      rmSync(tempDir, { recursive: true, force: true });
    }
  });

  it('AC-1: creates feedback row + updates highlight quality score', async () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, 'test-domain', 'test-track', 'implementation', 'summary', datetime('now'))`
      )
      .run(episodeId, mockContext.session_id);

    // Insert highlight with AI score 0.3
    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episode_highlights (id, episode_id, key_decisions, lessons, next_steps, quality_score, created_at)
         VALUES (?, ?, 'k', 'l', 'n', ?, datetime('now'))`
      )
      .run(highlightId, episodeId, 0.3);

    const result = await memoryPlugin.handlers['tag_episode_quality']?.(
      {
        highlight_id: highlightId,
        quality_score: 0.75,
        comment: 'Looks good',
      },
      mockContext
    );

    expect(result).toHaveProperty('success', true);
    expect(result).toHaveProperty('data');
    expect(result.data).toMatchObject({
      highlight_id: highlightId,
      new_quality_score: expect.any(Number),
      feedback_recorded_at: expect.any(String),
    });

    // Quality should be recomputed using the scoring function
    const { computedScore } = computeQualityScore(0.3, [0.75]);
    expect(result.data.new_quality_score).toBeCloseTo(computedScore, 4);

    // Verify DB persistence
    const feedbackRows = agentDb.listHighlightFeedback(highlightId);
    expect(feedbackRows.length).toBe(1);
    expect(feedbackRows[0].quality_score).toBeCloseTo(0.75, 4);
    expect(feedbackRows[0].comment).toBe('Looks good');
    expect(feedbackRows[0].rater_agent_id).toBe('rater-123');

    const highlightRow = agentDb.getEpisodeHighlightWithContextById(highlightId);
    expect(highlightRow?.quality_score).toBeCloseTo(computedScore, 4);
  });

  it('AC-2: rejects out-of-range quality_score values', async () => {
    const resultLow = await memoryPlugin.handlers['tag_episode_quality']?.(
      { highlight_id: 'does-not-matter', quality_score: -0.1 },
      mockContext
    );
    expect(resultLow).toHaveProperty('isError', true);
    expect(resultLow.error?.code).toBe(ErrorResponses.badRequest('').error.code);

    const resultHigh = await memoryPlugin.handlers['tag_episode_quality']?.(
      { highlight_id: 'does-not-matter', quality_score: 1.5 },
      mockContext
    );
    expect(resultHigh).toHaveProperty('isError', true);
    expect(resultHigh.error?.code).toBe(ErrorResponses.badRequest('').error.code);
  });

  it('AC-3: unknown highlight_id returns NOT_FOUND', async () => {
    const result = await memoryPlugin.handlers['tag_episode_quality']?.(
      { highlight_id: `hl-${uuid()}`, quality_score: 0.5 },
      mockContext
    );
    expect(result).toHaveProperty('isError', true);
    expect(result.error?.code).toBe(ErrorResponses.notFound('').error.code);
  });

  it('AC-5: supports multiple feedback rows and aggregates score', async () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, 'test-domain', 'test-track', 'implementation', 'summary', datetime('now'))`
      )
      .run(episodeId, mockContext.session_id);

    // AI score 0.2
    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episode_highlights (id, episode_id, key_decisions, lessons, next_steps, quality_score, created_at)
         VALUES (?, ?, 'k', 'l', 'n', ?, datetime('now'))`
      )
      .run(highlightId, episodeId, 0.2);

    await memoryPlugin.handlers['tag_episode_quality']?.(
      { highlight_id: highlightId, quality_score: 0.8 },
      mockContext
    );

    const result = await memoryPlugin.handlers['tag_episode_quality']?.(
      { highlight_id: highlightId, quality_score: 1.0 },
      mockContext
    );

    expect(result).toHaveProperty('success', true);

    // Compute expected based on two feedback scores (0.8, 1.0) and AI score 0.2
    const { computedScore } = computeQualityScore(0.2, [0.8, 1.0]);
    expect(result.data.new_quality_score).toBeCloseTo(computedScore, 4);

    const feedbackRows = agentDb.listHighlightFeedback(highlightId);
    expect(feedbackRows.length).toBe(2);
  });
});
