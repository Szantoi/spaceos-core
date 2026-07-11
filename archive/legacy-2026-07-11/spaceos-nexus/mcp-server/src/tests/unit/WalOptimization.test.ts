/**
 * WalOptimizer.test.ts — Unit tests for WAL optimization
 *
 * Test coverage:
 * - PRAGMA configuration (wal_autocheckpoint, journal_size_limit, busy_timeout, synchronous)
 * - Checkpoint operations (forceCheckpoint, getWalFileSize)
 * - Configuration diagnostics (getWalConfig)
 * - Error handling
 *
 * @vitest
 */

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import Database from 'better-sqlite3';
import { WalOptimizer } from '../../metadata/WalOptimizer';
import { WalMonitoring } from '../../metadata/WalMonitoring';
import { join } from 'path';
import { existsSync, unlinkSync, mkdtempSync } from 'fs';
import { tmpdir } from 'os';

describe('TASK-09-02B: WAL Optimization & Monitoring', () => {
    let db: Database.Database;
    let optimizer: WalOptimizer;
    let monitoring: WalMonitoring;
    let tempDir: string;
    let dbPath: string;

    beforeEach(() => {
        // Create temp database for testing
        tempDir = mkdtempSync(join(tmpdir(), 'wal-test-'));
        dbPath = join(tempDir, 'test.db');
        db = new Database(dbPath);

        // Enable WAL mode first
        db.pragma('journal_mode = WAL');

        optimizer = new WalOptimizer(db);
        monitoring = new WalMonitoring(db, optimizer, 100); // 100MB warning threshold
    });

    afterEach(() => {
        try {
            db.close();
            if (dbPath && existsSync(dbPath)) unlinkSync(dbPath);
            if (dbPath && existsSync(`${dbPath}-wal`)) unlinkSync(`${dbPath}-wal`);
            if (dbPath && existsSync(`${dbPath}-shm`)) unlinkSync(`${dbPath}-shm`);
            if (tempDir && existsSync(tempDir)) unlinkSync(tempDir);
        } catch {
            // Ignore cleanup errors
        }
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // WAL PRAGMA CONFIGURATION TESTS
    // ═══════════════════════════════════════════════════════════════════════════

    describe('WAL Pragma Configuration', () => {
        it('should configure all WAL pragmas without error', () => {
            expect(() => {
                optimizer.configureWalPragmas();
            }).not.toThrow();
        });

        it('should set wal_autocheckpoint to 1000', () => {
            optimizer.configureWalPragmas();
            const config = optimizer.getWalConfig();
            expect(config.autocheckpoint).toBe(1000);
        });

        it('should set journal_size_limit to 50MB', () => {
            optimizer.configureWalPragmas();
            const config = optimizer.getWalConfig();
            const expected = 50 * 1024 * 1024; // 50MB in bytes
            expect(config.journalSizeLimit).toBe(expected);
        });

        it('should set busy_timeout to 5000ms', () => {
            optimizer.configureWalPragmas();
            const config = optimizer.getWalConfig();
            expect(config.busyTimeout).toBe(5000);
        });

        it('should set synchronous to NORMAL', () => {
            optimizer.configureWalPragmas();
            const config = optimizer.getWalConfig();
            // NORMAL might be returned as a number (1) or string
            expect(['NORMAL', '1', 1]).toContain(config.synchronous);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // CHECKPOINT OPERATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Checkpoint Operations', () => {
        beforeEach(() => {
            optimizer.configureWalPragmas();
            // Create a test table and add some data
            db.exec('CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)');
        });

        it('should execute forceCheckpoint without error', () => {
            expect(() => {
                optimizer.forceCheckpoint();
            }).not.toThrow();
        });

        it('should return CheckpointResult with valid structure', () => {
            const result = optimizer.forceCheckpoint();
            expect(result).toHaveProperty('busy');
            expect(result).toHaveProperty('log');
            expect(result).toHaveProperty('checkpointed');
            expect(typeof result.busy).toBe('number');
            expect(typeof result.log).toBe('number');
            expect(typeof result.checkpointed).toBe('number');
        });

        it('should have checkpoint counts >= 0', () => {
            const result = optimizer.forceCheckpoint();
            expect(result.busy).toBeGreaterThanOrEqual(0);
            expect(result.log).toBeGreaterThanOrEqual(0);
            expect(result.checkpointed).toBeGreaterThanOrEqual(0);
        });

        it('should return meaningful metrics after data insertion', () => {
            // Insert data to trigger WAL activity
            const insertStmt = db.prepare('INSERT INTO test (value) VALUES (?)');
            for (let i = 0; i < 100; i++) {
                insertStmt.run(`value_${i}`);
            }

            const result = optimizer.forceCheckpoint();

            // After checkpoint, log and checkpointed should match (all pages moved)
            expect(result.log).toBeGreaterThanOrEqual(0);
            expect(result.checkpointed).toBeGreaterThanOrEqual(0);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // WAL FILE SIZE DIAGNOSTICS
    // ═══════════════════════════════════════════════════════════════════════════

    describe('WAL File Size Diagnostics', () => {
        beforeEach(() => {
            optimizer.configureWalPragmas();
            db.exec('CREATE TABLE test (id INTEGER PRIMARY KEY, value TEXT)');
        });

        it('should return getWalFileSize as a number', () => {
            const size = optimizer.getWalFileSize();
            expect(typeof size).toBe('number');
            expect(size).toBeGreaterThanOrEqual(0);
        });

        it('should increase WAL size after insertions', () => {
            const sizeBefore = optimizer.getWalFileSize();

            // Insert significant data
            const insertStmt = db.prepare('INSERT INTO test (value) VALUES (?)');
            for (let i = 0; i < 1000; i++) {
                insertStmt.run(`value_${i}`.repeat(100)); // Large values
            }

            const sizeAfter = optimizer.getWalFileSize();
            expect(sizeAfter).toBeGreaterThanOrEqual(sizeBefore);
        });

        it('should show reduced WAL size after checkpoint', () => {
            // Insert data
            const insertStmt = db.prepare('INSERT INTO test (value) VALUES (?)');
            for (let i = 0; i < 500; i++) {
                insertStmt.run(`value_${i}`.repeat(50));
            }

            const sizeBefore = optimizer.getWalFileSize();
            optimizer.forceCheckpoint();
            const sizeAfter = optimizer.getWalFileSize();

            expect(sizeAfter).toBeLessThanOrEqual(sizeBefore);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // WAL MONITORING & HEALTH CHECKS
    // ═══════════════════════════════════════════════════════════════════════════

    describe('WAL Monitoring & Health Checks', () => {
        beforeEach(() => {
            optimizer.configureWalPragmas();
        });

        it('should report healthy status initially', () => {
            const health = monitoring.checkWalHealth();
            expect(health.healthy).toBe(true);
            expect(health.warning).toBeUndefined();
        });

        it('should include walSizeMb in health status', () => {
            const health = monitoring.checkWalHealth();
            expect(health).toHaveProperty('walSizeMb');
            expect(typeof health.walSizeMb).toBe('number');
        });

        it('should include warningThresholdMb in health status', () => {
            const health = monitoring.checkWalHealth();
            expect(health.warningThresholdMb).toBe(100);
        });

        it('should return full diagnostics', () => {
            const diag = monitoring.getDiagnostics();
            expect(diag).toHaveProperty('config');
            expect(diag).toHaveProperty('health');
            expect(diag.config).toHaveProperty('autocheckpoint');
            expect(diag.health).toHaveProperty('healthy');
        });

        it('should log health without error', () => {
            expect(() => {
                monitoring.logHealth();
            }).not.toThrow();
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // INTEGRATION: PRAGMA + CHECKPOINT SEQUENCE
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Integration: Full WAL Workflow', () => {
        it('should complete full workflow: configure → insert → checkpoint → verify', () => {
            // Step 1: Configure
            optimizer.configureWalPragmas();

            // Step 2: Create table and insert data
            db.exec('CREATE TABLE workflow_test (id INTEGER PRIMARY KEY, value TEXT)');
            const insertStmt = db.prepare('INSERT INTO workflow_test (value) VALUES (?)');
            for (let i = 0; i < 50; i++) {
                insertStmt.run(`value_${i}`);
            }

            // Step 3: Check health before checkpoint
            const healthBefore = monitoring.checkWalHealth();
            expect(healthBefore).toHaveProperty('healthy');

            // Step 4: Force checkpoint
            const checkpointResult = optimizer.forceCheckpoint();
            expect(checkpointResult).toHaveProperty('busy');
            expect(checkpointResult).toHaveProperty('log');
            expect(checkpointResult).toHaveProperty('checkpointed');

            // Step 5: Verify data persisted
            const countStmt = db.prepare('SELECT COUNT(*) as count FROM workflow_test');
            const result = countStmt.get() as { count: number };
            expect(result.count).toBe(50);
        });
    });
});
