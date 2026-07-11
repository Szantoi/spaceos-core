/**
 * ContextSchemaInitializer — EPIC-09 schema migration runner
 *
 * Responsible for:
 * 1. Loading SQL DDL from migration file
 * 2. Executing migration (idempotent)
 * 3. Verifying schema integrity
 * 4. Reporting schema information
 *
 * @fileoverview Schema initialization for EPIC-09 context layer
 */

import type { Database as DatabaseType } from 'better-sqlite3';
import { readFileSync } from 'fs';
import { join } from 'path';
import { SchemaValidationError } from './ContextSchema';

export class ContextSchemaInitializer {
  private static readonly EXPECTED_TABLES = [
    'roles',
    'role_schemas',
    'runbooks',
    'workflows',
    'templates',
    'standards',
  ];

  private static readonly EXPECTED_TABLE_COUNT = this.EXPECTED_TABLES.length;

  constructor(private db: DatabaseType) {
    // Enable foreign keys for this connection
    this.db.pragma('foreign_keys = ON');
  }

  /**
   * Initialize the EPIC-09 context schema
   * Safe to call multiple times (idempotent DDL)
   *
   * @throws {ContextSchemaError} if initialization fails
   */
  async initialize(): Promise<void> {
    try {
      console.info('[ContextSchemaInitializer] Initializing EPIC-09 context schema...');

      const migrationPath = join(__dirname, 'migrations', '003_epic09_context_schema.sql');
      const migrationSql = readFileSync(migrationPath, 'utf-8');

      // Execute migration (idempotent: CREATE TABLE IF NOT EXISTS)
      this.db.exec(migrationSql);

      // Verify schema was created correctly
      await this.verifySchema();

      console.info('[ContextSchemaInitializer] ✅ Context schema initialized successfully');
    } catch (error) {
      console.error('[ContextSchemaInitializer] ❌ Initialization failed:', error);
      throw error;
    }
  }

  /**
   * Verify all expected tables exist in the database
   *
   * @throws {SchemaValidationError} if any table is missing
   */
  private async verifySchema(): Promise<void> {
    console.debug(
      `[ContextSchemaInitializer] Verifying ${ContextSchemaInitializer.EXPECTED_TABLE_COUNT} tables exist...`
    );

    for (const table of ContextSchemaInitializer.EXPECTED_TABLES) {
      const result = this.db
        .prepare("SELECT 1 FROM sqlite_master WHERE type='table' AND name=?")
        .get(table) as Record<string, unknown> | undefined;

      if (!result) {
        throw new SchemaValidationError(
          table,
          `Table '${table}' not found after migration`
        );
      }
    }

    console.debug(
      `[ContextSchemaInitializer] ✅ All ${ContextSchemaInitializer.EXPECTED_TABLE_COUNT} tables verified`
    );
  }

  /**
   * Get schema information for debugging/inspection
   *
   * @returns Object containing tables and indexes
   */
  getSchemaInfo(): {
    tables: Array<{ name: string; sql: string | null }>;
    indexes: Array<{ name: string; tbl_name: string; sql: string | null }>;
    table_count: number;
    index_count: number;
  } {
    interface SchemaRow {
      name: string;
      sql: string | null;
    }

    interface IndexRow {
      name: string;
      tbl_name: string;
      sql: string | null;
    }

    const tables = this.db
      .prepare(
        "SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
      )
      .all() as SchemaRow[];

    const indexes = this.db
      .prepare(
        "SELECT name, tbl_name, sql FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%' ORDER BY tbl_name"
      )
      .all() as IndexRow[];

    return {
      tables,
      indexes,
      table_count: tables.length,
      index_count: indexes.length,
    };
  }

  /**
   * Get table-level statistics for monitoring
   */
  getTableStats(): Record<
    string,
    {
      row_count: number;
      column_count: number;
    }
  > {
    const stats: Record<
      string,
      {
        row_count: number;
        column_count: number;
      }
    > = {};

    for (const table of ContextSchemaInitializer.EXPECTED_TABLES) {
      try {
        const countResult = this.db
          .prepare(`SELECT COUNT(*) as count FROM ${table}`)
          .get() as { count: number };

        const infoResult = this.db
          .prepare(`PRAGMA table_info(${table})`)
          .all() as Array<{ name: string }>;

        stats[table] = {
          row_count: countResult.count,
          column_count: infoResult.length,
        };
      } catch (error) {
        console.warn(`[ContextSchemaInitializer] Failed to get stats for ${table}:`, error);
        stats[table] = { row_count: 0, column_count: 0 };
      }
    }

    return stats;
  }

  /**
   * Check if foreign key constraints are enabled
   */
  checkForeignKeyConstraints(): boolean {
    const result = this.db.pragma('foreign_keys') as Array<{ foreign_keys: number }>;
    return Array.isArray(result) && result.length > 0 && result[0].foreign_keys === 1;
  }
}
