/**
 * WriteLayerSchema.test.ts
 *
 * Unit tests for EPIC-08 Write Layer SQLite schema.
 *
 * Coverage:
 *   1. Table creation (happy path)
 *   2. Foreign key constraints
 *   3. Unique constraints
 *   4. Default values
 *   5. Migration idempotency (backward compatibility)
 *
 * Test framework: Node.test (built-in) or Jest (if available)
 * Database: In-memory SQLite or temporary file
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
// @ts-ignore
const Database: any = require('better-sqlite3');
import * as fs from 'fs';
import * as path from 'path';
import WriteLayerInitializer from '../../metadata/WriteLayerInitializer';

// Helper: generate UUID-like string for testing
function generateTestId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

describe('WriteLayerSchema', () => {
    let db: any;
    let dbPath: string;

    beforeEach(() => {
        // Use in-memory SQLite for tests
        db = new Database(':memory:');
    });

    afterEach(() => {
        if (db) {
            try {
                db.close();
            } catch (e) {
                // Already closed
            }
        }
    });

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 1: Table Creation (Happy Path)
    // ─────────────────────────────────────────────────────────────────────────

    describe('Table Creation', () => {
        it('should create all 4 write-layer tables on init', () => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();

            const tables = db
                .prepare("SELECT name FROM sqlite_master WHERE type='table'")
                .all() as Array<{ name: string }>;

            const tableNames = tables.map(t => t.name);
            expect(tableNames).toContain('sessions');
            expect(tableNames).toContain('artifacts');
            expect(tableNames).toContain('workflow_events');
            expect(tableNames).toContain('checkpoints');
        });

        it('should create all indexes', () => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();

            const indexes = db
                .prepare("SELECT name FROM sqlite_master WHERE type='index' AND name LIKE 'idx_%'")
                .all() as Array<{ name: string }>;

            const indexNames = indexes.map(i => i.name);
            expect(indexNames.length).toBeGreaterThanOrEqual(5);
            expect(indexNames.some(n => n.includes('sessions'))).toBeTruthy();
            expect(indexNames.some(n => n.includes('artifacts'))).toBeTruthy();
        });

        it('should run idempotently (safely run multiple times)', () => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();  // First run
            initializer.init();  // Second run — should not error

            // Verify tables still exist and are intact
            const sessionCount = db.prepare('SELECT COUNT(*) as cnt FROM sessions').get() as { cnt: number };
            expect(sessionCount.cnt).toBe(0);
        });
    });

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 2: Foreign Key Constraints
    // ─────────────────────────────────────────────────────────────────────────

    describe('Foreign Key Constraints', () => {
        beforeEach(() => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();
        });

        it('should enforce FK constraint: artifact -> session', () => {
            const invalidSessionId = generateTestId();
            const artifactId = generateTestId();

            const insertArtifact = () => {
                db.prepare(
                    'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
                ).run(
                    artifactId,
                    invalidSessionId,  // Non-existent session
                    'implementation_summary',
                    'test content',
                    new Date().toISOString()
                );
            };

            expect(insertArtifact).toThrow(/FOREIGN KEY constraint failed/);
        });

        it('should allow artifact insert with valid session FK', () => {
            const sessionId = generateTestId();
            const artifactId = generateTestId();

            // Create session first
            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                new Date().toISOString()
            );

            // Insert artifact with valid FK
            db.prepare(
                'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                artifactId,
                sessionId,
                'implementation_summary',
                'test content',
                new Date().toISOString()
            );

            const artifact = db.prepare('SELECT * FROM artifacts WHERE id = ?').get(artifactId);
            expect(artifact).toBeDefined();
            expect(artifact.session_id).toBe(sessionId);
        });

        it('should cascade delete artifacts when session deleted', () => {
            const sessionId = generateTestId();
            const artifactId = generateTestId();

            // Create session + artifact
            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                new Date().toISOString()
            );

            db.prepare(
                'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                artifactId,
                sessionId,
                'implementation_summary',
                'test content',
                new Date().toISOString()
            );

            // Delete session
            db.prepare('DELETE FROM sessions WHERE id = ?').run(sessionId);

            // Artifact should be deleted (cascade)
            const artifact = db.prepare('SELECT * FROM artifacts WHERE id = ?').get(artifactId);
            expect(artifact).toBeUndefined();
        });
    });

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 3: Unique Constraints
    // ─────────────────────────────────────────────────────────────────────────

    describe('Unique Constraints', () => {
        beforeEach(() => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();
        });

        it('should enforce UNIQUE constraint on sessions (one active per agent_id)', () => {
            const sessionId1 = generateTestId();
            const sessionId2 = generateTestId();
            const agentId = 'backend_developer_001';

            // Insert first session
            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId1,
                agentId,
                'engineering',
                'backend_developer',
                new Date().toISOString()
            );

            // Try to insert second session with same agent_id
            const insertDuplicate = () => {
                db.prepare(
                    'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
                ).run(
                    sessionId2,
                    agentId,  // Duplicate agent_id
                    'engineering',
                    'backend_developer',
                    new Date().toISOString()
                );
            };

            expect(insertDuplicate).toThrow(/UNIQUE constraint failed/);
        });

        it('should enforce UNIQUE constraint on artifacts (session + type + timestamp)', () => {
            const sessionId = generateTestId();
            const artifactId1 = generateTestId();
            const artifactId2 = generateTestId();
            const timestamp = new Date().toISOString();

            // Create session
            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                timestamp
            );

            // Insert first artifact
            db.prepare(
                'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                artifactId1,
                sessionId,
                'implementation_summary',
                'content1',
                timestamp
            );

            // Try to insert duplicate (same session + type + timestamp)
            const insertDuplicate = () => {
                db.prepare(
                    'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
                ).run(
                    artifactId2,
                    sessionId,
                    'implementation_summary',  // Same type + timestamp
                    'content2',
                    timestamp
                );
            };

            expect(insertDuplicate).toThrow(/UNIQUE constraint failed/);
        });
    });

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 4: Default Values
    // ─────────────────────────────────────────────────────────────────────────

    describe('Default Values', () => {
        beforeEach(() => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();
        });

        it('should default fsm_state to "started" for new session', () => {
            const sessionId = generateTestId();

            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                new Date().toISOString()
            );

            const session = db.prepare('SELECT * FROM sessions WHERE id = ?').get(sessionId);
            expect(session.fsm_state).toBe('started');
        });

        it('should store timestamps in ISO 8601 format', () => {
            const sessionId = generateTestId();
            const now = new Date().toISOString();

            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                now
            );

            const session = db.prepare('SELECT * FROM sessions WHERE id = ?').get(sessionId);
            expect(session.started_at).toMatch(/^\d{4}-\d{2}-\d{2}T/);
        });

        it('should default embedded to false for artifacts', () => {
            const sessionId = generateTestId();
            const artifactId = generateTestId();

            db.prepare(
                'INSERT INTO sessions (id, agent_id, domain, role, started_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                sessionId,
                'backend_developer_001',
                'engineering',
                'backend_developer',
                new Date().toISOString()
            );

            db.prepare(
                'INSERT INTO artifacts (id, session_id, artifact_type, content, submitted_at) VALUES (?, ?, ?, ?, ?)'
            ).run(
                artifactId,
                sessionId,
                'implementation_summary',
                'test',
                new Date().toISOString()
            );

            const artifact = db.prepare('SELECT * FROM artifacts WHERE id = ?').get(artifactId);
            expect(artifact.embedded).toBe(0);
        });
    });

    // ─────────────────────────────────────────────────────────────────────────
    // TEST 5: Schema Verification Helper
    // ─────────────────────────────────────────────────────────────────────────

    describe('Schema Verification', () => {
        beforeEach(() => {
            const initializer = new WriteLayerInitializer(db);
            initializer.init();
        });

        it('should provide schema info for tables', () => {
            const initializer = new WriteLayerInitializer(db);
            const schemaInfo = initializer.getSchemaInfo();

            expect(schemaInfo.sessions).toBeDefined();
            expect(schemaInfo.artifacts).toBeDefined();
            expect(schemaInfo.workflow_events).toBeDefined();
            expect(schemaInfo.checkpoints).toBeDefined();

            expect(schemaInfo.sessions.columns.length).toBeGreaterThan(0);
            expect(
                schemaInfo.sessions.columns.some((c: any) => c.name === 'id' && c.primaryKey)
            ).toBeTruthy();
        });
    });
});
