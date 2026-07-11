import { describe, it, expect } from 'vitest';
import { PerformanceStats } from './PerformanceStats';

describe('PerformanceStats Utility', () => {
    it('should calculate percentiles correctly for a simple array', () => {
        const latencies = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        const report = PerformanceStats.calculate(latencies, 0, 10);

        expect(report.total).toBe(10);
        expect(report.p50).toBe(60); // index 5
        expect(report.p95).toBe(100); // index 9
        expect(report.mean).toBe(55);
        expect(report.successRate).toBe(100);
    });

    it('should handle lock timeouts correctly', () => {
        const latencies = [10, 20];
        const report = PerformanceStats.calculate(latencies, 1, 3);

        expect(report.total).toBe(3);
        expect(report.lockTimeouts).toBe(1);
        expect(report.lockTimeoutRate).toBeCloseTo(33.33, 1);
        expect(report.successRate).toBeCloseTo(66.66, 1);
    });

    it('should handle empty latencies', () => {
        const report = PerformanceStats.calculate([], 5, 5);
        expect(report.successRate).toBe(0);
        expect(report.errorRate).toBe(100);
        expect(report.lockTimeouts).toBe(5);
    });
});
