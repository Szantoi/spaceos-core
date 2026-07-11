# LOAD-TEST-README.md

## Overview
This infrastructure allows for high-concurrency performance validation of the MCP server, specifically focusing on the `bootstrap_agent` service.

## Running Tests

### Standard Performance Suite
Runs the full suite including 10, 50, and 100 concurrent agent scenarios.
```bash
npm run test:load
```

### Individual Load Test Script
You can run the raw harness with custom parameters:
```bash
ts-node src/tests/e2e/load-test.ts [agents] [queries_per_agent]
# Example: 20 agents, 50 queries each
ts-node src/tests/e2e/load-test.ts 20 50
```

## Interpreting Results
- **Success Rate:** Total successful operations vs attempts.
- **Lock Timeouts:** Occurrences of `SQLITE_BUSY`. Should be < 5%.
- **Latency Percentiles (p95):** The key metric for performance health.

## Baseline Management
The CI/CD pipeline compares current results against `test-results/performance-baseline.json`.
To update the baseline after an intentional performance change:
```bash
cp test-results/performance-latest.json test-results/performance-baseline.json
```
Then commit the updated baseline file.
