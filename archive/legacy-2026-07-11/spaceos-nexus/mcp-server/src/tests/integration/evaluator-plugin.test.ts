import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createEvaluatorToolModule } from '../../mcp/tools/evaluator';
import { GuardrailService } from '../../roles/GuardrailService';
import { McpContext } from '../../mcp/middleware/contextMiddleware';

describe('Evaluator Plugin Integration', () => {
    let mockGuardrailService: any;
    let mockContext: McpContext;

    beforeEach(() => {
        mockGuardrailService = {
            checkCompliance: vi.fn(),
        };

        mockContext = {
            session_id: 'test-session-id',
            user_id: 'test-agent-id',
            domain: 'engineering',
            role: 'backend_developer',
            phase: 'delivery',
            track: 'delivery'
        };
    });

    it('should evaluate compliance using explicit domain/role', async () => {
        const complianceResult = {
            verdict: 'PASS',
            reasoning: 'Matches all rules'
        };
        mockGuardrailService.checkCompliance.mockResolvedValue(complianceResult);

        const evaluatorModule = createEvaluatorToolModule(mockGuardrailService as unknown as GuardrailService);
        const handler = evaluatorModule.handlers['evaluate_compliance'];

        const result = await handler({
            domain: 'security',
            role: 'auditor',
            prompt: 'Test prompt',
            response: 'Test response'
        }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data).toEqual(complianceResult);
        expect(mockGuardrailService.checkCompliance).toHaveBeenCalledWith(
            'security',
            'auditor',
            'Test prompt',
            'Test response'
        );
    });

    it('should evaluate compliance falling back to context domain/role', async () => {
        const complianceResult = {
            verdict: 'PASS',
            reasoning: 'Matches context rules'
        };
        mockGuardrailService.checkCompliance.mockResolvedValue(complianceResult);

        const evaluatorModule = createEvaluatorToolModule(mockGuardrailService as unknown as GuardrailService);
        const handler = evaluatorModule.handlers['evaluate_compliance'];

        // No domain or role in args
        const result = await handler({
            prompt: 'Context prompt',
            response: 'Context response'
        }, mockContext);

        expect(result.success).toBe(true);
        expect(result.data).toEqual(complianceResult);
        expect(mockGuardrailService.checkCompliance).toHaveBeenCalledWith(
            mockContext.domain,
            mockContext.role,
            'Context prompt',
            'Context response'
        );
    });

    it('should return error if missing required prompt or response', async () => {
        const evaluatorModule = createEvaluatorToolModule(mockGuardrailService as unknown as GuardrailService);
        const handler = evaluatorModule.handlers['evaluate_compliance'];

        const result = await handler({
            prompt: 'Only prompt'
            // Missing response
        }, mockContext);

        expect(result.isError).toBe(true);
        expect(result.error.message).toContain('Missing required parameters');
    });

    it('should handle GuardrailService internal failures', async () => {
        mockGuardrailService.checkCompliance.mockRejectedValue(new Error('LLM Error'));

        const evaluatorModule = createEvaluatorToolModule(mockGuardrailService as unknown as GuardrailService);
        const handler = evaluatorModule.handlers['evaluate_compliance'];

        const result = await handler({
            prompt: 'Trigger error',
            response: 'Error response'
        }, mockContext);

        expect(result.isError).toBe(true);
        expect(result.error.message).toContain('Guardrail evaluation failed: LLM Error');
    });
});
