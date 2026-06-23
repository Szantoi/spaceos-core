# Dispatch Control & Token Budget System — Implementation Guide

> **Status:** IMPLEMENTED (2026-06-23)
> **Location:** `spaceos-nexus/knowledge-service/src/dispatch-control/`
> **Tests:** 57 passing (24 + 14 + 19)

## Overview

The Dispatch Control system provides comprehensive agent session management with:
- **Token Budget Tracking** — Per-terminal daily limits with threshold alerts
- **Conductor Orchestration** — Proposal-based dispatch workflow
- **Scheduled Windows** — Time-based autonomous session control

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Dispatch Control System                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ Token Budget │  │  Proposals   │  │  Scheduled Windows   │   │
│  │   Tracker    │  │   System     │  │                      │   │
│  ├──────────────┤  ├──────────────┤  ├──────────────────────┤   │
│  │ • Usage log  │  │ • Create     │  │ • Time windows       │   │
│  │ • Limits     │  │ • Approve    │  │ • Day-of-week        │   │
│  │ • Alerts     │  │ • Reject     │  │ • Terminal perms     │   │
│  │ • Reserve    │  │ • Expire     │  │ • Max sessions       │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
│                           │                                      │
│                    ┌──────┴──────┐                               │
│                    │  SQLite DB  │                               │
│                    │ dispatch.db │                               │
│                    └─────────────┘                               │
└─────────────────────────────────────────────────────────────────┘
```

## Database Schema

**Location:** `data/dispatch.db` (SQLite)

### Tables

| Table | Purpose |
|-------|---------|
| `token_usage` | Append-only log of token consumption |
| `dispatch_config` | Global dispatch mode (auto/manual/scheduled) |
| `dispatch_queue` | Pending/executing dispatch requests |
| `budget_config` | Per-terminal daily limits and reserves |
| `budget_alerts` | Alert history for audit |
| `dispatch_proposals` | Conductor→Root approval workflow |
| `dispatch_windows` | Scheduled time window definitions |
| `window_sessions` | Active sessions within windows |

### Views

- `v_today_usage` — Today's token usage by terminal
- `v_budget_status` — Real-time budget status per terminal

## Module Reference

### 1. Token Budget (`tokenBudget.ts`)

**Core Functions:**

```typescript
// Initialize database
initDispatchDb(): Database.Database

// Dispatch mode control
getDispatchMode(): 'auto' | 'manual' | 'scheduled'
setDispatchMode(mode: DispatchMode, updatedBy: string): void

// Token usage
recordTokenUsage(usage: TokenUsage): void

// Budget queries
getTerminalBudgetStatus(terminal: string): BudgetStatus
getDailyBudgetSummary(date?: string): DailyBudgetSummary

// Dispatch checks
canDispatch(terminal: string, estimatedTokens: number, priority?: string): DispatchCheck

// Queue management
queueDispatch(messageId: string, terminal: string, priority: string, estimatedTokens: number): void
getDispatchQueue(): QueuedDispatch[]
markDispatchExecuting(messageId: string, sessionId: string): void
markDispatchCompleted(messageId: string, tokensActual?: number): void
markDispatchFailed(messageId: string, error: string): void
```

**Budget Status Thresholds:**

| Usage % | Status | Action |
|---------|--------|--------|
| 0-79% | `ok` | Normal operation |
| 80-89% | `warning` | Telegram notification |
| 90-99% | `critical` | Pause autonomous sessions |
| 100%+ | `depleted` | Emergency stop (except critical priority) |

**Default Terminal Limits:**

```typescript
root: 20000, conductor: 15000, backend: 10000, frontend: 10000,
architect: 10000, librarian: 5000, explorer: 5000, designer: 5000
```

### 2. Dispatch Proposals (`dispatchProposal.ts`)

**Workflow:**

```
Conductor detects UNREAD inbox
        │
        ▼
createProposal() → status: 'pending'
        │
        ▼
notifyNewProposal() → Telegram notification
        │
        ▼
Root reviews proposal
        │
   ┌────┴────┐
   ▼         ▼
APPROVE    REJECT
   │         │
   ▼         ▼
queueDispatch()  Archive only
   │
   ▼
Session starts
```

**Core Functions:**

```typescript
// Create proposal
createProposal(params: ProposalCreateParams): DispatchProposal

// Query
getPendingProposals(): DispatchProposal[]
getProposal(proposalId: string): DispatchProposal | null

// Decide
decideProposal(decision: ProposalDecision): Promise<DecisionResult>

// Bulk operations
approveAllPending(decidedBy: string): { approved: number; proposals: string[] }
expireOldProposals(maxAgeHours?: number): number

// Statistics
getProposalStats(): ProposalStats
```

### 3. Scheduled Windows (`scheduledWindows.ts`)

**Core Functions:**

```typescript
// Window management
addWindow(window: DispatchWindow): void
removeWindow(name: string): boolean
getWindows(): DispatchWindow[]

// Mode control
setDefaultMode(mode: 'manual' | 'auto'): void
getDefaultMode(): 'manual' | 'auto'

// Window detection
getCurrentWindow(): DispatchWindow | null
getNextWindow(): { window: DispatchWindow; startsIn: number } | null
checkWindowForTerminal(terminal: string): WindowCheck

// Session tracking
registerWindowSession(terminal: string, windowName: string, sessionId?: string): void
endWindowSession(terminal: string): void
getAllActiveSessions(): ActiveSession[]

// Presets
loadDefaultWindows(): void
```

**Default Windows (loadDefaultWindows):**

| Window | Days | Time | Terminals | Max |
|--------|------|------|-----------|-----|
| Morning Planning | Mon-Fri | 08:00-09:00 | conductor, architect | 3 |
| Dev Session 1 | Mon-Fri | 10:00-12:00 | backend, frontend, designer | 2 |
| Dev Session 2 | Mon-Fri | 14:00-18:00 | backend, frontend, designer | 3 |
| Night Watch | Daily | 22:00-23:00 | conductor, librarian | 1 |

## API Reference

### Base URL: `http://localhost:3456/api/control`

### Dispatch Mode

```bash
# Get current mode
GET /mode
→ { "mode": "manual", "updatedAt": "...", "updatedBy": "..." }

# Set mode
POST /mode
{ "mode": "auto", "updatedBy": "root" }
```

### Token Budget

```bash
# Get all budgets
GET /budget
→ { "date": "2026-06-23", "totalUsed": 45000, "byTerminal": {...} }

# Get terminal budget
GET /budget/:terminal
→ { "terminal": "backend", "dailyLimit": 10000, "tokensUsed": 5000, ... }

# Set terminal budget
POST /budget/:terminal
{ "dailyLimit": 15000, "priorityReserve": 3000 }

# Record usage
POST /usage
{ "terminal": "backend", "tokensUsed": 5000, "sessionId": "...", "model": "sonnet" }

# Check if dispatch allowed
GET /can-dispatch?terminal=backend&estimatedTokens=5000&priority=high
→ { "allowed": true, "budgetRemaining": 5000 }
```

### Dispatch Queue

```bash
# Get queue
GET /queue
→ { "queue": [...], "stats": { "queued": 2, "executing": 1 } }

# Add to queue
POST /queue
{ "messageId": "MSG-BACKEND-001", "terminal": "backend", "priority": "high", "estimatedTokens": 5000 }

# Execute dispatch
POST /dispatch
{ "messageId": "MSG-BACKEND-001" }

# Emergency stop
POST /emergency-stop
→ { "success": true, "stoppedSessions": 3 }
```

### Proposals

```bash
# List pending
GET /proposals
→ { "proposals": [...], "stats": { "pending": 2, ... } }

# Get specific
GET /proposals/:id
→ { "proposalId": "PROP-...", "terminal": "backend", "status": "pending", ... }

# Create proposal
POST /proposals
{ "terminal": "backend", "taskId": "MSG-001", "reason": "UNREAD inbox", "proposedBy": "conductor" }

# Approve
POST /proposals/:id/approve
→ { "success": true, "sessionStarted": true }

# Reject
POST /proposals/:id/reject
{ "reason": "Not now" }

# Bulk approve
POST /proposals/approve-all
→ { "approved": 3, "proposals": ["PROP-1", "PROP-2", "PROP-3"] }

# Expire old
POST /proposals/expire
{ "maxAgeHours": 24 }
```

### Scheduled Windows

```bash
# Get windows
GET /windows
→ { "windows": [...], "currentWindow": "Dev Session 1", "defaultMode": "manual" }

# Add window
POST /windows
{
  "name": "Custom Session",
  "days": ["mon", "wed", "fri"],
  "startTime": "15:00",
  "endTime": "17:00",
  "allowedTerminals": ["backend"],
  "maxSessions": 2
}

# Remove window
DELETE /windows/:name

# Check terminal
GET /windows/check/:terminal
→ { "inWindow": true, "windowName": "Dev Session 1", "terminalAllowed": true, ... }

# Session tracking
POST /windows/session/start
{ "terminal": "backend", "windowName": "Dev Session 1" }

POST /windows/session/end
{ "terminal": "backend" }

GET /windows/sessions
→ { "sessions": [...] }

# Set default mode
POST /windows/default-mode
{ "mode": "auto" }

# Load defaults
POST /windows/load-defaults
→ { "windowsLoaded": 4, "windows": [...] }
```

## Usage Examples

### 1. Manual Dispatch Flow

```bash
# 1. Check if dispatch allowed
curl localhost:3456/api/control/can-dispatch?terminal=backend&estimatedTokens=5000

# 2. Queue the dispatch
curl -X POST localhost:3456/api/control/queue \
  -H "Content-Type: application/json" \
  -d '{"messageId":"MSG-001","terminal":"backend","priority":"high","estimatedTokens":5000}'

# 3. Execute
curl -X POST localhost:3456/api/control/dispatch \
  -H "Content-Type: application/json" \
  -d '{"messageId":"MSG-001"}'

# 4. After session, record actual usage
curl -X POST localhost:3456/api/control/usage \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","tokensUsed":4500,"sessionId":"session-123"}'
```

### 2. Conductor Proposal Flow

```bash
# Conductor creates proposal
curl -X POST localhost:3456/api/control/proposals \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","taskId":"MSG-BACKEND-042","reason":"UNREAD inbox detected","proposedBy":"conductor"}'

# Root approves
curl -X POST localhost:3456/api/control/proposals/PROP-xxx/approve \
  -H "Authorization: Bearer root-token"
```

### 3. Scheduled Windows Setup

```bash
# Load default windows
curl -X POST localhost:3456/api/control/windows/load-defaults

# Add custom window
curl -X POST localhost:3456/api/control/windows \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Night Maintenance",
    "days": ["sat", "sun"],
    "startTime": "02:00",
    "endTime": "04:00",
    "allowedTerminals": ["librarian"],
    "maxSessions": 1
  }'
```

## Integration Points

### Server Initialization

```typescript
// In server.ts startup
const dispatchDb = initDispatchDb();
setProposalDb(dispatchDb);
setWindowsDb(dispatchDb);
```

### Pipeline Integration

The Dispatch Control integrates with existing pipelines:

1. **nightwatch** — Checks `canDispatch()` before starting sessions
2. **sessionStarter** — Uses proposal workflow when mode is "conductor"
3. **watchIdle** — Records token usage on session end

## Testing

```bash
# Run all dispatch control tests
cd spaceos-nexus/knowledge-service
npx vitest run src/__tests__/unit/tokenBudget.test.ts \
  src/__tests__/unit/dispatchProposal.test.ts \
  src/__tests__/unit/scheduledWindows.test.ts

# Results: 57 tests passing
#   - tokenBudget: 24 tests
#   - dispatchProposal: 14 tests
#   - scheduledWindows: 19 tests
```

## Related Documents

- Design: `docs/agent-infrastructure/DISPATCH_CONTROL_DESIGN.md`
- ADR: `docs/architecture/decisions/ADR-046-*`
- Task Audit: `docs/agent-infrastructure/TASK_AUDIT_IMPLEMENTATION.md`
