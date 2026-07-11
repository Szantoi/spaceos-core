/**
 * WalOptimizer — SQLite WAL Mode Configuration & Checkpoint Management
 *
 * WAL (Write-Ahead Logging) improves concurrency but requires careful configuration:
 * - Readers can run during writes (unlike rollback journal mode)
 * - Checkpoint converts WAL → main DB file (can temporarily block everyone)
 * - Pragmas control checkpoint frequency + aggressiveness
 *
 * Reference: https://www.sqlite.org/wal.html
 */

import Database from 'better-sqlite3';

export interface CheckpointResult {
  busy: number;       // Number of busy readers (checkpoint incomplete if > 0)
  log: number;        // Pages in WAL file at start
  checkpointed: number; // Pages actually checkpointed
}

export class WalOptimizer {
  private readonly db: Database.Database;

  constructor(db: Database.Database) {
    this.db = db;
  }

  /**
   * Configure WAL pragmas for optimal concurrency.
   * Call once at startup (before any operations).
   *
   * Configuration rationale:
   * - wal_autocheckpoint: Fire checkpoint after N pages written
   *   Target: ~4MB (1000 pages × 4KB/page)
   *   Prevents WAL from growing unbounded
   *
   * - journal_size_limit: Hard limit on WAL file size
   *   Target: 50MB
   *   If exceeded, triggers RESTART checkpoint (more aggressive)
   *
   * - busy_timeout: Max wait time for locked database
   *   Target: 5000ms
   *   Prevents infinite waits if checkpoint is stuck
   *
   * - synchronous: Disk sync mode (NORMAL is safe with WAL)
   *   WAL guarantees atomicity; NORMAL skips some syncs
   *   Performance: FAR faster than FULL mode
   */
  public configureWalPragmas(): void {
    try {
      // Fire PASSIVE checkpoint after 1000 pages written (~4MB)
      this.db.pragma('wal_autocheckpoint = 1000');
      console.info('[WalOptimizer] ✓ wal_autocheckpoint = 1000');

      // Limit WAL file to 50MB
      const limitBytes = 50 * 1024 * 1024; // 50MB
      this.db.pragma(`journal_size_limit = ${limitBytes}`);
      console.info(`[WalOptimizer] ✓ journal_size_limit = ${limitBytes} bytes`);

      // 5-second timeout on locked database
      this.db.pragma('busy_timeout = 5000');
      console.info('[WalOptimizer] ✓ busy_timeout = 5000ms');

      // Synchronous mode: NORMAL (safe with WAL, faster than FULL)
      this.db.pragma('synchronous = NORMAL');
      console.info('[WalOptimizer] ✓ synchronous = NORMAL');

      console.info('[WalOptimizer] ✓ WAL pragmas configured for optimal concurrency');
    } catch (error) {
      console.error('[WalOptimizer] ❌ Failed to configure WAL pragmas:', error);
      throw error;
    }
  }

  /**
   * Force a FULL checkpoint (blocking until complete).
   *
   * Checkpoint modes:
   * - PASSIVE: Does work without interfering (may not complete)
   * - FULL: Blocks readers/writers until complete (⚠️ use carefully)
   * - RESTART: Like FULL, but also resets WAL file to zero
   *
   * Call after seeder bulk inserts to ensure agents see fresh data.
   * WARNING: FULL checkpoint blocks everyone; keep under 500ms.
   */
  public forceCheckpoint(): CheckpointResult {
    const startTime = Date.now();
    try {
      const result = this.db.pragma('wal_checkpoint(FULL)') as unknown;

      if (!Array.isArray(result) || result.length === 0) {
        throw new Error('Unexpected checkpoint result format');
      }

      const checkpoint = result[0] as CheckpointResult;
      const duration = Date.now() - startTime;

      console.info(
        `[WalOptimizer] ✓ Checkpoint complete (${duration}ms): ` +
          `busy=${checkpoint.busy}, log=${checkpoint.log}, checkpointed=${checkpoint.checkpointed}`
      );

      return checkpoint;
    } catch (error) {
      console.error('[WalOptimizer] ❌ Checkpoint failed:', error);
      throw error;
    }
  }

  /**
   * Retrieve current WAL file size (diagnostic).
   * Approximates size from wal_checkpoint pragma output.
   */
  public getWalFileSize(): number {
    try {
      const result = this.db.pragma('wal_checkpoint(PASSIVE)') as unknown;

      if (!Array.isArray(result) || result.length === 0) {
        return 0;
      }

      const checkpoint = result[0] as CheckpointResult;
      // Each page is ~4KB
      // 'log' = pages in WAL, so WAL size ≈ log * 4KB
      return checkpoint.log * 4096;
    } catch {
      return 0;
    }
  }

  /**
   * Get detailed WAL pragma values for diagnostics.
   */
  public getWalConfig(): {
    autocheckpoint: number;
    journalSizeLimit: number;
    busyTimeout: number;
    synchronous: string;
  } {
    return {
      autocheckpoint: (this.db.pragma('wal_autocheckpoint', { simple: true }) as number) || 0,
      journalSizeLimit: (this.db.pragma('journal_size_limit', { simple: true }) as number) || 0,
      busyTimeout: (this.db.pragma('busy_timeout', { simple: true }) as number) || 0,
      synchronous: String(this.db.pragma('synchronous', { simple: true }) || 'UNKNOWN'),
    };
  }
}
