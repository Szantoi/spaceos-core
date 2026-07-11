/**
 * PerformanceStats.ts
 *
 * Utility class for calculating latency percentiles and aggregation metrics.
 * Used by load-test.ts to generate performance reports.
 */

export interface LatencyReport {
    total: number;
    successRate: number;
    errorRate: number;
    p50: number;
    p95: number;
    p99: number;
    min: number;
    max: number;
    mean: number;
    lockTimeouts: number;
    lockTimeoutRate: number;
}

export class PerformanceStats {
    /**
     * Calculate percentiles and summary metrics for a list of latencies.
     *
     * @param latencies - Array of latency values in milliseconds
     * @param lockTimeouts - Count of SQLITE_BUSY or lock timeout errors
     * @param totalRequests - Total count of attempted requests
     */
    public static calculate(latencies: number[], lockTimeouts: number, totalRequests: number): LatencyReport {
        if (latencies.length === 0) {
            return {
                total: totalRequests,
                successRate: 0,
                errorRate: 100,
                p50: 0, p95: 0, p99: 0,
                min: 0, max: 0, mean: 0,
                lockTimeouts: lockTimeouts,
                lockTimeoutRate: (lockTimeouts / totalRequests) * 100
            };
        }

        const sorted = [...latencies].sort((a, b) => a - b);
        const sum = sorted.reduce((a, b) => a + b, 0);

        const p50 = sorted[Math.floor(sorted.length * 0.50)];
        const p95 = sorted[Math.floor(sorted.length * 0.95)];
        const p99 = sorted[Math.floor(sorted.length * 0.99)];

        return {
            total: totalRequests,
            successRate: (latencies.length / totalRequests) * 100,
            errorRate: ((totalRequests - latencies.length) / totalRequests) * 100,
            p50,
            p95,
            p99,
            min: sorted[0],
            max: sorted[sorted.length - 1],
            mean: sum / latencies.length,
            lockTimeouts,
            lockTimeoutRate: (lockTimeouts / totalRequests) * 100
        };
    }
}
