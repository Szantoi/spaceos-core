# Dispatch Control & Token Budget System

> **Status:** IDEA — Not implemented yet
> **Created:** 2026-06-23
> **Priority:** High (after Task Audit Phase 0-3)
> **Problem:** Autonomous systems burn tokens uncontrollably, need coordination

---

## Problem Statement

**Current situation:**
- Heartbeat sends MCP nudges every 5-6 minutes to idle terminals
- AutonomousDev spawns Conductor sessions automatically
- Message Router moves messages without human oversight
- **Result:** Token burn without control, unpredictable costs

**User requirement:**
> "lekel tudni kapcsoni a rendszert mert égeti a tokent és kontrolalni kell tudni. nagyon jó hogy ívy fejlödik de erősen bepörgeti magát. kell egy ötlet arra, hogy kordinált legyen az űzenetek inyektálása."

---

## Short-term Solution (IMPLEMENTED 2026-06-23)

**Emergency Mode:**
```bash
# .env — all autonomous systems DISABLED
ENABLE_NIGHTWATCH=false
ENABLE_HEARTBEAT=false
ENABLE_MESSAGE_ROUTER=false
ENABLE_AUTONOMOUS_DEV=false
# ... all other ENABLE_* flags = false
```

**Manual dispatch only:**
- Root manually triggers sessions via API
- Conductor can request session start (requires Root approval)
- No automatic wake-on-inbox

---

## Long-term Solution: Three Approaches

### Option A: Conductor-based Manual Dispatch

**Concept:** Conductor acts as dispatcher, Root has final say

```yaml
# config/dispatch-mode.yaml
mode: manual  # auto | manual | scheduled

dispatch_rules:
  allowed_triggers:
    - root        # can trigger anyone
    - conductor   # can trigger workers (backend, frontend, etc.)

  rate_limiting:
    max_sessions_per_hour: 5
    max_parallel_sessions: 2

  priority_queue:
    - critical
    - high
    - medium
    - low
```

**Workflow:**
1. UNREAD inbox message detected → **NO auto-start**
2. Conductor receives notification → reviews → **proposes dispatch**
3. Root approves/rejects → session starts

**Pros:**
- Full control over token usage
- Human-in-the-loop for critical decisions
- Simple to implement

**Cons:**
- Slower response time
- Requires Conductor/Root availability

---

### Option B: Token Budget System

**Concept:** Daily/hourly token budget with automatic throttling

```yaml
# config/token-budget.yaml
daily_budget:
  total_tokens: 100000        # 100K tokens/day
  reserved_for_critical: 20000

per_terminal_limit:
  root: 20000
  conductor: 15000
  backend: 10000
  frontend: 10000
  architect: 10000
  librarian: 5000
  explorer: 5000
  designer: 5000

alerts:
  - threshold: 80%
    action: telegram_notify
    message: "⚠️ 80% token budget used today"

  - threshold: 90%
    action: pause_autonomous
    message: "🚨 90% budget — pausing autonomous sessions"

  - threshold: 100%
    action: emergency_stop
    message: "❌ Budget depleted — all sessions stopped"
```

**Implementation:**

```typescript
// src/pipeline/tokenBudget.ts

interface TokenBudget {
  total: number;
  used: number;
  remaining: number;
  byTerminal: Record<string, { used: number; limit: number }>;
  resetAt: Date;
}

class TokenBudgetTracker {
  private db: Database;

  recordUsage(terminal: string, tokens: number): void {
    // Log token usage to SQLite
    // Check against budget
    // Trigger alerts if thresholds exceeded
  }

  canDispatch(terminal: string, estimatedTokens: number): boolean {
    const budget = this.getBudget();
    const terminalUsed = budget.byTerminal[terminal]?.used ?? 0;
    const terminalLimit = budget.byTerminal[terminal]?.limit ?? 10000;

    return (
      budget.remaining >= estimatedTokens &&
      (terminalUsed + estimatedTokens) <= terminalLimit
    );
  }

  getBudget(): TokenBudget {
    // Query today's usage from SQLite
    // Calculate remaining budget
  }
}
```

**Pros:**
- Automatic cost control
- Allows autonomous operation within limits
- Real-time visibility

**Cons:**
- Requires token usage tracking
- Estimation accuracy critical

---

### Option C: Scheduled Dispatch Windows

**Concept:** Time-based windows for autonomous sessions

```yaml
# config/dispatch-windows.yaml
timezone: Europe/Budapest

windows:
  - name: "Morning Planning"
    days: [mon, tue, wed, thu, fri]
    start: "08:00"
    end: "09:00"
    allowed_terminals: [conductor, architect]
    max_sessions: 3

  - name: "Dev Session 1"
    days: [mon, tue, wed, thu, fri]
    start: "10:00"
    end: "12:00"
    allowed_terminals: [backend, frontend, designer]
    max_sessions: 2

  - name: "Lunch Break"
    days: [mon, tue, wed, thu, fri]
    start: "12:00"
    end: "13:00"
    allowed_terminals: []  # NO sessions during lunch

  - name: "Dev Session 2"
    days: [mon, tue, wed, thu, fri]
    start: "14:00"
    end: "18:00"
    allowed_terminals: [backend, frontend, designer]
    max_sessions: 3

  - name: "Night Watch"
    days: [mon, tue, wed, thu, fri, sat, sun]
    start: "22:00"
    end: "23:00"
    allowed_terminals: [conductor, librarian]
    max_sessions: 1

# Outside windows: manual dispatch only
default_mode: manual
```

**Pros:**
- Predictable token usage patterns
- Work-life balance for operators
- Aligns with business hours

**Cons:**
- Less flexible
- May miss urgent tasks outside windows

---

## Recommended Hybrid Approach

**Phase 1: Emergency Mode (NOW — 2026-06-23)**
- ✅ All autonomous systems DISABLED
- ✅ Manual dispatch only

**Phase 2: Token Budget (1 week)**
- Implement token usage tracking
- Set daily/hourly budgets
- Add alerts (Telegram)
- Allow autonomous operation within budget

**Phase 3: Conductor Orchestration (2 weeks)**
- Conductor reviews inbox → proposes dispatch
- Root can approve/reject/override
- Priority queue for critical tasks

**Phase 4: Scheduled Windows (1 month)**
- Define work hours for each terminal
- Outside windows: manual only
- Optimize for cost/response time trade-off

---

## API Design

### Master Control API

```typescript
// Emergency Stop - kill all autonomous processes
POST /api/control/emergency-stop
Response: {
  success: true,
  stopped: ['heartbeat', 'message_router', 'nightwatch'],
  timestamp: '2026-06-23T07:00:00Z'
}

// Set Dispatch Mode
POST /api/control/dispatch-mode
Body: { mode: 'manual' | 'auto' | 'scheduled' }
Response: {
  mode: 'manual',
  effectiveAt: '2026-06-23T07:00:00Z',
  previousMode: 'auto'
}

// Manual Session Dispatch
POST /api/control/dispatch
Body: {
  terminal: 'backend',
  reason: 'Manual Root trigger',
  taskId: 'MSG-BACKEND-031',
  estimatedTokens: 5000
}
Response: {
  sessionId: 'abc123',
  started: true,
  budgetRemaining: 95000
}

// Get Token Usage
GET /api/control/token-usage?period=today
Response: {
  total: 25000,
  byTerminal: {
    backend: { used: 12000, limit: 10000, overbudget: true },
    frontend: { used: 8000, limit: 10000, ok: true },
    conductor: { used: 5000, limit: 15000, ok: true }
  },
  remaining: 75000,
  resetAt: '2026-06-24T00:00:00Z'
}

// Get Dispatch Queue
GET /api/control/dispatch-queue
Response: {
  pending: [
    {
      messageId: 'MSG-BACKEND-031',
      priority: 'high',
      estimatedTokens: 5000,
      queuedAt: '2026-06-23T06:30:00Z'
    }
  ],
  executing: [
    {
      sessionId: 'abc123',
      terminal: 'frontend',
      startedAt: '2026-06-23T07:00:00Z',
      tokensUsed: 2300
    }
  ]
}

// Approve/Reject Dispatch (Conductor → Root)
POST /api/control/dispatch/approve
Body: { proposalId: 'prop-123', approved: true }
Response: {
  proposalId: 'prop-123',
  status: 'approved',
  sessionStarted: true
}
```

---

## Database Schema (SQLite)

```sql
-- Token usage log
CREATE TABLE token_usage (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  terminal TEXT NOT NULL,
  session_id TEXT,
  task_id TEXT,
  tokens_used INTEGER NOT NULL,
  timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,

  INDEX idx_terminal_date (terminal, DATE(timestamp))
);

-- Dispatch proposals (Conductor → Root)
CREATE TABLE dispatch_proposals (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  proposal_id TEXT UNIQUE NOT NULL,
  terminal TEXT NOT NULL,
  task_id TEXT NOT NULL,
  reason TEXT,
  estimated_tokens INTEGER,
  proposed_by TEXT NOT NULL,  -- 'conductor'
  proposed_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  status TEXT CHECK(status IN ('pending', 'approved', 'rejected')),
  approved_by TEXT,
  approved_at DATETIME
);

-- Dispatch queue
CREATE TABLE dispatch_queue (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  message_id TEXT UNIQUE NOT NULL,
  terminal TEXT NOT NULL,
  priority TEXT CHECK(priority IN ('critical', 'high', 'medium', 'low')),
  estimated_tokens INTEGER,
  queued_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  status TEXT CHECK(status IN ('queued', 'executing', 'completed', 'failed')),
  session_id TEXT,
  started_at DATETIME,
  completed_at DATETIME
);
```

---

## Implementation Timeline

| Phase | Component | Time | Depends On |
|---|---|---|---|
| **Phase 1** | Emergency Mode | 5 min | Nothing (DONE) |
| **Phase 2** | Token Budget API | 2 óra | SQLite schema |
| **Phase 3** | Budget Alerts | 1 óra | Token Budget API |
| **Phase 4** | Conductor Orchestration | 3 óra | Token Budget |
| **Phase 5** | Scheduled Windows | 2 óra | Conductor Orchestration |
| **Phase 6** | Datahaven Widget | 2 óra | Token Budget API |

**Total:** ~10 óra (parallel with Task Audit implementation)

---

## Success Metrics

| Metric | Target | Measure |
|---|---|---|
| Daily token cost | <100K tokens/day | SQLite query |
| Response time (manual) | <5 min for high priority | Dispatch queue |
| False alerts | <5% | Alert logs |
| Uptime | >99% (no emergency stops) | Server logs |

---

## Open Questions

1. **Budget allocation:** 100K tokens/day reális?
2. **Priority escalation:** Critical tasks bypass budget?
3. **Cost tracking:** Claude API nem ad token count-ot, becsüljük?
4. **Rollover:** Unused budget carries over next day?
5. **External triggers:** API endpoint public? Auth token?

---

## Related Documents

- `TASK_AUDIT_DESIGN.md` — Task creation + audit trail (parallel effort)
- `NEXUS_INFRASTRUCTURE_AUDIT.md` — Existing infrastructure
- `REVIEWER_SECURITY_ARCHITECTURE.md` — Reviewer token limits

---

**Next Action:** Implement Phase 2 (Token Budget) after Task Audit Phase 0-3 complete.
