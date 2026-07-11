import { describe, expect, test } from 'vitest';
import { SamplingService } from '../../mcp/sampling/SamplingService';

describe('TASK-14-09 SamplingService', () => {
    test('AC-1/AC-2: request queues and resolves with selected options', async () => {
        const service = new SamplingService(5000);
        const pendingPromise = service.requestSampling('session-1', {
            prompt: 'Which filters did you mean?',
            options: [
                { label: 'by-role', value: 'role' },
                { label: 'by-phase', value: 'phase' }
            ]
        });

        const pending = service.listPending('session-1');
        expect(pending.length).toBe(1);
        expect(pending[0].prompt).toContain('Which filters');

        const accepted = service.resolveSampling(pending[0].requestId, ['role']);
        expect(accepted).toBe(true);

        const result = await pendingPromise;
        expect(result.selected).toEqual(['role']);
        expect(result.error).toBeUndefined();
    });

    test('AC-3/AC-4: timeout returns needs clarification error', async () => {
        const service = new SamplingService(20);
        const result = await service.requestSampling('session-timeout', {
            prompt: 'Need clarification',
            options: [{ label: 'role', value: 'role' }]
        });

        expect(result.selected).toEqual([]);
        expect(result.needsClarification).toBe(true);
        expect(result.error).toContain('timed out');
    });

    test('resolveSampling returns false for unknown request id', () => {
        const service = new SamplingService();
        const resolved = service.resolveSampling('missing-id', ['role']);
        expect(resolved).toBe(false);
    });
});
