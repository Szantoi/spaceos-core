/**
 * WriteLayerInitializer.ts
 *
 * Initializes EPIC-08 write-layer schema in SQLite database.
 *
 * Responsible for:
 *   1. Loading migration SQL from 002_write_layer_schema.sql
 *   2. Executing DDL to create tables (idempotent)
 *   3. Setting PRAGMA configuration for consistency + concurrency
 *   4. Error handling + logging
 *   5. Backward compatibility (existing DB update path)
 *
 * Used by: WorkflowStateTracker.init() → agent-system startup
 */

import * as fs from 'fs';
import * as path from 'path';

// @ts-ignore
const Database: any = require('better-sqlite3');

export class WriteLayerInitializer {
    private db: any;

    constructor(db: any) {
        this.db = db;
    }

    /**
     * Initialize write-layer schema.
     *
     * Runs idempotently:
     *   - First call: Creates tables, indexes, constraints
     *   - Subsequent calls: No-op (tables already exist)
     *
     * Must be called during WorkflowStateTracker initialization.
     */
    public init(): void {
        try {
            // Step 1: Configure PRAGMA settings for reliability + concurrency
            this.configurePragma();

            // Step 2: Load and execute migration SQL
            this.executeMigration();

            // Step 3: Verify tables created successfully
            this.verifySchema();

            console.log('[WriteLayerInitializer] ✅ Write-layer schema initialized successfully');
        } catch (error) {
            console.error('[WriteLayerInitializer] ❌ Failed to initialize schema:', error);
            throw error;
        }
    }

    /**
     * Configure PRAGMA settings for SQLite reliability and concurrency.
     *
     * Settings:
     *   - journal_mode = WAL: Write-Ahead Logging (multiple concurrent reads)
     *   - synchronous = NORMAL: Balance safety + performance
     *   - foreign_keys = ON: Enforce referential integrity
     *   - locking_mode = NORMAL: Allow multiple writers
     *   - busy_timeout = 5000ms: Wait 5s before timeout on lock
     */
    private configurePragma(): void {
        try {
            this.db.pragma('journal_mode = WAL');
            this.db.pragma('synchronous = NORMAL');
            this.db.pragma('foreign_keys = ON');
            this.db.pragma('locking_mode = NORMAL');
            this.db.pragma('busy_timeout = 5000');

            console.log('[WriteLayerInitializer] PRAGMA configured for WAL mode + consistency');
        } catch (error) {
            console.error('[WriteLayerInitializer] Failed to configure PRAGMA:', error);
            throw error;
        }
    }

    /**
     * Load and execute migration SQL from 002_write_layer_schema.sql.
     *
     * Migration file location: src/metadata/migrations/002_write_layer_schema.sql
     *
     * Idempotent: Uses IF NOT EXISTS clauses, so safe to run multiple times.
     */
    private executeMigration(): void {
        try {
            const migrationPath = path.join(__dirname, 'migrations', '002_write_layer_schema.sql');

            if (!fs.existsSync(migrationPath)) {
                throw new Error(`Migration file not found: ${migrationPath}`);
            }

            const migrationSql = fs.readFileSync(migrationPath, 'utf-8');

            // Execute migration SQL
            // Note: better-sqlite3 exec() runs multiple statements
            this.db.exec(migrationSql);

            console.log('[WriteLayerInitializer] Migration SQL executed');
        } catch (error) {
            console.error('[WriteLayerInitializer] Failed to execute migration:', error);
            throw error;
        }
    }

    /**
     * Verify all write-layer tables created successfully.
     *
     * Checks:
     *   - sessions table exists
     *   - artifacts table exists
     *   - workflow_events table exists
     *   - checkpoints table exists
     */
    private verifySchema(): void {
        try {
            const requiredTables = ['sessions', 'artifacts', 'workflow_events', 'checkpoints'];

            const tables = this.db
                .prepare("SELECT name FROM sqlite_master WHERE type='table' AND name IN (?, ?, ?, ?)")
                .all(...requiredTables) as Array<{ name: string }>;

            const tableNames = tables.map(t => t.name);
            const missingTables = requiredTables.filter(t => !tableNames.includes(t));

            if (missingTables.length > 0) {
                throw new Error(`Missing tables: ${missingTables.join(', ')}`);
            }

            console.log('[WriteLayerInitializer] ✓ All required tables exist');
        } catch (error) {
            console.error('[WriteLayerInitializer] Schema verification failed:', error);
            throw error;
        }
    }

    /**
     * Get schema information for debugging.
     *
     * Returns:
     *   - Table list
     *   - Column info per table
     *   - Index list
     */
    public getSchemaInfo(): Record<string, any> {
        try {
            const tables = this.db
                .prepare("SELECT name FROM sqlite_master WHERE type='table'")
                .all() as Array<{ name: string }>;

            const tableInfo: Record<string, any> = {};

            for (const table of tables) {
                const columns = this.db
                    .prepare(`PRAGMA table_info(${table.name})`)
                    .all() as Array<{ name: string; type: string; notnull: number; default_value: any; pk: number }>;

                tableInfo[table.name] = {
                    columns: columns.map(c => ({
                        name: c.name,
                        type: c.type,
                        required: c.notnull === 1,
                        primaryKey: c.pk === 1,
                        default: c.default_value,
                    })),
                };
            }

            return tableInfo;
        } catch (error) {
            console.error('[WriteLayerInitializer] Failed to get schema info:', error);
            throw error;
        }
    }
}

export default WriteLayerInitializer;
