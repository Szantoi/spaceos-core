/**
 * TASK-09-03B: RetryableSeeder — Exponential Backoff + Retry Strategy
 *
 * Wraps database operations with exponential backoff retry logic to handle
 * transient SQLITE_BUSY errors during concurrent loads.
 *
 * **Strategy:**
 * - Max retries: 3 attempts
 * - Backoff formula: cap = min(8000, 100 * 2^(attempt-1))
 *                   backoff = cap/2 + random(0, cap/2) = [cap/2, cap]
 * - Jitter: Randomized to prevent thundering herd
 * - Max delay: 8 seconds total
 *
 * **Errors Handled:**
 * - SQLITE_BUSY: Database locked (WAL contention)
 * - SQLITE_IOERR: I/O errors during checkpoint
 * - Custom retry-able errors
 *
 * **Usage:**
 * ```typescript
 * const seeder = new RetryableSeeder();
 * await seeder.executeWithRetry(async () => {
 *   stmt.run(domain, role_name, content, version, now, now);
 * });
 * ```
 */

interface RetryOptions {
    maxRetries?: number;
    initialDelayMs?: number;
    maxDelayMs?: number;
}

interface RetryResult {
    attempts: number;
    totalDelayMs: number;
    success: boolean;
}

export class RetryableSeeder {
    private maxRetries: number = 3;
    private initialDelayMs: number = 100;
    private maxDelayMs: number = 8000;

    constructor(options?: RetryOptions) {
        if (options?.maxRetries) this.maxRetries = options.maxRetries;
        if (options?.initialDelayMs) this.initialDelayMs = options.initialDelayMs;
        if (options?.maxDelayMs) this.maxDelayMs = options.maxDelayMs;
    }

    /**
     * Calculate exponential backoff with jitter
     *
     * Formula:
     * - cap = min(maxDelayMs, initialDelayMs * 2^(attempt-1))
     * - backoff = random(cap/2, cap)
     *
     * Examples:
     * - Attempt 1: cap = 100, backoff = [50, 100]ms
     * - Attempt 2: cap = 200, backoff = [100, 200]ms
     * - Attempt 3: cap = 400, backoff = [200, 400]ms
     */
    public calculateBackoffMs(attempt: number): number {
        // Cap grows exponentially: 100, 200, 400, 800, ...
        const cap = Math.min(this.maxDelayMs, this.initialDelayMs * Math.pow(2, attempt - 1));

        // Jitter: backoff = random(cap/2, cap)
        const minDelay = cap / 2;
        const maxDelay = cap;
        const backoff = minDelay + Math.random() * (maxDelay - minDelay);

        return Math.round(backoff);
    }

    /**
     * Execute function with exponential backoff retry on failure
     *
     * @param fn Async function to execute
     * @param operationName Optional name for logging
     * @returns RetryResult with attempt count and total delay
     * @throws Error if all retries exhausted
     */
    public async executeWithRetry<T>(
        fn: () => Promise<T> | T,
        operationName: string = 'database operation'
    ): Promise<{ result: T; retryInfo: RetryResult }> {
        let lastError: Error | null = null;
        let totalDelayMs = 0;

        for (let attempt = 1; attempt <= this.maxRetries + 1; attempt++) {
            try {
                // Attempt execution
                const result = await fn();

                // Success
                if (attempt > 1) {
                    console.info(
                        `[RetryableSeeder] ✓ ${operationName} succeeded after ${attempt - 1} retries (total delay: ${totalDelayMs}ms)`
                    );
                }

                return {
                    result,
                    retryInfo: {
                        attempts: attempt - 1, // Number of retries (not including first attempt)
                        totalDelayMs,
                        success: true,
                    },
                };
            } catch (error) {
                lastError = error as Error;
                const err = error as any;

                // Check if error is retry-able
                const isRetryable =
                    err.code === 'SQLITE_BUSY' ||
                    err.code === 'SQLITE_IOERR' ||
                    err.message?.includes('database is locked') ||
                    err.message?.includes('timeout');

                // If not retry-able or all retries exhausted, throw
                if (!isRetryable || attempt > this.maxRetries) {
                    console.error(
                        `[RetryableSeeder] ✗ ${operationName} failed after ${attempt - 1} retries (${totalDelayMs}ms delay): ${err.message}`
                    );
                    throw error;
                }

                // Calculate backoff for next attempt
                const backoffMs = this.calculateBackoffMs(attempt);
                totalDelayMs += backoffMs;

                console.warn(
                    `[RetryableSeeder] ⚠ Attempt ${attempt} failed: ${err.code || err.message}. Retrying in ${backoffMs}ms... (total delay: ${totalDelayMs}ms)`
                );

                // Wait before retry
                await new Promise((resolve) => setTimeout(resolve, backoffMs));
            }
        }

        // Should not reach here, but just in case
        throw new Error(
            `${operationName} failed after ${this.maxRetries} retries: ${lastError?.message || 'Unknown error'}`
        );
    }

    /**
     * Synchronous version for bulk operations
     * Wraps sync function with retry logic
     */
    public executeWithRetrSync<T>(
        fn: () => T,
        operationName: string = 'database operation'
    ): { result: T; retryInfo: RetryResult } {
        let lastError: Error | null = null;
        let totalDelayMs = 0;

        for (let attempt = 1; attempt <= this.maxRetries + 1; attempt++) {
            try {
                const result = fn();

                if (attempt > 1) {
                    console.info(
                        `[RetryableSeeder] ✓ ${operationName} succeeded after ${attempt - 1} retries (total delay: ${totalDelayMs}ms)`
                    );
                }

                return {
                    result,
                    retryInfo: {
                        attempts: attempt - 1,
                        totalDelayMs,
                        success: true,
                    },
                };
            } catch (error) {
                lastError = error as Error;
                const err = error as any;

                const isRetryable =
                    err.code === 'SQLITE_BUSY' ||
                    err.code === 'SQLITE_IOERR' ||
                    err.message?.includes('database is locked') ||
                    err.message?.includes('timeout');

                if (!isRetryable || attempt > this.maxRetries) {
                    console.error(
                        `[RetryableSeeder] ✗ ${operationName} failed after ${attempt - 1} retries: ${err.message}`
                    );
                    throw error;
                }

                const backoffMs = this.calculateBackoffMs(attempt);
                totalDelayMs += backoffMs;

                console.warn(
                    `[RetryableSeeder] ⚠ Attempt ${attempt} failed. Retrying in ${backoffMs}ms...`
                );

                // Synchronous sleep (blocking wait)
                const start = Date.now();
                while (Date.now() - start < backoffMs) {
                    // Busy-wait
                }
            }
        }

        throw new Error(
            `${operationName} failed after ${this.maxRetries} retries: ${lastError?.message}`
        );
    }

    /**
     * Get retry statistics (for monitoring)
     */
    public getConfig() {
        return {
            maxRetries: this.maxRetries,
            initialDelayMs: this.initialDelayMs,
            maxDelayMs: this.maxDelayMs,
            backoffFormula: 'cap = min(maxDelayMs, initialDelayMs * 2^(attempt-1)); backoff = random(cap/2, cap)',
        };
    }
}
