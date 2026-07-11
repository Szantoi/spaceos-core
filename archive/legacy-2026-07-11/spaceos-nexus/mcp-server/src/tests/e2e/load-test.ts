import path from 'path';
import fs from 'fs';
import { performance } from 'perf_hooks';
import { AgentDb } from '../../mcp/AgentDb';
import { SessionManager } from '../../mcp/SessionManager';
import { BootstrapService } from '../../mcp/BootstrapService';
import { DatabaseConnectionManager } from '../../metadata/DatabaseConnectionManager';
import { PerformanceStats, LatencyReport } from './PerformanceStats';

/**
 * TASK-10-07: Performance Load Test Harness for bootstrap_agent
 *
 * This script simulates concurrent agents calling the bootstrap_agent service.
 * It measures latency percentiles, success rates, and lock timeouts.
 */

async function runLoadTest(concurrentAgents: number, queriesPerAgent: number) {
    const tmpDir = path.join(process.cwd(), '.test-temp');
    if (!fs.existsSync(tmpDir)) fs.mkdirSync(tmpDir, { recursive: true });

    const testDbPath = path.join(tmpDir, `load-test-bootstrap-${Date.now()}.db`);
    const connectionManager = new DatabaseConnectionManager(testDbPath);
    const agentDb = new AgentDb(connectionManager);
    const sessionManager = new SessionManager(testDbPath);
    const bootstrapService = new BootstrapService(agentDb, sessionManager);

    console.log(`\n🚀 Initializing Load Test: ${concurrentAgents} agents × ${queriesPerAgent} queries`);
    console.log(`📂 Database: ${testDbPath}`);

    // 1. Initialize Schema
    agentDb.initSchema();

    // 2. Seed Test Data
    const adminDb = connectionManager.getAdminPool();
    const now = new Date().toISOString();

    const roles = [
        ['engineering', 'backend_developer'],
        ['engineering', 'tech_lead'],
        ['management', 'pm'],
        ['discovery', 'explorer']
    ];

    for (const [domain, role] of roles) {
        adminDb.prepare('INSERT INTO roles (domain, role_name, content, created_at, last_updated) VALUES (?, ?, ?, ?, ?)')
            .run(domain, role, `# ${role} role content`, now, now);

        adminDb.prepare('INSERT INTO role_schemas (domain, role_name, mcp_tool_permissions, created_at, last_updated) VALUES (?, ?, ?, ?, ?)')
            .run(domain, role, JSON.stringify(['read_file', 'write_file']), now, now);

        adminDb.prepare('INSERT INTO runbooks (domain, role_name, content, created_at, last_updated) VALUES (?, ?, ?, ?, ?)')
            .run(domain, role, `Runbook for ${role}`, now, now);

        adminDb.prepare('INSERT INTO workflows (domain, role_name, workflow_type, content, created_at, last_updated) VALUES (?, ?, ?, ?, ?, ?)')
            .run(domain, role, 'default', `Workflow for ${role}`, now, now);

        adminDb.prepare('INSERT INTO templates (domain, role_name, template_name, content, created_at, last_updated) VALUES (?, ?, ?, ?, ?, ?)')
            .run(domain, role, 'summary', `Template for ${role}`, now, now);
    }

    console.log(`✅ Test data seeded (${roles.length} roles)`);

    // 3. Run Load
    const latencies: number[] = [];
    let lockTimeouts = 0;
    let errors = 0;
    const totalRequests = concurrentAgents * queriesPerAgent;

    const promises: Promise<void>[] = [];

    const startTime = performance.now();

    for (let i = 0; i < concurrentAgents; i++) {
        promises.push((async () => {
            for (let j = 0; j < queriesPerAgent; j++) {
                const [domain, role] = roles[j % roles.length];
                const queryStart = process.hrtime.bigint();

                try {
                    const result = await bootstrapService.bootstrap(domain, role, 'identify');
                    const queryEnd = process.hrtime.bigint();
                    const latencyMs = Number(queryEnd - queryStart) / 1_000_000;

                    if ('error' in result) {
                        errors++;
                        // Check if it's a lock timeout in the error message or code
                        if (JSON.stringify(result).includes('SQLITE_BUSY')) {
                            lockTimeouts++;
                        }
                    } else {
                        latencies.push(latencyMs);
                    }
                } catch (err: any) {
                    errors++;
                    if (err.code === 'SQLITE_BUSY' || err.message?.includes('timeout')) {
                        lockTimeouts++;
                    }
                }
            }
        })());
    }

    await Promise.all(promises);
    const totalDuration = performance.now() - startTime;

    // 4. Calculate Stats
    const report = PerformanceStats.calculate(latencies, lockTimeouts, totalRequests);

    // 5. Output Results
    console.log(`\n📊 LOAD TEST RESULTS (${concurrentAgents} agents, ${queriesPerAgent} ops/agent)`);
    console.log(`─────────────────────────────────────────────────────────────────────────────`);
    console.log(`Total Requests:  ${report.total}`);
    console.log(`Success Rate:    ${report.successRate.toFixed(2)}%`);
    console.log(`Error Count:     ${errors} (${report.errorRate.toFixed(2)}%)`);
    console.log(`Lock Timeouts:   ${report.lockTimeouts} (${report.lockTimeoutRate.toFixed(2)}%)`);
    console.log(`Total Duration:  ${(totalDuration / 1000).toFixed(2)}s`);
    console.log(`Throughput:      ${(report.total / (totalDuration / 1000)).toFixed(2)} req/s`);
    console.log(`─────────────────────────────────────────────────────────────────────────────`);
    console.log(`LATENCY (ms):`);
    console.log(`  P50: ${report.p50.toFixed(2)}ms`);
    console.log(`  P95: ${report.p95.toFixed(2)}ms  (Target: < 50ms)`);
    console.log(`  P99: ${report.p99.toFixed(2)}ms`);
    console.log(`  Min: ${report.min.toFixed(2)}ms | Max: ${report.max.toFixed(2)}ms | Mean: ${report.mean.toFixed(2)}ms`);
    console.log(`─────────────────────────────────────────────────────────────────────────────`);

    // 6. Cleanup
    connectionManager.close();
    if (fs.existsSync(testDbPath)) {
        try {
            fs.unlinkSync(testDbPath);
            fs.unlinkSync(`${testDbPath}-wal`);
            fs.unlinkSync(`${testDbPath}-shm`);
        } catch { }
    }

    return report;
}

// Support CLI execution
if (require.main === module) {
    const agents = parseInt(process.argv[2]) || 10;
    const ops = parseInt(process.argv[3]) || 50;
    runLoadTest(agents, ops).catch(err => {
        console.error('Fatal load test error:', err);
        process.exit(1);
    });
}

export { runLoadTest };
