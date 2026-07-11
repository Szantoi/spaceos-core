# ADR-052: FSM Subscription System (MCP Event Pub/Sub)

**Státusz:** IMPLEMENTÁCIÓRA KÉSZ
**Dátum:** 2026-06-30
**Döntéshozó:** Architect
**Kontextus:** Real-time push notification rendszer terminálok számára

---

## Kontextus

### Jelenlegi helyzet: Polling-based Workflow

Jelenleg a terminálok polling-gal ellenőrzik a feladatok állapotát:

```typescript
// Terminal waiting for task completion
while (!isDone) {
  const status = await checkTaskStatus(taskId);
  if (status === 'done' || status === 'blocked') break;
  await sleep(5000); // 5 másodperces polling
}
```

**Problémák:**
- **Latency:** 5 másodperc átlagos késleltetés
- **Resource waste:** Felesleges API hívások (üres válaszok)
- **Nem skálázódik:** 10 terminál × 12 poll/perc = 120 felesleges request/perc

### Cél: Push-based Notification

Terminálok feliratkoznak eseményekre, és push notification-t kapnak amikor az esemény bekövetkezik:

```typescript
// Subscribe to task completion
await subscribe_to_task(taskId, ['done', 'blocked']);

// Wait for notification (SSE, Telegram, or Inbox)
// No polling needed!
```

---

## Döntés

### 1. Subscription Registry

**Adatstruktúra:**

```typescript
interface Subscription {
  id: string;                    // Unique subscription ID (UUID)
  terminal: string;              // Who subscribed (architect, backend, etc.)
  type: 'task' | 'terminal';     // Subscription type
  target: string;                // Task ID or terminal name
  events: EventType[];           // Which events to listen for
  deliveryMethod: 'sse' | 'telegram' | 'inbox';  // Preferred delivery
  createdAt: string;             // ISO timestamp
  expiresAt?: string;            // Optional expiration
}

type EventType =
  // Task events
  | 'task:done'
  | 'task:blocked'
  | 'task:progress'
  // Terminal events
  | 'terminal:inbox_new'
  | 'terminal:outbox_done'
  | 'terminal:session_started'
  | 'terminal:session_ended';

// Global registry
const subscriptionRegistry = new Map<string, Subscription>();
```

**Storage:**
- In-memory Map (reset on service restart)
- Persistent storage (SQLite) for critical subscriptions (Phase 2)

### 2. MCP Tool Interface-ek

**Tool 1: subscribe_to_task**

```typescript
{
  name: 'subscribe_to_task',
  description: 'Subscribe to task state changes (done, blocked, progress)',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: {
        type: 'string',
        description: 'Terminal name (e.g., architect, backend)',
      },
      task_id: {
        type: 'string',
        description: 'Task message ID (e.g., MSG-BACKEND-045)',
      },
      events: {
        type: 'array',
        items: {
          type: 'string',
          enum: ['done', 'blocked', 'progress'],
        },
        description: 'Events to subscribe to',
      },
      delivery_method: {
        type: 'string',
        enum: ['sse', 'telegram', 'inbox', 'auto'],
        description: 'Preferred delivery method (default: auto)',
      },
      expires_in: {
        type: 'number',
        description: 'Expiration in seconds (default: 3600, max: 86400)',
      },
    },
    required: ['terminal', 'task_id', 'events'],
  },
}
```

**Tool 2: subscribe_to_terminal**

```typescript
{
  name: 'subscribe_to_terminal',
  description: 'Subscribe to terminal events (inbox, outbox, session)',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: {
        type: 'string',
        description: 'Subscribing terminal name',
      },
      target_terminal: {
        type: 'string',
        description: 'Terminal to watch',
      },
      events: {
        type: 'array',
        items: {
          type: 'string',
          enum: ['inbox_new', 'outbox_done', 'session_started', 'session_ended'],
        },
      },
      delivery_method: {
        type: 'string',
        enum: ['sse', 'telegram', 'inbox', 'auto'],
      },
    },
    required: ['terminal', 'target_terminal', 'events'],
  },
}
```

**Tool 3: unsubscribe**

```typescript
{
  name: 'unsubscribe',
  description: 'Unsubscribe from events by subscription ID',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: {
        type: 'string',
        description: 'Terminal name (for auth)',
      },
      subscription_id: {
        type: 'string',
        description: 'Subscription ID to cancel',
      },
    },
    required: ['terminal', 'subscription_id'],
  },
}
```

**Tool 4: get_subscriptions**

```typescript
{
  name: 'get_subscriptions',
  description: 'List active subscriptions for a terminal',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: {
        type: 'string',
        description: 'Terminal name',
      },
      include_expired: {
        type: 'boolean',
        description: 'Include expired subscriptions (default: false)',
      },
    },
    required: ['terminal'],
  },
}
```

### 3. Event Bus Integration

**Kapcsolódás a meglévő eventBus.ts-hez:**

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/subscriptionManager.ts

import { pipelineEvents } from './eventBus';

// Listen to all pipeline events
pipelineEvents.onAny((event) => {
  // Check if any subscription matches this event
  const matchingSubscriptions = findMatchingSubscriptions(event);

  // Deliver notifications
  for (const subscription of matchingSubscriptions) {
    deliverNotification(subscription, event);
  }
});

function findMatchingSubscriptions(event: PipelineEvent): Subscription[] {
  const matches: Subscription[] = [];

  for (const subscription of subscriptionRegistry.values()) {
    if (isMatch(subscription, event)) {
      matches.push(subscription);
    }
  }

  return matches;
}

function isMatch(subscription: Subscription, event: PipelineEvent): boolean {
  // Task subscription
  if (subscription.type === 'task') {
    if (event.messageId !== subscription.target) return false;

    const eventMap: Record<string, string[]> = {
      'done': ['outbox:done'],
      'blocked': ['outbox:blocked'],
      'progress': ['inbox:read'],
    };

    return subscription.events.some(subEvent =>
      eventMap[subEvent]?.includes(event.type)
    );
  }

  // Terminal subscription
  if (subscription.type === 'terminal') {
    if (event.terminal !== subscription.target) return false;

    const eventMap: Record<string, string[]> = {
      'inbox_new': ['inbox:new'],
      'outbox_done': ['outbox:done'],
      'session_started': ['session:started'],
      'session_ended': ['session:ended'],
    };

    return subscription.events.some(subEvent =>
      eventMap[subEvent]?.includes(event.type)
    );
  }

  return false;
}
```

### 4. SSE Endpoint Design

**Endpoint:** `GET /api/subscriptions/events`

```typescript
// spaceos-nexus/knowledge-service/src/routes/subscriptionRoutes.ts

import { Router } from 'express';

const router = Router();
const sseClients = new Map<string, Set<Response>>();  // terminal -> clients

router.get('/events', (req, res) => {
  const terminal = req.query.terminal as string;

  if (!terminal) {
    return res.status(400).json({ error: 'terminal parameter required' });
  }

  // SSE headers
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  // Register client
  if (!sseClients.has(terminal)) {
    sseClients.set(terminal, new Set());
  }
  sseClients.get(terminal)!.add(res);

  console.log(`[SSE] ${terminal} connected (total: ${sseClients.get(terminal)!.size})`);

  // Send initial subscriptions list
  const subscriptions = Array.from(subscriptionRegistry.values())
    .filter(sub => sub.terminal === terminal);

  res.write(`data: ${JSON.stringify({
    type: 'init',
    subscriptions,
  })}\n\n`);

  // Cleanup on disconnect
  req.on('close', () => {
    const clients = sseClients.get(terminal);
    if (clients) {
      clients.delete(res);
      if (clients.size === 0) {
        sseClients.delete(terminal);
      }
    }
    console.log(`[SSE] ${terminal} disconnected`);
  });
});

export function broadcastToTerminal(terminal: string, data: unknown): void {
  const clients = sseClients.get(terminal);
  if (!clients) return;

  const message = `data: ${JSON.stringify(data)}\n\n`;

  clients.forEach(client => {
    try {
      client.write(message);
    } catch (err) {
      clients.delete(client);
    }
  });
}

export default router;
```

**Client-side usage:**

```typescript
// Terminal session connects to SSE
const eventSource = new EventSource('/api/subscriptions/events?terminal=architect');

eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);

  if (data.type === 'task:done') {
    console.log(`Task ${data.taskId} completed!`);
    // Continue work...
  }
};
```

### 5. Notification Delivery Prioritás

**Auto-detect delivery method:**

```typescript
async function deliverNotification(subscription: Subscription, event: PipelineEvent): Promise<void> {
  const { terminal, deliveryMethod } = subscription;

  // 1. SSE (if active session)
  if (deliveryMethod === 'auto' || deliveryMethod === 'sse') {
    const sseDelivered = tryDeliverSSE(terminal, event);
    if (sseDelivered) {
      logDelivery(subscription.id, 'sse', 'success');
      return;
    }
  }

  // 2. Telegram (if no active session)
  if (deliveryMethod === 'auto' || deliveryMethod === 'telegram') {
    const hasSession = await checkActiveSession(terminal);
    if (!hasSession) {
      await deliverTelegram(terminal, event);
      logDelivery(subscription.id, 'telegram', 'success');
      return;
    }
  }

  // 3. Inbox fallback
  await deliverInbox(terminal, event);
  logDelivery(subscription.id, 'inbox', 'success');
}

function tryDeliverSSE(terminal: string, event: PipelineEvent): boolean {
  const clients = sseClients.get(terminal);
  if (!clients || clients.size === 0) return false;

  broadcastToTerminal(terminal, {
    type: 'notification',
    event,
    timestamp: new Date().toISOString(),
  });

  return true;
}

async function deliverTelegram(terminal: string, event: PipelineEvent): Promise<void> {
  const message = formatEventForTelegram(event);
  await sendTelegramMessage(terminal, message);
}

async function deliverInbox(terminal: string, event: PipelineEvent): Promise<void> {
  const messageContent = formatEventForInbox(event);

  await createTask({
    from: 'nexus',
    to: terminal,
    title: `Notification: ${event.type}`,
    description: messageContent,
    priority: 'medium',
  });
}
```

**Delivery priority matrix:**

| Scenario | Method | Latency | Reliability |
|----------|--------|---------|-------------|
| Active session (SSE open) | **SSE** | <100ms | High |
| No session, Telegram registered | **Telegram** | 1-2s | Medium |
| No session, no Telegram | **Inbox** | N/A (async) | High |
| SSE failed | Fallback to Telegram/Inbox | - | High |

---

## Indoklás

### Miért Event Bus + Subscription?

| Alternatíva | Probléma |
|-------------|----------|
| **Polling** | Latency, resource waste, nem skálázódik |
| **Direct callbacks** | Tight coupling, nehéz debuggolni |
| **Queue (RabbitMQ)** | Újabb dependency, komplexitás |
| **Event Bus + Subscription** | ✅ Loose coupling, skálázódik, simple |

### Miért SSE (nem WebSocket)?

| Szempont | SSE | WebSocket |
|----------|-----|-----------|
| **Bi-directional** | Nem (server → client) | Igen |
| **Complexity** | Egyszerű (HTTP) | Komplexebb (upgrade handshake) |
| **Use case** | ✅ One-way push (notification) | Bi-directional chat |
| **Auto-reconnect** | ✅ Beépített | Manual |
| **Firewall** | ✅ HTTP port | Gyakran blokkolva |

**Következtetés:** SSE tökéletes erre a use case-re (server push notification).

### Miért In-Memory Registry? (Phase 1)

| Szempont | In-Memory | SQLite Persistent |
|----------|-----------|-------------------|
| **Latency** | <1ms | 5-10ms |
| **Setup** | Zero | Schema + migrations |
| **Restart behavior** | ❌ Lost | ✅ Persistent |
| **Phase 1 elég?** | ✅ Igen (rövid sessionök) | Phase 2 upgrade |

**Következtetés:** Phase 1-ben in-memory elég. Phase 2-ben SQLite upgrade long-running subscriptions számára.

---

## Következmények

### Pozitív

- **90% latency csökkenés:** 5000ms polling → <100ms SSE push
- **Resource savings:** 120 felesleges poll/perc → 0
- **Skálázódik:** Több terminál, több task, ugyanaz a pattern
- **3-tier fallback:** SSE → Telegram → Inbox (mindig van delivery)
- **Event-driven:** Loose coupling, könnyű új event típus hozzáadása

### Negatív

- **SSE connection management:** Reconnect logic client-side
- **In-memory registry:** Service restart → lost subscriptions (Phase 2: SQLite)
- **Telegram rate limit:** Több terminál esetén rate limit figyelés kell

### Semleges

- **Új endpoint:** `/api/subscriptions/events` SSE endpoint
- **Új MCP tools:** 4 új tool (subscribe_to_task, subscribe_to_terminal, unsubscribe, get_subscriptions)
- **Event Bus dependency:** subscriptionManager függ az eventBus-tól

---

## Implementáció

### Phase 1: Core Subscription System (Backend Terminal)

**Task 1:** Subscription Manager + Registry

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/subscriptionManager.ts

export class SubscriptionManager {
  private registry = new Map<string, Subscription>();

  subscribe(params: SubscribeParams): Subscription { /* ... */ }
  unsubscribe(subscriptionId: string): boolean { /* ... */ }
  findMatchingSubscriptions(event: PipelineEvent): Subscription[] { /* ... */ }
  getSubscriptions(terminal: string): Subscription[] { /* ... */ }
  cleanupExpired(): void { /* ... */ }
}

export const subscriptionManager = new SubscriptionManager();
```

**Task 2:** SSE Endpoint

```typescript
// spaceos-nexus/knowledge-service/src/routes/subscriptionRoutes.ts
// (lásd fent a design-ban)
```

**Task 3:** MCP Tools

```typescript
// spaceos-nexus/knowledge-service/src/mcp.ts

// Add 4 new tools:
// - subscribe_to_task
// - subscribe_to_terminal
// - unsubscribe
// - get_subscriptions
```

**Task 4:** Event Bus Integration

```typescript
// subscriptionManager.ts

pipelineEvents.onAny((event) => {
  const subscriptions = subscriptionManager.findMatchingSubscriptions(event);
  subscriptions.forEach(sub => deliverNotification(sub, event));
});
```

### Phase 2: Persistent Storage (Backend Terminal)

**Task 5:** SQLite Subscription Table

```sql
CREATE TABLE subscriptions (
  id TEXT PRIMARY KEY,
  terminal TEXT NOT NULL,
  type TEXT NOT NULL,
  target TEXT NOT NULL,
  events TEXT NOT NULL,  -- JSON array
  delivery_method TEXT NOT NULL,
  created_at TEXT NOT NULL,
  expires_at TEXT
);

CREATE INDEX idx_subscriptions_terminal ON subscriptions(terminal);
CREATE INDEX idx_subscriptions_target ON subscriptions(target);
```

**Task 6:** Migrate to Persistent Registry

```typescript
// Load from SQLite on startup
// Write to SQLite on subscribe/unsubscribe
// In-memory cache for fast lookup
```

### Phase 3: Advanced Features (Conductor)

**Task 7:** Telegram Notification Templates

```typescript
// Format event → user-friendly Telegram message
function formatEventForTelegram(event: PipelineEvent): string {
  if (event.type === 'outbox:done') {
    return `✅ Task ${event.messageId} completed by ${event.terminal}`;
  }
  // ... more templates
}
```

**Task 8:** Subscription Analytics

```typescript
// Dashboard: Active subscriptions per terminal
// Dashboard: Notification delivery stats (SSE vs Telegram vs Inbox)
// Dashboard: Failed delivery alerts
```

---

## Példa Használat

### Scenario 1: Conductor vár Backend task befejezésre

**Conductor session:**

```typescript
// Conductor assigns task to backend
await create_task({
  from: 'conductor',
  to: 'backend',
  title: 'Implement CQRS handler',
  description: '...',
  priority: 'high',
});

const taskId = 'MSG-BACKEND-123';

// Subscribe to completion
const subscription = await subscribe_to_task({
  terminal: 'conductor',
  task_id: taskId,
  events: ['done', 'blocked'],
  delivery_method: 'auto',  // SSE if active, Telegram if not
});

console.log(`Subscribed: ${subscription.id}`);

// Conductor continues other work, no polling needed!
// When backend finishes, Conductor receives push notification via SSE
```

**Backend session:**

```typescript
// Backend completes task
await complete_task({
  terminal: 'backend',
  message_id: 'MSG-BACKEND-123',
  status: 'done',
  summary: 'CQRS handler implemented',
});

// Event Bus emits: outbox:done
// subscriptionManager delivers notification to Conductor via SSE
```

**Conductor receives notification:**

```typescript
// SSE message arrives
{
  type: 'notification',
  event: {
    type: 'outbox:done',
    terminal: 'backend',
    messageId: 'MSG-BACKEND-123',
    timestamp: '2026-06-30T19:30:00Z'
  }
}

// Conductor automatically unsubscribes and continues workflow
```

### Scenario 2: Architect watches Frontend inbox

**Architect session:**

```typescript
// Architect wants to know when frontend receives new task
await subscribe_to_terminal({
  terminal: 'architect',
  target_terminal: 'frontend',
  events: ['inbox_new'],
  delivery_method: 'telegram',  // Send to Telegram (no active session)
});

// Architect session ends, but subscription remains active
```

**Later: Frontend receives task**

```typescript
// Conductor sends task to frontend
await create_task({
  from: 'conductor',
  to: 'frontend',
  title: 'Implement UI component',
  // ...
});

// Event Bus emits: inbox:new (terminal: frontend)
// subscriptionManager finds Architect's subscription
// Delivers via Telegram: "📬 Frontend received new task: MSG-FRONTEND-045"
```

---

## Extended Feature: Monitoring Automation

### Extended Event Types

A KITERJESZTÉS részeként monitoring event types definiálva a terminál folyamat figyelés automatizálására:

```typescript
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

### Auto-Recovery Configuration

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
```

**MCP Tools:**

```typescript
// Tool 1: Set auto-recovery rules
{
  name: 'set_auto_recovery',
  description: 'Configure auto-recovery rules for a terminal',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string' },
      config: {
        type: 'object',
        properties: {
          enabled: { type: 'boolean' },
          rules: {
            type: 'object',
            properties: {
              onStuck: { type: 'string', enum: ['nudge', 'restart', 'escalate', 'none'] },
              stuckThresholdMinutes: { type: 'number', default: 5 },
              onCrash: { type: 'string', enum: ['restart', 'escalate', 'none'] },
              maxAutoRestarts: { type: 'number', default: 3 },
              onCostAlert: { type: 'string', enum: ['pause', 'warn', 'none'] },
              costAlertThreshold: { type: 'number', default: 10 },
            },
          },
        },
      },
    },
    required: ['terminal', 'config'],
  },
}

// Tool 2: Get auto-recovery config
{
  name: 'get_auto_recovery',
  description: 'Get current auto-recovery configuration',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string' },
    },
    required: ['terminal'],
  },
}
```

### Dashboard Real-time Streaming

**SSE Endpoint for Dashboard:**

```typescript
// GET /api/monitoring/stream
// → Server-Sent Events stream minden monitoring eventtel
// → Dashboard real-time frissül (WORKING/IDLE/STUCK/CRASHED)

app.get('/api/monitoring/stream', (req, res) => {
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  const listener = (event: MonitoringEvent) => {
    res.write(`data: ${JSON.stringify(event)}\n\n`);
  };

  monitoringEvents.onAny(listener);

  req.on('close', () => {
    monitoringEvents.offAny(listener);
  });
});

// GET /api/monitoring/stream/:terminal
// → Szűrt stream egy terminálra
```

### Extended MCP Tools

**Tool 3: Subscribe to monitoring events**

```typescript
{
  name: 'subscribe_to_monitoring',
  description: 'Subscribe to monitoring events (session, cost, health)',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string', description: 'Subscribing terminal' },
      events: {
        type: 'array',
        items: {
          type: 'string',
          enum: ['session_started', 'session_ended', 'session_stuck', 'session_crashed', 'todo_progress', 'cost_alert', 'heartbeat_missed'],
        },
      },
      terminals: {
        type: 'array',
        items: { type: 'string' },
        description: 'Watch specific terminals (empty = all)',
      },
      delivery_method: {
        type: 'string',
        enum: ['sse', 'telegram', 'inbox', 'auto'],
      },
    },
    required: ['terminal', 'events'],
  },
}
```

**Tool 4: Subscribe to all terminals (Conductor/Root)**

```typescript
{
  name: 'subscribe_to_all_terminals',
  description: 'Subscribe to all terminal events (Conductor/Root only)',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string', description: 'Must be conductor or root' },
      events: {
        type: 'array',
        items: {
          type: 'string',
          enum: ['done', 'blocked', 'stuck', 'crashed', 'cost_alert'],
        },
      },
    },
    required: ['terminal', 'events'],
  },
}
```

**Tool 5: Get terminal health**

```typescript
{
  name: 'get_terminal_health',
  description: 'Poll terminal health status (fallback if SSE unavailable)',
  inputSchema: {
    type: 'object',
    properties: {},
  },
  returns: {
    type: 'array',
    items: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        status: { type: 'string', enum: ['idle', 'working', 'stuck', 'crashed'] },
        lastActivity: { type: 'string' },
        currentTask: { type: 'string' },
        sessionCost: { type: 'number' },
      },
    },
  },
}
```

### Integration with Existing Watchers

A meglévő `nightwatch.sh` komponensek beépülnek a subscription rendszerbe:

- `watch-stuck.sh` → `session_stuck` event emit
- `watch-done.sh` → `done` event emit
- `watch-priority.sh` → `session_started` event emit

**Event Bus Integration:**

```typescript
// nightwatch.sh stuck detection → emit event
if (stuckDuration > 300) {  // 5 minutes
  pipelineEvents.emit('monitoring:session_stuck', {
    type: 'session_stuck',
    terminal: 'backend',
    timestamp: new Date(),
    details: { stuckDuration: 300, sessionId: 'session-123' },
  });
}

// subscriptionManager delivers to subscribers
pipelineEvents.on('monitoring:session_stuck', (event) => {
  const subscriptions = subscriptionManager.findMatchingSubscriptions(event);
  subscriptions.forEach(sub => deliverNotification(sub, event));

  // Auto-recovery action
  const config = autoRecoveryManager.getConfig(event.terminal);
  if (config.enabled && config.rules.onStuck === 'nudge') {
    sendNudge(event.terminal);
  }
});
```

---

## Elfogadási Kritériumok

### Core Subscription System
- [x] ADR-052 dokumentáció elkészült
- [x] MCP tool interface-ek definiálva (4 core tool)
- [x] Subscription Registry design (Map-based, expiration support)
- [x] SSE + Telegram + Inbox delivery terv (3-tier fallback)
- [ ] SubscriptionManager implementálva (Phase 1)
- [ ] SSE endpoint működik (Phase 1)
- [ ] MCP tools regisztrálva (Phase 1)
- [ ] Event Bus integration (Phase 1)
- [ ] SQLite persistent storage (Phase 2)
- [ ] Telegram notification templates (Phase 3)

### Monitoring Automation Extension
- [x] Monitoring event types definiálva (7 MonitoringEventType)
- [x] AutoRecoveryConfig interface tervezve
- [x] Extended MCP tools definiálva (3 monitoring tool)
- [x] Dashboard SSE streaming specifikálva
- [x] Nightwatch integration design
- [ ] MonitoringEvent emission implementálva (Phase 1)
- [ ] Auto-recovery logic implementálva (Phase 2)
- [ ] Dashboard real-time display (Phase 3)

---

## Kapcsolódó Dokumentáció

- `spaceos-nexus/knowledge-service/src/pipeline/eventBus.ts` — Event Bus implementation
- `datahaven-web/src/routes/sseRoutes.js` — SSE endpoint example
- `spaceos-nexus/knowledge-service/src/mcp.ts` — MCP tool definitions
- ADR-041: Graph-Based Workflow — Task dependency tracking
- ADR-049: Dual Session Architecture — Chat vs Work sessions

---

## Felülvizsgálat

**Phase 1:** 2026-07 (Backend Terminal implementálja a core rendszert)
**Phase 2:** 2026 Q3 (SQLite persistent storage)
**Phase 3:** 2026 Q4 (Advanced features, analytics)
