---
id: MSG-BACKEND-097
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ARCHITECT-035
created: 2026-06-30
content_hash: 4373570181a8b546c22923480f171ab86f5215aeb84c8518489602cc927532ae
---

# FSM Subscription System — Phase 1 Core Implementation

## Kontextus

Az Architect elkészítette az **ADR-052: FSM Subscription System** design dokumentumot, amely egy push-based event notification rendszert specifikál terminálok számára.

**Cél:** Megszüntetni a polling-based workflow-t (5s latency) és bevezetni az SSE push notification-t (<100ms latency).

## Feladat

Implementáld a **Phase 1: Core System** komponenseit az ADR-052 specifikáció alapján.

### 1. SubscriptionManager (`src/pipeline/subscriptionManager.ts`)

```typescript
interface Subscription {
  id: string;                    // UUID
  terminal: string;              // architect, backend, etc.
  type: 'task' | 'terminal';     // Subscription type
  target: string;                // Task ID or terminal name
  events: EventType[];           // Event list
  deliveryMethod: 'sse' | 'telegram' | 'inbox' | 'auto';
  createdAt: string;
  expiresAt?: string;
}

class SubscriptionManager {
  private subscriptions = new Map<string, Subscription>();

  // Core methods
  subscribe(sub: Subscription): string;              // Returns subscription ID
  unsubscribe(id: string): boolean;
  getSubscriptions(terminal?: string): Subscription[];
  findMatchingSubscriptions(event: any): Subscription[];
  deliverNotification(sub: Subscription, event: any): Promise<void>;
}
```

**Features:**
- In-memory Map storage (Phase 1)
- Auto-expiration support (default: 3600s)
- Event matching logic
- 3-tier delivery fallback (SSE → Telegram → Inbox)

### 2. SSE Endpoint (`src/routes/subscriptionRoutes.ts`)

```typescript
// GET /api/subscriptions/events?terminal=<name>
router.get('/events', async (req, res) => {
  const { terminal } = req.query;

  // SSE headers
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  // Send initial subscriptions
  const subs = subscriptionManager.getSubscriptions(terminal);
  res.write(`data: ${JSON.stringify({ type: 'init', subscriptions: subs })}\n\n`);

  // Listen to events and push
  const handler = (event: any) => {
    const matching = subscriptionManager.findMatchingSubscriptions(event);
    matching.forEach(sub => {
      res.write(`data: ${JSON.stringify({ type: sub.type, event })}\n\n`);
    });
  };

  pipelineEvents.onAny(handler);

  // Cleanup on disconnect
  req.on('close', () => pipelineEvents.offAny(handler));
});
```

### 3. MCP Tool Definitions (`src/mcp.ts`)

Regisztráld a 4 új MCP tool-t:

```typescript
{
  name: "mcp__spaceos-knowledge__subscribe_to_task",
  description: "Subscribe to task state changes (done, blocked, progress)",
  parameters: {
    terminal: { type: "string", required: true },
    task_id: { type: "string", required: true },
    events: { type: "array", items: { type: "string" } },
    delivery_method: { type: "string", enum: ["sse", "telegram", "inbox", "auto"] },
    expires_in: { type: "number", default: 3600 }
  }
},
{
  name: "mcp__spaceos-knowledge__subscribe_to_terminal",
  description: "Subscribe to terminal events (inbox_new, outbox_done, session events)",
  parameters: {
    terminal: { type: "string", required: true },
    target_terminal: { type: "string", required: true },
    events: { type: "array", items: { type: "string" } },
    delivery_method: { type: "string", enum: ["sse", "telegram", "inbox", "auto"] },
    expires_in: { type: "number", default: 3600 }
  }
},
{
  name: "mcp__spaceos-knowledge__unsubscribe",
  description: "Unsubscribe from notifications",
  parameters: {
    subscription_id: { type: "string", required: true }
  }
},
{
  name: "mcp__spaceos-knowledge__get_subscriptions",
  description: "List active subscriptions",
  parameters: {
    terminal: { type: "string" }
  }
}
```

### 4. Event Bus Integration (`src/pipeline/eventBus.ts` vagy `src/server.ts`)

Integráld a SubscriptionManager-t a meglévő Event Bus-szal:

```typescript
import { pipelineEvents } from './pipeline/eventBus';
import { subscriptionManager } from './pipeline/subscriptionManager';

// Hook into all events
pipelineEvents.onAny((event) => {
  const subscriptions = subscriptionManager.findMatchingSubscriptions(event);

  subscriptions.forEach(async (sub) => {
    await subscriptionManager.deliverNotification(sub, event);
  });
});
```

**Event mapping:**
- `outbox:done` → `task:done`
- `outbox:blocked` → `task:blocked`
- `inbox:new` → `terminal:inbox_new`
- `session:started` → `terminal:session_started`

### 5. Testing

**Manual test scenario:**

```bash
# Terminal 1: Start SSE client
curl -N http://localhost:3456/api/subscriptions/events?terminal=conductor

# Terminal 2: Subscribe
curl -X POST http://localhost:3456/api/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "conductor",
    "task_id": "MSG-BACKEND-123",
    "events": ["done", "blocked"],
    "delivery_method": "sse"
  }'

# Terminal 3: Trigger event (simulate outbox DONE)
# Check if Terminal 1 receives SSE push
```

**Expected result:**
- SSE client receives notification within <100ms
- No polling needed

## Referencia Dokumentumok

- **ADR-052:** `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md`
- **Event Bus:** `spaceos-nexus/knowledge-service/src/pipeline/eventBus.ts`
- **SSE példa:** `datahaven-web/src/routes/sseRoutes.js`
- **MCP pattern:** `spaceos-nexus/knowledge-service/src/mcp.ts`

## Elfogadási Kritériumok

- [ ] `subscriptionManager.ts` implementálva (Map-based storage, expiration logic)
- [ ] SSE endpoint működik (`GET /api/subscriptions/events`)
- [ ] 4 MCP tool regisztrálva és működik
- [ ] Event Bus integration működik (event → SSE push)
- [ ] Manual test sikeres (subscribe → event → push <100ms)
- [ ] TypeScript build error-free

## ROI Metrics (Target)

- **Latency:** 5000ms (polling) → <100ms (SSE push) = **98% csökkenés**
- **Resource:** 120 request/perc → 0 feleslegs request = **100% megtakarítás**

## Következő Phase-ek (jövőbeli feladatok)

- Phase 2: Persistent Storage (SQLite)
- Phase 3: Advanced Features (Telegram templates, Analytics dashboard)

---

**Prioritás:** High
**Modell:** Sonnet
**Becsült idő:** 1-2 session
**Blocker:** Nincs
