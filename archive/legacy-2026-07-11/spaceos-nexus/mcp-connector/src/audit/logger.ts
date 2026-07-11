/**
 * Audit Logger - Centralized tool call logging
 */

import fs from 'fs';
import path from 'path';
import { AuditEntry, AuditConfig } from '../types';

export class AuditLogger {
  private logFile: string;
  private logFormat: 'json' | 'text';
  private enabled: boolean;

  constructor(config: AuditConfig) {
    this.enabled = config.enabled;
    this.logFile = config.log_file;
    this.logFormat = config.log_format;

    // Ensure log directory exists
    if (this.enabled) {
      const logDir = path.dirname(this.logFile);
      if (!fs.existsSync(logDir)) {
        fs.mkdirSync(logDir, { recursive: true });
      }
    }
  }

  log(entry: Omit<AuditEntry, 'timestamp'>): void {
    if (!this.enabled) return;

    const timestamp = new Date().toISOString();
    const fullEntry: AuditEntry = { ...entry, timestamp };

    // Format log line
    let logLine: string;
    if (this.logFormat === 'json') {
      logLine = JSON.stringify(fullEntry) + '\n';
    } else {
      const statusIcon = this.getStatusIcon(entry.status);
      logLine = `[${timestamp}] ${entry.terminal} → ${entry.tool} → ${entry.backend} (${entry.latency}ms) ${statusIcon}${entry.error ? ' ' + entry.error : ''}\n`;
    }

    // Write to file (async, fire-and-forget)
    fs.appendFile(this.logFile, logLine, (err) => {
      if (err) {
        console.error('[AuditLogger] Failed to write to log file:', err.message);
      }
    });

    // Console log (colored)
    this.consoleLog(fullEntry);
  }

  private getStatusIcon(status: AuditEntry['status']): string {
    switch (status) {
      case 'success':
        return '✅';
      case 'error':
        return '❌';
      case 'permission_denied':
        return '🚫';
      default:
        return '❓';
    }
  }

  private consoleLog(entry: AuditEntry): void {
    const icon = this.getStatusIcon(entry.status);
    const errorSuffix = entry.error ? ` (${entry.error})` : '';
    console.log(`[Audit] ${entry.terminal} → ${entry.tool} → ${entry.backend} (${entry.latency}ms) ${icon}${errorSuffix}`);
  }

  /**
   * Query recent log entries (for debugging)
   */
  async getRecentEntries(count: number = 100): Promise<AuditEntry[]> {
    if (!this.enabled || !fs.existsSync(this.logFile)) {
      return [];
    }

    try {
      const content = fs.readFileSync(this.logFile, 'utf8');
      const lines = content.trim().split('\n').slice(-count);

      if (this.logFormat === 'json') {
        return lines.map((line) => JSON.parse(line) as AuditEntry);
      }

      // Text format parsing not implemented
      return [];
    } catch (error) {
      console.error('[AuditLogger] Failed to read log file:', error);
      return [];
    }
  }

  /**
   * Get summary statistics
   */
  getStats(): { totalCalls: number; byStatus: Record<string, number>; avgLatency: number } {
    // Basic in-memory stats (could be expanded with proper metrics)
    return {
      totalCalls: 0,
      byStatus: {},
      avgLatency: 0,
    };
  }
}
