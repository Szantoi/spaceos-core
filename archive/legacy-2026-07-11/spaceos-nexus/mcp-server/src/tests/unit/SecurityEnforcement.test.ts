/**
 * SecurityEnforcement.test.ts — Unit tests for DatabaseConnectionManager
 *
 * **Test Coverage:**
 * - Admin pool allows write operations (INSERT/UPDATE/DELETE)
 * - Agent pool rejects write operations (read-only enforcement)
 * - Agent pool allows SELECT queries
 * - Foreign key constraints enforced
 * - Connection cleanup
 *
 * @vitest
 */

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { join } from 'path';
import { existsSync, unlinkSync, mkdtempSync } from 'fs';
import { tmpdir } from 'os';

describe('TASK-09-01B: Security Enforcement (Dual-Pool Manager)', () => {
    let manager: DatabaseConnectionManager;
    let tempDir: string;
    let dbPath: string;

    beforeEach(() => {
        // Create temporary directory for test database (supports pooling)
        tempDir = mkdtempSync(join(tmpdir(), 'jest-db-'));
        dbPath = join(tempDir, 'test.db');

        // Create manager with file-based database (shared by both pools)
        manager = new DatabaseConnectionManager(dbPath);

        // Initialize schema (admin pool)
        const adminDb = manager.getAdminPool();
        adminDb.exec(`
      CREATE TABLE test_table (
        id INTEGER PRIMARY KEY,
        name TEXT NOT NULL
      )
    `);

        // Insert test data (admin pool)
        const stmt = adminDb.prepare('INSERT INTO test_table (name) VALUES (?)');
        stmt.run('Test Row 1');
        stmt.run('Test Row 2');
    });

    afterEach(() => {
        manager.close();

        // Clean up temp directory
        try {
            if (dbPath && existsSync(dbPath)) unlinkSync(dbPath);
            if (dbPath && existsSync(`${dbPath}-wal`)) unlinkSync(`${dbPath}-wal`);
            if (dbPath && existsSync(`${dbPath}-shm`)) unlinkSync(`${dbPath}-shm`);
            if (tempDir && existsSync(tempDir)) unlinkSync(tempDir);
        } catch {
            // Ignore cleanup errors
        }
    });

    describe('Admin Pool — Write Operations Allowed', () => {
        it('should allow INSERT on admin pool', () => {
            const adminDb = manager.getAdminPool();
            const stmt = adminDb.prepare('INSERT INTO test_table (name) VALUES (?)');

            expect(() => {
                stmt.run('Admin Insert');
            }).not.toThrow();

            // Verify data was inserted
            const result = adminDb
                .prepare('SELECT COUNT(*) as count FROM test_table')
                .get() as { count: number };
            expect(result.count).toBe(3);
        });

        it('should allow UPDATE on admin pool', () => {
            const adminDb = manager.getAdminPool();
            const stmt = adminDb.prepare('UPDATE test_table SET name = ? WHERE id = ?');

            expect(() => {
                stmt.run('Updated Name', 1);
            }).not.toThrow();

            // Verify update was applied
            const result = adminDb.prepare('SELECT name FROM test_table WHERE id = 1').get() as {
                name: string;
            };
            expect(result.name).toBe('Updated Name');
        });

        it('should allow DELETE on admin pool', () => {
            const adminDb = manager.getAdminPool();
            const stmt = adminDb.prepare('DELETE FROM test_table WHERE id = ?');

            expect(() => {
                stmt.run(1);
            }).not.toThrow();

            // Verify deletion was applied
            const result = adminDb
                .prepare('SELECT COUNT(*) as count FROM test_table')
                .get() as { count: number };
            expect(result.count).toBe(1);
        });
    });

    describe('Agent Pool — Write Operations Blocked (Read-Only)', () => {
        it('should reject INSERT on agent pool', () => {
            const agentDb = manager.getAgentPool();
            const stmt = agentDb.prepare('INSERT INTO test_table (name) VALUES (?)');

            expect(() => {
                stmt.run('Agent Insert');
            }).toThrow(/SQLITE_READONLY|attempt to write a readonly database/i);
        });

        it('should reject UPDATE on agent pool', () => {
            const agentDb = manager.getAgentPool();
            const stmt = agentDb.prepare('UPDATE test_table SET name = ? WHERE id = ?');

            expect(() => {
                stmt.run('Agent Update', 1);
            }).toThrow(/SQLITE_READONLY|attempt to write a readonly database/i);
        });

        it('should reject DELETE on agent pool', () => {
            const agentDb = manager.getAgentPool();
            const stmt = agentDb.prepare('DELETE FROM test_table WHERE id = ?');

            expect(() => {
                stmt.run(1);
            }).toThrow(/SQLITE_READONLY|attempt to write a readonly database/i);
        });
    });

    describe('Agent Pool — Read Operations Allowed', () => {
        it('should allow SELECT on agent pool', () => {
            const agentDb = manager.getAgentPool();
            const result = agentDb.prepare('SELECT * FROM test_table').all();

            expect(result).toBeDefined();
            expect(Array.isArray(result)).toBe(true);
            expect(result.length).toBe(2); // 2 rows inserted in beforeEach
        });

        it('should allow complex SELECT on agent pool', () => {
            const agentDb = manager.getAgentPool();
            const result = agentDb
                .prepare(
                    `
        SELECT id, name FROM test_table
        WHERE id = ?
        LIMIT 1
      `
                )
                .get(1) as { id: number; name: string };

            expect(result).toBeDefined();
            expect(result.id).toBe(1);
            expect(result.name).toBe('Test Row 1');
        });
    });

    describe('Connection Manager — Diagnostics & Verification', () => {
        it('should report agent pool as read-only', () => {
            const isReadOnly = manager.isAgentPoolReadOnly();
            expect(isReadOnly).toBe(true);
        });

        it('should report foreign keys enforced', () => {
            const areFkEnforced = manager.areForeignKeysEnforced();
            expect(areFkEnforced).toBe(true);
        });

        it('should provide diagnostics', () => {
            const diag = manager.getDiagnostics();

            expect(diag).toMatchObject({
                adminPoolOpen: true,
                agentPoolReadOnly: true,
                foreignKeysEnforced: true,
            });
            // WAL mode: 'wal' for file-based DB, 'memory' for :memory: DB
            expect(['wal', 'memory']).toContain(diag.walMode);
        });

        it('should close pools without error', () => {
            expect(() => {
                manager.close();
            }).not.toThrow();

            // Verify pools are closed (attempting query should fail)
            expect(() => {
                manager.getAdminPool().prepare('SELECT 1').get();
            }).toThrow();
        });
    });
});
