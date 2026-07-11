import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ContextMiddleware } from '../../mcp/middleware/contextMiddleware';
import { SessionManager } from '../../mcp/SessionManager';
import { WorkflowStateTracker } from '../../metadata/WorkflowStateTracker';
import { ErrorCodes } from '../../mcp/ErrorCodes';

describe('ContextMiddleware', () => {
    let sessionManager: SessionManager;
    let workflowTracker: WorkflowStateTracker;
    let middleware: ContextMiddleware;

    beforeEach(() => {
        sessionManager = {
            get: vi.fn(),
            resolveDomainNameById: vi.fn().mockReturnValue(null)
        } as unknown as SessionManager;

        workflowTracker = {
            getState: vi.fn(),
            lockTrack: vi.fn()
        } as unknown as WorkflowStateTracker;

        middleware = new ContextMiddleware(sessionManager, workflowTracker);
    });

    it('should throw UNAUTHORIZED if session_id is missing', () => {
        try {
            middleware.handle({ arguments: {} });
            expect.fail('Should have thrown an error');
        } catch (error: any) {
            expect(error.code).toBe(ErrorCodes.UNAUTHORIZED);
            expect(error.message).toContain('Missing session_id');
        }
    });

    it('should throw UNAUTHORIZED if session does not exist', () => {
        vi.mocked(sessionManager.get).mockReturnValue(undefined);

        try {
            middleware.handle({ arguments: { session_id: 'test-session-123' } });
            expect.fail('Should have thrown an error');
        } catch (error: any) {
            expect(error.code).toBe(ErrorCodes.UNAUTHORIZED);
            expect(error.message).toContain('Invalid or expired session');
        }
    });

    it('should extract context successfully from payload arguments', () => {
        const mockSession = { domain: 'test-domain', role: 'Architect', status: 'active' };
        vi.mocked(sessionManager.get).mockReturnValue(mockSession as any);
        vi.mocked(workflowTracker.getState).mockReturnValue({ state: 'phase-1-planning' } as any);

        const context = middleware.handle({
            arguments: { session_id: 'test-session-123', user_id: 'user-001' }
        });

        expect(context.session_id).toBe('test-session-123');
        expect(context.domain).toBe('test-domain');
        expect(context.role).toBe('Architect');
        expect(context.phase).toBe('phase-1-planning');
        expect(context.user_id).toBe('user-001');
        // default track inference (tracker returned no track)
        expect(context.track).toBe('delivery');
        expect((workflowTracker as any).lockTrack).toHaveBeenCalledWith('test-session-123', 'delivery');
    });

    it('should persist inferred discovery track when domain is discovery', () => {
        const mockSession = { domain: 'discovery', role: 'Explorer', status: 'active' };
        vi.mocked(sessionManager.get).mockReturnValue(mockSession as any);
        vi.mocked(workflowTracker.getState).mockReturnValue({ current_state: 'foo' } as any);

        const context = middleware.handle({ arguments: { session_id: 'discovery-session', user_id: 'u2' } });
        expect(context.track).toBe('discovery');
        expect((workflowTracker as any).lockTrack).toHaveBeenCalledWith('discovery-session', 'discovery');
    });

    it('should extract context successfully from headers if missing in arguments', () => {
        const mockSession = { domain: 'platform', role: 'Developer', status: 'active' };
        vi.mocked(sessionManager.get).mockReturnValue(mockSession as any);
        vi.mocked(workflowTracker.getState).mockReturnValue(undefined as any); // Should Fallback to session.status

        const headers = { 'x-session-id': 'header-session', 'x-user-id': 'header-user' };
        const context = middleware.handle({ arguments: {} }, headers);

        expect(context.session_id).toBe('header-session');
        expect(context.user_id).toBe('header-user');
        expect(context.domain).toBe('platform');
        expect(context.role).toBe('Developer');
        expect(context.phase).toBe('active'); // fallback
        expect(context.track).toBe('delivery');
    });

    it('should populate track when workflow state contains it', () => {
        const mockSession = { domain: 'platform', role: 'Developer', status: 'active' };
        vi.mocked(sessionManager.get).mockReturnValue(mockSession as any);
        vi.mocked(workflowTracker.getState).mockReturnValue({ current_state: 'foo', track: 'discovery' } as any);

        const context = middleware.handle({ arguments: { session_id: 'xf', user_id: 'u1' } });
        expect(context.track).toBe('discovery');
    });

    it('should meet performance constraints (<5ms middleware overhead)', () => {
        const mockSession = { domain: 'perf', role: 'Tester', status: 'perf-active' };
        vi.mocked(sessionManager.get).mockReturnValue(mockSession as any);
        vi.mocked(workflowTracker.getState).mockReturnValue({ current_state: 'perf-state' } as any);

        const start = performance.now();
        // Run it 1000 times
        for (let i = 0; i < 1000; i++) {
            middleware.handle({
                arguments: { session_id: 'perf-session-123', user_id: 'perf-user' }
            });
        }
        const end = performance.now();

        const avgMs = (end - start) / 1000;

        // Assert it takes less than 5ms
        expect(avgMs).toBeLessThan(5);
    });
});
