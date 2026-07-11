# FSM Subscription Skill

> Feliratkozás task és terminál eseményekre push notification-nel (polling helyett).

## Mikor használd

- Ha várnod kell egy másik terminál DONE/BLOCKED üzenetére
- Ha értesítést akarsz kapni amikor új inbox üzenet érkezik
- Ha session indulásról/leállásról akarsz tudni
- Ha real-time koordinációra van szükség (<100ms latency)

## MCP Toolok

### 1. subscribe_to_task
Feliratkozás task állapotváltozásra.

```bash
# MCP hívás
{
  "name": "subscribe_to_task",
  "arguments": {
    "terminal": "conductor",           # Ki iratkozott fel
    "task_id": "MSG-BACKEND-100",      # Melyik task-ra
    "events": ["done", "blocked"],     # Milyen eseményekre
    "delivery_method": "sse"           # sse | telegram | inbox
  }
}
```

**Events:** `done`, `blocked`, `progress`

### 2. subscribe_to_terminal
Feliratkozás terminál eseményekre.

```bash
{
  "name": "subscribe_to_terminal",
  "arguments": {
    "terminal": "conductor",
    "target_terminal": "backend",
    "events": ["inbox_new", "outbox_done", "session_started"],
    "delivery_method": "sse"
  }
}
```

**Events:** `inbox_new`, `outbox_done`, `session_started`, `session_ended`

### 3. unsubscribe
Feliratkozás törlése.

```bash
{
  "name": "unsubscribe",
  "arguments": {
    "subscription_id": "uuid-here"
  }
}
```

### 4. get_subscriptions
Aktív feliratkozások listázása.

```bash
{
  "name": "get_subscriptions",
  "arguments": {
    "terminal": "conductor"
  }
}
```

## SSE Endpoint

Ha SSE delivery method-ot választasz, nyiss kapcsolatot:

```bash
curl -N "http://localhost:3456/api/subscriptions/events?terminal=conductor"
```

**Response stream:**
```
data: {"type":"connected","terminal":"conductor","timestamp":"..."}
data: {"type":"init","subscriptions":[...],"timestamp":"..."}
data: {"type":"task","event":{"name":"outbox:done","task_id":"MSG-BACKEND-100"}}
```

## Használati Példák

### Conductor vár Backend DONE-ra

```typescript
// 1. Feliratkozás
const sub = await mcp.subscribe_to_task({
  terminal: "conductor",
  task_id: "MSG-BACKEND-100",
  events: ["done", "blocked"],
  delivery_method: "sse"
});

// 2. SSE listener
const es = new EventSource("/api/subscriptions/events?terminal=conductor");
es.onmessage = (e) => {
  const data = JSON.parse(e.data);
  if (data.type === "task" && data.event.name === "outbox:done") {
    console.log("Backend DONE!");
    // Process next task...
  }
};

// 3. Cleanup
await mcp.unsubscribe({ subscription_id: sub.id });
```

### Root figyeli az összes terminált

```typescript
// Feliratkozás minden terminál session eseményeire
for (const terminal of ["backend", "frontend", "architect"]) {
  await mcp.subscribe_to_terminal({
    terminal: "root",
    target_terminal: terminal,
    events: ["session_started", "session_ended", "outbox_done"],
    delivery_method: "sse"
  });
}
```

## Delivery Method Prioritás

1. **SSE** — Aktív session esetén (<100ms)
2. **Telegram** — Ha nincs aktív session (1-2s)
3. **Inbox** — Fallback, garantált delivery

## Expiration

- Default: 1 óra (3600s)
- Auto-cleanup: 60s intervallumonként
- Expired subscriptions automatikusan törlődnek

## Referencia

- **ADR-052:** `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md`
- **Implementáció:** `spaceos-nexus/knowledge-service/src/pipeline/subscriptionManager.ts`
- **MCP Tools:** `spaceos-nexus/knowledge-service/src/pipeline/subscriptionTools.ts`
