import { describe, it, expect } from 'vitest';
import { runLoadTest } from './load-test';
import fs from 'fs';
import path from 'path';

/**
 * TASK-10-07: Performance & Latency Validation
 */
describe('Performance & Load Tests', () => {
    it('SLA Validation: 50 concurrent agents (p95 < 50ms)', async () => {
        const result = await runLoadTest(50, 20); // 1000 total queries

        console.log(`\nSLA Test Result (50 agents): p95 = ${result.p95.toFixed(2)}ms`);

        expect(result.successRate).toBeGreaterThan(99);
        expect(result.p95).toBeLessThan(50);
        expect(result.p99).toBeLessThan(100);
        expect(result.lockTimeoutRate).toBeLessThan(5);

        // Save latest result for regression comparison
        const latestPath = path.join(process.cwd(), 'test-results', 'performance-latest.json');
        if (!fs.existsSync(path.dirname(latestPath))) {
            fs.mkdirSync(path.dirname(latestPath), { recursive: true });
        }

        const snapshot = {
            timestamp: new Date().toISOString(),
            concurrency: 50,
            total_queries: result.total,
            success_rate: `${result.successRate.toFixed(2)}%`,
            percentiles: {
                p50_ms: result.p50,
                p95_ms: result.p95,
                p99_ms: result.p99,
                min_ms: result.min,
                max_ms: result.max,
                mean_ms: result.mean
            },
            lock_timeouts: result.lockTimeouts
        };

        fs.writeFileSync(latestPath, JSON.stringify(snapshot, null, 2));
    }, 60000); // 60s timeout

    it('Stress Test: 100 concurrent agents (p95 < 60ms)', async () => {
        const result = await runLoadTest(100, 10); // 1000 total queries

        console.log(`\nStress Test Result (100 agents): p95 = ${result.p95.toFixed(2)}ms`);

        expect(result.successRate).toBeGreaterThan(99);
        expect(result.p95).toBeLessThan(60);
    }, 60000);

    it('Quick Load Test: 10 concurrent agents (p95 < 30ms)', async () => {
        const result = await runLoadTest(10, 10);
        expect(result.p95).toBeLessThan(30);
    });
});
