import NodeCache from 'node-cache';
import { AgentDb } from './AgentDb';
import { WorkflowTrack } from '../metadata/FSMTypes';

/**
 * RBAC Filter for MCP Tool Access Control
 *
 * Controls which MCP tools are visible to a client based on the active role.
 * Reads permissions from SQLite role_schemas table via AgentDb.
 * Optimized with an in-memory LRU cache.
 *
 * @example
 *   const filter = new RbacFilter(agentDb);
 *   const allowed = filter.getAllowedTools('backend_developer'); // returns Set<string>
 */
export class RbacFilter {
    // Cache for role permissions (Set<string>)
    // LRU behavior: standard node-cache doesn't strictly do LRU,
    // but we'll use it with a limit and TTL as specified.
    private cache: NodeCache;
    private lastDbVersion: number = -1;

    // Tools accessible without any role (unauthenticated / unknown role)
    private readonly publicTools: Set<string> = new Set([
        'list_domains',
        'list_roles',
        'get_policy',
        'search_knowledge',
        'list_available_domains',
        'switch_domain',
        'get_project_state',
        'list_my_team_tasks',
        'get_task_context',
        'search_tasks',
    ]);

    constructor(private readonly agentDb: AgentDb) {
        this.cache = new NodeCache({
            stdTTL: 1800, // 30 minutes
            checkperiod: 600,
            useClones: false,
            maxKeys: 50 // Keep memory footprint small
        });
    }

    /**
     * Returns the set of tool names allowed for a given role.
     * Lazy-loads from AgentDb and caches the result.
     */
    getAllowedTools(role: string): Set<string> {
        // 0. Cache Invalidation Check (AC-13)
        this.checkVersionAndInvalidate();

        // 1. Check Cache
        const cached = this.cache.get<Set<string>>(role);
        if (cached) {
            return new Set([...this.publicTools, ...cached]);
        }

        // 2. Query AgentDb
        try {
            const schema = this.agentDb.findSchemaByRoleName(role);
            if (!schema || !schema.mcp_tool_permissions) {
                // Unknown or unpermissioned role → public tools only (empty role set)
                this.cache.set(role, new Set());
                return this.publicTools;
            }

            try {
                const permissions = JSON.parse(schema.mcp_tool_permissions);
                if (Array.isArray(permissions)) {
                    const toolSet = new Set<string>(permissions);
                    this.cache.set(role, toolSet);
                    return new Set([...this.publicTools, ...toolSet]);
                } else {
                    console.warn(`[RbacFilter] Schema permissions for role "${role}" is not an array (type: ${typeof permissions}). Falling back to public tools.`);
                }
            } catch (jsonErr) {
                console.error(`[RbacFilter] Failed to parse permissions for role "${role}". Raw data: ${schema.mcp_tool_permissions}`, jsonErr);
            }
        } catch (dbErr) {
            console.error(`[RbacFilter] Database error fetching permissions for role "${role}":`, dbErr);
        }

        // Fallback: Cache negative result (empty set) to avoid DB spam
        this.cache.set(role, new Set());
        return this.publicTools;
    }

    /**
     * Checks whether a specific tool is allowed for a given role.
     */
    /**
     * Checks whether a specific tool is allowed for a given role.
     * Optionally supply a track to enforce discovery/delivery segregation.
     * If track is provided and the tool has an associated track, they must match.
     */
    hasPermission(toolName: string, role: string, track?: WorkflowTrack | null): boolean {
        const allowed = this.getAllowedTools(role).has(toolName);
        if (!allowed) {
            return false;
        }
        if (track) {
            const toolTrack = this.getToolTrack(toolName);
            if (toolTrack && toolTrack !== track) {
                // explicit mismatch
                return false;
            }
        }
        return true;
    }

    /**
     * Simple heuristic to associate certain tools with a track.
     * Tools prefixed with "discovery_" are discovery-only, likewise for "delivery_".
     * Returns null for tools that are track-agnostic (public or generic).
     */
    private getToolTrack(toolName: string): 'discovery' | 'delivery' | null {
        if (toolName.startsWith('discovery_')) return 'discovery';
        if (toolName.startsWith('delivery_')) return 'delivery';
        // individual exceptions & non-prefixed discovery tools
        const discoveryTools = new Set([
            'reference_prior_discovery',
            'submit_discovery_outcome',
            'track_blocker',
            'query_blockers',
            'check_constraints',
            'get_phase_guidance'
        ]);
        if (discoveryTools.has(toolName)) return 'discovery';
        return null;
    }

    /**
     * Returns the list of public tool names (accessible without any role).
     */
    getPublicTools(): Set<string> {
        return this.publicTools;
    }

    /**
     * Checks if the DB schema version has changed and flushes cache if so (AC-13).
     */
    private checkVersionAndInvalidate(): void {
        try {
            const version = this.agentDb.getSchemaVersion('read-layer');
            if (this.lastDbVersion !== -1 && version !== this.lastDbVersion) {
                console.info(`[RbacFilter] Schema version bump detected (${this.lastDbVersion} -> ${version}). Invalidating cache.`);
                this.invalidateCache();
            }
            this.lastDbVersion = version;
        } catch (err) {
            console.warn(`[RbacFilter] Schema version check failed:`, err);
        }
    }

    /**
     * Invalidate cache for a specific role (e.g. on schema change).
     */
    invalidateCache(role?: string): void {
        if (role) {
            this.cache.del(role);
        } else {
            this.cache.flushAll();
        }
    }
}
