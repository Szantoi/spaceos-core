# PERFORMANCE-SLA.md

## Latency Targets
Targets are measured at 50 concurrent agents with a baseline workload.

| Metric | Target | Description |
|:-------|:-------|:------------|
| p50 | < 20ms | Median response time |
| p95 | < 50ms | **Main SLA** (95th percentile) |
| p99 | < 100ms | 99th percentile |

## Availability & Reliability
- **Uptime:** 99.9% target.
- **Error Rate:** < 0.1% for standard operations.
- **Lock Timeout Rate:** < 5% under maximum concurrent load.

## Regression Threshold
- **Threshold:** 10% degradation against the latest locked baseline.
- **Action:** CI/CD will fail if p95 exceeds `baseline_p95 * 1.1`.

## Monitoring
Performance is automatically validated on every Pull Request and significant Push to the `main` branch.
Run `npm run test:performance` locally to verify targets before submission.
