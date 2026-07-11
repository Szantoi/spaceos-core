import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { mkdtempSync, rmSync, existsSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { v4 as uuid } from 'uuid';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { computeQualityScore } from '../../metadata/qualityScoring';

describe('TASK-18-06: Highlight quality score computation', () => {
  let tempDir: string;
  let dbPath: string;
  let connManager: DatabaseConnectionManager;
  let agentDb: AgentDb;

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), 'quality-score-'));
    dbPath = join(tempDir, 'metadata.db');
    connManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connManager);
    agentDb.initSchema();
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

  it('AC-1/AC-2: computes valid score in [0,1] for ai-only case', () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;
    const sessionId = `sess-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, 'test-domain', 'test-track', 'implementation', 'summary', datetime('now'))`
      )
      .run(episodeId, sessionId);

    // Debug: list columns in episode_highlights
    // AI-provided quality score only
    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episode_highlights (id, episode_id, key_decisions, lessons, next_steps, quality_score, created_at)
         VALUES (?, ?, 'k', 'l', 'n', ?, datetime('now'))`
      )
      .run(highlightId, episodeId, 0.72);

    const computed = agentDb.recalculateHighlightQuality(highlightId);
    expect(computed).toBeGreaterThanOrEqual(0);
    expect(computed).toBeLessThanOrEqual(1);
    expect(computed).toBeCloseTo(0.72, 4);
  });

  it('AC-3: computes weighted average with feedback', () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;
    const sessionId = `sess-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, 'test-domain', 'test-track', 'implementation', 'summary', datetime('now'))`
      )
      .run(episodeId, sessionId);

    // AI scores 0.2
    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episode_highlights (id, episode_id, key_decisions, lessons, next_steps, quality_score, created_at)
         VALUES (?, ?, 'k', 'l', 'n', ?, datetime('now'))`
      )
      .run(highlightId, episodeId, 0.2);

    // Two feedback rows: 0.8 and 1.0
    const f1 = `fb-${uuid()}`;
    const f2 = `fb-${uuid()}`;

    agentDb.insertHighlightFeedback({ id: f1, highlight_id: highlightId, quality_score: 0.8 });
    agentDb.insertHighlightFeedback({ id: f2, highlight_id: highlightId, quality_score: 1.0 });

    // Expect weighting: feedbackCount=2 => w=2/3 ~ 0.6667
    // feedbackAvg=0.9 -> combined = 0.2*(1-0.6667)+0.9*0.6667 = 0.6667
    const computed = agentDb.recalculateHighlightQuality(highlightId);
    expect(computed).toBeGreaterThan(0.65);
    expect(computed).toBeLessThan(0.68);
  });

  it('AC-4: no feedback does not fail and uses AI score', () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;
    const sessionId = `sess-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, 'test-domain', 'test-track', 'implementation', 'summary', datetime('now'))`
      )
      .run(episodeId, sessionId);

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episode_highlights (id, episode_id, key_decisions, lessons, next_steps, quality_score, created_at)
         VALUES (?, ?, 'k', 'l', 'n', ?, datetime('now'))`
      )
      .run(highlightId, episodeId, 0.4);

    const computed = agentDb.recalculateHighlightQuality(highlightId);
    expect(computed).toBeCloseTo(0.4, 4);
  });

  it('AC-5: clamps out-of-range values consistently', () => {
    const result = computeQualityScore(2.5, [-1, 0.5, 1.5]);
    // aiScore is clamped to 1.0, feedback clamped to [0,1] => [0,0.5,1] avg=0.5
    // weight = 3/4 = 0.75 => combined = 1*0.25 + 0.5*0.75 = 0.625
    expect(result.computedScore).toBeCloseTo(0.625, 4);
  });
});
