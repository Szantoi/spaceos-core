/**
 * Enum representing the current status of a plugin.
 */
export enum PluginStatus {
    NOT_LOADED = "NOT_LOADED",
    LOADING = "LOADING",
    LOADED = "LOADED",
    FAILED = "FAILED",
    UNLOADING = "UNLOADING"
}

import { AgentDb } from '../mcp/AgentDb';
import { SessionManager } from '../mcp/SessionManager';
import { RbacFilter } from '../mcp/RbacFilter';
import { WorkflowStateTracker } from '../metadata/WorkflowStateTracker';

import { GuardrailService } from '../roles/GuardrailService';

/**
 * System context provided to plugins.
 */
export interface PluginManagerLike {
    invokeTool(toolName: string, args: any, context: any): Promise<any>;
}

export interface SystemContext {
    agentDb: AgentDb;
    sessionManager: SessionManager;
    rbacFilter: RbacFilter;
    workflowTracker: WorkflowStateTracker;
    guardrailService: GuardrailService;
    pluginManager?: PluginManagerLike;
}

/**
 * Interface for plugin manifest data.
 */
export interface PluginManifest {
    id?: string;
    name: string;
    version: string;
    entry: string;
    className: string;
    dependencies?: string[];
    critical?: boolean;
}

/**
 * Error thrown when a circular dependency is detected between plugins.
 */
export class CircularDependencyError extends Error {
    constructor(public readonly cycle: string[]) {
        super(`Circular dependency detected: ${cycle.join(" -> ")}`);
        this.name = "CircularDependencyError";
    }
}

/**
 * Error thrown when a required dependency is not found.
 */
export class DependencyNotFoundError extends Error {
    constructor(public readonly pluginName: string, public readonly dependencyName: string) {
        super(`Dependency "${dependencyName}" not found for plugin "${pluginName}"`);
        this.name = "DependencyNotFoundError";
    }
}
