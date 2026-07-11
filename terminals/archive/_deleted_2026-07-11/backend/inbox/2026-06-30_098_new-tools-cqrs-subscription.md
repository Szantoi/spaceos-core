---
id: MSG-BACKEND-098
from: root
to: backend
type: info
priority: medium
status: READ
model: haiku
created: 2026-06-30
content_hash: 0010e607dac4d6f1ea4b740044c2dbd94ae38372efb54679bdb6679576395022
---

# Új Toolok Elérhetők — Backend Terminál

2 új tool implementálva a mai napon. Használd ezeket a fejlesztéshez.

## 1. CQRS Handler Generator

**Skill:** `/cqrs-handler-generator`

**Script:** `/opt/spaceos/scripts/codegen/generate-handler.sh`

### Query Handler
```bash
./generate-handler.sh GetOrderStatus \
  --type query \
  --module Procurement \
  --repository IProcurementRepository \
  --aggregate Order \
  --properties '[{"name":"OrderId","type":"Guid"}]'
```

**Generált fájlok:** Query.cs, QueryHandler.cs, Response.cs

### Command Handler
```bash
./generate-handler.sh WithdrawComplaint \
  --type command \
  --module Procurement \
  --repository IComplaintRepository \
  --aggregate Complaint \
  --properties '[{"name":"ComplaintId","type":"Guid"},{"name":"Reason","type":"string"}]'
```

**Generált fájlok:** Command.cs, CommandHandler.cs

**ROI:** 90% időmegtakarítás (20 perc → 2 perc per handler)

## 2. FSM Subscription MCP Tools

**Skill:** `/fsm-subscription`

Ha várnod kell egy másik terminál DONE-jára, használd:

```typescript
// Feliratkozás task-ra
await mcp.subscribe_to_task({
  terminal: "backend",
  task_id: "MSG-FRONTEND-050",
  events: ["done", "blocked"],
  delivery_method: "sse"
});

// Aktív subscriptions
await mcp.get_subscriptions({ terminal: "backend" });

// Leiratkozás
await mcp.unsubscribe({ subscription_id: "uuid" });
```

**SSE Endpoint:** `GET /api/subscriptions/events?terminal=backend`

## Skill Fájlok

```
.claude/skills/
├── cqrs-handler-generator/SKILL.md
└── fsm-subscription/SKILL.md
```

Részletek: `/opt/spaceos/docs/architecture/decisions/ADR-051-cqrs-handler-generator.md`
