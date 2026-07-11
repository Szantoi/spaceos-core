import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { McpContext } from '../../mcp/middleware/contextMiddleware';

describe('EPIC-18 TASK-18-03: reflect_session', () => {
    let tempDir: string;
    let dbPath: string;
    let connectionManager: DatabaseConnectionManager;
    let agentDb: AgentDb;
    let plugin: MemoryPlugin;
    let context: McpContext;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'ep18-reflect-session-'));
        dbPath = join(tempDir, 'metadata.db');
        connectionManager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(connectionManager);
        agentDb.initSchema();

        plugin = new MemoryPlugin({ agentDb } as any);
        context = {
            session_id: 'ctx-session',
            domain: 'engineering',
            role: 'backend_developer',
            phase: 'implementation',
            track: 'delivery'
        };
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

    it('AC-1/2: returns current and prior highlights with a plain-English summary', async () => {
        insertEpisode('ep-current', 'sess-current', 'Validated deployment readiness and captured release notes.');
        insertEpisode('ep-prior', 'sess-prior', 'Validated release assumptions before deployment to reduce regressions.');

        agentDb.upsertEpisodeHighlight({
            id: 'hl-current',
            episode_id: 'ep-current',
            key_decisions: JSON.stringify(['Validate deployment readiness before release']),
            lessons: JSON.stringify(['Validation before deployment prevents regressions']),
            next_steps: JSON.stringify(['Apply the release checklist to the final build']),
            quality_score: 0.88,
            ai_generated: true,
            ai_model: 'gpt-5.4',
            ai_tokens_used: 220,
        });
        agentDb.upsertEpisodeHighlight({
            id: 'hl-prior',
            episode_id: 'ep-prior',
            key_decisions: JSON.stringify(['Validate assumptions before deployment']),
            lessons: JSON.stringify(['Validation before deployment reduces rollback risk']),
            next_steps: JSON.stringify(['Reuse the validation checklist for the next release']),
            quality_score: 0.84,
            ai_generated: false,
        });

        const result = await plugin.reflect_session({ session_id: 'sess-current' }, context);

        expect(result.success).toBe(true);
        expect(result.data.current_highlights).toHaveLength(1);
        expect(result.data.prior_highlights).toHaveLength(1);
        expect(result.data.prior_highlights[0].highlight_id).toBe('hl-prior');
        expect(result.data.prior_highlights[0].retrieval_reason).toBe('keyword-match');
        expect(result.data.learnings_applied.length).toBeGreaterThan(0);
        expect(result.data.session_summary).toContain('sess-current');
        expect(result.data).toHaveProperty('reflection_at');
    });

    it('AC-3/4/5: returns success with empty prior_highlights and does not mutate stored highlights', async () => {
        insertEpisode('ep-solo', 'sess-solo', 'Recorded implementation learnings for the current session only.');
        agentDb.upsertEpisodeHighlight({
            id: 'hl-solo',
            episode_id: 'ep-solo',
            key_decisions: JSON.stringify(['Keep reflection read-only']),
            lessons: JSON.stringify(['Current-session summaries should not mutate history']),
            next_steps: JSON.stringify(['Add retrieval bridges incrementally']),
            quality_score: 0.79,
            ai_generated: false,
        });

        const before = agentDb.getEpisodeHighlightByEpisodeId('ep-solo');
        const result = await plugin.reflect_session({ session_id: 'sess-solo' }, context);
        const after = agentDb.getEpisodeHighlightByEpisodeId('ep-solo');

        expect(result.success).toBe(true);
        expect(result.data.current_highlights).toHaveLength(1);
        expect(result.data.prior_highlights).toEqual([]);
        expect(result.data.learnings_applied).toEqual([]);
        expect(after).toEqual(before);
    });

    it('AC-6: returns standardized INVALID_INPUT response for malformed session input', async () => {
        const result = await plugin.reflect_session({ session_id: 42 }, context);

        expect(result.success).toBe(false);
        expect(result.error.code).toBe('INVALID_INPUT');
    });

    function insertEpisode(id: string, sessionId: string, outcomeSummary: string): void {
        connectionManager.getAdminPool().prepare(`
            INSERT INTO episodes (
                id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        `).run(
            id,
            sessionId,
            'engineering',
            'delivery',
            'implementation',
            '[]',
            '[]',
            outcomeSummary
        );
    }
});