import { describe, it, expect, beforeEach, vi, Mock } from 'vitest';
import { AuditLogger } from '../../metadata/auditLogger';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';

// We need to test setImmediate behavior, so we can mock it or use fake timers
vi.useFakeTimers();

describe('AuditLogger', () => {
    let connectionManager: DatabaseConnectionManager;
    let logger: AuditLogger;
    let mockRun: Mock;

    beforeEach(() => {
        mockRun = vi.fn();
        const mockDb = {
            prepare: vi.fn().mockReturnValue({ run: mockRun })
        };

        connectionManager = {
            getAdminPool: vi.fn().mockReturnValue(mockDb)
        } as unknown as DatabaseConnectionManager;

        logger = new AuditLogger(connectionManager);
    });

    it('should log audit entry asynchronously', async () => {
        const logParams = {
            session_id: 'test-session',
            domain: 'test-domain',
            role: 'test-role',
            tool_name: 'test-tool',
            input: { key: 'value' },
            output: { result: 'success' },
            latency_ms: 42,
            status_code: 'SUCCESS'
        };

        // Call the log method
        logger.log(logParams);

        // At this point, the query shouldn't have run yet because it uses setImmediate
        expect(mockRun).not.toHaveBeenCalled();

        // Fast-forward timers to pop the setImmediate queue
        vi.runAllTimers();

        expect(connectionManager.getAdminPool).toHaveBeenCalled();
        expect(mockRun).toHaveBeenCalledTimes(1);

        // the arguments passed to run() should be 8 items
        const args = mockRun.mock.calls[0];
        expect(args).toHaveLength(8);
        expect(args[0]).toBe('test-session');
        expect(args[1]).toBe('test-domain');
        expect(args[2]).toBe('test-role');
        expect(args[3]).toBe('test-tool');
        // args[4] and args[5] are hashes, just ensure they are strings
        expect(typeof args[4]).toBe('string');
        expect(typeof args[5]).toBe('string');
        expect(args[6]).toBe(42);
        expect(args[7]).toBe('SUCCESS');
    });

    it('should handle undefined input/output safely', async () => {
        logger.log({
            session_id: 'session2',
            domain: 'domain2',
            role: 'role2',
            tool_name: 'tool2',
            input: undefined,
            output: null,
            latency_ms: 10,
            status_code: 'ERROR'
        });

        vi.runAllTimers();

        expect(mockRun).toHaveBeenCalledTimes(1);
        const args = mockRun.mock.calls[0];
        expect(args[0]).toBe('session2');
        expect(args[4]).toBe(''); // input empty hash
        expect(args[5]).toBe(''); // output empty hash
    });

    it('should not crash if database throws an error', async () => {
        const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => { });

        const mockDb = {
            prepare: vi.fn().mockImplementation(() => {
                throw new Error('Database disconnected');
            })
        };
        vi.mocked(connectionManager.getAdminPool).mockReturnValue(mockDb as any);

        logger.log({
            session_id: 'err-session',
            domain: 'err-domain',
            role: 'err-role',
            tool_name: 'err-tool',
            input: {},
            output: {},
            latency_ms: 1,
            status_code: 'FAIL'
        });

        // Shouldn't throw an unhandled exception
        expect(() => vi.runAllTimers()).not.toThrow();

        // Console error should have been logged
        expect(consoleSpy).toHaveBeenCalledWith(
            '[AuditLogger] ❌ Failed to write to audit_log:',
            expect.any(Error)
        );

        consoleSpy.mockRestore();
    });

    it('should meet performance constraints (<10ms audit write overhead)', async () => {
        // Measure the time it takes to just queue the log (the immediate overhead)
        const logParams = {
            session_id: 'perf-session',
            domain: 'perf-domain',
            role: 'perf-role',
            tool_name: 'perf-tool',
            input: { complex: 'object', with: { nested: 'data' } },
            output: { result: 'success', status: 200 },
            latency_ms: 100,
            status_code: 'SUCCESS'
        };

        const start = performance.now();
        // Run it 1000 times to get a measurable average
        for (let i = 0; i < 1000; i++) {
            logger.log(logParams);
        }
        const end = performance.now();

        const avgMs = (end - start) / 1000;

        // Assert it takes less than 10ms (should be well under 1ms since it's just JSON.parse/stringify and setImmediate)
        expect(avgMs).toBeLessThan(10);

        // Let timers clear out
        vi.runAllTimers();
    });
});
