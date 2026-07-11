# ADR-052: Task Subscription & Escalation System

**Dátum:** 2026-07-02
**Státusz:** PROPOSED
**Kontextus:** JoineryTech folyamatos munka monitoring

## Probléma

Ha egy terminál (pl. Conductor) kioszt egy feladatot másik terminálnak (pl. Frontend, Backend), és **nem kap választ egy megadott időn belül**, akkor:

1. Nem látja hogy halad-e a munka
2. Nem tudja hogy a terminál megkapta-e az üzenetet
3. Nem tudja hogy elakadt-e a terminál vagy blokkolva lett
4. **Manual beavatkozás kell** hogy lássuk mi a helyzet

## Megoldás: Task Subscription API

### Koncepció

Minden terminál **feliratkozhat** egy task státuszára. Ha X időn belül nem történik változás:
1. **1. retry** — Nudge küldés a target terminálnak
2. **2. retry** — Másik nudge stratégiával (session restart, inbox re-inject)
3. **3. escalation** — Root-nak eszkaláció teljes kontextussal

### API Design

```typescript
// MCP Tool: subscribe_to_task
mcp__spaceos-knowledge__subscribe_to_task
  terminal: "conductor"           // Ki iratkozott fel
  task_id: "MSG-FRONTEND-089"     // Melyik task-ra
  timeout_minutes: 60             // Mennyi idő alatt várunk választ
  events: ["progress", "done", "blocked"]  // Milyen event-ekre figyelünk
  retry_strategy: "nudge-twice"   // nudge-twice | nudge-restart | immediate-escalate
  escalate_to: "root"             // Kinek eszkaláljuk ha nem sikerül

// Response
{
  subscription_id: "uuid-...",
  expires_at: "2026-07-02T08:45:00Z",
  retry_count: 0,
  max_retries: 2
}
```

### Workflow

```
T+0min:  Conductor dispatches MSG-FRONTEND-089
T+0min:  Conductor subscribes (timeout=60min)

T+60min: No progress event → Retry #1
         - Nudge küldés Frontend session-nek (tmux send-keys)
         - Telegram alert: "Frontend nem reagált 60 perc alatt"
         - Log: "Subscription retry 1/2 for MSG-FRONTEND-089"

T+75min: Still no progress → Retry #2
         - Session restart vagy inbox re-inject
         - Telegram alert: "Frontend 2. retry - session újraindítva"
         - Log: "Subscription retry 2/2 for MSG-FRONTEND-089"

T+90min: Still no progress → ESCALATE to Root
         - Root inbox üzenet: "MSG-FRONTEND-089 eszkaláció"
         - Context: subscription log, frontend stdout, inbox/outbox státusz
         - Telegram alert: "ROOT ESCALATION: Frontend nem reagált 90 perc"
```

### Database Schema

```sql
-- spaceos-nexus/knowledge-service/data/knowledge.db
CREATE TABLE task_subscriptions (
  id TEXT PRIMARY KEY,
  subscriber_terminal TEXT NOT NULL,
  task_id TEXT NOT NULL,
  timeout_minutes INTEGER NOT NULL,
  events TEXT NOT NULL,  -- JSON array: ["progress", "done", "blocked"]
  retry_strategy TEXT NOT NULL,
  escalate_to TEXT NOT NULL,
  created_at TEXT NOT NULL,
  expires_at TEXT NOT NULL,
  retry_count INTEGER DEFAULT 0,
  max_retries INTEGER DEFAULT 2,
  last_retry_at TEXT,
  status TEXT DEFAULT 'active',  -- active | completed | escalated | cancelled
  escalation_reason TEXT
);

CREATE INDEX idx_subscriptions_status ON task_subscriptions(status);
CREATE INDEX idx_subscriptions_expires ON task_subscriptions(expires_at);
```

### Nightwatch Integration

```typescript
// spaceos-nexus/knowledge-service/src/pipeline/watchSubscriptions.ts

export async function watchSubscriptions() {
  const now = new Date().toISOString();

  // Get active subscriptions that expired
  const expired = db.prepare(`
    SELECT * FROM task_subscriptions
    WHERE status = 'active' AND expires_at < ?
  `).all(now);

  for (const sub of expired) {
    const task = getTaskStatus(sub.task_id);

    // Check if task progressed
    if (task.status === 'DONE' || task.status === 'COMPLETED') {
      // Success - mark subscription as completed
      completeSubscription(sub.id);
      continue;
    }

    // No progress - retry or escalate
    if (sub.retry_count < sub.max_retries) {
      // RETRY
      await retryTask(sub);
    } else {
      // ESCALATE
      await escalateTask(sub);
    }
  }
}

async function retryTask(sub: Subscription) {
  sub.retry_count++;

  if (sub.retry_strategy === 'nudge-twice') {
    if (sub.retry_count === 1) {
      // First retry: Simple nudge
      await sendNudge(sub.task_id);
      await telegram(`⚠️ Task ${sub.task_id} - Retry ${sub.retry_count}/2 (nudge)`);
    } else {
      // Second retry: Session restart
      await restartSession(sub.task_id);
      await telegram(`⚠️ Task ${sub.task_id} - Retry ${sub.retry_count}/2 (restart)`);
    }
  }

  // Extend timeout for next check
  sub.expires_at = addMinutes(sub.timeout_minutes);
  updateSubscription(sub);
}

async function escalateTask(sub: Subscription) {
  const terminal = getTerminalForTask(sub.task_id);

  // Collect context
  const context = {
    task: await getTaskDetails(sub.task_id),
    session_log: await capturePane(terminal, 50),
    inbox_status: await getInboxStatus(terminal),
    outbox_status: await getOutboxStatus(terminal),
    retry_history: await getRetryHistory(sub.id)
  };

  // Create escalation inbox for Root
  await createEscalationInbox({
    from: sub.subscriber_terminal,
    to: sub.escalate_to,
    task_id: sub.task_id,
    reason: `No progress after ${sub.retry_count} retries (${sub.timeout_minutes * (sub.retry_count + 1)} minutes)`,
    context: JSON.stringify(context, null, 2)
  });

  // Alert
  await telegram(`🚨 ROOT ESCALATION: ${sub.task_id} no response from ${terminal}`);

  // Mark subscription as escalated
  sub.status = 'escalated';
  sub.escalation_reason = `No progress after ${sub.retry_count} retries`;
  updateSubscription(sub);
}
```

### MCP Tools

```typescript
// spaceos-nexus/knowledge-service/src/mcp.ts

// Subscribe to task
tools.push({
  name: 'mcp__spaceos-knowledge__subscribe_to_task',
  description: 'Subscribe to task progress events with auto-retry and escalation',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string', description: 'Subscriber terminal (e.g., conductor)' },
      task_id: { type: 'string', description: 'Task message ID (e.g., MSG-FRONTEND-089)' },
      timeout_minutes: { type: 'number', default: 60, description: 'Timeout in minutes' },
      events: {
        type: 'array',
        items: { type: 'string', enum: ['progress', 'done', 'blocked'] },
        default: ['progress', 'done']
      },
      retry_strategy: {
        type: 'string',
        enum: ['nudge-twice', 'nudge-restart', 'immediate-escalate'],
        default: 'nudge-twice'
      },
      escalate_to: { type: 'string', default: 'root' }
    },
    required: ['terminal', 'task_id']
  }
});

// Unsubscribe from task
tools.push({
  name: 'mcp__spaceos-knowledge__unsubscribe_task',
  description: 'Cancel task subscription',
  inputSchema: {
    type: 'object',
    properties: {
      subscription_id: { type: 'string', description: 'Subscription ID to cancel' }
    },
    required: ['subscription_id']
  }
});

// Get active subscriptions
tools.push({
  name: 'mcp__spaceos-knowledge__get_subscriptions',
  description: 'List active task subscriptions for a terminal',
  inputSchema: {
    type: 'object',
    properties: {
      terminal: { type: 'string', description: 'Terminal name (optional - all if omitted)' }
    }
  }
});
```

## JoineryTech használati példa

### Conductor dispatches tasks and subscribes

```typescript
// Conductor session
// 1. Dispatch tasks
await createTask({
  from: 'conductor',
  to: 'frontend',
  task_id: 'MSG-FRONTEND-089',
  title: 'JoineryTech UI/UX Audit',
  priority: 'medium'
});

await createTask({
  from: 'conductor',
  to: 'backend',
  task_id: 'MSG-BACKEND-105',
  title: 'JoineryTech Backend Architecture',
  priority: 'high'
});

// 2. Subscribe to both tasks
await subscribe_to_task({
  terminal: 'conductor',
  task_id: 'MSG-FRONTEND-089',
  timeout_minutes: 180,  // 3 hours (audit task)
  events: ['progress', 'done', 'blocked'],
  retry_strategy: 'nudge-twice',
  escalate_to: 'root'
});

await subscribe_to_task({
  terminal: 'conductor',
  task_id: 'MSG-BACKEND-105',
  timeout_minutes: 300,  // 5 hours (architecture task)
  events: ['progress', 'done', 'blocked'],
  retry_strategy: 'nudge-twice',
  escalate_to: 'root'
});
```

### Timeline Example

```
T+0:00   Conductor dispatches MSG-FRONTEND-089 + MSG-BACKEND-105
         Subscriptions created (timeouts: 3h, 5h)

T+2:30   Backend sends progress update → subscription satisfied, auto-renewed
T+4:45   Backend sends DONE → subscription completed ✓

T+3:00   Frontend timeout #1 → Retry 1/2 (nudge)
         tmux send-keys to spaceos-frontend

T+4:30   Frontend timeout #2 → Retry 2/2 (session restart)
         Session killed & restarted, inbox re-injected

T+6:00   Frontend still no response → ESCALATE to Root
         Root inbox: "MSG-ROOT-XXX: Frontend escalation"
         Context: 6 hour log, retry history, session dump
```

## Implementation Plan

### Phase 1: Core Subscription (Week 1)
- [ ] Database schema + migration
- [ ] `subscribe_to_task` MCP tool
- [ ] `unsubscribe_task` MCP tool
- [ ] `get_subscriptions` MCP tool
- [ ] watchSubscriptions.ts Nightwatch module

### Phase 2: Retry Strategies (Week 1)
- [ ] `nudge-twice` implementation
- [ ] `nudge-restart` implementation
- [ ] Session restart helper
- [ ] Inbox re-inject helper

### Phase 3: Escalation (Week 2)
- [ ] Escalation inbox template
- [ ] Context collection (session log, inbox/outbox)
- [ ] Root escalation workflow
- [ ] Telegram alerts

### Phase 4: JoineryTech Integration (Week 2)
- [ ] Conductor subscription on JoineryTech tasks
- [ ] Frontend/Backend progress event emission
- [ ] Testing: simulate stuck Frontend
- [ ] Testing: verify escalation to Root

## Alternatives Considered

### 1. Manual polling by Conductor
- **Rejected:** Requires Conductor to stay active and poll
- Subscription is event-driven and automatic

### 2. Simple timeout without retry
- **Rejected:** No graceful degradation
- 2-retry approach gives terminals a chance to recover

### 3. Root monitors all tasks
- **Rejected:** Root should only handle escalations
- Conductor (or any terminal) can subscribe to its own dispatched tasks

## Consequences

### Pozitív
- Automatikus monitoring JoineryTech (és minden más) projekthez
- 2-szintű retry → magasabb success rate terminal recovery
- Root csak kritikus eszkalációkat kap, nem minden kis issue-t
- Audit trail: subscription DB + retry log + escalation context

### Negatív
- Új DB tábla + Nightwatch modul → complexity
- Subscription lifecycle management (cancel, renew, expire)
- Túl agresszív retry → spam nudge ha terminal valóban blokkolt

## Related

- ADR-046: Tiered Memory (hot/warm/cold session context)
- ADR-049: Dual Session Mode (chat vs work session)
- Pipeline coordination workflow

## Status

**IMPLEMENTED** — 2026-07-02 07:50 UTC

### Implementation Notes

- **File:** `spaceos-nexus/knowledge-service/src/pipeline/taskEscalation.ts`
- **Nightwatch integration:** Added `watchTaskEscalations()` to nightwatch.ts
- **Database:** In-memory escalation registry (Map-based, can migrate to SQLite later)
- **Retry logic:** 2 retries (nudge → restart) before escalation
- **Escalation target:** Root inbox with full context

### Testing

```bash
# Start Conductor and subscribe to a test task
cd /opt/spaceos/terminals/conductor
# Process MSG-CONDUCTOR-063 to set up JoineryTech monitoring
```

---

**Next Steps:**
1. Root review és jóváhagyás
2. Phase 1 implementálás (subscription core)
3. JoineryTech pilot: Frontend + Backend subscription
