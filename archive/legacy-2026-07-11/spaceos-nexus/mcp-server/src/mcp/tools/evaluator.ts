import { z } from 'zod';
import { IToolModule } from './IToolModule';
import { GuardrailService } from '../../roles/GuardrailService';
import { ErrorResponses } from '../ErrorResponses';
import { McpContext } from '../middleware/contextMiddleware';

/**
 * Creates the Evaluator tool module.
 * Grouping tools related to compliance and guardrail evaluation.
 */
export function createEvaluatorToolModule(guardrailService: GuardrailService): IToolModule {
    return {
        name: 'evaluator',
        version: '1.0.0',
        tools: [
            {
                name: 'evaluate_compliance',
                description: 'Evaluate an agent response against role-specific guardrails and limitations. Returns PASS/FAIL and reasoning.',
                inputSchema: z.object({
                    domain: z.string().describe('Optional: The domain of the role being evaluated. Falls back to current context if omitted.').optional(),
                    role: z.string().describe('Optional: The role name being evaluated. Falls back to current context if omitted.').optional(),
                    prompt: z.string().describe('The original task/prompt requested by the user'),
                    response: z.string().describe('The generated string response from the agent')
                })
            }
        ],
        handlers: {
            'evaluate_compliance': async (args: any, context: McpContext) => {
                const domain = args.domain || context.domain;
                const role = args.role || context.role;
                const { prompt, response } = args;

                if (!domain || !role || !prompt || !response) {
                    return ErrorResponses.badRequest('Missing required parameters for evaluate_compliance. Need domain, role, prompt, and response.');
                }

                try {
                    const result = await guardrailService.checkCompliance(domain, role, prompt, response);
                    return {
                        success: true,
                        data: result
                    };
                } catch (error: any) {
                    return ErrorResponses.internalError(`Guardrail evaluation failed: ${error.message}`);
                }
            }
        }
    };
}
