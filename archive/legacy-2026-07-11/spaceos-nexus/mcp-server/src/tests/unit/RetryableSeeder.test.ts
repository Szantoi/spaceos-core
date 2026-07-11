import { describe, it, expect, beforeEach } from 'vitest';
import { RetryableSeeder } from '../../rag/RetryableSeeder';

/**
 * TASK-09-03B: RetryableSeeder — Unit Tests
 *
 * Tests:
 * 1. Exponential backoff calculation
 * 2. Retry on SQLITE_BUSY (succeeds after N retries)
 * 3. Throws after max retries exceeded
 * 4. Backoff increases exponentially
 * 5. Jitter prevents thundering herd
 */

describe('TASK-09-03B: RetryableSeeder (Exponential Backoff)', () => {
    let seeder: RetryableSeeder;

    beforeEach(() => {
        seeder = new RetryableSeeder({
            maxRetries: 3,
            initialDelayMs: 100,
            maxDelayMs: 8000,
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // EXPONENTIAL BACKOFF CALCULATION
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Exponential Backoff Formula', () => {
        it('should calculate backoff for attempt 1: [50, 100]ms', () => {
            const delays: number[] = [];

            for (let i = 0; i < 50; i++) {
                const backoff = seeder.calculateBackoffMs(1);
                delays.push(backoff);
            }

            const minDelay = Math.min(...delays);
            const maxDelay = Math.max(...delays);

            expect(minDelay).toBeGreaterThanOrEqual(50);
            expect(maxDelay).toBeLessThanOrEqual(100);
        });

        it('should calculate backoff for attempt 2: [100, 200]ms', () => {
            const delays: number[] = [];

            for (let i = 0; i < 50; i++) {
                const backoff = seeder.calculateBackoffMs(2);
                delays.push(backoff);
            }

            const minDelay = Math.min(...delays);
            const maxDelay = Math.max(...delays);

            expect(minDelay).toBeGreaterThanOrEqual(100);
            expect(maxDelay).toBeLessThanOrEqual(200);
        });

        it('should calculate backoff for attempt 3: [200, 400]ms', () => {
            const delays: number[] = [];

            for (let i = 0; i < 50; i++) {
                const backoff = seeder.calculateBackoffMs(3);
                delays.push(backoff);
            }

            const minDelay = Math.min(...delays);
            const maxDelay = Math.max(...delays);

            expect(minDelay).toBeGreaterThanOrEqual(200);
            expect(maxDelay).toBeLessThanOrEqual(400);
        });

        it('should cap backoff at maxDelayMs (8000)', () => {
            // Attempt 10 would be: 100 * 2^9 = 51200 (exceeds 8000)
            const backoff = seeder.calculateBackoffMs(10);

            expect(backoff).toBeGreaterThanOrEqual(4000); // cap/2 = 8000/2
            expect(backoff).toBeLessThanOrEqual(8000); // cap
        });

        it('should include jitter (randomness)', () => {
            // Calculate many times and verify we get varied results
            const delays = new Set<number>();

            for (let i = 0; i < 20; i++) {
                delays.add(seeder.calculateBackoffMs(2));
            }

            // With jitter, should get multiple different values
            expect(delays.size).toBeGreaterThan(1);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // RETRY ON TRANSIENT ERRORS
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Retry on SQLITE_BUSY', () => {
        it('should succeed immediately on first try (no error)', async () => {
            const workFn = async () => 'success';

            const { result, retryInfo } = await seeder.executeWithRetry(workFn, 'test operation');

            expect(result).toBe('success');
            expect(retryInfo.attempts).toBe(0); // No retries
            expect(retryInfo.success).toBe(true);
        });

        it('should retry and succeed after 1 failure', async () => {
            let attemptCount = 0;

            const workFn = async () => {
                attemptCount++;
                if (attemptCount === 1) {
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }
                return 'success';
            };

            const { result, retryInfo } = await seeder.executeWithRetry(workFn, 'test operation');

            expect(result).toBe('success');
            expect(retryInfo.attempts).toBe(1); // 1 retry
            expect(retryInfo.success).toBe(true);
        });

        it('should retry and succeed after 2 failures', async () => {
            let attemptCount = 0;

            const workFn = async () => {
                attemptCount++;
                if (attemptCount <= 2) {
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }
                return 'success';
            };

            const { result, retryInfo } = await seeder.executeWithRetry(workFn, 'test operation');

            expect(result).toBe('success');
            expect(retryInfo.attempts).toBe(2); // 2 retries
            expect(retryInfo.success).toBe(true);
        });

        it('should track total delay accumulated during retries', async () => {
            let attemptCount = 0;

            const workFn = async () => {
                attemptCount++;
                if (attemptCount <= 1) {
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }
                return 'success';
            };

            const { retryInfo } = await seeder.executeWithRetry(workFn, 'test operation');

            expect(retryInfo.totalDelayMs).toBeGreaterThanOrEqual(50); // Minimum backoff for attempt 1
            expect(retryInfo.totalDelayMs).toBeLessThanOrEqual(100); // Maximum backoff for attempt 1
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // THROW AFTER MAX RETRIES
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Throw After Max Retries', () => {
        it('should throw after 3 retries (4 total attempts) exhausted', async () => {
            let attemptCount = 0;

            const workFn = async () => {
                attemptCount++;
                const err = new Error('database is locked') as any;
                err.code = 'SQLITE_BUSY';
                throw err;
            };

            // Should throw after 3 retries (4 total attempts)
            await expect(seeder.executeWithRetry(workFn, 'test operation')).rejects.toThrow(
                'database is locked'
            );

            expect(attemptCount).toBe(4); // First attempt + 3 retries
        });

        it('should throw on non-retryable error immediately', async () => {
            const workFn = async () => {
                const err = new Error('syntax error') as any;
                err.code = 'SQLITE_SYNTAX';
                throw err;
            };

            // Should throw immediately (not retry-able)
            await expect(seeder.executeWithRetry(workFn, 'test operation')).rejects.toThrow('syntax error');
        });

        it('should throw on permission denied immediately', async () => {
            const workFn = async () => {
                throw new Error('PERMISSION DENIED');
            };

            // Should throw immediately
            await expect(seeder.executeWithRetry(workFn, 'test operation')).rejects.toThrow('PERMISSION DENIED');
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // SYNCHRONOUS VERSION (for bulk operations)
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Synchronous Retry (executeWithRetrSync)', () => {
        it('should work with synchronous functions', () => {
            let attemptCount = 0;

            const workFn = () => {
                attemptCount++;
                if (attemptCount === 1) {
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }
                return 'success';
            };

            const { result, retryInfo } = seeder.executeWithRetrSync(workFn, 'test operation');

            expect(result).toBe('success');
            expect(retryInfo.attempts).toBe(1);
        });

        it('should throw on exhausted retries (sync)', () => {
            const workFn = () => {
                const err = new Error('database is locked') as any;
                err.code = 'SQLITE_BUSY';
                throw err;
            };

            expect(() => seeder.executeWithRetrSync(workFn, 'test operation')).toThrow('database is locked');
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // CONFIGURATION & MONITORING
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Configuration & Monitoring', () => {
        it('should expose configuration via getConfig()', () => {
            const config = seeder.getConfig();

            expect(config).toEqual({
                maxRetries: 3,
                initialDelayMs: 100,
                maxDelayMs: 8000,
                backoffFormula: 'cap = min(maxDelayMs, initialDelayMs * 2^(attempt-1)); backoff = random(cap/2, cap)',
            });
        });

        it('should allow custom configuration', () => {
            const customSeeder = new RetryableSeeder({
                maxRetries: 5,
                initialDelayMs: 50,
                maxDelayMs: 4000,
            });

            const config = customSeeder.getConfig();

            expect(config.maxRetries).toBe(5);
            expect(config.initialDelayMs).toBe(50);
            expect(config.maxDelayMs).toBe(4000);
        });
    });

    // ═══════════════════════════════════════════════════════════════════════════
    // INTEGRATION: Simulated Database Insert with Retries
    // ═══════════════════════════════════════════════════════════════════════════

    describe('Integration: Simulated Database Insert', () => {
        it('should simulate insert that succeeds after lock contention', async () => {
            let attemptCount = 0;

            const insertFn = async () => {
                attemptCount++;

                if (attemptCount <= 2) {
                    // Simulate lock contention on first 2 attempts
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }

                // On 3rd attempt, insert succeeds
                return { id: 123, domain: 'engineering', role_name: 'backend_developer' };
            };

            const { result, retryInfo } = await seeder.executeWithRetry(insertFn, 'insert role');

            expect(result.id).toBe(123);
            expect(retryInfo.attempts).toBe(2); // 2 retries before success
            expect(retryInfo.success).toBe(true);
            expect(retryInfo.totalDelayMs).toBeGreaterThan(0); // Some delay accumulated
        });

        it('should accumulate delay for multiple retries', async () => {
            let attemptCount = 0;

            const insertFn = async () => {
                attemptCount++;

                if (attemptCount <= 3) {
                    const err = new Error('database is locked') as any;
                    err.code = 'SQLITE_BUSY';
                    throw err;
                }

                return { id: 456 };
            };

            const { retryInfo } = await seeder.executeWithRetry(insertFn, 'insert role');

            // With 3 retries: [50-100] + [100-200] + [200-400] = [350, 700]ms total
            expect(retryInfo.totalDelayMs).toBeGreaterThanOrEqual(200); // At least [50+100+200]
            expect(retryInfo.totalDelayMs).toBeLessThanOrEqual(700); // At most [100+200+400]
        });
    });
});
