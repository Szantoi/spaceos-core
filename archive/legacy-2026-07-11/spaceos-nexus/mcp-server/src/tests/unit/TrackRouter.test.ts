import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TrackRouter, TrackImmutabilityError, AuthorizationError } from '../../metadata/TrackRouter';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { RbacFilter } from '../../mcp/RbacFilter';
import { SessionState, FSMState } from '../../metadata/types';

describe('TrackRouter', () => {
    let tracker: WorkflowStateTracker;
    let rbac: RbacFilter;
    let router: TrackRouter;

    beforeEach(() => {
        // Mock WorkflowStateTracker
        tracker = {
            getState: vi.fn(),
            lockTrack: vi.fn(),
        } as unknown as WorkflowStateTracker;

        // Mock RbacFilter
        rbac = {
            getAllowedTools: vi.fn(),
        } as unknown as RbacFilter;

        router = new TrackRouter(tracker, rbac);
    });

    const createMockSession = (overrides: Partial<SessionState> = {}): SessionState => ({
        sessionId: 'test-session',
        state: 'in_progress' as FSMState,
        workflowId: 'test-workflow',
        domain: 'test-domain',
        roleName: 'test-role',
        track: null,
        trackLocked: false,
        createdAt: new Date(),
        updatedAt: new Date(),
        ...overrides,
    });

    it('should route discovery track successfully if authorized and not locked', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession());
        vi.mocked(rbac.getAllowedTools).mockReturnValue(new Set(['discovery_tools']));

        const result = router.route('test-session', 'discovery');

        expect(result).toBe('discovery');
        expect(tracker.lockTrack).toHaveBeenCalledWith('test-session', 'discovery');
    });

    it('should route delivery track successfully if authorized and not locked', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession());
        vi.mocked(rbac.getAllowedTools).mockReturnValue(new Set(['delivery_tools']));

        const result = router.route('test-session', 'delivery');

        expect(result).toBe('delivery');
        expect(tracker.lockTrack).toHaveBeenCalledWith('test-session', 'delivery');
    });

    it('should throw Error if session is in terminal state "submitted"', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession({ state: 'submitted' }));

        expect(() => router.route('test-session', 'discovery'))
            .toThrow('Session test-session is in terminal state: submitted');
    });

    it('should throw Error if session is in terminal state "abandoned"', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession({ state: 'abandoned' }));

        expect(() => router.route('test-session', 'delivery'))
            .toThrow('Session test-session is in terminal state: abandoned');
    });

    it('should throw TrackImmutabilityError if trying to route to discovery when locked to delivery', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession({
            track: 'delivery',
            trackLocked: true
        }));

        expect(() => router.route('test-session', 'discovery'))
            .toThrow(TrackImmutabilityError);
    });

    it('should ALLOW routing from discovery to delivery despite lock (workflow progression)', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession({
            track: 'discovery',
            trackLocked: true
        }));
        vi.mocked(rbac.getAllowedTools).mockReturnValue(new Set(['delivery_tools']));

        const result = router.route('test-session', 'delivery');

        expect(result).toBe('delivery');
        expect(tracker.lockTrack).toHaveBeenCalledWith('test-session', 'delivery');
    });

    it('should throw AuthorizationError if role lacks track permissions', () => {
        vi.mocked(tracker.getState).mockReturnValue(createMockSession());
        vi.mocked(rbac.getAllowedTools).mockReturnValue(new Set(['some_other_tools'])); // Missing 'discovery_tools'

        expect(() => router.route('test-session', 'discovery'))
            .toThrow(AuthorizationError);
        expect(() => router.route('test-session', 'discovery'))
            .toThrow('Role test-role cannot access discovery');
    });
});
