/**
 * AgentSessionBootstrap.ts — Session startup/shutdown schema version tracking
 *
 * TASK-09-04B Integration Point:
 * - Load schema version on agent session start
 * - Check version at session end
 * - Log warning if schema was updated during session (indicates seeder ran concurrently)
 *
 * **Usage (in agent session lifecycle):**
 * ```typescript
 * const bootstrap = new AgentSessionBootstrap(schemaVersionManager);
 * const initialCtx = await bootstrap.onSessionStart();  // Load context + version
 * // ... agent work ...
 * await bootstrap.onSessionEnd();  // Check for version changes, warn if detected
 * ```
 */

import { SchemaVersionManager } from '../mcp/SchemaVersionManager';

export interface SessionContext {
    readLayerVersion: number;
    writeLayerVersion: number;
    sessionStartTime: string;
}

export class AgentSessionBootstrap {
    private sessionContext: SessionContext | null = null;

    constructor(private schemaVersionManager: SchemaVersionManager) { }

    /**
     * Called at agent session start.
     * Load initial schema versions.
     */
    public onSessionStart(): SessionContext {
        const readVersion = this.schemaVersionManager.getReadLayerVersion();
        const writeVersion = this.schemaVersionManager.getWriteLayerVersion();

        this.sessionContext = {
            readLayerVersion: readVersion,
            writeLayerVersion: writeVersion,
            sessionStartTime: new Date().toISOString(),
        };

        console.info('[AgentSessionBootstrap] 🚀 Session started');
        console.info(`  - Read-layer version: ${readVersion}`);
        console.info(`  - Write-layer version: ${writeVersion}`);

        return this.sessionContext;
    }

    /**
     * Called at agent session end.
     * Check for schema version changes (indicates concurrent seeder updates).
     */
    public onSessionEnd(): void {
        if (!this.sessionContext) {
            console.warn('[AgentSessionBootstrap] ⚠️  Session end called before start');
            return;
        }

        const currentReadVersion = this.schemaVersionManager.getReadLayerVersion();
        const currentWriteVersion = this.schemaVersionManager.getWriteLayerVersion();

        console.info('[AgentSessionBootstrap] 🏁 Session ending');

        // Detect read-layer changes
        if (currentReadVersion > this.sessionContext.readLayerVersion) {
            console.warn(
                `[AgentSessionBootstrap] ⚠️  Read-layer schema was updated during session: ` +
                `v${this.sessionContext.readLayerVersion} → v${currentReadVersion}`
            );
            console.warn('  📝 ACTION: Reload context data from database');
        }

        // Detect write-layer changes
        if (currentWriteVersion > this.sessionContext.writeLayerVersion) {
            console.warn(
                `[AgentSessionBootstrap] ⚠️  Write-layer schema was updated during session: ` +
                `v${this.sessionContext.writeLayerVersion} → v${currentWriteVersion}`
            );
            console.warn('  📝 ACTION: Reload workflow definitions');
        }

        if (
            currentReadVersion === this.sessionContext.readLayerVersion &&
            currentWriteVersion === this.sessionContext.writeLayerVersion
        ) {
            console.info('[AgentSessionBootstrap] ✓ No schema changes during session');
        }
    }

    /**
     * Get current session context.
     */
    public getSessionContext(): SessionContext | null {
        return this.sessionContext;
    }
}
