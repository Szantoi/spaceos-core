---
id: MSG-ARCHITECT-034
from: root
to: architect
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
content_hash: b9c23840f9be0f6fa9c978dfcfc11e2733558ce277597a56eaf5d420102094d3
---

# FSM Subscription System Design (MCP Event Pub/Sub)

Tervezd meg az FSM Subscription rendszert, ami lehetővé teszi terminálok számára, hogy feliratkozzanak state változásokra.

## Probléma

Jelenleg a terminálok csak polling-gal tudják ellenőrizni, hogy egy feladat DONE/BLOCKED lett-e. Nincs push notification.

## Követelmények

### MCP Toolok

```typescript
// Feliratkozás task-ra
subscribe_to_task(taskId: string, events: ('done' | 'blocked' | 'progress')[])

// Feliratkozás terminál eseményekre  
subscribe_to_terminal(terminal: string, events: ('inbox_new' | 'outbox_done')[])

// Leiratkozás
unsubscribe(subscriptionId: string)

// Aktív feliratkozások listázása
get_subscriptions(terminal: string)
```

### Notification Delivery

1. **SSE push** - aktív session-be real-time
2. **Telegram** - ha nincs aktív session
3. **Inbox üzenet** - fallback

## Elvárt Output

Készíts `docs/architecture/decisions/ADR-052-fsm-subscription-system.md`:

1. Subscription Registry adatstruktúra
2. Event Bus integráció (eventBus.ts már létezik)
3. MCP tool interface-ek
4. SSE endpoint design
5. Notification delivery prioritás

## Referencia

- `spaceos-nexus/knowledge-service/src/pipeline/eventBus.ts`
- `spaceos-nexus/knowledge-service/src/mcp.ts`
- `docs/memory/root.md` - koncepció leírás

## Acceptance Criteria

- [ ] ADR-052 dokumentáció elkészült
- [ ] MCP tool interface-ek definiálva
- [ ] Subscription Registry design
- [ ] SSE + Telegram + Inbox delivery terv
- [ ] Monitoring event types definiálva
- [ ] Auto-recovery config tervezve

---

## KITERJESZTÉS: Monitoring Automation (Root kérés 2026-06-30)

A terminál folyamat figyelés automatizálása a subscription rendszerbe integrálva.

### Extended Event Types

```typescript
// Monitoring events (a task/terminal events mellé)
type MonitoringEventType =
  | 'session_started'      // Terminál session indult
  | 'session_ended'        // Terminál session véget ért (normál)
  | 'session_stuck'        // >5 perc inaktivitás detektálva
  | 'session_crashed'      // Claude process crash
  | 'todo_progress'        // TodoWrite frissítés (in_progress → completed)
  | 'cost_alert'           // Budget küszöb átlépése
  | 'heartbeat_missed';    // MCP heartbeat hiányzik

interface MonitoringEvent {
  type: MonitoringEventType;
  terminal: string;
  timestamp: Date;
  details: {
    sessionId?: string;
    taskId?: string;
    stuckDuration?: number;      // másodpercben
    costSoFar?: number;          // USD
    budgetLimit?: number;
    todoStats?: { total: number; completed: number; inProgress: number };
  };
}
```

### Auto-Recovery Config

```typescript
interface AutoRecoveryConfig {
  terminal: string;
  enabled: boolean;
  rules: {
    onStuck: 'nudge' | 'restart' | 'escalate' | 'none';
    stuckThresholdMinutes: number;      // default: 5
    onCrash: 'restart' | 'escalate' | 'none';
    maxAutoRestarts: number;            // default: 3
    onCostAlert: 'pause' | 'warn' | 'none';
    costAlertThreshold: number;         // USD, default: 10
  };
}

// MCP tool
set_auto_recovery(terminal: string, config: AutoRecoveryConfig)
get_auto_recovery(terminal: string): AutoRecoveryConfig
```

### Dashboard Real-time Streaming

```typescript
// SSE endpoint a dashboard számára
GET /api/monitoring/stream
  → Server-Sent Events minden monitoring eventtel
  → Dashboard real-time frissül (WORKING/IDLE/STUCK/CRASHED)

// Szűrt stream egy terminálra
GET /api/monitoring/stream/:terminal
```

### Extended MCP Tools

```typescript
// Feliratkozás monitoring eseményekre
subscribe_to_monitoring(
  events: MonitoringEventType[],
  terminals?: string[]  // ha üres, minden terminálra
)

// Gyors feliratkozás minden terminál minden eseményére (Conductor/Root számára)
subscribe_to_all_terminals(
  events: ('done' | 'blocked' | 'stuck' | 'crashed' | 'cost_alert')[]
)

// Terminál státusz poll (ha SSE nem elérhető)
get_terminal_health(): {
  terminal: string;
  status: 'idle' | 'working' | 'stuck' | 'crashed';
  lastActivity: Date;
  currentTask?: string;
  sessionCost: number;
}[]
```

### Integráció a meglévő watcherekkel

A `nightwatch.sh` komponensek beépülnek a subscription rendszerbe:
- `watch-stuck.sh` → `session_stuck` event emit
- `watch-done.sh` → `done` event emit
- `watch-priority.sh` → `session_started` event emit

A pipeline átáll polling → push modellre ahol lehetséges.
