import { DatabaseConnectionManager } from './DatabaseConnectionManager';
import { createHash } from 'crypto';

/**
 * AuditLogger Service — Responsible for persisting tool call metadata.
 * Implements non-blocking, asynchronous logging for performance.
 */
export class AuditLogger {
    private connectionManager: DatabaseConnectionManager;

    constructor(connectionManager: DatabaseConnectionManager) {
        this.connectionManager = connectionManager;
    }

    /**
     * Log a tool call execution with optional cost tracking (TASK-18-09).
     */
    public async log(params: {
        session_id: string;
        domain: string;
        role: string;
        tool_name: string;
        input: any;
        output: any;
        latency_ms: number;
        status_code: string;
        ai_model?: string | null;
        ai_tokens_used?: number | null;
        cost_amount_usd?: number | null;
    }): Promise<void> {
        // Process in background to avoid blocking the main tool execution flow
        setImmediate(() => {
            try {
                const db = this.connectionManager.getAdminPool();
                const stmt = db.prepare(`
          INSERT INTO audit_log (
            session_id, domain, role, tool_name, input_hash, output_hash, latency_ms, status_code,
            ai_model, ai_tokens_used, cost_amount_usd
          ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);

                const inputHash = this.hash(params.input);
                const outputHash = this.hash(params.output);

                stmt.run(
                    params.session_id,
                    params.domain,
                    params.role,
                    params.tool_name,
                    inputHash,
                    outputHash,
                    params.latency_ms,
                    params.status_code,
                    params.ai_model ?? null,
                    params.ai_tokens_used ?? null,
                    params.cost_amount_usd ?? null
                );
            } catch (error) {
                console.error('[AuditLogger] ❌ Failed to write to audit_log:', error);
            }
        });
    }

    /**
     * Helper to create a stable hash of tool input/output.
     */
    private hash(data: any): string {
        if (!data) return '';
        try {
            const str = typeof data === 'string' ? data : JSON.stringify(data);
            return createHash('sha256').update(str).digest('hex');
        } catch {
            return 'hash_error';
        }
    }
}
