import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import path from 'path';
import fs from 'fs';
import { performance } from 'perf_hooks';
import Database from 'better-sqlite3';

/**
 * TASK-09-04C: Quick Load Testing Harness
 *
 * Tests:
 * 1. 50 concurrent agents × 20 queries = 1000 total queries
 * 2. Measures p50/p95/p99 latency
 * 3. Validates < 5% lock timeout rate
 */

describe('Load Testing: SQLite Concurrency', () => {
    let db: Database.Database;
    let testDbPath: string;

    beforeAll(() => {
        // Create temp test database
        const tmpDir = path.join(process.cwd(), '.test-temp');
        if (!fs.existsSync(tmpDir)) fs.mkdirSync(tmpDir, { recursive: true });

        testDbPath = path.join(tmpDir, `load-test-${Date.now()}.db`);
        db = new Database(testDbPath);

        // Enable WAL + pragmas (from TASK-09-02B)
        db.pragma('journal_mode = WAL');
        db.pragma('foreign_keys = ON');
        db.pragma('wal_autocheckpoint = 1000');
        db.pragma('busy_timeout = 5000');

        // Create test table
        db.exec(`
      CREATE TABLE IF NOT EXISTS roles (
        id INTEGER PRIMARY KEY,
        domain TEXT NOT NULL,
        role_name TEXT NOT NULL,
        content TEXT,
        created_at TEXT,
        UNIQUE(domain, role_name)
      )
    `);

        // Seed test data: 6 roles
        const insertStmt = db.prepare(
            'INSERT OR REPLACE INTO roles (domain, role_name, content, created_at) VALUES (?, ?, ?, ?)'
        );
        const now = new Date().toISOString();

        const roles = [
            ['engineering', 'backend_developer', 'Backend role', now],
            ['engineering', 'frontend_developer', 'Frontend role', now],
            ['engineering', 'devops_engineer', 'DevOps role', now],
            ['management', 'pm', 'PM role', now],
            ['management', 'tech_lead', 'Tech lead role', now],
            ['discovery', 'researcher', 'Researcher role', now],
        ];

        for (const role of roles) {
            insertStmt.run(...role);
        }

        console.log(`\n✅ Test DB initialized: ${testDbPath}`);
    });

    afterAll(() => {
        db.close();
        if (fs.existsSync(testDbPath)) {
            fs.unlinkSync(testDbPath);
        }
        console.log(`✅ Test DB cleaned up\n`);
    });

    it('50 concurrent agents read roles (p95 < 50ms, p99 < 100ms)', async () => {
        const concurrentAgents = 50;
        const queriesPerAgent = 20;
        const latencies: number[] = [];
        let lockTimeouts = 0;

        console.log(`\n📊 Load Test: ${concurrentAgents} agents × ${queriesPerAgent} queries = ${concurrentAgents * queriesPerAgent} total`);

        const promises: Promise<void>[] = [];

        // Spawn concurrent agents
        for (let agentId = 0; agentId < concurrentAgents; agentId++) {
            promises.push((async () => {
                const stmt = db.prepare('SELECT id, domain, role_name FROM roles WHERE domain = ? LIMIT 1');

                const domains = ['engineering', 'management', 'discovery'];

                for (let queryId = 0; queryId < queriesPerAgent; queryId++) {
                    try {
                        const domain = domains[queryId % domains.length];
                        const queryStart = performance.now();
                        const result = stmt.get(domain);
                        const latency = performance.now() - queryStart;

                        latencies.push(latency);
                    } catch (err: any) {
                        if (err.code === 'SQLITE_BUSY' || err.message?.includes('timeout')) {
                            lockTimeouts++;
                        } else {
                            throw err;
                        }
                    }
                }
            })());
        }

        // Wait for all agents
        await Promise.all(promises);

        // Calculate percentiles
        latencies.sort((a, b) => a - b);
        const p50 = latencies[Math.floor(latencies.length * 0.50)];
        const p95 = latencies[Math.floor(latencies.length * 0.95)];
        const p99 = latencies[Math.floor(latencies.length * 0.99)];
        const max = latencies[latencies.length - 1];
        const mean = latencies.reduce((a, b) => a + b) / latencies.length;

        const successRate = ((latencies.length - lockTimeouts) / latencies.length) * 100;
        const timeoutRate = (lockTimeouts / latencies.length) * 100;

        // Log results
        console.log(`\n✓ Results:`);
        console.log(`  Total Queries: ${latencies.length}`);
        console.log(`  Success Rate: ${successRate.toFixed(2)}%`);
        console.log(`  Lock Timeouts: ${lockTimeouts} (${timeoutRate.toFixed(2)}%)`);
        console.log(`  P50: ${p50.toFixed(2)}ms | P95: ${p95.toFixed(2)}ms | P99: ${p99.toFixed(2)}ms`);
        console.log(`  Mean: ${mean.toFixed(2)}ms | Max: ${max.toFixed(2)}ms`);

        // Save results to file
        const resultFile = path.join(
            process.cwd(),
            `LOAD-TEST-RESULTS-${new Date().toISOString().split('T')[0]}.md`
        );

        const markdown = `# Load Test Results

**Date:** ${new Date().toISOString()}

## Summary

- **Concurrent Agents:** ${concurrentAgents}
- **Queries per Agent:** ${queriesPerAgent}
- **Total Queries:** ${latencies.length}
- **Success Rate:** ${successRate.toFixed(2)}%
- **Lock Timeouts:** ${lockTimeouts} (${timeoutRate.toFixed(2)}%)

## Latency Percentiles

| Metric | Value |
|:-------|:------|
| P50 | ${p50.toFixed(2)}ms |
| P95 | ${p95.toFixed(2)}ms |
| P99 | ${p99.toFixed(2)}ms |
| Mean | ${mean.toFixed(2)}ms |
| Max | ${max.toFixed(2)}ms |

## Baseline Targets

| Target | Value | Status |
|:-------|:------|:-------|
| P95 < 50ms | ${p95.toFixed(2)}ms | ${p95 < 50 ? '✅ PASS' : '❌ FAIL'} |
| P99 < 100ms | ${p99.toFixed(2)}ms | ${p99 < 100 ? '✅ PASS' : '❌ FAIL'} |
| Success Rate > 99% | ${successRate.toFixed(2)}% | ${successRate > 99 ? '✅ PASS' : '❌ FAIL'} |
| Lock Timeout Rate < 5% | ${timeoutRate.toFixed(2)}% | ${timeoutRate < 5 ? '✅ PASS' : '❌ FAIL'} |

---
Generated: ${new Date().toISOString()}
`;

        fs.writeFileSync(resultFile, markdown);
        console.log(`\n✅ Results saved: ${resultFile}\n`);

        // Assertions
        expect(p95).toBeLessThan(50);
        expect(p99).toBeLessThan(100);
        expect(successRate).toBeGreaterThan(99);
        expect(timeoutRate).toBeLessThan(5);
    });

    it('WAL checkpoint completes in < 500ms', () => {
        const checkpointStart = performance.now();
        const result = db.pragma('wal_checkpoint(FULL)') as unknown;
        const checkpoint = result as Array<{ busy: number; log: number; checkpointed: number }>;
        const checkpointDuration = performance.now() - checkpointStart;

        console.log(`\n⏱️  Checkpoint Duration: ${checkpointDuration.toFixed(2)}ms`);
        console.log(`   Metrics: busy=${checkpoint[0]?.busy}, log=${checkpoint[0]?.log}, checkpointed=${checkpoint[0]?.checkpointed}`);

        expect(checkpointDuration).toBeLessThan(500);
    });

    it('Seeder throughput > 100 inserts/sec', () => {
        const stmt = db.prepare(
            'INSERT OR REPLACE INTO roles (domain, role_name, content, created_at) VALUES (?, ?, ?, ?)'
        );

        const testRoles = 100;
        const now = new Date().toISOString();

        const seedStart = performance.now();
        for (let i = 0; i < testRoles; i++) {
            stmt.run('perf_test', `role_${i}`, `content ${i}`, now);
        }
        const seedDuration = performance.now() - seedStart;

        const throughput = (testRoles / seedDuration) * 1000;
        console.log(`\n📈 Seeder Throughput: ${throughput.toFixed(0)} inserts/sec (${seedDuration.toFixed(0)}ms for ${testRoles} rows)`);

        expect(throughput).toBeGreaterThan(100);
    });
});
