import { beforeEach, describe, expect, it, vi } from 'vitest';
import { PmQueryPlugin, createPmQueryToolModule } from '../../mcp/tools/pm-query';
import { SystemContext } from '../../plugins/PluginTypes';

describe('EPIC-15: PmQueryPlugin', () => {
  let mockContext: SystemContext;

  beforeEach(() => {
    mockContext = {
      agentDb: {
        getProjectState: vi.fn(),
        listPmTasks: vi.fn(),
        getTaskContext: vi.fn(),
        listDwiDashboard: vi.fn(),
      } as any,
      sessionManager: {} as any,
      rbacFilter: {} as any,
      workflowTracker: {} as any,
      guardrailService: {} as any,
    } as SystemContext;
  });

  it('get_project_state returns notFound when project does not exist', async () => {
    (mockContext.agentDb.getProjectState as any).mockReturnValue(null);
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.getProjectState({ project_id: 'PROJ-X' }, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(false);
    expect((result as any).code).toBeDefined();
  });

  it('list_my_team_tasks enforces domain scoping for non-admin roles', async () => {
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.listMyTeamTasks({ domain: 'management' }, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(false);
    expect((result as any).code).toBeDefined();
    expect(mockContext.agentDb.listPmTasks).not.toHaveBeenCalled();
  });

  it('list_my_team_tasks uses context domain when caller is non-admin', async () => {
    (mockContext.agentDb.listPmTasks as any).mockReturnValue([
      { id: 'TASK-1', title: 'A', status: 'Open', assigned_to: 'alice', domain: 'engineering', track: 'delivery' }
    ]);
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.listMyTeamTasks({}, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(true);
    expect((mockContext.agentDb.listPmTasks as any).mock.calls[0][0].domain).toBe('engineering');
  });

  it('get_task_context blocks cross-domain access for non-admin roles', async () => {
    (mockContext.agentDb.getTaskContext as any).mockReturnValue({
      id: 'TASK-42',
      title: 'Cross domain task',
      status: 'Open',
      assigned_to: 'bob',
      domain: 'management',
      track: 'delivery',
      description: null,
      acceptance_criteria: null,
      workflow: null,
      template: null
    });
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.getTaskContext({ task_id: 'TASK-42' }, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(false);
    expect((result as any).code).toBeDefined();
  });

  it('search_tasks forwards query + filters and returns success payload', async () => {
    (mockContext.agentDb.listPmTasks as any).mockReturnValue([
      { id: 'TASK-77', title: 'Investigate cache miss', status: 'Open', assigned_to: 'alice', domain: 'engineering', track: 'delivery' }
    ]);
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.searchTasks({
      query: 'cache',
      filters: { status: 'Open' },
      limit: 10
    }, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(true);
    expect((result as any).data.total).toBe(1);
    expect((mockContext.agentDb.listPmTasks as any).mock.calls[0][0]).toMatchObject({
      domain: 'engineering',
      status: 'Open',
      query: 'cache',
      limit: 10
    });
  });

  it('list_dwi_dashboard returns dashboard results using agentDb', async () => {
    (mockContext.agentDb.listDwiDashboard as any).mockReturnValue([
      { dwi_id: 'dwi-abc', topic: 'Alpha', status: 'open', current_phase: 1, next_action: 'Do X', verdict: null, hypothesis_count: 0, validated_count: 0, updated_at: new Date().toISOString() }
    ]);
    const plugin = new PmQueryPlugin(mockContext);

    const result = await plugin.listDwiDashboard({ status: 'open', current_phase: 1, topic: 'Alpha', limit: 20 }, {
      session_id: 's-1',
      domain: 'engineering',
      role: 'backend_developer',
      phase: 'in_progress',
      track: 'delivery'
    });

    expect(result.success).toBe(true);
    expect((result as any).data.total).toBe(1);
    expect((mockContext.agentDb.listDwiDashboard as any).mock.calls[0][0]).toMatchObject({
      status: 'open',
      currentPhase: 1,
      topic: 'Alpha',
      limit: 20
    });
  });
});

describe('EPIC-15: createPmQueryToolModule()', () => {
  it('exposes all required PM query tool names', () => {
    const module = createPmQueryToolModule({
      getProjectState: vi.fn(),
      listPmTasks: vi.fn(),
      getTaskContext: vi.fn(),
    } as any);

    const names = module.tools.map(t => t.name);
    expect(names).toContain('get_project_state');
    expect(names).toContain('list_my_team_tasks');
    expect(names).toContain('get_task_context');
    expect(names).toContain('search_tasks');
    expect(names).toContain('list_dwi_dashboard');
  });
});
