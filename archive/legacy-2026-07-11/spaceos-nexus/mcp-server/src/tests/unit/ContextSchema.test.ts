/**
 * ContextSchema.test.ts — Unit tests for EPIC-09 context layer
 *
 * Test matrix:
 * - Schema initialization (idempotent, all 6 tables)
 * - Foreign key enforcement (role-based cascade)
 * - UNIQUE constraints (domain+role_name composites)
 * - Default values (timestamps, versions)
 * - Type safety (no nulls where not allowed)
 *
 * Coverage target: 85%+ for context layer
 * @vitest
 */

import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import Database from 'better-sqlite3';
import { ContextSchemaInitializer } from '../../metadata/ContextSchemaInitializer';

describe('EPIC-09: ContextSchema', () => {
    let db: Database.Database;
    let initializer: ContextSchemaInitializer;

    beforeEach(() => {
        db = new Database(':memory:');
        initializer = new ContextSchemaInitializer(db);
    });

    afterEach(() => {
        db.close();
    });

    // ==========================================================================
    // SCHEMA INITIALIZATION TESTS
    // ==========================================================================

    describe('Schema Initialization', () => {
        it('should create all 6 context tables', async () => {
            await initializer.initialize();

            const tables = db
                .prepare(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
                )
                .all() as Array<{ name: string }>;

            const tableNames = tables.map(t => t.name).sort();
            const expectedTables = [
                'role_schemas',
                'roles',
                'runbooks',
                'standards',
                'templates',
                'workflows',
            ].sort();

            expect(tableNames).toEqual(expectedTables);
        });

        it('should be idempotent (run twice without error)', async () => {
            await initializer.initialize();
            await initializer.initialize(); // Should not error

            const result = db
                .prepare(
                    "SELECT COUNT(*) as count FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
                )
                .get() as { count: number };

            expect(result.count).toBe(6); // Exactly 6 tables, not 12 duplicates
        });

        it('should create composite indexes on (domain, role_name)', async () => {
            await initializer.initialize();

            const indexes = db
                .prepare(
                    "SELECT name, tbl_name FROM sqlite_master WHERE type='index' AND name LIKE 'idx_%' ORDER BY name"
                )
                .all() as Array<{ name: string; tbl_name: string }>;

            // Should have indexes on main role-keyed tables
            const tableIndexMap = new Map<string, string[]>();
            indexes.forEach(idx => {
                let table = idx.tbl_name;
                if (!tableIndexMap.has(table)) {
                    tableIndexMap.set(table, []);
                }
                tableIndexMap.get(table)!.push(idx.name);
            });

            expect(tableIndexMap.get('roles')).toBeDefined();
            expect(tableIndexMap.get('role_schemas')).toBeDefined();
            expect(tableIndexMap.get('runbooks')).toBeDefined();
        });

        it('should enable foreign key support', async () => {
            await initializer.initialize();
            const fkEnabled = initializer.checkForeignKeyConstraints();
            expect(fkEnabled).toBe(true);
        });
    });

    // ==========================================================================
    // ROLES TABLE TESTS
    // ==========================================================================

    describe('Roles Table', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should insert role record with required fields', () => {
            const stmt = db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            );
            stmt.run('engineering', 'backend_dev', 'Backend Developer role');

            const role = db
                .prepare('SELECT * FROM roles WHERE domain = ? AND role_name = ?')
                .get('engineering', 'backend_dev') as Record<string, unknown> | undefined;

            expect(role).toBeDefined();
            expect(role?.domain).toBe('engineering');
            expect(role?.role_name).toBe('backend_dev');
        });

        it('should enforce UNIQUE(domain, role_name)', () => {
            const stmt = db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            );
            stmt.run('engineering', 'backend_dev', 'Content 1');

            expect(() => {
                stmt.run('engineering', 'backend_dev', 'Content 2');
            }).toThrow(); // Should fail on duplicate
        });

        it('should allow same role_name in different domains', () => {
            const stmt = db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            );
            stmt.run('engineering', 'architect', 'Engineering Architect');
            stmt.run('management', 'architect', 'Management Architect');

            const roles = db
                .prepare('SELECT COUNT(*) as count FROM roles WHERE role_name = ?')
                .get('architect') as { count: number };

            expect(roles.count).toBe(2);
        });

        it('should set created_at and last_updated timestamps', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'content');

            const role = db
                .prepare('SELECT created_at, last_updated FROM roles LIMIT 1')
                .get() as { created_at: string; last_updated: string } | undefined;

            expect(role?.created_at).toBeTruthy();
            expect(role?.last_updated).toBeTruthy();
        });

        it('should set default version to 1.0', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'content');

            const role = db
                .prepare('SELECT version FROM roles LIMIT 1')
                .get() as { version: string } | undefined;

            expect(role?.version).toBe('1.0');
        });
    });

    // ==========================================================================
    // ROLE_SCHEMAS TABLE TESTS
    // ==========================================================================

    describe('Role Schemas Table', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should insert role_schema with mcp_tool_permissions', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'test');

            db.prepare(
                'INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', JSON.stringify(['submit_artifact', 'update_workflow']));

            const schema = db
                .prepare(
                    'SELECT * FROM role_schemas WHERE domain = ? AND role_name = ?'
                )
                .get('engineering', 'backend_dev') as Record<string, unknown> | undefined;

            expect(schema).toBeDefined();
            expect(schema?.mcp_tool_permissions).toBe(
                JSON.stringify(['submit_artifact', 'update_workflow'])
            );
        });

        it('should enforce FK (domain, role_name) -> roles', () => {
            // Try to insert FK without parent
            expect(() => {
                db.prepare(
                    'INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)'
                ).run('nonexistent', 'nonexistent', JSON.stringify([]));
            }).toThrow(); // FK violation
        });

        it('should allow nullable persona fields', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'test');

            db.prepare(
                'INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', JSON.stringify([]));

            const schema = db
                .prepare('SELECT persona_identity, persona_style FROM role_schemas LIMIT 1')
                .get() as Record<string, unknown> | undefined;

            expect(schema?.persona_identity).toBeNull();
            expect(schema?.persona_style).toBeNull();
        });
    });

    // ==========================================================================
    // WORKFLOWS TABLE TESTS
    // ==========================================================================

    describe('Workflows Table', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should support multiple workflows per role (discovery, delivery, etc)', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'test');

            const stmt = db.prepare(
                'INSERT INTO workflows (domain, role_name, workflow_type, content) VALUES (?, ?, ?, ?)'
            );

            stmt.run('engineering', 'backend_dev', 'discovery', 'Discovery workflow...');
            stmt.run('engineering', 'backend_dev', 'delivery', 'Delivery workflow...');
            stmt.run('engineering', 'backend_dev', 'ideation', 'Ideation workflow...');

            const workflows = db
                .prepare(
                    'SELECT COUNT(*) as count FROM workflows WHERE domain = ? AND role_name = ?'
                )
                .get('engineering', 'backend_dev') as { count: number };

            expect(workflows.count).toBe(3);
        });

        it('should enforce UNIQUE(domain, role_name, workflow_type)', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'test');

            const stmt = db.prepare(
                'INSERT INTO workflows (domain, role_name, workflow_type, content) VALUES (?, ?, ?, ?)'
            );

            stmt.run('engineering', 'backend_dev', 'discovery', 'Workflow 1');

            expect(() => {
                stmt.run('engineering', 'backend_dev', 'discovery', 'Workflow 2');
            }).toThrow(); // Duplicate
        });
    });

    // ==========================================================================
    // STANDARDS TABLE TESTS (role-independent)
    // ==========================================================================

    describe('Standards Table', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should insert global standards independent of roles', () => {
            db.prepare(
                'INSERT INTO standards (std_id, content) VALUES (?, ?)'
            ).run('03-agent-system/standards/adr-001', 'Content...');

            const standard = db
                .prepare('SELECT * FROM standards WHERE std_id = ?')
                .get('03-agent-system/standards/adr-001') as Record<string, unknown> | undefined;

            expect(standard).toBeDefined();
        });

        it('should enforce UNIQUE on std_id', () => {
            const stmt = db.prepare(
                'INSERT INTO standards (std_id, content) VALUES (?, ?)'
            );

            stmt.run('03-agent-system/standards/adr-001', 'Content 1');

            expect(() => {
                stmt.run('03-agent-system/standards/adr-001', 'Content 2');
            }).toThrow();
        });
    });

    // ==========================================================================
    // CASCADE DELETE TESTS
    // ==========================================================================

    describe('Cascade Deletes', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should have FK constraint with ON DELETE CASCADE configured', async () => {
            // Verify that FK is set up with CASCADE
            const schema = initializer.getSchemaInfo();
            const roleSchemaIndex = schema.tables.findIndex(t => t.name === 'role_schemas');

            expect(roleSchemaIndex).toBeGreaterThanOrEqual(0);
            expect(schema.tables[roleSchemaIndex].sql).toContain('ON DELETE CASCADE');
        });
    });

    // ==========================================================================
    // SCHEMA INFO RETRIEVAL TESTS
    // ==========================================================================

    describe('Schema Info Methods', () => {
        beforeEach(async () => {
            await initializer.initialize();
        });

        it('should retrieve schema information', () => {
            const info = initializer.getSchemaInfo();

            expect(info.table_count).toBe(6);
            expect(info.tables.length).toBe(6);
            expect(info.indexes.length).toBeGreaterThan(0);
        });

        it('should retrieve table statistics', () => {
            db.prepare(
                'INSERT INTO roles (domain, role_name, content) VALUES (?, ?, ?)'
            ).run('engineering', 'backend_dev', 'test');

            const stats = initializer.getTableStats();

            expect(stats.roles.row_count).toBe(1);
            expect(stats.roles.column_count).toBeGreaterThan(0);
            expect(stats.workflows.row_count).toBe(0);
        });
    });
});
