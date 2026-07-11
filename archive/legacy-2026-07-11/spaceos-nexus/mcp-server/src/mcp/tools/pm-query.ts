import { z } from 'zod';
import { McpContext } from '../middleware/contextMiddleware';
import { ErrorResponses } from '../ErrorResponses';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';
import { IToolModule } from './IToolModule';
import { AgentDb } from '../AgentDb';

function isAdminRole(role: string): boolean {
    const normalized = role.toLowerCase();
    return normalized.includes('admin') || normalized === 'architect';
}

@Plugin({
    name: 'pm-query',
    version: '1.0.0',
    dependencies: ['bootstrap']
})
export class PmQueryPlugin extends BasePlugin {

    @Tool({
        name: 'get_project_state',
        description: 'Read-only project state query: returns milestone, open tasks count, and due date.',
        schema: z.object({
            project_id: z.string().min(1)
        })
    })
    async getProjectState(args: { project_id: string }, _context: McpContext) {
        const state = this.context.agentDb.getProjectState(args.project_id);
        if (!state) {
            return ErrorResponses.notFound(`Project not found or PM schema unavailable: ${args.project_id}`);
        }

        return { success: true, data: state };
    }

    @Tool({
        name: 'list_my_team_tasks',
        description: 'Read-only task listing filtered by agent domain (RBAC), optional track and status.',
        schema: z.object({
            domain: z.string().optional(),
            track: z.enum(['discovery', 'delivery']).optional(),
            status: z.string().optional(),
            limit: z.number().int().positive().max(100).optional()
        })
    })
    async listMyTeamTasks(
        args: { domain?: string; track?: 'discovery' | 'delivery'; status?: string; limit?: number },
        context: McpContext
    ) {
        const admin = isAdminRole(context.role);
        if (!admin && args.domain && args.domain !== context.domain) {
            return ErrorResponses.forbidden('You can only query tasks in your own domain', {
                requested_domain: args.domain,
                context_domain: context.domain
            });
        }

        const effectiveDomain = admin ? (args.domain || context.domain) : (args.domain || context.domain);
        const tasks = this.context.agentDb.listPmTasks({
            domain: effectiveDomain,
            track: args.track,
            status: args.status,
            limit: args.limit ?? 20
        });

        return {
            success: true,
            data: {
                tasks,
                total: tasks.length,
                filters: {
                    domain: effectiveDomain ?? null,
                    track: args.track ?? null,
                    status: args.status ?? null
                }
            }
        };
    }

    @Tool({
        name: 'get_task_context',
        description: 'Read-only task context query: task details + acceptance criteria + workflow + template.',
        schema: z.object({
            task_id: z.string().min(1)
        })
    })
    async getTaskContext(args: { task_id: string }, context: McpContext) {
        const task = this.context.agentDb.getTaskContext(args.task_id);
        if (!task) {
            return ErrorResponses.notFound(`Task not found: ${args.task_id}`);
        }

        const admin = isAdminRole(context.role);
        if (!admin && task.domain && task.domain !== context.domain) {
            return ErrorResponses.forbidden('You can only access task context in your own domain', {
                task_domain: task.domain,
                context_domain: context.domain,
                task_id: args.task_id
            });
        }

        return { success: true, data: task };
    }

    @Tool({
        name: 'search_tasks',
        description: 'Read-only fuzzy task search with optional domain/track/status filters (max 10 by default).',
        schema: z.object({
            query: z.string().min(1),
            filters: z.object({
                domain: z.string().optional(),
                track: z.enum(['discovery', 'delivery']).optional(),
                status: z.string().optional()
            }).optional(),
            limit: z.number().int().positive().max(100).optional()
        })
    })
    async searchTasks(
        args: {
            query: string;
            filters?: { domain?: string; track?: 'discovery' | 'delivery'; status?: string };
            limit?: number;
        },
        context: McpContext
    ) {
        const admin = isAdminRole(context.role);
        const requestedDomain = args.filters?.domain;

        if (!admin && requestedDomain && requestedDomain !== context.domain) {
            return ErrorResponses.forbidden('You can only search tasks in your own domain', {
                requested_domain: requestedDomain,
                context_domain: context.domain
            });
        }

        const effectiveDomain = admin ? (requestedDomain || context.domain) : (requestedDomain || context.domain);
        const tasks = this.context.agentDb.listPmTasks({
            domain: effectiveDomain,
            track: args.filters?.track,
            status: args.filters?.status,
            query: args.query,
            limit: args.limit ?? 10
        });

        return {
            success: true,
            data: {
                tasks,
                total: tasks.length,
                query: args.query,
                filters: {
                    domain: effectiveDomain ?? null,
                    track: args.filters?.track ?? null,
                    status: args.filters?.status ?? null
                }
            }
        };
    }

    @Tool({
        name: 'list_dwi_dashboard',
        description: 'Read-only discovery work item (DWI) dashboard with optional filters.',
        schema: z.object({
            status: z.enum(['open', 'in_progress', 'concluded', 'archived']).optional(),
            current_phase: z.number().int().min(0).max(4).optional(),
            topic: z.string().min(1).optional(),
            limit: z.number().int().positive().max(200).optional()
        })
    })
    async listDwiDashboard(
        args: { status?: string; current_phase?: number; topic?: string; limit?: number },
        _context: McpContext
    ) {
        const items = this.context.agentDb.listDwiDashboard({
            status: args.status,
            currentPhase: args.current_phase,
            topic: args.topic,
            limit: args.limit
        });

        return {
            success: true,
            data: {
                items,
                total: items.length,
                filters: {
                    status: args.status ?? null,
                    current_phase: args.current_phase ?? null,
                    topic: args.topic ?? null
                }
            }
        };
    }
}

export default PmQueryPlugin;

/**
 * @deprecated Use PmQueryPlugin class instead.
 */
export function createPmQueryToolModule(agentDb: AgentDb): IToolModule {
    return {
        name: 'pm-query',
        version: '1.0.0',
        dependencies: ['bootstrap'],
        tools: [
            {
                name: 'get_project_state',
                description: 'Read-only project state query',
                inputSchema: z.object({ project_id: z.string().min(1) })
            },
            {
                name: 'list_my_team_tasks',
                description: 'Read-only domain-scoped team task listing',
                inputSchema: z.object({
                    domain: z.string().optional(),
                    track: z.enum(['discovery', 'delivery']).optional(),
                    status: z.string().optional(),
                    limit: z.number().int().positive().max(100).optional()
                })
            },
            {
                name: 'get_task_context',
                description: 'Read-only task context query',
                inputSchema: z.object({ task_id: z.string().min(1) })
            },
            {
                name: 'search_tasks',
                description: 'Read-only fuzzy task search',
                inputSchema: z.object({
                    query: z.string().min(1),
                    filters: z.object({
                        domain: z.string().optional(),
                        track: z.enum(['discovery', 'delivery']).optional(),
                        status: z.string().optional()
                    }).optional(),
                    limit: z.number().int().positive().max(100).optional()
                })
            },
            {
                name: 'list_dwi_dashboard',
                description: 'Read-only discovery work item (DWI) dashboard query.',
                inputSchema: z.object({
                    status: z.enum(['open', 'in_progress', 'concluded', 'archived']).optional(),
                    current_phase: z.number().int().min(0).max(4).optional(),
                    topic: z.string().min(1).optional(),
                    limit: z.number().int().positive().max(200).optional()
                })
            }
        ],
        handlers: {
            get_project_state: async (args: { project_id: string }) => {
                const state = agentDb.getProjectState(args.project_id);
                if (!state) {
                    return ErrorResponses.notFound(`Project not found or PM schema unavailable: ${args.project_id}`);
                }
                return { success: true, data: state };
            },
            list_my_team_tasks: async (args: { domain?: string; track?: 'discovery' | 'delivery'; status?: string; limit?: number }, context: McpContext) => {
                const admin = isAdminRole(context.role);
                if (!admin && args.domain && args.domain !== context.domain) {
                    return ErrorResponses.forbidden('You can only query tasks in your own domain', {
                        requested_domain: args.domain,
                        context_domain: context.domain
                    });
                }
                const effectiveDomain = admin ? (args.domain || context.domain) : (args.domain || context.domain);
                const tasks = agentDb.listPmTasks({
                    domain: effectiveDomain,
                    track: args.track,
                    status: args.status,
                    limit: args.limit ?? 20
                });
                return {
                    success: true,
                    data: {
                        tasks,
                        total: tasks.length,
                        filters: {
                            domain: effectiveDomain ?? null,
                            track: args.track ?? null,
                            status: args.status ?? null
                        }
                    }
                };
            },
            get_task_context: async (args: { task_id: string }, context: McpContext) => {
                const task = agentDb.getTaskContext(args.task_id);
                if (!task) {
                    return ErrorResponses.notFound(`Task not found: ${args.task_id}`);
                }
                const admin = isAdminRole(context.role);
                if (!admin && task.domain && task.domain !== context.domain) {
                    return ErrorResponses.forbidden('You can only access task context in your own domain', {
                        task_domain: task.domain,
                        context_domain: context.domain,
                        task_id: args.task_id
                    });
                }
                return { success: true, data: task };
            },
            search_tasks: async (args: { query: string; filters?: { domain?: string; track?: 'discovery' | 'delivery'; status?: string }; limit?: number }, context: McpContext) => {
                const admin = isAdminRole(context.role);
                const requestedDomain = args.filters?.domain;
                if (!admin && requestedDomain && requestedDomain !== context.domain) {
                    return ErrorResponses.forbidden('You can only search tasks in your own domain', {
                        requested_domain: requestedDomain,
                        context_domain: context.domain
                    });
                }
                const effectiveDomain = admin ? (requestedDomain || context.domain) : (requestedDomain || context.domain);
                const tasks = agentDb.listPmTasks({
                    domain: effectiveDomain,
                    track: args.filters?.track,
                    status: args.filters?.status,
                    query: args.query,
                    limit: args.limit ?? 10
                });
                return {
                    success: true,
                    data: {
                        tasks,
                        total: tasks.length,
                        query: args.query,
                        filters: {
                            domain: effectiveDomain ?? null,
                            track: args.filters?.track ?? null,
                            status: args.filters?.status ?? null
                        }
                    }
                };
            },
            list_dwi_dashboard: async (args: { status?: string; current_phase?: number; topic?: string; limit?: number }) => {
                const items = agentDb.listDwiDashboard({
                    status: args.status,
                    currentPhase: args.current_phase,
                    topic: args.topic,
                    limit: args.limit
                });
                return {
                    success: true,
                    data: {
                        items,
                        total: items.length,
                        filters: {
                            status: args.status ?? null,
                            current_phase: args.current_phase ?? null,
                            topic: args.topic ?? null
                        }
                    }
                };
            }
        }
    };
}
