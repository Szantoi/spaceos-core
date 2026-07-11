import { AgentDb } from '../AgentDb';

export enum Track {
    DISCOVERY = 'discovery',
    DELIVERY = 'delivery'
}

/**
 * TwoTrackRouter: Implements dual-track tool routing.
 *
 * Logic:
 * 1. Identify session track (via AgentDb)
 * 2. Filter available tools based on track classification
 * 3. Enforce immutability (track cannot change during session)
 */
export class TwoTrackRouter {
    private agentDb: AgentDb;

    // Tool categorization (AC-10, AC-11, AC-12)
    private static DISCOVERY_TOOLS = new Set([
        'agent_context',
        'search_knowledge',
        'search_knowledge_base',
        'list_workflows',
        'validate_input',
        'brainstorm',
        'get_role',
        'get_role_context',
        'get_workflow',
        'get_template',
        'list_templates',
        'get_core',
        'get_policy',
        'list_domains',
        'list_roles'
    ]);

    private static DELIVERY_TOOLS = new Set([
        'bootstrap_agent',
        'submit_workflow',
        'get_workflow_state',
        'list_artifacts',
        'record_result',
        'request_workflow_transition',
        'session_register',
        'artifact_submit',
        'session_complete'
    ]);

    private static SHARED_TOOLS = new Set([
        'get_session_context',
        'get_audit_log',
        'is_allowed'
    ]);

    constructor(agentDb: AgentDb) {
        this.agentDb = agentDb;
    }

    /**
     * Returns the track classification for a given session.
     */
    public async getSessionTrack(sessionId: string): Promise<Track | null> {
        const track = this.agentDb.getSessionTrack(sessionId);
        return (track as Track) || null;
    }

    /**
     * Validates if a tool can be accessed within the current session's track.
     */
    public async canAccessTool(sessionId: string | null, toolName: string): Promise<boolean> {
        // Shared tools are always permitted
        if (TwoTrackRouter.SHARED_TOOLS.has(toolName)) {
            return true;
        }

        // Sessions starting tools are allowed if no session_id is provided yet
        if (!sessionId) {
            return toolName === 'bootstrap_agent' || toolName === 'session_register';
        }

        const track = await this.getSessionTrack(sessionId);
        if (!track) {
            // Session exists but has no track classification
            return false;
        }

        if (track === Track.DISCOVERY) {
            return TwoTrackRouter.DISCOVERY_TOOLS.has(toolName);
        }

        if (track === Track.DELIVERY) {
            return TwoTrackRouter.DELIVERY_TOOLS.has(toolName);
        }

        return false;
    }

    /**
     * Returns the required track for a given tool.
     */
    public static getToolTrack(toolName: string): Track | 'shared' | 'restricted' {
        if (this.SHARED_TOOLS.has(toolName)) return 'shared';
        if (this.DISCOVERY_TOOLS.has(toolName)) return Track.DISCOVERY;
        if (this.DELIVERY_TOOLS.has(toolName)) return Track.DELIVERY;
        return 'restricted';
    }

    /**
     * Filters a list of tool names based on session track.
     */
    public async filterTools(sessionId: string | null, toolNames: string[]): Promise<string[]> {
        const results = await Promise.all(
            toolNames.map(async (name) => {
                const allowed = await this.canAccessTool(sessionId, name);
                return allowed ? name : null;
            })
        );
        return results.filter((name): name is string => name !== null);
    }
}
