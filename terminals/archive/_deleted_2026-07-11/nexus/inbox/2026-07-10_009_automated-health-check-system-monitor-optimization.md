---
id: MSG-NEXUS-009
from: root
to: nexus
type: task
priority: medium
status: READ
model: sonnet
created: 2026-07-10
content_hash: a4e357b767b093848d2b7d98d13f5f50f0546cccfdba115cddb95d912a3a4b45
---

# Automated Health Check System — Monitor Optimization

## Kontextus

A Monitor terminál inbox-a tele van automatikusan generált health check üzenetekkel (nightwatch.sh 5-cycle interval). Ezek manuális feldolgozása pazarlás.

## Probléma

- Nightwatch minden 10 percben health check-et generál
- Monitor inbox-ba kerül, de senki nem dolgozza fel
- Backlog képződik (3+ UNREAD health check)

## Feladat

Implementálj automatikus health check rendszert:

### 1. Health Check Automation
```typescript
// watchMonitor.ts bővítés
async function processHealthCheck() {
  const status = await getTerminalStatusAggregate('alerts_only');
  
  if (status.healthScore < 50) {
    // Kritikus → Root inbox
    await createTask({ to: 'root', priority: 'high', ... });
  } else {
    // OK → Auto-complete, no manual intervention
    await completeInboxMessage(monitor, msgId, 'done', 'Auto-processed');
  }
}
```

### 2. Threshold-Based Alerting
| HealthScore | Action |
|-------------|--------|
| 80-100 | Auto-complete (no action) |
| 50-79 | Log warning, continue |
| 0-49 | Alert Root + Conductor |

### 3. Backlog Prevention
- Max 1 UNREAD health check at a time
- Auto-archive older health checks

## Érintett Fájlok
- `src/pipeline/watchMonitor.ts`
- `src/pipeline/nightwatch.ts`

## Acceptance Criteria
- [ ] Health checks auto-complete ha score > 80
- [ ] Kritikus alert Root-nak ha score < 50
- [ ] Max 1 UNREAD health check maintained
- [ ] Metrics: health check processed count

## Acceptance Criteria

- [ ] Health checks auto-complete ha score > 80
- [ ] Kritikus alert Root-nak ha score < 50
- [ ] Max 1 UNREAD health check maintained
- [ ] Metrics: health check processed count
