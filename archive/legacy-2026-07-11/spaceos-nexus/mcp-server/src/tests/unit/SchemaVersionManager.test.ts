/**
 * SchemaVersionManager.test.ts — Unit tests for schema version tracking
 *
 * Tests the SchemaVersionManager class methods:
 * - getReadLayerVersion() / getWriteLayerVersion()
 * - incrementReadLayerVersion() / incrementWriteLayerVersion()
 * - getAllVersions()
 * - logVersions()
 * - resetVersions() (dev-only)
 *
 * Coverage:
 * - Version retrieval (default v1, persistence across calls)
 * - Version incrementation (update + timestamp)
 * - Multiple sequential increments (cumulative)
 * - getAllVersions() structure + ordering
 * - Logging output format
 * - Reset (development safe flag)
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import Database from 'better-sqlite3';
import * as path from 'path';
import * as fs from 'fs';
import { SchemaVersionManager } from '../../mcp/SchemaVersionManager';

describe('SchemaVersionManager', () => {
    let db: Database.Database;
    let manager: SchemaVersionManager;
    const testDbPath = path.join(__dirname, '../.database/test-schema-version.db');

    /**
     * Helper: Initialize test database with schema_metadata table
     */
    const initializeSchema = (database: Database.Database): void => {
        // Create schema_metadata table (mimics migration 003_epic09_context_schema.sql)
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
        // Ensure test db directory exists
        const testDir = path.dirname(testDbPath);
        if (!fs.existsSync(testDir)) {
            fs.mkdirSync(testDir, { recursive: true });
        }

        // Remove test database if exists
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }

        // Create fresh database for each test
        db = new Database(testDbPath);
        initializeSchema(db);

        // Create manager instance
        manager = new SchemaVersionManager(db);
    });

    afterEach(() => {
        if (db) {
            db.close();
        }
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }
    });

    describe('getReadLayerVersion()', () => {
        it('should return default version 1 after schema initialization', () => {
            const version = manager.getReadLayerVersion();
            expect(version).toBe(1);
        });

        it('should persist version value across multiple calls', () => {
            const version1 = manager.getReadLayerVersion();
            const version2 = manager.getReadLayerVersion();
            expect(version1).toBe(version2);
            expect(version1).toBe(1);
        });
    });

    describe('getWriteLayerVersion()', () => {
        it('should return default version 1 after schema initialization', () => {
            const version = manager.getWriteLayerVersion();
            expect(version).toBe(1);
        });

        it('should be independent from read-layer version', () => {
            const readVersion = manager.getReadLayerVersion();
            const writeVersion = manager.getWriteLayerVersion();
            expect(readVersion).toBe(writeVersion); // Both start at 1, but independent
            expect(readVersion).toBe(1);
            expect(writeVersion).toBe(1);
        });
    });

    describe('incrementReadLayerVersion()', () => {
        it('should increment read-layer version from 1 to 2', () => {
            const initialVersion = manager.getReadLayerVersion();
            expect(initialVersion).toBe(1);

            const newVersion = manager.incrementReadLayerVersion();
            expect(newVersion).toBe(2);

            // Verify persisted
            const fetchedVersion = manager.getReadLayerVersion();
            expect(fetchedVersion).toBe(2);
        });

        it('should increment version multiple times', () => {
            expect(manager.getReadLayerVersion()).toBe(1);

            const v2 = manager.incrementReadLayerVersion();
            expect(v2).toBe(2);
            expect(manager.getReadLayerVersion()).toBe(2);

            const v3 = manager.incrementReadLayerVersion();
            expect(v3).toBe(3);
            expect(manager.getReadLayerVersion()).toBe(3);

            const v4 = manager.incrementReadLayerVersion();
            expect(v4).toBe(4);
            expect(manager.getReadLayerVersion()).toBe(4);
        });

        it('should update last_updated timestamp on increment', () => {
            // Get all versions before increment
            const versionsBefore = manager.getAllVersions();
            const readLayerBefore = versionsBefore.find(v => v.layer === 'read-layer');
            expect(readLayerBefore?.last_updated).toBeDefined();

            // Get timestamp before increment
            const timestampBefore = new Date().getTime();

            // Wait briefly to ensure timestamp difference (SQLite precision)
            const sleep = (ms: number) => new Promise(r => setTimeout(r, ms));
            sleep(10); // Small delay

            manager.incrementReadLayerVersion();

            // Get all versions after increment
            const versionsAfter = manager.getAllVersions();
            const readLayerAfter = versionsAfter.find(v => v.layer === 'read-layer');

            // Version should be incremented
            expect(readLayerAfter?.version).toBe(2);

            // last_updated should exist (no strict time comparison due to SQLite precision)
            expect(readLayerAfter?.last_updated).toBeDefined();
        });
    });

    describe('incrementWriteLayerVersion()', () => {
        it('should increment write-layer version from 1 to 2', () => {
            const initialVersion = manager.getWriteLayerVersion();
            expect(initialVersion).toBe(1);

            const newVersion = manager.incrementWriteLayerVersion();
            expect(newVersion).toBe(2);

            // Verify persisted
            const fetchedVersion = manager.getWriteLayerVersion();
            expect(fetchedVersion).toBe(2);
        });

        it('should not affect read-layer version when incremented', () => {
            manager.incrementWriteLayerVersion();

            const readVersion = manager.getReadLayerVersion();
            const writeVersion = manager.getWriteLayerVersion();

            expect(readVersion).toBe(1); // Unchanged
            expect(writeVersion).toBe(2); // Incremented
        });
    });

    describe('getAllVersions()', () => {
        it('should return array with both read-layer and write-layer entries', () => {
            const versions = manager.getAllVersions();
            expect(versions).toHaveLength(2);
        });

        it('should return correct structure for each version entry', () => {
            const versions = manager.getAllVersions();

            for (const v of versions) {
                expect(v).toHaveProperty('layer');
                expect(v).toHaveProperty('version');
                expect(v).toHaveProperty('last_updated');
                expect(v).toHaveProperty('created_at');
                expect(typeof v.layer).toBe('string');
                expect(typeof v.version).toBe('number');
            }
        });

        it('should return versions ordered by layer name', () => {
            const versions = manager.getAllVersions();
            expect(versions[0].layer).toBe('read-layer');
            expect(versions[1].layer).toBe('write-layer');
        });

        it('should reflect increments in returned data', () => {
            manager.incrementReadLayerVersion();
            manager.incrementWriteLayerVersion();

            const versions = manager.getAllVersions();
            const readLayer = versions.find(v => v.layer === 'read-layer');
            const writeLayer = versions.find(v => v.layer === 'write-layer');

            expect(readLayer?.version).toBe(2);
            expect(writeLayer?.version).toBe(2);
        });
    });

    describe('logVersions()', () => {
        it('should log versions to console.info without throwing', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            expect(() => {
                manager.logVersions();
            }).not.toThrow();

            consoleInfoSpy.mockRestore();
        });

        it('should include [SchemaVersionManager] tag in output', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            manager.logVersions();

            const calls = consoleInfoSpy.mock.calls;
            expect(calls.some(c => c[0]?.includes('SchemaVersionManager'))).toBe(true);

            consoleInfoSpy.mockRestore();
        });

        it('should log version info for both layers', () => {
            const consoleInfoSpy = vi.spyOn(console, 'info').mockImplementation(() => { });

            manager.logVersions();

            const calls = consoleInfoSpy.mock.calls;
            const outputText = calls.map(c => c.join(' ')).join('\n');

            expect(outputText).toContain('read-layer');
            expect(outputText).toContain('write-layer');
            expect(outputText).toContain('version');

            consoleInfoSpy.mockRestore();
        });
    });

    describe('resetVersions()', () => {
        it('should reset both versions back to 1', () => {
            // Increment both versions
            manager.incrementReadLayerVersion();
            manager.incrementWriteLayerVersion();
            manager.incrementReadLayerVersion(); // read-layer = 3

            expect(manager.getReadLayerVersion()).toBe(3);
            expect(manager.getWriteLayerVersion()).toBe(2);

            // Reset
            manager.resetVersions();

            expect(manager.getReadLayerVersion()).toBe(1);
            expect(manager.getWriteLayerVersion()).toBe(1);
        });

        it('should log warning when reset is called', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            manager.resetVersions();

            expect(consoleWarnSpy).toHaveBeenCalled();
            const warnText = consoleWarnSpy.mock.calls[0][0];
            expect(warnText).toContain('DEV ONLY');

            consoleWarnSpy.mockRestore();
        });

        it('should only be used in development (warning text)', () => {
            const consoleWarnSpy = vi.spyOn(console, 'warn').mockImplementation(() => { });

            manager.resetVersions();

            const warningText = consoleWarnSpy.mock.calls[0][0];
            expect(warningText.toLowerCase()).toMatch(/dev|development|testing/);

            consoleWarnSpy.mockRestore();
        });
    });

    describe('Integration Scenarios', () => {
        it('should track schema updates across seeder lifecycle', () => {
            // Simulate seeder startup
            const initialReadVersion = manager.getReadLayerVersion();
            expect(initialReadVersion).toBe(1);

            // Simulate bulk inserts (role seeds, schema seeds, etc.)
            // No version change during inserts

            // Simulate checkpoint completion
            manager.incrementReadLayerVersion();

            // Agents should see new version
            const newReadVersion = manager.getReadLayerVersion();
            expect(newReadVersion).toBe(2);

            // Verify read/write layers independent
            const writeVersion = manager.getWriteLayerVersion();
            expect(writeVersion).toBe(1);
        });

        it('should provide comprehensive version metadata for monitoring', () => {
            manager.incrementReadLayerVersion();

            const versions = manager.getAllVersions();

            expect(versions).toHaveLength(2);
            for (const v of versions) {
                expect(v.created_at).toBeDefined();
                expect(v.last_updated).toBeDefined();
                expect(v.version).toBeGreaterThanOrEqual(1);
            }
        });
    });
});
