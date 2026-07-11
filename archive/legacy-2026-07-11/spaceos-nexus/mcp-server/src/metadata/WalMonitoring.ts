/**
 * WalMonitoring — WAL Health Check & Diagnostics
 *
 * Provides health checks and diagnostics for WAL mode operation.
 * Alerts on potential issues (excessive WAL growth, long checkpoints).
 */

import Database from 'better-sqlite3';
import { WalOptimizer } from './WalOptimizer';

export interface WalHealthStatus {
  healthy: boolean;
  walSizeMb: number;
  warningThresholdMb: number;
  warning?: string;
  lastCheckpoint?: {
    duration: number;
    busy: number;
    checkpoint: number;
    backfill: number;
  };
}

export class WalMonitoring {
  private readonly db: Database.Database;
  private readonly optimizer: WalOptimizer;
  private readonly warningThresholdMb: number;

  constructor(db: Database.Database, optimizer: WalOptimizer, warningThresholdMb: number = 100) {
    this.db = db;
    this.optimizer = optimizer;
    this.warningThresholdMb = warningThresholdMb;
  }

  /**
   * Check WAL health and return diagnostics.
   *
   * Returns:
   * - healthy: true if WAL size < warningThreshold
   * - walSizeMb: Current approximate WAL file size in MB
   * - warning: Optional alert if WAL is growing excessively
   */
  public checkWalHealth(): WalHealthStatus {
    const walSizeBytes = this.optimizer.getWalFileSize();
    const walSizeMb = walSizeBytes / (1024 * 1024);

    let warning: string | undefined;
    let healthy = true;

    if (walSizeMb > this.warningThresholdMb) {
      healthy = false;
      warning = `WAL file size (${walSizeMb.toFixed(2)}MB) exceeds threshold (${this.warningThresholdMb}MB)`;
      console.warn(`[WalMonitoring] ⚠️ ${warning}`);
    }

    return {
      healthy,
      walSizeMb: parseFloat(walSizeMb.toFixed(2)),
      warningThresholdMb: this.warningThresholdMb,
      warning,
    };
  }

  /**
   * Get comprehensive WAL diagnostics for logging/monitoring.
   */
  public getDiagnostics(): {
    config: ReturnType<WalOptimizer['getWalConfig']>;
    health: WalHealthStatus;
  } {
    return {
      config: this.optimizer.getWalConfig(),
      health: this.checkWalHealth(),
    };
  }

  /**
   * Log WAL health status (for observability).
   */
  public logHealth(): void {
    const health = this.checkWalHealth();
    const config = this.optimizer.getWalConfig();

    console.info('[WalMonitoring] WAL Status:', {
      walSizeMb: health.walSizeMb,
      healthy: health.healthy,
      autocheckpoint: config.autocheckpoint,
      journalSizeLimit: config.journalSizeLimit / (1024 * 1024),
      busyTimeout: config.busyTimeout,
    });

    if (health.warning) {
      console.warn('[WalMonitoring] ⚠️ ' + health.warning);
    }
  }
}
