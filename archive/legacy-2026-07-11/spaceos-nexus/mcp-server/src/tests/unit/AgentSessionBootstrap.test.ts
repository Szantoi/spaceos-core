/**
 * AgentSessionBootstrap.test.ts — Session-level schema version tracking integration tests
 *
 * Validates:
 * - Session startup loads current versions
 * - Session end detects version changes
 * - Warnings logged for concurrent seeder updates
 * - Independent read/write layer tracking
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import Database from 'better-sqlite3';
import * as path from 'path';
import * as fs from 'fs';
import { SchemaVersionManager } from '../../mcp/SchemaVersionManager';
import { AgentSessionBootstrap } from '../../mcp/AgentSessionBootstrap';

describe('AgentSessionBootstrap', () => {
    let db: Database.Database;
    let schemaVersionManager: SchemaVersionManager;
    let bootstrap: AgentSessionBootstrap;
    const testDbPath = path.join(__dirname, '../.database/test-agent-bootstrap.db');

    const initializeSchema = (database: Database.Database): void => {
        database.exec(`
      CREATE TABLE IF NOT EXISTS schema_metadata (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        layer TEXT UNIQUE NOT NULL,
        version INTEGER NOT NULL DEFAULT 1,
        last_updated TEXT DEFAULT (datetime('now')),
        created_at TEXT DEFAULT (datetime('now'))
      );

      CREATE INDEX IF NOT EXISTS idx_schema_metadata_layer ON schema_metadata(layer);

      INSERT OR IGNORE INTO schema_metadata (layer, version)
      VALUES ('read-layer', 1), ('write-layer', 1);
    `);
    };

    beforeEach(() => {
        const testDir = path.dirname(testDbPath);
        if (!fs.existsSync(testDir)) {
            fs.mkdirSync(testDir, { recursive: true });
        }

        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }

        db = new Database(testDbPath);
        initializeSchema(db);

        schemaVersionManager = new SchemaVersionManager(db);
        bootstrap = new AgentSessionBootstrap(schemaVersionManager);
    });

    afterEach(() => {
        if (db) {
            db.close();
        }
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }
    });

    describe('onSessionStart()', () => {
        it('should load read-layer and write-layer versions at startup', () => {
            const ctx = bootstrap.onSessionStart();

            expect(ctx.readLayerVersion).toBe(1);
            expect(ctx.writeLayerVersion).toBe(1);
            expect(ctx.sessionStartTime).toBeDefined();
        });

        it('should log startup message to console', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            bootstrap.onSessionStart();

            const calls = consoleInfoSpy.mock.calls;
            const outputText = calls.map(c => c.join(' ')).join('\n');

            expect(outputText).toContain('Session started');
            expect(outputText).toContain('Read-layer version');
            expect(outputText).toContain('Write-layer version');

            consoleInfoSpy.mockRestore();
        });

        it('should store session context internally', () => {
            bootstrap.onSessionStart();

            const ctx = bootstrap.getSessionContext();
            expect(ctx).not.toBeNull();
            expect(ctx?.readLayerVersion).toBe(1);
        });
    });

    describe('onSessionEnd()', () => {
        it('should detect read-layer version increment', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            // Start session at v1
            bootstrap.onSessionStart();

            // Update version to v2 (simulating concurrent seeder)
            schemaVersionManager.incrementReadLayerVersion();

            // End session - should detect change
            bootstrap.onSessionEnd();

            const calls = consoleWarnSpy.mock.calls;
            const warnText = calls.map(c => c.join(' ')).join('\n');

            expect(warnText).toContain('Read-layer schema was updated');
            expect(warnText).toContain('v1');
            expect(warnText).toContain('v2');

            consoleWarnSpy.mockRestore();
        });

        it('should detect write-layer version increment', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            // Start session at v1
            bootstrap.onSessionStart();

            // Update write-layer version to v2
            schemaVersionManager.incrementWriteLayerVersion();

            // End session - should detect change
            bootstrap.onSessionEnd();

            const calls = consoleWarnSpy.mock.calls;
            const warnText = calls.map(c => c.join(' ')).join('\n');

            expect(warnText).toContain('Write-layer schema was updated');
            expect(warnText).toContain('v1');
            expect(warnText).toContain('v2');

            consoleWarnSpy.mockRestore();
        });

        it('should log success when no version changes occur', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            // Start session
            bootstrap.onSessionStart();

            // End session without any version changes
            bootstrap.onSessionEnd();

            const calls = consoleInfoSpy.mock.calls;
            const infoText = calls.map(c => c.join(' ')).join('\n');

            expect(infoText).toContain('No schema changes during session');

            consoleInfoSpy.mockRestore();
        });

        it('should handle multiple version increments during session', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            // Start session at v1
            bootstrap.onSessionStart();

            // Simulate multiple seeder runs
            schemaVersionManager.incrementReadLayerVersion(); // v1 → v2
            schemaVersionManager.incrementReadLayerVersion(); // v2 → v3

            // End session
            bootstrap.onSessionEnd();

            const calls = consoleWarnSpy.mock.calls;
            const warnText = calls.map(c => c.join(' ')).join('\n');

            expect(warnText).toContain('Read-layer schema was updated');
            expect(warnText).toContain('v1');
            expect(warnText).toContain('v3');

            consoleWarnSpy.mockRestore();
        });

        it('should warn about reading context when changes detected', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            bootstrap.onSessionStart();
            schemaVersionManager.incrementReadLayerVersion();
            bootstrap.onSessionEnd();

            const calls = consoleWarnSpy.mock.calls;
            const warnText = calls.map(c => c.join(' ')).join('\n');

            expect(warnText).toContain('Reload context data');

            consoleWarnSpy.mockRestore();
        });

        it('should warn about reloading workflows when write-layer changes', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            bootstrap.onSessionStart();
            schemaVersionManager.incrementWriteLayerVersion();
            bootstrap.onSessionEnd();

            const calls = consoleWarnSpy.mock.calls;
            const warnText = calls.map(c => c.join(' ')).join('\n');

            expect(warnText).toContain('Reload workflow');

            consoleWarnSpy.mockRestore();
        });
    });

    describe('getSessionContext()', () => {
        it('should return null before session start', () => {
            const ctx = bootstrap.getSessionContext();
            expect(ctx).toBeNull();
        });

        it('should return session context after start', () => {
            bootstrap.onSessionStart();
            const ctx = bootstrap.getSessionContext();

            expect(ctx).not.toBeNull();
            expect(ctx?.readLayerVersion).toBe(1);
            expect(ctx?.writeLayerVersion).toBe(1);
        });

        it('should preserve initial versions after session end', () => {
            bootstrap.onSessionStart();
            const ctxBefore = bootstrap.getSessionContext();

            // Simulate updates
            schemaVersionManager.incrementReadLayerVersion();

            bootstrap.onSessionEnd();
            const ctxAfter = bootstrap.getSessionContext();

            // Context should still reflect initial versions (not updated)
            expect(ctxAfter?.readLayerVersion).toBe(ctxBefore?.readLayerVersion);
            expect(ctxAfter?.readLayerVersion).toBe(1);
        });
    });

    describe('Error Handling', () => {
        it('should handle onSessionEnd before onSessionStart gracefully', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            expect(() => {
                bootstrap.onSessionEnd();
            }).not.toThrow();

            expect(consoleWarnSpy).toHaveBeenCalled();

            consoleWarnSpy.mockRestore();
        });

        it('should handle multiple onSessionStart calls', () => {
            expect(() => {
                bootstrap.onSessionStart();
                bootstrap.onSessionStart(); // Call again
            }).not.toThrow();

            // Second context should overwrite first
            const ctx = bootstrap.getSessionContext();
            expect(ctx?.readLayerVersion).toBe(1);
        });
    });

    describe('Full Session Lifecycle', () => {
        it('should track complete session from start to end with no changes', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            // Session start
            const ctx = bootstrap.onSessionStart();
            expect(ctx.readLayerVersion).toBe(1);

            // Session work (no version changes)

            // Session end
            bootstrap.onSessionEnd();

            const calls = consoleInfoSpy.mock.calls;
            const infoText = calls.map(c => c.join(' ')).join('\n');

            expect(infoText).toContain('Session started');
            expect(infoText).toContain('No schema changes during session');

            consoleInfoSpy.mockRestore();
        });

        it('should track complete session from start to end with schema updates', () => {
            const consoleSpy = vi.spyOn(console, 'info').mockImplementation(() => { });
            const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            // Session start
            bootstrap.onSessionStart();

            // Concurrent seeder updates schema
            schemaVersionManager.incrementReadLayerVersion();
            schemaVersionManager.incrementReadLayerVersion();

            // Session end
            bootstrap.onSessionEnd();

            expect(warnSpy).toHaveBeenCalled();
            const warnText = warnSpy.mock.calls.map(c => c.join(' ')).join('\n');
            expect(warnText).toContain('v1');
            expect(warnText).toContain('v3');

            consoleSpy.mockRestore();
            warnSpy.mockRestore();
        });
    });
});
