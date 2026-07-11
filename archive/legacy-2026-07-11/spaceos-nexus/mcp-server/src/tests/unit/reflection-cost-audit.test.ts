import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { mkdtempSync, rmSync, existsSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { v4 as uuid } from 'uuid';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { AgentDb } from '../../mcp/AgentDb';
import { AuditLogger } from '../../metadata/AuditLogger';

describe('TASK-18-09: Reflection cost audit logging', () => {
  let tempDir: string;
  let dbPath: string;
  let connManager: DatabaseConnectionManager;
  let agentDb: AgentDb;
  let auditLogger: AuditLogger;
  const sessionId = `sess-${uuid()}`;
  const domain = 'test-domain';
  const role = 'test-role';

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), 'cost-audit-'));
    dbPath = join(tempDir, 'metadata.db');
    connManager = new DatabaseConnectionManager(dbPath);
    agentDb = new AgentDb(connManager);
    auditLogger = new AuditLogger(connManager);
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

  it('AC-1: tool captures ai_model and ai_tokens_used in highlight record', async () => {
    const episodeId = `ep-${uuid()}`;
    const highlightId = `hl-${uuid()}`;

    connManager
      .getAdminPool()
      .prepare(
        `INSERT INTO episodes (id, session_id, domain, track, phase, outcome_summary, created_at)
         VALUES (?, ?, ?, ?, ?, ?, datetime('now'))`
      )
      .run(episodeId, sessionId, domain, 'test-track', 'implementation', 'summary');

    // Insert highlight with ai_model and ai_tokens_used
    agentDb.upsertEpisodeHighlight({
      id: highlightId,
      episode_id: episodeId,
      key_decisions: JSON.stringify(['decision1']),
      lessons: JSON.stringify(['lesson1']),
      next_steps: JSON.stringify(['step1']),
      quality_score: 0.8,
      ai_generated: true,
      ai_model: 'gpt-4-turbo',
      ai_tokens_used: 1250,
    });

    const highlight = agentDb.getEpisodeHighlightWithContextById(highlightId);
    expect(highlight).toBeDefined();
    expect(highlight?.ai_model).toBe('gpt-4-turbo');
    expect(highlight?.ai_tokens_used).toBe(1250);
  });

  it('AC-2: audit log records reflection operation with latency and cost fields', async () => {
    // Simulate logging a tool call with cost metadata
    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'generate_episode_highlights',
      input: { episode_data: 'test' },
      output: { highlight_id: `hl-${uuid()}` },
      latency_ms: 1234,
      status_code: 'SUCCESS',
      ai_model: 'gpt-4-turbo',
      ai_tokens_used: 2500,
      cost_amount_usd: 0.075,  // ~0.075 USD for 2500 tokens
    });

    // Wait for background write
    await new Promise((resolve) => setTimeout(resolve, 150));

    const costs = agentDb.queryAuditCostData(sessionId);
    expect(costs.length).toBeGreaterThan(0);

    const entry = costs.find((c) => c.tool_name === 'generate_episode_highlights');
    expect(entry).toBeDefined();
    expect(entry?.ai_model).toBe('gpt-4-turbo');
    expect(entry?.ai_tokens_used).toBe(2500);
    expect(entry?.cost_amount_usd).toBeCloseTo(0.075, 3);
  });

  it('AC-3: missing token data does not break tool flow (safe defaults)', async () => {
    // Log a tool call without cost data (backward compatible)
    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'reflect_session',
      input: { query: 'test' },
      output: { result: 'ok' },
      latency_ms: 500,
      status_code: 'SUCCESS',
      // ai_model, ai_tokens_used, cost_amount_usd all omitted
    });

    // Wait for background write
    await new Promise((resolve) => setTimeout(resolve, 150));

    const costs = agentDb.queryAuditCostData(sessionId);
    expect(costs.length).toBeGreaterThan(0);

    const entry = costs.find((c) => c.tool_name === 'reflect_session');
    expect(entry).toBeDefined();
    // Missing fields should be null
    expect(entry?.ai_model).toBeNull();
    expect(entry?.ai_tokens_used).toBeNull();
    expect(entry?.cost_amount_usd).toBeNull();
  });

  it('AC-4: cost metadata is queryable for later reporting', async () => {
    // Log multiple reflection tool calls with varying cost data
    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'generate_episode_highlights',
      input: {},
      output: {},
      latency_ms: 800,
      status_code: 'SUCCESS',
      ai_model: 'claude-3-opus',
      ai_tokens_used: 3000,
      cost_amount_usd: 0.15,
    });

    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'reflect_session',
      input: {},
      output: {},
      latency_ms: 600,
      status_code: 'SUCCESS',
      ai_model: 'claude-3-opus',
      ai_tokens_used: 1500,
      cost_amount_usd: 0.075,
    });

    // Wait for background writes
    await new Promise((resolve) => setTimeout(resolve, 200));

    // Query by session and compute metrics
    const costData = agentDb.queryAuditCostData(sessionId);
    expect(costData.length).toBeGreaterThanOrEqual(2);

    // Compute aggregated metrics
    const metrics = agentDb.computeSessionCostMetrics(sessionId);
    expect(metrics.session_id).toBe(sessionId);
    expect(metrics.total_tools_called).toBeGreaterThanOrEqual(2);
    expect(metrics.total_latency_ms).toBeGreaterThanOrEqual(1400);
    expect(metrics.total_tokens_used).toBe(4500);
    expect(metrics.total_cost_usd).toBeCloseTo(0.225, 2);
    expect(metrics.ai_models_used).toContain('claude-3-opus');

    // Query by tool name
    const highlightCosts = agentDb.queryAuditCostData(sessionId, 'generate_episode_highlights');
    expect(highlightCosts.length).toBeGreaterThanOrEqual(1);
    expect(highlightCosts.every((c) => c.tool_name === 'generate_episode_highlights')).toBe(true);
  });

  it('AC-5: unit tests validate logging payload shape and no-crash fallback', async () => {
    // Test with minimal payload
    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'test_tool',
      input: {},
      output: {},
      latency_ms: 100,
      status_code: 'SUCCESS',
    });

    // Wait for background write
    await new Promise((resolve) => setTimeout(resolve, 150));

    const costs = agentDb.queryAuditCostData(sessionId);
    expect(costs.length).toBeGreaterThan(0);

    // Test computeSessionCostMetrics with no cost data
    const metrics = agentDb.computeSessionCostMetrics(sessionId);
    expect(metrics).toMatchObject({
      session_id: sessionId,
      total_tools_called: expect.any(Number),
      total_latency_ms: expect.any(Number),
      ai_models_used: expect.any(Array),
    });
  });

  it('AC-6: no write-path regression to unrelated tools', async () => {
    // Verify that existing query tool logging still works
    await auditLogger.log({
      session_id: sessionId,
      domain,
      role,
      tool_name: 'get_task_context',
      input: { task_id: 'TASK-123' },
      output: { task_context: {} },
      latency_ms: 50,
      status_code: 'SUCCESS',
      // No cost fields for non-reflection tools
    });

    // Wait for background write
    await new Promise((resolve) => setTimeout(resolve, 150));

    const allCosts = agentDb.queryAuditCostData(sessionId);
    expect(allCosts.length).toBeGreaterThan(0);

    const pmToolEntry = allCosts.find((c) => c.tool_name === 'get_task_context');
    expect(pmToolEntry).toBeDefined();
    expect(pmToolEntry?.ai_model).toBeNull();  // PM tools don't have AI models
    expect(pmToolEntry?.cost_amount_usd).toBeNull();
  });
});
