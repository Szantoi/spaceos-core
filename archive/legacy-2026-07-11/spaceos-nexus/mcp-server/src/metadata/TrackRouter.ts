// ==========================================================================
// EPIC-11: Two-Track Routing Service
// ==========================================================================
// Integrates with WorkflowStateTracker and RbacFilter to enforce
// separation of concerns between discovery and delivery workflows.
// ==========================================================================

import { WorkflowStateTracker } from './WorkflowStateTracker';
import { RbacFilter } from '../mcp/RbacFilter';
import { FSMState, TERMINAL_STATES } from './types';

export class TrackImmutabilityError extends Error {
    constructor(sessionId: string, currentTrack: string, attemptedTrack: string) {
        super(`Session ${sessionId} is locked to ${currentTrack} track, cannot switch to ${attemptedTrack}`);
        this.name = 'TrackImmutabilityError';
    }
}

export class AuthorizationError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'AuthorizationError';
    }
}

export class TrackRouter {
    constructor(
        private tracker: WorkflowStateTracker,
        private rbac: RbacFilter,
    ) { }

    /**
     * Routes a session to a specific track and locks it if allowed.
     * @param sessionId The FSM session ID
     * @param track The track to route to ('discovery' or 'delivery')
     * @returns The successfully routed track
     * @throws Error if terminal state, TrackImmutabilityError if track changed, AuthorizationError if role unauthorized
     */
    route(sessionId: string, track: 'discovery' | 'delivery'): string {
        // 1. Get session (SZINKRON!)
        const session = this.tracker.getState(sessionId); // throws SessionNotFoundError

        // 2. Check terminal state — submitted/failed-ből nem lehet route-olni
        if (TERMINAL_STATES.has(session.state)) {
            throw new Error(`Session ${sessionId} is in terminal state: ${session.state}`);
        }

        // 3. Check track immutability
        if (session.trackLocked && session.track !== track) {
            // Only allow transition from discovery to delivery, never delivery to discovery
            if (session.track === 'delivery' && track === 'discovery') {
                throw new TrackImmutabilityError(sessionId, session.track, track);
            } else if (session.track === 'discovery' && track === 'delivery') {
                // Discovery -> Delivery transition is allowed by requirements:
                // "Can transition to delivery after adequate research"
                // The track will be overriden in step 5
            } else {
                throw new TrackImmutabilityError(sessionId, session.track!, track);
            }
        }

        // 4. Check RBAC (SZINKRON!)
        const allowed = this.rbac.getAllowedTools(session.roleName);
        const trackTool = track === 'discovery' ? 'discovery_tools' : 'delivery_tools';

        // NOTE: Actually RBAC permission names are exactly matching the tool names in the YAML/DB.
        // Wait, 'discovery_tools' might just be a logical tool/permission in RBAC.
        // The assignment uses this exactly:
        if (!allowed.has(trackTool)) {
            throw new AuthorizationError(`Role ${session.roleName} cannot access ${track}`);
        }

        // 5. Lock track
        this.tracker.lockTrack(sessionId, track);
        return track;
    }
}
