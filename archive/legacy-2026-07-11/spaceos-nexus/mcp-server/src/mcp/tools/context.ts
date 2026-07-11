import { z } from 'zod';
import * as fs from 'fs';
import * as path from 'path';
import { McpContext } from '../middleware/contextMiddleware';
import { Plugin, Tool } from '../../plugins/PluginDecorators';
import { BasePlugin } from '../../plugins/BasePlugin';
import { IToolModule } from './IToolModule';

type ContextFilters = string | string[] | undefined;

function isAmbiguousFilterInput(filters: ContextFilters): boolean {
    if (!filters) {
        return false;
    }

    const asArray = Array.isArray(filters) ? filters : [filters];
    return asArray.some(value => {
        const lowered = value.toLowerCase().trim();
        return lowered === 'ambiguous' || lowered === 'all' || lowered === 'everything' || lowered === '*';
    });
}

function normalizeFilterSelection(filters: ContextFilters): string[] {
    if (!filters) {
        return [];
    }
    return (Array.isArray(filters) ? filters : [filters]).map(item => item.toLowerCase().trim()).filter(Boolean);
}

async function resolveFiltersWithSampling(filters: ContextFilters, context: McpContext): Promise<{ selected: string[]; error?: { code: number; message: string; needs_clarification: boolean; needsClarification: boolean; }; }> {
    if (!isAmbiguousFilterInput(filters)) {
        return { selected: normalizeFilterSelection(filters) };
    }

    if (!context.requestSampling) {
        return {
            selected: [],
            error: {
                code: 400,
                message: 'Ambiguous filters require clarification, but sampling is unavailable',
                needs_clarification: true,
                needsClarification: true
            }
        };
    }

    const sampling = await context.requestSampling({
        prompt: 'Which filters did you mean?',
        options: [
            { label: 'by-role', value: 'role' },
            { label: 'by-phase', value: 'phase' },
            { label: 'by-status', value: 'status' }
        ],
        timeoutMs: 5000
    });

    if (sampling.error || sampling.selected.length === 0) {
        return {
            selected: [],
            error: {
                code: 400,
                message: sampling.error || 'Clarification needed before continuing',
                needs_clarification: true,
                needsClarification: true
            }
        };
    }

    return { selected: sampling.selected };
}

function assertValidPhase(phase: string): asserts phase is 'ideation' | 'validation' | 'iteration' | 'delivery_handoff' {
    const allowed = ['ideation', 'validation', 'iteration', 'delivery_handoff'];
    if (!allowed.includes(phase)) {
        throw new Error(`Invalid phase: ${phase}`);
    }
}

/**
 * Context Plugin (TASK-14-05)
 *
 * Provides session and request metadata to agents.
 * Refactored from createContextToolModule() factory to a proper Plugin class.
 */
@Plugin({
    name: "context",
    version: "1.0.0"
})
export class ContextPlugin extends BasePlugin {

    @Tool({
        name: "request_context",
        description: "Get current execution context or discovery phase context",
        schema: z.object({
            phase: z.enum(["ideation", "validation", "iteration", "delivery_handoff"]).optional(),
            filters: z.union([z.string(), z.array(z.string())]).optional()
        })
    })
    async requestContext(args: { phase?: string; filters?: ContextFilters }, context: McpContext) {
        const filterResolution = await resolveFiltersWithSampling(args.filters, context);
        if (filterResolution.error) {
            return { success: false, error: filterResolution.error };
        }

        // Legacy behavior when no phase requested
        if (!args.phase) {
            return {
                success: true,
                data: {
                    sessionId: context.session_id,
                    agentId: context.user_id,
                    domain: context.domain,
                    role: context.role,
                    track: context.track,
                    selected_filters: filterResolution.selected,
                    timestamp: new Date().toISOString(),
                }
            };
        }

        // Phase behaviour is only valid on discovery track.
        if (context.track !== 'discovery') {
            return { success: false, error: { code: 403, message: 'Phase context only available on discovery track' } };
        }

        const allowedPhases = new Set(['ideation', 'validation', 'iteration', 'delivery_handoff']);
        if (!allowedPhases.has(args.phase)) {
            return { success: false, error: { code: 400, message: `Invalid phase: ${args.phase}` } };
        }

        assertValidPhase(args.phase);
        const phase = args.phase;
        // load workflow template and artifacts from disk
        const templatesDir = path.resolve(__dirname, '../../../database/roles/discovery/templates');
        const workflowPath = path.resolve(__dirname, '../../../database/roles/discovery/workflows/DWI.workflow.md');
        let workflowTemplate = '';
        try { workflowTemplate = fs.readFileSync(workflowPath, 'utf8'); } catch { workflowTemplate = ''; }

        const artifactMap: Record<string, string[]> = {
            ideation: ['ideation-artifact.md'],
            validation: ['validation-report.md'],
            iteration: ['refined-design.md'],
            delivery_handoff: ['handoff-ticket.md']
        };

        const artifactTemplates: Array<{ name: string; content: string }> = [];
        for (const fname of artifactMap[phase] || []) {
            try {
                const content = fs.readFileSync(path.join(templatesDir, fname), 'utf8');
                artifactTemplates.push({ name: fname, content });
            } catch { /* ignore missing template */ }
        }

        const phaseChecklist = [
            `Complete all required steps in ${phase}`,
            `Produce required artifacts for ${phase}`
        ];

        let available_tools: string[] = [];
        if (this.context.rbacFilter) {
            available_tools = Array.from(this.context.rbacFilter.getAllowedTools(context.role))
                .filter(t => t.startsWith('discovery_') || t === 'request_context');
        }

        return {
            success: true,
            data: {
                workflow_template: workflowTemplate,
                artifact_templates: artifactTemplates,
                phase_checklist: phaseChecklist,
                selected_filters: filterResolution.selected,
                available_tools
            }
        };
    }

    @Tool({
        name: "lookup_context",
        description: "Detailed lookup of session metadata and current state",
        schema: z.object({
            sessionId: z.string().uuid().optional(),
        })
    })
    async lookupContext(args: { sessionId?: string }, context: McpContext) {
        const targetSessionId = args.sessionId || context.session_id;

        return {
            success: true,
            data: {
                sessionId: targetSessionId,
                agentId: context.user_id,
                domain: context.domain,
                role: context.role,
                isCurrentSession: targetSessionId === context.session_id,
                timestamp: new Date().toISOString(),
            }
        };
    }
}

export default ContextPlugin;

/**
 * @deprecated Use ContextPlugin class instead.
 * Kept for backward-compatibility with existing consumers.
 */
export function createContextToolModule(): IToolModule {
    return {
        name: "context",
        version: "1.0.0",
        tools: [
            {
                name: "request_context",
                description: "Get current execution context (sessionId, agentId, domain, role) or discovery phase",
                inputSchema: z.object({
                    phase: z.string().optional(),
                    filters: z.union([z.string(), z.array(z.string())]).optional()
                }),
            },
            {
                name: "lookup_context",
                description: "Detailed lookup of session metadata and current state",
                inputSchema: z.object({
                    sessionId: z.string().uuid().optional(),
                }),
            }
        ],
        handlers: {
            "request_context": async (args: any, context: McpContext) => {
                const filterResolution = await resolveFiltersWithSampling(args.filters, context);
                if (filterResolution.error) {
                    return { success: false, error: filterResolution.error };
                }

                if (!args.phase) {
                    return {
                        success: true,
                        data: {
                            sessionId: context.session_id,
                            agentId: context.user_id,
                            domain: context.domain,
                            role: context.role,
                            track: context.track,
                            selected_filters: filterResolution.selected,
                            timestamp: new Date().toISOString(),
                        }
                    };
                }

                if (context.track !== 'discovery') {
                    return { success: false, error: { code: 403, message: 'Phase context only available on discovery track' } };
                }

                const allowedPhases = new Set(['ideation', 'validation', 'iteration', 'delivery_handoff']);
                if (!allowedPhases.has(args.phase)) {
                    return { success: false, error: { code: 400, message: `Invalid phase: ${args.phase}` } };
                }

                assertValidPhase(args.phase);
                const phase = args.phase;
                const templatesDir = path.resolve(__dirname, '../../../database/roles/discovery/templates');
                const workflowPath = path.resolve(__dirname, '../../../database/roles/discovery/workflows/DWI.workflow.md');
                let workflowTemplate = '';
                try { workflowTemplate = fs.readFileSync(workflowPath, 'utf8'); } catch { }
                const artifactMap: Record<string, string[]> = {
                    ideation: ['ideation-artifact.md'],
                    validation: ['validation-report.md'],
                    iteration: ['refined-design.md'],
                    delivery_handoff: ['handoff-ticket.md']
                };
                const artifactTemplates: Array<{ name: string; content: string }> = [];
                for (const fname of artifactMap[phase] || []) {
                    try { artifactTemplates.push({ name: fname, content: fs.readFileSync(path.join(templatesDir, fname), 'utf8') }); } catch { }
                }
                const phaseChecklist = [`Complete all required steps in ${phase}`];
                return {
                    success: true,
                    data: {
                        workflow_template: workflowTemplate,
                        artifact_templates: artifactTemplates,
                        phase_checklist: phaseChecklist,
                        selected_filters: filterResolution.selected,
                        available_tools: []
                    }
                };
            },


            "lookup_context": async (args, context: McpContext) => {
                const targetSessionId = args.sessionId || context.session_id;
                return {
                    success: true,
                    data: {
                        sessionId: targetSessionId,
                        agentId: context.user_id,
                        domain: context.domain,
                        role: context.role,
                        track: context.track,
                        isCurrentSession: targetSessionId === context.session_id,
                        timestamp: new Date().toISOString(),
                    }
                };
            }
        }
    };
}
