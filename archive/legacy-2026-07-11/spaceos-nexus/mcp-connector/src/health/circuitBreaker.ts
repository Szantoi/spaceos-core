/**
 * Circuit Breaker - Prevents cascade failures
 */

export type CircuitState = 'closed' | 'open' | 'half-open';

interface CircuitBreakerOptions {
  failureThreshold: number;
  timeout: number;
  halfOpenAfter: number;
}

export class CircuitBreaker {
  private name: string;
  private failureThreshold: number;
  private timeout: number;
  private halfOpenAfter: number;

  private failureCount = 0;
  private state: CircuitState = 'closed';
  private nextAttempt?: number;
  private lastFailure?: Date;

  constructor(name: string, options: CircuitBreakerOptions) {
    this.name = name;
    this.failureThreshold = options.failureThreshold;
    this.timeout = options.timeout;
    this.halfOpenAfter = options.halfOpenAfter;
  }

  async execute<T>(fn: () => Promise<T>): Promise<T> {
    // Check if circuit is open
    if (this.state === 'open') {
      if (Date.now() < this.nextAttempt!) {
        throw new Error(`Circuit breaker OPEN for ${this.name} - requests blocked`);
      }

      // Time to try half-open
      this.state = 'half-open';
      console.log(`[CircuitBreaker] ${this.name}: OPEN → HALF-OPEN (attempting recovery)`);
    }

    try {
      const result = await fn();

      // Success - reset or close circuit
      if (this.state === 'half-open') {
        this.state = 'closed';
        this.failureCount = 0;
        console.log(`[CircuitBreaker] ${this.name}: HALF-OPEN → CLOSED (recovered)`);
      } else if (this.state === 'closed') {
        this.failureCount = 0; // Reset on success
      }

      return result;
    } catch (error) {
      this.recordFailure();
      throw error;
    }
  }

  private recordFailure(): void {
    this.failureCount++;
    this.lastFailure = new Date();

    if (this.state === 'half-open') {
      // Failed in half-open, go back to open
      this.state = 'open';
      this.nextAttempt = Date.now() + this.halfOpenAfter;
      console.log(`[CircuitBreaker] ${this.name}: HALF-OPEN → OPEN (recovery failed)`);
    } else if (this.failureCount >= this.failureThreshold) {
      // Threshold exceeded, open circuit
      this.state = 'open';
      this.nextAttempt = Date.now() + this.halfOpenAfter;
      console.error(`[CircuitBreaker] ${this.name}: CLOSED → OPEN (${this.failureCount} failures)`);
    }
  }

  getState(): CircuitState {
    // Update state if time for half-open
    if (this.state === 'open' && Date.now() >= this.nextAttempt!) {
      this.state = 'half-open';
    }
    return this.state;
  }

  getStats(): {
    state: CircuitState;
    failureCount: number;
    lastFailure: Date | undefined;
    nextAttempt: number | undefined;
  } {
    return {
      state: this.getState(),
      failureCount: this.failureCount,
      lastFailure: this.lastFailure,
      nextAttempt: this.nextAttempt,
    };
  }

  reset(): void {
    this.state = 'closed';
    this.failureCount = 0;
    this.nextAttempt = undefined;
    this.lastFailure = undefined;
    console.log(`[CircuitBreaker] ${this.name}: Manual reset to CLOSED`);
  }
}

/**
 * Circuit Breaker Registry - Manages circuit breakers per backend
 */
export class CircuitBreakerRegistry {
  private breakers = new Map<string, CircuitBreaker>();
  private options: CircuitBreakerOptions;

  constructor(options: CircuitBreakerOptions) {
    this.options = options;
  }

  getBreaker(name: string): CircuitBreaker {
    if (!this.breakers.has(name)) {
      this.breakers.set(name, new CircuitBreaker(name, this.options));
    }
    return this.breakers.get(name)!;
  }

  getAllStats(): Record<string, ReturnType<CircuitBreaker['getStats']>> {
    const stats: Record<string, ReturnType<CircuitBreaker['getStats']>> = {};
    for (const [name, breaker] of this.breakers) {
      stats[name] = breaker.getStats();
    }
    return stats;
  }

  resetAll(): void {
    for (const breaker of this.breakers.values()) {
      breaker.reset();
    }
  }
}
