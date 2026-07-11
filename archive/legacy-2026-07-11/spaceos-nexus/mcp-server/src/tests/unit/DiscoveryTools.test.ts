import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ContextPlugin } from '../../mcp/tools/context';
import { DiscoveryPlugin } from '../../mcp/tools/discovery';
import { McpContext } from '../../mcp/middleware/contextMiddleware';
import { AgentDb } from '../../mcp/AgentDb';

// reuse simple mock context from other tests
const baseContext: McpContext = {
    session_id: 's1',
    user_id: 'u1',
    domain: 'discovery',
    role: 'architect',
    phase: 'ideation',
    track: 'discovery'
};

function makeMockAgentDb(): Partial<AgentDb> {
    const storage: any[] = [];
    return {
        getRolesByDomain: vi.fn().mockReturnValue([]),
        getRoleSchema: vi.fn(),
        getWorkflowsByRole: vi.fn(),
        getTemplatesByRole: vi.fn(),
        trackBlocker: vi.fn((sid, phase, severity, text) => {
            storage.push({ session_id: sid, phase, severity, text });
        }),
        getBlockers: vi.fn((sid, phase) => {
            return storage.filter(b => b.session_id === sid && (!phase || b.phase === phase));
        }),
    };
}

// mock EpisodeStore for outcome tool
vi.mock('../../episodic/EpisodeStore', () => {
    class FakeStore {
        public initialize = async () => { };
        public storeExperience = async (params: any) => ({ episodeId: 'fake-ep', createdAt: new Date() });
    }
    return { EpisodeStore: FakeStore };
});

// mock searchExperience used by referencePriorDiscovery
vi.mock('../../episodic/FtsSearch', () => ({
    searchExperience: vi.fn().mockReturnValue([])
}));

describe('Discovery tools (unit)', () => {
    let contextPlugin: ContextPlugin;
    let discoveryPlugin: DiscoveryPlugin;
    let systemContext: any;
    let mockDb: any;

    beforeEach(() => {
        mockDb = makeMockAgentDb();
        systemContext = {
            agentDb: mockDb,
            sessionManager: {} as any,
            rbacFilter: { getAllowedTools: vi.fn().mockReturnValue(new Set(['request_context', 'reference_prior_discovery'])) } as any,
            workflowTracker: {} as any
        };
        contextPlugin = new ContextPlugin(systemContext);
        discoveryPlugin = new DiscoveryPlugin(systemContext);
    });

    describe('request_context tool', () => {
        it('returns basic context when no phase provided', async () => {
            const res: any = await contextPlugin.requestContext({}, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.sessionId).toBe(baseContext.session_id);
            expect(res.data.track).toBe('discovery');
        });

        it('returns phase-specific payload when phase provided on discovery track', async () => {
            const res: any = await contextPlugin.requestContext({ phase: 'ideation' }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.workflow_template).toContain('Phase 1: IDEATION');
            expect(Array.isArray(res.data.artifact_templates)).toBe(true);
            expect(res.data.available_tools).toContain('request_context');
        });

        it('denies phase context on non-discovery track', async () => {
            const other = { ...baseContext, track: 'delivery' as any };
            const res = await contextPlugin.requestContext({ phase: 'ideation' }, other);
            expect(res.success).toBe(false);
        });

        it('throws schema error for invalid phase', async () => {
            const res = await contextPlugin.requestContext({ phase: 'bad' as any }, baseContext);
            expect(res.success).toBe(false);
            expect(res.error.code).toBe(400);
        });
    });

    describe('reference_prior_discovery tool', () => {
        it('returns empty result set by default', async () => {
            const res = await discoveryPlugin.referencePriorDiscovery({ search_text: 'foo' }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.total_found).toBe(0);
            expect(Array.isArray(res.data.episodes)).toBe(true);
        });

        it('respects limit parameter', async () => {
            const res = await discoveryPlugin.referencePriorDiscovery({ search_text: 'foo', limit: 2 }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.total_found).toBe(0);
        });

        it('runs quickly (<200ms)', async () => {
            const start = performance.now();
            await discoveryPlugin.referencePriorDiscovery({ search_text: 'foo' }, baseContext);
            const elapsed = performance.now() - start;
            expect(elapsed).toBeLessThan(200);
        });
    });

    describe('submit_discovery_outcome tool', () => {
        it('stores an episode and returns metadata', async () => {
            const res = await discoveryPlugin.submitDiscoveryOutcome({
                outcome_type: 'VALIDATED_IDEA',
                summary: 'Test outcome',
                blockers: ['none'],
                phase: 'ideation',
                artifacts: ['a1']
            }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.episode_id).toBe('fake-ep');
            expect(res.data.track).toBe('discovery');
        });

        it('denies when not on discovery track', async () => {
            const other = { ...baseContext, track: 'delivery' as any };
            const res = await discoveryPlugin.submitDiscoveryOutcome({
                outcome_type: 'LEARNING',
                summary: 'X',
                blockers: [],
                phase: 'validation'
            }, other);
            expect(res.success).toBe(false);
        });

        it('validates summary length via schema', async () => {
            const long = 'x'.repeat(600);
            const res = await discoveryPlugin.submitDiscoveryOutcome({
                outcome_type: 'LEARNING',
                summary: long,
                blockers: [],
                phase: 'iteration'
            }, baseContext);
            expect(res.success).toBe(false);
            expect(res.error.code).toBe(400);
        });

        it('enforces phase order: cannot submit iteration before validation complete', async () => {
            // baseContext track discovery
            const res = await discoveryPlugin.submitDiscoveryOutcome({
                outcome_type: 'LEARNING',
                summary: 'X',
                blockers: [],
                phase: 'iteration'
            }, baseContext);
            expect(res.success).toBe(false);
            expect(res.error.message).toContain('Cannot submit outcome');
        });
    });

    describe('phase-specific tools (TASK-13-05)', () => {
        it('check_constraints returns go_no_go and handles violation', async () => {
            const res = await discoveryPlugin.checkConstraints({ idea_summary: 'short', constraint_set: 'technical' }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.go_no_go).toBe(true);

            const res2 = await discoveryPlugin.checkConstraints({ idea_summary: 'x'.repeat(300), constraint_set: 'technical' }, baseContext);
            expect(res2.success).toBe(true);
            expect(res2.data.go_no_go).toBe(false);
            expect(res2.data.violations.length).toBeGreaterThan(0);
        });

        it('phase tools are denied on delivery track', async () => {
            const deliverCtx = { ...baseContext, track: 'delivery' as any };
            const r1 = await discoveryPlugin.checkConstraints({ idea_summary: 'x', constraint_set: 'technical' }, deliverCtx);
            expect(r1.success).toBe(false);
            const r2 = await discoveryPlugin.getPhaseGuidance({ current_phase: 'ideation' }, deliverCtx);
            expect(r2.success).toBe(false);
        });

        describe('blocker tracking tools', () => {
            it('track_blocker stores blocker and query returns it', async () => {
                const res = await discoveryPlugin.trackBlocker({ blocker_text: 'network outage', severity: 'HIGH', blocking_phase: 'ideation' }, baseContext);
                expect(res.success).toBe(true);
                const list = await discoveryPlugin.queryBlockers({}, baseContext);
                expect(list.success).toBe(true);
                expect(list.data.length).toBe(1);
                expect(list.data[0].text).toBe('network outage');
            });

            it('query_blockers filters by phase', async () => {
                // same session, add another
                await discoveryPlugin.trackBlocker({ blocker_text: 'slow cpu', severity: 'MEDIUM', blocking_phase: 'validation' }, baseContext);
                const phaseOnly = await discoveryPlugin.queryBlockers({ phase: 'validation' }, baseContext);
                expect(phaseOnly.data.every((b: any) => b.phase === 'validation')).toBe(true);
            });
        });

        describe('semantic fallback in reference_prior_discovery', () => {
            it('uses fallback when semantic search throws', async () => {
                // mock trySemanticSearch to throw
                const spy = vi.spyOn(discoveryPlugin as any, 'trySemanticSearch').mockRejectedValue(new Error('fail'));
                const start = performance.now();
                const res = await discoveryPlugin.referencePriorDiscovery({ search_text: 'x' }, baseContext);
                const elapsed = performance.now() - start;
                expect(res.success).toBe(true);
                expect(res.data.fallback_used).toBe(true);
                expect(elapsed).toBeLessThan(50); // AC: fallback fast
                spy.mockRestore();
            });
        });

        it('get_phase_guidance returns proper structure and validates phase', async () => {
            const res = await discoveryPlugin.getPhaseGuidance({ current_phase: 'validation' }, baseContext);
            expect(res.success).toBe(true);
            expect(res.data.next_phase).toBe('iteration');
            expect(Array.isArray(res.data.exit_criteria)).toBe(true);

            const bad = await discoveryPlugin.getPhaseGuidance({ current_phase: 'unknown' as any }, baseContext);
            expect(bad.success).toBe(false);
        });
    });
});