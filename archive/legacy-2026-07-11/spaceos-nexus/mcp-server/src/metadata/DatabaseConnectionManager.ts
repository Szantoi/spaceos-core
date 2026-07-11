/**
 * DatabaseConnectionManager — Dual-Pool SQLite Connection Strategy with WAL Optimization
 *
 * **Security Model:**
 * - **Admin Pool**: Writable (schema init, seeding, admin operations)
 * - **Agent Pool**: Read-only (`PRAGMA query_only = ON`) — zero write risk
 *
 * **Performance Model (TASK-09-02B):**
 * - **WAL Mode**: Readers run during writes (better concurrency)
 * - **Checkpointing**: Auto-checkpoint every 1000 pages (~4MB)
 * - **Lock Timeout**: 5 seconds prevents infinite waits
 *
 * Enforces write/read separation at connection level, preventing accidental
 * data modification via agent code paths.
 *
 * @see TASK-09-01B: Security Hardening
 * @see TASK-09-02B: WAL Optimization
 */

import Database from 'better-sqlite3';
import { readFileSync } from 'fs';
import { join } from 'path';
import { WalOptimizer } from './WalOptimizer';
import { WalMonitoring } from './WalMonitoring';

export class DatabaseConnectionManager {
    private adminPool: Database.Database;
    private agentPool: Database.Database;
    private walOptimizer: WalOptimizer;
    private walMonitoring: WalMonitoring;
    private initialized: boolean = false;

    /**
     * Constructor: Initialize dual-pool strategy.
     *
     * @param dbPath Path to SQLite database
     * @throws Error if database connection fails
     */
    constructor(dbPath: string) {
        try {
            // ┌─ ADMIN POOL ─────────────────────────────────────────────────────┐
            // │ Writable connection for schema init, seeding, admin operations    │
            // └──────────────────────────────────────────────────────────────────┘

            this.adminPool = new Database(dbPath);

            // Configure for durability + concurrency
            try {
                this.adminPool.pragma('journal_mode = WAL');
                this.adminPool.pragma('synchronous = FULL');
            } catch {
                // WAL may fail on :memory: databases — continue
            }
            this.adminPool.pragma('foreign_keys = ON');

            // ┌─ AGENT POOL ─────────────────────────────────────────────────────┐
            // │ Read-only connection for queries (no write capability)            │
            // │ Both pools connect to same database (supports both WAL + RO)      │
            // └──────────────────────────────────────────────────────────────────┘

            this.agentPool = new Database(dbPath);

            // Enforce read-only at connection level
            // PRAGMA query_only prevents INSERT/UPDATE/DELETE/CREATE
            this.agentPool.pragma('query_only = ON');

            // Still allow schema reading
            this.agentPool.pragma('foreign_keys = ON');

            // ┌─ WAL OPTIMIZATION (TASK-09-02B) ──────────────────────────────────┐
            // │ Initialize WAL optimizer for concurrent access + checkpoint mgmt   │
            // └────────────────────────────────────────────────────────────────────┘
            this.walOptimizer = new WalOptimizer(this.adminPool);
            this.walMonitoring = new WalMonitoring(this.adminPool, this.walOptimizer, 100);

            // Note: WAL pragmas will be configured via initWalOptimization()
            // (called from AgentDb.initSchema() or separately as needed)

            console.info(
                '[DatabaseConnectionManager] ✅ Dual-pool initialized (admin=RW, agent=RO)'
            );
        } catch (error) {
            console.error(
                '[DatabaseConnectionManager] ❌ Initialization failed:',
                error instanceof Error ? error.message : String(error)
            );
            throw new Error(
                `Failed to initialize database connection manager: ${error instanceof Error ? error.message : String(error)
                }`
            );
        }
    }

    /**
     * Get admin pool (writable).
     *
     * **Use for:**
     * - Schema initialization (CREATE TABLE, CREATE INDEX)
     * - Seeding (INSERT, UPDATE, DELETE)
     * - Admin operations
     *
     * @returns Database connection (writable)
     */
    public getAdminPool(): Database.Database {
        return this.adminPool;
    }

    /**
     * Get agent pool (read-only).
     *
     * **Use for:**
     * - SELECT queries
     * - Role/schema/runbook/workflow/template/standard lookups
     * - Agent data retrieval
     *
     * **Note:** Writes will throw SQLITE_READONLY error.
     *
     * @returns Database connection (read-only)
     */
    public getAgentPool(): Database.Database {
        return this.agentPool;
    }

    /**
     * Check if write operations are blocked on agent pool.
     *
     * **Returns:** true if query_only is ON (write-protected)
     *
     * @returns boolean
     */
    public isAgentPoolReadOnly(): boolean {
        try {
            const result = this.agentPool.pragma('query_only', { simple: true });
            return result === 1;
        } catch (error) {
            console.warn(
                '[DatabaseConnectionManager.isAgentPoolReadOnly] ⚠️ Could not verify read-only status:',
                error instanceof Error ? error.message : String(error)
            );
            return false;
        }
    }

    /**
     * Check foreign key enforcement status.
     *
     * **Returns:** true if FK constraints are enforced
     *
     * @returns boolean
     */
    public areForeignKeysEnforced(): boolean {
        try {
            const result = this.adminPool.pragma('foreign_keys', { simple: true });
            return result === 1;
        } catch (error) {
            console.warn(
                '[DatabaseConnectionManager.areForeignKeysEnforced] ⚠️ Could not verify FK status:',
                error instanceof Error ? error.message : String(error)
            );
            return false;
        }
    }

    /**
     * Close both connection pools cleanly.
     *
     * **Call on:**
     * - Application shutdown
     * - SIGTERM/SIGINT
     * - Resource cleanup
     *
     * @throws Error if database close fails
     */
    public close(): void {
        try {
            this.adminPool.close();
            this.agentPool.close();
            console.info('[DatabaseConnectionManager] ✅ Connection pools closed');
        } catch (error) {
            console.error(
                '[DatabaseConnectionManager.close] ❌ Error closing pools:',
                error instanceof Error ? error.message : String(error)
            );
            throw new Error(
                `Failed to close connection pools: ${error instanceof Error ? error.message : String(error)
                }`
            );
        }
    }

    /**
     * Get connection pool diagnostics.
     *
     * **Use for:**
     * - Health checks
     * - Monitoring
     * - Debugging
     *
     * @returns Object with pool status
     */
    public getDiagnostics(): {
        adminPoolOpen: boolean;
        agentPoolReadOnly: boolean;
        foreignKeysEnforced: boolean;
        walMode: string;
    } {
        try {
            const walMode = this.adminPool.pragma('journal_mode', { simple: true });
            return {
                adminPoolOpen: true,
                agentPoolReadOnly: this.isAgentPoolReadOnly(),
                foreignKeysEnforced: this.areForeignKeysEnforced(),
                walMode: String(walMode),
            };
        } catch {
            return {
                adminPoolOpen: false,
                agentPoolReadOnly: false,
                foreignKeysEnforced: false,
                walMode: 'unknown',
            };
        }
    }

    /**
     * Initialize WAL optimization (TASK-09-02B).
     * Call after schema initialization to configure WAL pragmas.
     */
    public initWalOptimization(): void {
        this.walOptimizer.configureWalPragmas();
    }

    /**
     * Get WAL optimizer instance.
     * Use for checkpoint operations, WAL diagnostics.
     */
    public getWalOptimizer(): WalOptimizer {
        return this.walOptimizer;
    }

    /**
     * Get WAL monitoring instance.
     * Use for health checks, WAL diagnostics.
     */
    public getWalMonitoring(): WalMonitoring {
        return this.walMonitoring;
    }
}

export default DatabaseConnectionManager;
