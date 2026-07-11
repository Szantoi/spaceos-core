# SpaceOS Kanban Dual-Track Specification v1

> **Status:** DRAFT
> **Created:** 2026-06-19
> **Author:** Root + Architect collaboration

---

## 1. Célkitűzés

Két különálló, de összekapcsolt Kanban board:
- **Discovery Track** — tervezés, kutatás, ötletelés (planning pipeline)
- **Delivery Track** — kivitelezés, kód, deploy (mailbox pipeline)

Mindkét track támogatja az **opcionális 2-szereplős kritikai értékelést** (A/B review), amely:
- Növeli a minőséget
- De növeli a token költséget is
- Ezért **konfiguráció alapján kapcsolható**

---

## 2. Two-Track Meta-Framework

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           DISCOVERY TRACK                                        │
│                    (Hypothesis-driven, DWI semantics)                            │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   IDEAS ──► SELECTED ──► [DEBATE] ──► CONSENSUS ──► QUEUE ──────────────────►  │
│     💡         📋          💬           🤝           📦                    │    │
│                            ▲                                               │    │
│                     Optional A/B                                           │    │
│                     (2x Sonnet)                                           DoR   │
│                                                                            │    │
└────────────────────────────────────────────────────────────────────────────│────┘
                                                                             │
                                                                             ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           DELIVERY TRACK                                         │
│                    (FSM-driven, deterministic lifecycle)                         │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   INBOX ──► ACTIVE ──► [REVIEW] ──► DONE ──► ARCHIVE                            │
│     📬        🔨          👁️         ✅        📁                                │
│                           ▲                                                      │
│                    Optional A/B                                                  │
│                    (2x Haiku)                                                    │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Discovery Track — Részletes FSM

### 3.1 Oszlopok és állapotok

| Oszlop | Mappa | Állapot | Leírás |
|--------|-------|---------|--------|
| **IDEAS** | `planning/ideas/` | `idea` | Nyers ötletek, feature requests, bug reports |
| **SELECTED** | `planning/selected/` | `selected` | TOP3 kiválasztott (plan-scan + plan-select) |
| **DEBATE** | `planning/consensus/` | `debating` | A/B Sonnet értékelés alatt |
| **CONSENSUS** | `planning/consensus/` | `consensus` | v3 merged, egyeztetett |
| **QUEUE** | `planning/queue/` | `ready` | v4 végleges, DoR teljesült |

### 3.2 Kritikai értékelés — Discovery (OPCIONÁLIS)

```yaml
# plan-config.yaml
discovery_review:
  enabled: true              # false = skip debate, direct to queue
  model: sonnet
  reviewers: 2               # A/B parallel
  consensus_threshold: 0.8   # 80% agreement required
  max_iterations: 2          # max javítási körök
```

**Folyamat:**
```
SELECTED
    │
    ▼ (if discovery_review.enabled)
┌───────────────────────────────────────┐
│           DEBATE PHASE                 │
├───────────────────────────────────────┤
│  ┌─────────┐        ┌─────────┐       │
│  │Reviewer │        │Reviewer │       │
│  │   A     │        │   B     │       │
│  │(Sonnet) │        │(Sonnet) │       │
│  └────┬────┘        └────┬────┘       │
│       │                  │            │
│       ▼                  ▼            │
│  ┌─────────┐        ┌─────────┐       │
│  │ v1.A.md │        │ v1.B.md │       │
│  │critique │        │critique │       │
│  └────┬────┘        └────┬────┘       │
│       │                  │            │
│       └────────┬─────────┘            │
│                ▼                      │
│         ┌───────────┐                 │
│         │ Consensus │                 │
│         │  Merger   │                 │
│         │ (Sonnet)  │                 │
│         └─────┬─────┘                 │
│               │                       │
│               ▼                       │
│         ┌───────────┐                 │
│         │   v3.md   │                 │
│         │  merged   │                 │
│         └───────────┘                 │
└───────────────────────────────────────┘
    │
    ▼ (if consensus_threshold met)
QUEUE (v4 = v3 + final polish)
```

### 3.3 DoR (Definition of Ready) kritériumok

Mielőtt QUEUE → INBOX (Delivery) átmenet:

- [ ] `status: ready` a frontmatter-ben
- [ ] Legalább 1 acceptance criteria definiálva
- [ ] Terminál assignee meghatározva
- [ ] Becsült komplexitás (S/M/L/XL)
- [ ] Dependency-k feloldva vagy dokumentálva

---

## 4. Delivery Track — Részletes FSM

### 4.1 Oszlopok és állapotok

| Oszlop | Mappa | Állapot | Leírás |
|--------|-------|---------|--------|
| **INBOX** | `mailbox/*/inbox/` | `UNREAD` | Kiosztott feladat, még nem indult |
| **ACTIVE** | (tmux session) | `READ` | Terminál dolgozik rajta |
| **REVIEW** | `mailbox/*/outbox/` | `DONE` | Review alatt |
| **DONE** | `mailbox/*/outbox/` | `APPROVED` | Elfogadva, merge ready |
| **ARCHIVE** | `mailbox/*/archive/` | `ARCHIVED` | Lezárt, history |

### 4.2 Kritikai értékelés — Delivery (OPCIONÁLIS)

```yaml
# reviewer-config.yaml
delivery_review:
  enabled: true              # false = auto-approve
  model: haiku
  reviewers: 2               # A/B parallel
  approval_threshold: 2      # both must approve
  reject_action: inbox       # send back to inbox
  max_iterations: 3          # max revision rounds
```

**Folyamat:**
```
DONE (outbox)
    │
    ▼ (if delivery_review.enabled)
┌───────────────────────────────────────┐
│           REVIEW PHASE                 │
├───────────────────────────────────────┤
│  ┌─────────┐        ┌─────────┐       │
│  │Reviewer │        │Reviewer │       │
│  │   A     │        │   B     │       │
│  │(Haiku)  │        │(Haiku)  │       │
│  └────┬────┘        └────┬────┘       │
│       │                  │            │
│       ▼                  ▼            │
│  ┌─────────┐        ┌─────────┐       │
│  │APPROVE/ │        │APPROVE/ │       │
│  │REJECT   │        │REJECT   │       │
│  └────┬────┘        └────┬────┘       │
│       │                  │            │
│       └────────┬─────────┘            │
│                ▼                      │
│    ┌─────────────────────┐            │
│    │  Decision Matrix    │            │
│    ├─────────────────────┤            │
│    │ A✓ + B✓ = APPROVED  │            │
│    │ A✓ + B✗ = RE-REVIEW │            │
│    │ A✗ + B✓ = RE-REVIEW │            │
│    │ A✗ + B✗ = REJECTED  │            │
│    └─────────────────────┘            │
└───────────────────────────────────────┘
    │
    ├──► APPROVED ──► ARCHIVE (pipeline.sh)
    │
    └──► REJECTED ──► INBOX (revision round)
```

---

## 5. MCP Tool Definitions

### 5.1 Discovery Tools

```typescript
// Tool: discovery_review
{
  name: "discovery_review",
  description: "Trigger A/B review on a discovery item",
  inputSchema: {
    type: "object",
    properties: {
      item_path: { type: "string", description: "Path to selected item" },
      skip_review: { type: "boolean", default: false, description: "Skip A/B, direct to queue" },
      model: { type: "string", enum: ["sonnet", "opus"], default: "sonnet" }
    },
    required: ["item_path"]
  }
}

// Tool: discovery_status
{
  name: "discovery_status",
  description: "Get current discovery pipeline status",
  inputSchema: {
    type: "object",
    properties: {
      filter: { type: "string", enum: ["all", "ideas", "selected", "debating", "queue"] }
    }
  }
}

// Tool: promote_to_delivery
{
  name: "promote_to_delivery",
  description: "Move item from Discovery queue to Delivery inbox (DoR gate)",
  inputSchema: {
    type: "object",
    properties: {
      item_path: { type: "string" },
      target_terminal: { type: "string" },
      priority: { type: "string", enum: ["critical", "high", "medium", "low"] },
      model: { type: "string", enum: ["haiku", "sonnet", "opus"] }
    },
    required: ["item_path", "target_terminal"]
  }
}
```

### 5.2 Delivery Tools

```typescript
// Tool: delivery_review
{
  name: "delivery_review",
  description: "Trigger A/B review on a delivery output",
  inputSchema: {
    type: "object",
    properties: {
      outbox_path: { type: "string", description: "Path to DONE outbox message" },
      skip_review: { type: "boolean", default: false, description: "Skip A/B, auto-approve" },
      model: { type: "string", enum: ["haiku", "sonnet"], default: "haiku" }
    },
    required: ["outbox_path"]
  }
}

// Tool: delivery_status
{
  name: "delivery_status",
  description: "Get current delivery pipeline status",
  inputSchema: {
    type: "object",
    properties: {
      terminal: { type: "string", description: "Filter by terminal" },
      status: { type: "string", enum: ["all", "inbox", "active", "review", "done", "archive"] }
    }
  }
}

// Tool: revision_request
{
  name: "revision_request",
  description: "Send item back for revision with feedback",
  inputSchema: {
    type: "object",
    properties: {
      outbox_path: { type: "string" },
      feedback: { type: "string" },
      iteration: { type: "number", default: 1 }
    },
    required: ["outbox_path", "feedback"]
  }
}
```

### 5.3 Kanban Board Tools

```typescript
// Tool: kanban_snapshot
{
  name: "kanban_snapshot",
  description: "Get full dual-track kanban board state",
  inputSchema: {
    type: "object",
    properties: {
      include_discovery: { type: "boolean", default: true },
      include_delivery: { type: "boolean", default: true },
      terminal_filter: { type: "array", items: { type: "string" } }
    }
  }
}

// Tool: kanban_metrics
{
  name: "kanban_metrics",
  description: "Get kanban flow metrics (WIP, cycle time, throughput)",
  inputSchema: {
    type: "object",
    properties: {
      period_days: { type: "number", default: 7 },
      breakdown: { type: "string", enum: ["track", "terminal", "priority"] }
    }
  }
}
```

---

## 6. Review Quality Presets

```yaml
# quality-presets.yaml

presets:
  fast:
    # Minimal review, max speed
    discovery_review:
      enabled: false
    delivery_review:
      enabled: false
    estimated_token_multiplier: 1.0x

  standard:
    # Single reviewer, balanced
    discovery_review:
      enabled: true
      reviewers: 1
      model: haiku
    delivery_review:
      enabled: true
      reviewers: 1
      model: haiku
    estimated_token_multiplier: 1.5x

  quality:
    # A/B review, recommended for production
    discovery_review:
      enabled: true
      reviewers: 2
      model: sonnet
      consensus_threshold: 0.8
    delivery_review:
      enabled: true
      reviewers: 2
      model: haiku
      approval_threshold: 2
    estimated_token_multiplier: 2.5x

  rigorous:
    # Full review with iterations
    discovery_review:
      enabled: true
      reviewers: 2
      model: opus
      consensus_threshold: 0.9
      max_iterations: 3
    delivery_review:
      enabled: true
      reviewers: 2
      model: sonnet
      approval_threshold: 2
      max_iterations: 3
    estimated_token_multiplier: 4.0x
```

---

## 7. Kanban UI Components (React)

### 7.1 Component Hierarchy

```
KanbanApp
├── KanbanHeader
│   ├── QualityPresetSelector (fast/standard/quality/rigorous)
│   ├── TokenEstimator
│   └── RefreshButton
├── DiscoveryBoard
│   ├── Column (IDEAS)
│   │   └── Card[] (draggable)
│   ├── Column (SELECTED)
│   ├── Column (DEBATE) [optional, if review enabled]
│   │   └── ReviewProgress (A/B status)
│   ├── Column (CONSENSUS)
│   └── Column (QUEUE)
│       └── DoRBadge (ready/not-ready)
├── DoRGate (visual separator)
├── DeliveryBoard
│   ├── TerminalSwimlane[] (kernel, fe, joinery...)
│   │   ├── Column (INBOX)
│   │   ├── Column (ACTIVE)
│   │   │   └── SessionIndicator (tmux status)
│   │   ├── Column (REVIEW) [optional]
│   │   │   └── ReviewProgress (A✓/B✗)
│   │   ├── Column (DONE)
│   │   └── Column (ARCHIVE) [collapsed]
│   └── WIPLimitIndicator
└── KanbanFooter
    ├── MetricsBar (throughput, cycle time)
    └── TelegramActions (nudge, notify)
```

### 7.2 Card Component

```typescript
interface KanbanCard {
  id: string;
  title: string;
  track: 'discovery' | 'delivery';
  status: string;
  priority: 'critical' | 'high' | 'medium' | 'low';
  terminal?: string;           // delivery only
  assignee?: string;
  created_at: string;
  updated_at: string;

  // Review state
  review_enabled: boolean;
  review_status?: 'pending' | 'in_progress' | 'approved' | 'rejected';
  reviewer_a_status?: 'pending' | 'approved' | 'rejected';
  reviewer_b_status?: 'pending' | 'approved' | 'rejected';
  iteration: number;

  // Metadata
  blocked_by?: string[];
  dependencies?: string[];
  complexity?: 'S' | 'M' | 'L' | 'XL';
  estimated_tokens?: number;
}
```

---

## 8. API Endpoints

### 8.1 REST API

```
# Discovery
GET    /api/kanban/discovery                    # Full discovery board
GET    /api/kanban/discovery/ideas              # Ideas column
POST   /api/kanban/discovery/review             # Trigger A/B review
POST   /api/kanban/discovery/promote            # Move to delivery (DoR)

# Delivery
GET    /api/kanban/delivery                     # Full delivery board
GET    /api/kanban/delivery/:terminal           # Terminal swimlane
POST   /api/kanban/delivery/review              # Trigger A/B review
POST   /api/kanban/delivery/approve             # Approve & archive
POST   /api/kanban/delivery/reject              # Reject & revision

# Board
GET    /api/kanban/snapshot                     # Full dual-track state
GET    /api/kanban/metrics                      # Flow metrics
PUT    /api/kanban/settings                     # Update quality preset
GET    /api/kanban/events                       # SSE stream

# Actions
POST   /api/kanban/nudge/:terminal              # Telegram nudge
POST   /api/kanban/move                         # Drag & drop update
```

### 8.2 SSE Events

```typescript
interface KanbanEvent {
  type: 'card_moved' | 'review_started' | 'review_completed' |
        'session_active' | 'session_idle' | 'metrics_updated';
  track: 'discovery' | 'delivery';
  card_id?: string;
  from_column?: string;
  to_column?: string;
  terminal?: string;
  timestamp: string;
}
```

---

## 9. Token Cost Estimation

| Művelet | Model | Input tokens | Output tokens | Cost (approx) |
|---------|-------|--------------|---------------|---------------|
| Plan-select (TOP3) | Haiku | ~2000 | ~500 | $0.002 |
| Discovery A review | Sonnet | ~5000 | ~2000 | $0.03 |
| Discovery B review | Sonnet | ~5000 | ~2000 | $0.03 |
| Discovery merge | Sonnet | ~8000 | ~3000 | $0.05 |
| **Discovery total (quality)** | | | | **~$0.11** |
| | | | | |
| Delivery A review | Haiku | ~3000 | ~500 | $0.003 |
| Delivery B review | Haiku | ~3000 | ~500 | $0.003 |
| **Delivery total (quality)** | | | | **~$0.006** |
| | | | | |
| **Full pipeline (quality preset)** | | | | **~$0.12/item** |
| **Full pipeline (fast preset)** | | | | **~$0.002/item** |

---

## 10. Implementation Phases

### Phase 1: Backend API (2-3 nap)
- [ ] Kanban routes + services
- [ ] File system readers (planning/, mailbox/)
- [ ] Quality preset config
- [ ] SSE events

### Phase 2: Review Integration (2 nap)
- [ ] plan-debate.sh MCP bridge
- [ ] reviewer.sh MCP bridge
- [ ] Revision loop handling

### Phase 3: React Frontend (3-4 nap)
- [ ] Vite + React setup
- [ ] Board components
- [ ] Drag & drop
- [ ] SSE integration

### Phase 4: MCP Tools (2 nap)
- [ ] Discovery tools
- [ ] Delivery tools
- [ ] Kanban tools

### Phase 5: Polish (1-2 nap)
- [ ] Metrics dashboard
- [ ] Telegram integration
- [ ] Documentation

---

## 11. Fájl struktúra

```
datahaven-kanban/
├── package.json
├── vite.config.ts
├── .env
├── src/
│   ├── server/                    # Backend (3-layer)
│   │   ├── server.ts
│   │   ├── routes/
│   │   │   ├── discoveryRoutes.ts
│   │   │   ├── deliveryRoutes.ts
│   │   │   ├── kanbanRoutes.ts
│   │   │   └── sseRoutes.ts
│   │   ├── services/
│   │   │   ├── discoveryService.ts
│   │   │   ├── deliveryService.ts
│   │   │   ├── reviewService.ts
│   │   │   └── metricsService.ts
│   │   └── data/
│   │       ├── fileReader.ts      # Read planning/, mailbox/
│   │       ├── configLoader.ts    # Quality presets
│   │       └── eventEmitter.ts
│   │
│   ├── client/                    # Frontend (React)
│   │   ├── App.tsx
│   │   ├── components/
│   │   │   ├── KanbanBoard.tsx
│   │   │   ├── DiscoveryBoard.tsx
│   │   │   ├── DeliveryBoard.tsx
│   │   │   ├── Column.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── ReviewProgress.tsx
│   │   │   ├── DoRGate.tsx
│   │   │   ├── QualitySelector.tsx
│   │   │   └── MetricsBar.tsx
│   │   ├── hooks/
│   │   │   ├── useKanban.ts
│   │   │   ├── useSSE.ts
│   │   │   └── useDragDrop.ts
│   │   └── styles/
│   │       └── kanban.css
│   │
│   └── mcp/                       # MCP Tools
│       ├── discoveryTools.ts
│       ├── deliveryTools.ts
│       └── kanbanTools.ts
│
├── config/
│   ├── quality-presets.yaml
│   ├── plan-config.yaml
│   └── reviewer-config.yaml
│
└── scripts/
    └── install-service.sh
```

---

## 12. Összefoglaló

| Feature | Discovery | Delivery |
|---------|-----------|----------|
| **Oszlopok** | IDEAS → SELECTED → DEBATE → CONSENSUS → QUEUE | INBOX → ACTIVE → REVIEW → DONE → ARCHIVE |
| **A/B Review** | ✅ Opcionális (Sonnet) | ✅ Opcionális (Haiku) |
| **Revision loops** | max 2 | max 3 |
| **Quality gate** | DoR (Definition of Ready) | APPROVED decision |
| **MCP támogatás** | ✅ 3 tool | ✅ 3 tool |
| **Token cost** | ~$0.11/item (quality) | ~$0.006/item (quality) |
| **Preset override** | ✅ per-item | ✅ per-item |

---

**Következő lépés:** Elfogadod a spec-et? Ha igen, melyik fázissal kezdjük?
