/**
 * src/tests/unit/dwi-schema.test.ts
 *
 * Unit tests for DWI schema validation, database structure, and constraints.
 * Standards: database/standards/01-discovery/discovery.work-item.standard.md
 */

import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import Database from 'better-sqlite3';
import { z } from 'zod';
import {
    DiscoveryWorkItemSchema,
    DwiPhaseGateSchema,
    DwiHypothesisSchema,
} from '../../metadata/dwi-schema';
import fs from 'fs';
import path from 'path';

describe('DWI Schema — Database and Zod Validation', () => {
    let db: Database.Database;
    const testDbPath = path.join(__dirname, '../../..', 'test-dwi-schema.db');

    beforeAll(() => {
        // Create test database
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }

        db = new Database(testDbPath);
        db.pragma('foreign_keys = ON');

        // Load migration
        const migrationPath = path.join(__dirname, '../../metadata/migrations/004-dwi-schema.sql');
        const migration = fs.readFileSync(migrationPath, 'utf8');
        db.exec(migration);
    });

    afterAll(() => {
        db.close();
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }
    });

    // =========================================================================
    // Schema Structure Tests
    // =========================================================================

    describe('discovery_work_items table', () => {
        it('should have all required columns', () => {
            const info = db.prepare(`PRAGMA table_info(discovery_work_items)`).all();
            const columnNames = (info as any[]).map((col) => col.name);

            expect(columnNames).toContain('id');
            expect(columnNames).toContain('topic');
            expect(columnNames).toContain('status');
            expect(columnNames).toContain('current_phase');
            expect(columnNames).toContain('next_action');
            expect(columnNames).toContain('verdict');
            expect(columnNames).toContain('hypothesis_count');
            expect(columnNames).toContain('validated_count');
            expect(columnNames).toContain('created_at');
            expect(columnNames).toContain('updated_at');
        });

        it('should enforce NOT NULL constraints', () => {
            const columns = (db.prepare(`PRAGMA table_info(discovery_work_items)`).all() as any[])
                .filter((col) => col.notnull);

            const notNullColumns = columns.map((col) => col.name);
            // Note: PRIMARY KEY columns have implicit NOT NULL; may not appear in PRAGMA notnull check
            // Check only the non-PK NOT NULL constraints
            expect(notNullColumns).toContain('topic');
            expect(notNullColumns).toContain('status');
            expect(notNullColumns).toContain('current_phase');
            expect(notNullColumns).toContain('next_action');
        });

        it('should allow NULL for verdict', () => {
            const columns = (db.prepare(`PRAGMA table_info(discovery_work_items)`).all() as any[])
                .filter((col) => col.name === 'verdict');

            expect(columns[0].notnull).toBe(0); // 0 = allows NULL
        });

        it('should have id as PRIMARY KEY', () => {
            const info = db.prepare(`PRAGMA table_info(discovery_work_items)`).all() as any[];
            const idCol = info.find((col) => col.name === 'id');
            expect(idCol.pk).toBe(1);
        });
    });

    describe('dwi_phase_gates table', () => {
        it('should have all required columns', () => {
            const info = db.prepare(`PRAGMA table_info(dwi_phase_gates)`).all();
            const columnNames = (info as any[]).map((col) => col.name);

            expect(columnNames).toContain('id');
            expect(columnNames).toContain('dwi_id');
            expect(columnNames).toContain('phase');
            expect(columnNames).toContain('gate_crossed');
            expect(columnNames).toContain('gate_crossed_date');
            expect(columnNames).toContain('notes');
            expect(columnNames).toContain('created_at');
        });

        it('should have dwi_id as FOREIGN KEY to discovery_work_items', () => {
            const fks = db.prepare(`PRAGMA foreign_key_list(dwi_phase_gates)`).all() as any[];
            expect(fks.length).toBeGreaterThan(0);

            const dwiFK = fks.find((fk) => fk.from === 'dwi_id');
            expect(dwiFK).toBeDefined();
            expect(dwiFK.table).toBe('discovery_work_items');
            expect(dwiFK.to).toBe('id');
        });
    });

    describe('dwi_hypotheses table', () => {
        it('should have all required columns', () => {
            const info = db.prepare(`PRAGMA table_info(dwi_hypotheses)`).all();
            const columnNames = (info as any[]).map((col) => col.name);

            expect(columnNames).toContain('id');
            expect(columnNames).toContain('dwi_id');
            expect(columnNames).toContain('statement');
            expect(columnNames).toContain('status');
            expect(columnNames).toContain('phase');
            expect(columnNames).toContain('artifact_path');
            expect(columnNames).toContain('created_at');
            expect(columnNames).toContain('closed_at');
        });

        it('should have dwi_id as FOREIGN KEY to discovery_work_items', () => {
            const fks = db.prepare(`PRAGMA foreign_key_list(dwi_hypotheses)`).all() as any[];
            const dwiFK = fks.find((fk) => fk.from === 'dwi_id');
            expect(dwiFK).toBeDefined();
            expect(dwiFK.table).toBe('discovery_work_items');
        });
    });

    // =========================================================================
    // Data Integrity Tests
    // =========================================================================

    describe('Foreign Key Constraints', () => {
        it('should prevent orphaned phase gates', () => {
            expect(() => {
                db.prepare(
                    `INSERT INTO dwi_phase_gates (dwi_id, phase, gate_crossed, created_at)
           VALUES (?, ?, ?, CURRENT_TIMESTAMP)`
                ).run('dwi-nonexistent', 0, 0);
            }).toThrow();
        });

        it('should prevent orphaned hypotheses', () => {
            expect(() => {
                db.prepare(
                    `INSERT INTO dwi_hypotheses (id, dwi_id, statement, status, phase, created_at)
           VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP)`
                ).run('hyp-001', 'dwi-nonexistent', 'Test hypothesis', 'open', 1);
            }).toThrow();
        });

        it('should cascade delete phase gates when DWI is deleted', () => {
            // Insert DWI
            db.prepare(
                `INSERT INTO discovery_work_items (id, topic, status, current_phase, next_action, created_at, updated_at)
         VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)`
            ).run('dwi-test-cascade', 'Test Topic', 'open', 0, 'Test action');

            // Insert phase gate
            db.prepare(
                `INSERT INTO dwi_phase_gates (dwi_id, phase, gate_crossed, created_at)
         VALUES (?, ?, ?, CURRENT_TIMESTAMP)`
            ).run('dwi-test-cascade', 0, 1);

            // Verify phase gate exists
            let count = (
                db.prepare(`SELECT COUNT(*) as cnt FROM dwi_phase_gates WHERE dwi_id = ?`)
                    .get('dwi-test-cascade') as any
            ).cnt;
            expect(count).toBe(1);

            // Delete DWI
            db.prepare(`DELETE FROM discovery_work_items WHERE id = ?`).run('dwi-test-cascade');

            // Verify phase gate was cascade deleted
            count = (
                db.prepare(`SELECT COUNT(*) as cnt FROM dwi_phase_gates WHERE dwi_id = ?`)
                    .get('dwi-test-cascade') as any
            ).cnt;
            expect(count).toBe(0);
        });

        it('should cascade delete hypotheses when DWI is deleted', () => {
            // Insert DWI
            db.prepare(
                `INSERT INTO discovery_work_items (id, topic, status, current_phase, next_action, created_at, updated_at)
         VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)`
            ).run('dwi-test-cascade-hyp', 'Test Topic', 'open', 1, 'Test action');

            // Insert hypothesis
            db.prepare(
                `INSERT INTO dwi_hypotheses (id, dwi_id, statement, status, phase, created_at)
         VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP)`
            ).run('hyp-001', 'dwi-test-cascade-hyp', 'Test hypothesis statement', 'open', 1);

            // Verify hypothesis exists
            let count = (
                db.prepare(`SELECT COUNT(*) as cnt FROM dwi_hypotheses WHERE dwi_id = ?`)
                    .get('dwi-test-cascade-hyp') as any
            ).cnt;
            expect(count).toBe(1);

            // Delete DWI
            db.prepare(`DELETE FROM discovery_work_items WHERE id = ?`).run('dwi-test-cascade-hyp');

            // Verify hypothesis was cascade deleted
            count = (
                db.prepare(`SELECT COUNT(*) as cnt FROM dwi_hypotheses WHERE dwi_id = ?`)
                    .get('dwi-test-cascade-hyp') as any
            ).cnt;
            expect(count).toBe(0);
        });
    });

    // =========================================================================
    // Zod Schema Validation Tests
    // =========================================================================

    describe('Zod Schema Validation', () => {
        it('should validate valid DWI object', () => {
            const validDwi = {
                id: 'dwi-test-topic',
                topic: 'Test Topic',
                status: 'open' as const,
                current_phase: 1,
                next_action: 'Explorer: log next observation',
                verdict: null,
                hypothesis_count: 0,
                validated_count: 0,
                created_at: new Date().toISOString(),
                updated_at: new Date().toISOString(),
            };

            expect(() => {
                DiscoveryWorkItemSchema.parse(validDwi);
            }).not.toThrow();
        });

        it('should reject invalid status enum', () => {
            const invalidDwi = {
                id: 'dwi-test-topic',
                topic: 'Test Topic',
                status: 'invalid-status',
                current_phase: 1,
                next_action: 'Test action',
                verdict: null,
                hypothesis_count: 0,
                validated_count: 0,
                created_at: new Date().toISOString(),
                updated_at: new Date().toISOString(),
            };

            expect(() => {
                DiscoveryWorkItemSchema.parse(invalidDwi);
            }).toThrow();
        });

        it('should reject invalid phase (out of bounds)', () => {
            const invalidDwi = {
                id: 'dwi-test-topic',
                topic: 'Test Topic',
                status: 'open' as const,
                current_phase: 5, // invalid
                next_action: 'Test action',
                verdict: null,
                hypothesis_count: 0,
                validated_count: 0,
                created_at: new Date().toISOString(),
                updated_at: new Date().toISOString(),
            };

            expect(() => {
                DiscoveryWorkItemSchema.parse(invalidDwi);
            }).toThrow();
        });

        it('should accept all valid verdicts', () => {
            const verdicts: Array<'validated' | 'invalidated' | 'pivoted' | null> = [
                'validated',
                'invalidated',
                'pivoted',
                null,
            ];

            verdicts.forEach((verdict) => {
                expect(() => {
                    DiscoveryWorkItemSchema.parse({
                        id: 'dwi-test-topic',
                        topic: 'Test Topic',
                        status: 'concluded' as const,
                        current_phase: 4,
                        next_action: 'Final verdict recorded',
                        verdict,
                        hypothesis_count: 1,
                        validated_count: 1,
                        created_at: new Date().toISOString(),
                        updated_at: new Date().toISOString(),
                    });
                }).not.toThrow();
            });
        });

        it('should validate hypothesis schema', () => {
            const validHyp = {
                id: 'hyp-001',
                dwi_id: 'dwi-test-topic',
                statement: 'If we implement caching then query latency will decrease because...',
                status: 'open' as const,
                phase: 1,
                created_at: new Date().toISOString(),
                closed_at: null,
            };

            expect(() => {
                DwiHypothesisSchema.parse(validHyp);
            }).not.toThrow();
        });

        it('should reject phase gate with invalid hypothesis', () => {
            expect(() => {
                DwiHypothesisSchema.parse({
                    id: 'hyp-001',
                    dwi_id: 'dwi-test-topic',
                    statement: 'Short',  // too short
                    status: 'open',
                    phase: 1,
                    created_at: new Date().toISOString(),
                    closed_at: null,
                });
            }).toThrow();
        });
    });

    // =========================================================================
    // Index Tests
    // =========================================================================

    describe('Query Performance Indexes', () => {
        it('should have index on discovery_work_items(status, current_phase)', () => {
            const indexes = db.prepare(`PRAGMA index_list(discovery_work_items)`).all() as any[];
            const statusPhaseIdx = indexes.find(
                (idx) => idx.name === 'idx_discovery_work_items_status_phase'
            );
            expect(statusPhaseIdx).toBeDefined();
        });

        it('should have index on discovery_work_items(topic)', () => {
            const indexes = db.prepare(`PRAGMA index_list(discovery_work_items)`).all() as any[];
            const topicIdx = indexes.find((idx) => idx.name === 'idx_discovery_work_items_topic');
            expect(topicIdx).toBeDefined();
        });

        it('should have index on dwi_phase_gates(dwi_id, phase)', () => {
            const indexes = db.prepare(`PRAGMA index_list(dwi_phase_gates)`).all() as any[];
            const phaseGateIdx = indexes.find(
                (idx) => idx.name === 'idx_dwi_phase_gates_dwi_id'
            );
            expect(phaseGateIdx).toBeDefined();
        });

        it('should have index on dwi_hypotheses(dwi_id, status)', () => {
            const indexes = db.prepare(`PRAGMA index_list(dwi_hypotheses)`).all() as any[];
            const hypothesisIdx = indexes.find(
                (idx) => idx.name === 'idx_dwi_hypotheses_dwi_id_status'
            );
            expect(hypothesisIdx).toBeDefined();
        });
    });
});
