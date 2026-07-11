---
id: MSG-NEXUS-007
from: root
to: nexus
type: task
priority: low
status: READ
model: haiku
created: 2026-07-10
content_hash: e0eacd2af5811c6280c587e7e7d90742c9071165d6b276a96f0f7e863ce14e7e
---

# Pipeline Monitoring Metrics — Prometheus/Grafana Integration

## Kontextus

Jelenleg a pipeline állapota csak log fájlokból deríthető ki. Proaktív monitoring hiányzik.

## Feladat

Implementálj egyszerű metrics endpoint-ot:

### 1. Metrics Endpoint
```
GET /api/metrics → Prometheus format

# HELP nightwatch_cycle_count Total Nightwatch cycles
# TYPE nightwatch_cycle_count counter
nightwatch_cycle_count 142

# HELP pipeline_task_processed Tasks processed by pipeline
# TYPE pipeline_task_processed counter
pipeline_task_processed{type="done"} 45
pipeline_task_processed{type="blocked"} 3

# HELP terminal_session_active Active terminal sessions
# TYPE terminal_session_active gauge
terminal_session_active{terminal="conductor"} 1
terminal_session_active{terminal="backend"} 0
```

### 2. Tracked Metrics
- Nightwatch cycle count
- Tasks processed (done/blocked)
- Terminal session state
- MCP tool call count
- Average response time

### 3. Implementation
- `src/pipeline/systemMetrics.ts` (már létezik, bővítendő)
- Prometheus-kompatibilis formátum

## Acceptance Criteria
- [ ] /api/metrics endpoint Prometheus formátumban
- [ ] Legalább 5 metric tracked
- [ ] Dokumentáció (METRICS_GUIDE.md)
- [ ] Grafana dashboard config (opcionális)

## Acceptance Criteria

- [ ] /api/metrics endpoint Prometheus formátumban
- [ ] Legalább 5 metric tracked
- [ ] Dokumentáció (METRICS_GUIDE.md)
