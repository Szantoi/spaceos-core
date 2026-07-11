import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { join } from 'path';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { MemoryPlugin } from '../../mcp/tools/memory';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { McpContext } from '../../mcp/middleware/contextMiddleware';

describe('EPIC-18 TASK-18-02: generate_episode_highlights', () => {
    let tempDir: string;
    let dbPath: string;
    let connectionManager: DatabaseConnectionManager;
    let agentDb: AgentDb;
    let plugin: MemoryPlugin;
    let context: McpContext;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'ep18-highlights-tool-'));
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

    it('AC-1/2/5: returns normalized highlight payload and upserts one row per episode', async () => {
        insertEpisode({
            id: 'ep-100',
            sessionId: 'sess-100',
            outcomeSummary: 'Implemented domain-aware highlight generation and verified the persistence flow.',
        });

        const firstResult = await plugin.generate_episode_highlights({
            session_id: 'sess-100',
            llm_output: {
                key_decisions: ['Persist highlight rows through AgentDb'],
                lessons: ['Validate LLM output before writing to storage'],
                next_steps: ['Add vector sync in a later task'],
                quality_score: 0.91,
            },
            ai_model: 'gpt-5.4',
            ai_tokens_used: 420,
        }, context);

        expect(firstResult.success).toBe(true);
        expect(firstResult.data.episode_id).toBe('ep-100');
        expect(firstResult.data.key_decisions).toEqual(['Persist highlight rows through AgentDb']);
        expect(firstResult.data.lessons).toEqual(['Validate LLM output before writing to storage']);
        expect(firstResult.data.next_steps).toEqual(['Add vector sync in a later task']);
        expect(firstResult.data.quality_score).toBe(0.91);
        expect(firstResult.data.prompt_contract_version).toBe('epic-18.v1');

        const secondResult = await plugin.generate_episode_highlights({
            session_id: 'sess-100',
            llm_output: {
                key_decisions: ['Prefer idempotent upsert semantics for retries'],
                lessons: ['Keep one highlight row per episode'],
                next_steps: ['Continue with reflection workflow'],
                quality_score: 0.95,
            },
            ai_model: 'gpt-5.4',
            ai_tokens_used: 512,
        }, context);

        expect(secondResult.success).toBe(true);
        expect(secondResult.data.highlight_id).toBe(firstResult.data.highlight_id);

        const persisted = agentDb.getEpisodeHighlightByEpisodeId('ep-100');
        expect(persisted).not.toBeNull();
        expect(JSON.parse(persisted!.key_decisions ?? '[]')).toEqual(['Prefer idempotent upsert semantics for retries']);
        expect(persisted?.quality_score).toBe(0.95);
        expect(persisted?.ai_model).toBe('gpt-5.4');
        expect(persisted?.ai_tokens_used).toBe(512);

        const db = connectionManager.getAdminPool();
        const countRow = db.prepare('SELECT COUNT(*) as count FROM episode_highlights WHERE episode_id = ?').get('ep-100') as { count: number };
        expect(countRow.count).toBe(1);
    });

    it('AC-3: returns standardized NOT_FOUND response when session has no episodes', async () => {
        const result = await plugin.generate_episode_highlights({ session_id: 'missing-session' }, context);

        expect(result.success).toBe(false);
        expect(result.error.code).toBe('NOT_FOUND');
    });

    it('AC-6: falls back to deterministic summary when llm_output is malformed', async () => {
        insertEpisode({
            id: 'ep-200',
            sessionId: 'sess-200',
            outcomeSummary: 'Captured release-readiness notes and missing audit details for the next pass.',
            toolCallsJson: JSON.stringify([{ tool: 'session_complete', args: {} }]),
            artifactsJson: JSON.stringify([{ type: 'report', path: 'Docs/report.md' }]),
        });

        const result = await plugin.generate_episode_highlights({
            session_id: 'sess-200',
            llm_output: {
                key_decisions: 'not-an-array',
                lessons: [],
                next_steps: [],
                quality_score: '0.5',
            },
            ai_model: 'gpt-5.4',
            ai_tokens_used: 321,
        }, context);

        expect(result.success).toBe(true);
        expect(result.data.ai_generated).toBe(false);
        expect(result.data.warnings).toContain('INVALID_LLM_OUTPUT_FALLBACK');
        expect(result.data.key_decisions.length).toBeGreaterThan(0);
        expect(result.data.lessons.length).toBeGreaterThan(0);
        expect(result.data.next_steps.length).toBeGreaterThan(0);

        const persisted = agentDb.getEpisodeHighlightByEpisodeId('ep-200');
        expect(persisted).not.toBeNull();
        expect(persisted?.ai_generated).toBe(0);
        expect(persisted?.ai_model).toBe('gpt-5.4');
        expect(persisted?.ai_tokens_used).toBe(321);
    });

    function insertEpisode(params: {
        id: string;
        sessionId: string;
        outcomeSummary: string;
        toolCallsJson?: string;
        artifactsJson?: string;
    }): void {
        connectionManager.getAdminPool().prepare(`
            INSERT INTO episodes (
                id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        `).run(
            params.id,
            params.sessionId,
            'engineering',
            'delivery',
            'implementation',
            params.toolCallsJson ?? '[]',
            params.artifactsJson ?? '[]',
            params.outcomeSummary
        );
    }
});