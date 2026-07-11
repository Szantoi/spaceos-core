import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { join } from 'path';
import { existsSync, mkdtempSync, rmSync } from 'fs';
import { tmpdir } from 'os';
import { AgentDb } from '../../mcp/AgentDb';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';

describe('EPIC-18 TASK-18-01: Episode highlights schema and helpers', () => {
    let tempDir: string;
    let dbPath: string;
    let manager: DatabaseConnectionManager;
    let agentDb: AgentDb;

    beforeEach(() => {
        tempDir = mkdtempSync(join(tmpdir(), 'agentdb-ep18-'));
        dbPath = join(tempDir, 'metadata.db');
        manager = new DatabaseConnectionManager(dbPath);
        agentDb = new AgentDb(manager);
        agentDb.initSchema();
    });

    afterEach(() => {
        try {
            manager.close();
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

    it('AC-1: episode_highlights table exists with expected columns', () => {
        const db = manager.getAdminPool();
        const cols = (db.prepare('PRAGMA table_info(episode_highlights)').all() as Array<{ name: string }>).map(c => c.name);

        expect(cols).toContain('id');
        expect(cols).toContain('episode_id');
        expect(cols).toContain('key_decisions');
        expect(cols).toContain('lessons');
        expect(cols).toContain('next_steps');
        expect(cols).toContain('quality_score');
        expect(cols).toContain('ai_generated');
        expect(cols).toContain('ai_model');
        expect(cols).toContain('ai_tokens_used');
        expect(cols).toContain('created_at');
    });

    it('AC-2: highlight_feedback table exists', () => {
        const db = manager.getAdminPool();
        const row = db.prepare("SELECT name FROM sqlite_master WHERE type='table' AND name='highlight_feedback'").get() as { name: string } | undefined;
        expect(row?.name).toBe('highlight_feedback');
    });

    it('AC-3: highlights_chromadb_sync table exists', () => {
        const db = manager.getAdminPool();
        const row = db.prepare("SELECT name FROM sqlite_master WHERE type='table' AND name='highlights_chromadb_sync'").get() as { name: string } | undefined;
        expect(row?.name).toBe('highlights_chromadb_sync');
    });

    it('AC-4: initSchema remains idempotent with EPIC-18 migration', () => {
        const manager2 = new DatabaseConnectionManager(dbPath);
        const agentDb2 = new AgentDb(manager2);
        expect(() => agentDb2.initSchema()).not.toThrow();
        manager2.close();
    });

    it('AC-5: deleting highlight cascades highlight_feedback rows', () => {
        const db = manager.getAdminPool();

        db.prepare(`
      INSERT INTO episodes (
        id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `).run('ep-1', 'sess-1', 'engineering', 'delivery', 'implementation', '[]', '[]', 'summary');

        agentDb.upsertEpisodeHighlight({
            id: 'hl-1',
            episode_id: 'ep-1',
            key_decisions: JSON.stringify(['Use migration 009']),
            lessons: JSON.stringify(['Keep migration idempotent']),
            next_steps: JSON.stringify(['Add tests']),
            quality_score: 0.8,
            ai_generated: true,
            ai_model: 'test-model',
            ai_tokens_used: 123,
        });

        agentDb.insertHighlightFeedback({
            id: 'fb-1',
            highlight_id: 'hl-1',
            rater_agent_id: 'agent-a',
            quality_score: 0.7,
            comment: 'Looks good',
        });

        const before = agentDb.listHighlightFeedback('hl-1');
        expect(before).toHaveLength(1);

        db.prepare('DELETE FROM episode_highlights WHERE id = ?').run('hl-1');

        const after = agentDb.listHighlightFeedback('hl-1');
        expect(after).toHaveLength(0);
    });

    it('AC-6: helper methods read/write highlights and sync metadata', () => {
        const db = manager.getAdminPool();

        db.prepare(`
      INSERT INTO episodes (
        id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `).run('ep-2', 'sess-2', 'engineering', 'delivery', 'review', '[]', '[]', 'summary');

        agentDb.upsertEpisodeHighlight({
            id: 'hl-2',
            episode_id: 'ep-2',
            lessons: JSON.stringify(['Write failing test first'])
        });

        const row = agentDb.getEpisodeHighlightByEpisodeId('ep-2');
        expect(row).not.toBeNull();
        expect(row?.id).toBe('hl-2');

        agentDb.upsertHighlightChromaSync({
            highlight_id: 'hl-2',
            vector_id: 'vec-2',
            embedding_model: 'test-embedding-model'
        });

        const sync = agentDb.getHighlightChromaSync('hl-2');
        expect(sync).not.toBeNull();
        expect(sync?.vector_id).toBe('vec-2');
    });
});
