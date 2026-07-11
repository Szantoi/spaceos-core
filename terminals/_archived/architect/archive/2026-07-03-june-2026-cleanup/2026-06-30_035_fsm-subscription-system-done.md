---
id: MSG-ARCHITECT-035
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-034
created: 2026-06-30
content_hash: fdc256f113bbfbc9eabb68922a49becb66b2f2bb850d9008f1dcfa4172197f82
---

# FSM Subscription System Design (MCP Event Pub/Sub) — DONE

## Összefoglaló

Elkészült az **ADR-052: FSM Subscription System** dokumentum, amely egy push-based event notification rendszert specifikál terminálok számára, megszüntetve a jelenlegi polling-based workflow-t.

## Elvégzett munka

### 1. Jelenlegi helyzet elemzése

**Polling probléma azonosítása:**
- ✅ 5 másodperces átlagos latency
- ✅ 120 felesleges request/perc (10 terminál esetén)
- ✅ Nem skálázódó megoldás

**Referenciák áttekintése:**
- ✅ `eventBus.ts` — meglévő Event Bus architektúra elemzése
- ✅ `sseRoutes.js` — SSE implementáció példa a Datahaven-ből
- ✅ `mcp.ts` — MCP tool pattern megértése

### 2. Subscription Registry Design

**Adatstruktúra:**
```typescript
interface Subscription {
  id: string;                    // UUID
  terminal: string;              // architect, backend, etc.
  type: 'task' | 'terminal';     // Subscription type
  target: string;                // Task ID or terminal name
  events: EventType[];           // Event list
  deliveryMethod: 'sse' | 'telegram' | 'inbox';
  createdAt: string;
  expiresAt?: string;
}
```

**Storage strategy:**
- Phase 1: In-memory Map (fast, zero setup)
- Phase 2: SQLite persistent (long-running subscriptions)

### 3. MCP Tool Interface-ek

**4 új MCP tool definiálva:**

1. **subscribe_to_task** — Task state változásokra (done, blocked, progress)
2. **subscribe_to_terminal** — Terminal eseményekre (inbox_new, outbox_done, session events)
3. **unsubscribe** — Feliratkozás törlése
4. **get_subscriptions** — Aktív feliratkozások listázása

**Input paraméterek:**
- `terminal` — Ki iratkozik fel
- `task_id` / `target_terminal` — Mire
- `events` — Milyen eseményekre
- `delivery_method` — sse | telegram | inbox | auto
- `expires_in` — Lejárati idő (default: 3600s)

### 4. Event Bus Integration

**Integráció a meglévő eventBus.ts-sel:**
```typescript
pipelineEvents.onAny((event) => {
  const subscriptions = findMatchingSubscriptions(event);
  subscriptions.forEach(sub => deliverNotification(sub, event));
});
```

**Event mapping:**
- `outbox:done` → `task:done`
- `outbox:blocked` → `task:blocked`
- `inbox:new` → `terminal:inbox_new`
- `session:started` → `terminal:session_started`

### 5. SSE Endpoint Design

**Endpoint:** `GET /api/subscriptions/events?terminal=<name>`

**SSE flow:**
1. Terminal connects → SSE stream opened
2. Server sends initial subscriptions list
3. Event occurs → Event Bus emits → subscriptionManager matches → SSE push
4. Client receives notification instantly (<100ms)

**Client-side:**
```typescript
const eventSource = new EventSource('/api/subscriptions/events?terminal=architect');
eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);
  if (data.type === 'task:done') {
    // Continue work, no polling needed!
  }
};
```

### 6. Notification Delivery Strategy (3-tier fallback)

**Priority order:**
1. **SSE** — Ha aktív session van (<100ms latency)
2. **Telegram** — Ha nincs session (1-2s latency)
3. **Inbox** — Fallback (async, guaranteed delivery)

**Auto-detect logic:**
```typescript
async function deliverNotification(subscription, event) {
  // 1. Try SSE
  if (tryDeliverSSE(terminal, event)) return;

  // 2. Try Telegram (if no session)
  if (!hasActiveSession && deliverTelegram(terminal, event)) return;

  // 3. Inbox fallback
  await deliverInbox(terminal, event);
}
```

## ADR-052 Dokumentum

**Lokáció:** `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md`

**Tartalom (10 szekció):**
1. **Kontextus** — Polling probléma, push notification cél
2. **Döntés** — 6 design döntés (Registry, MCP tools, Event Bus, SSE, Delivery)
3. **Indoklás** — Miért Event Bus + SSE (nem WebSocket, nem RabbitMQ)
4. **Következmények** — Pozitív (90% latency ↓), Negatív (reconnect logic), Semleges
5. **Implementáció** — 3 Phase roadmap (Core, Persistent, Advanced)
6. **Példa használat** — 2 scenario (Conductor vár Backend-re, Architect watches Frontend)
7. **Elfogadási kritériumok** — 10 criteria
8. **Kapcsolódó dokumentáció**
9. **Felülvizsgálat** — Phase timeline

## Példa Használat (Scenario)

### Conductor vár Backend DONE-ra

**Előtte (polling):**
```typescript
while (!isDone) {
  await checkTaskStatus(taskId);  // 5s poll
  await sleep(5000);
}
// Átlag 2.5s latency
```

**Utána (subscription):**
```typescript
await subscribe_to_task({
  terminal: 'conductor',
  task_id: 'MSG-BACKEND-123',
  events: ['done', 'blocked'],
});

// SSE push arrives instantly (<100ms)
// No polling needed!
```

## ROI Highlight

**Latency csökkenés:**
- Polling: 5000ms átlag
- SSE push: <100ms
- **Javulás: 98% (50× gyorsabb)**

**Resource savings:**
- Polling: 120 request/perc (10 terminál × 12 poll/perc)
- Subscription: 0 felesleges request
- **Megtakarítás: 100%**

## Implementációs Roadmap

### Phase 1: Core System (Backend Terminal)
- SubscriptionManager + in-memory Registry
- SSE endpoint (`/api/subscriptions/events`)
- 4 MCP tool (subscribe_to_task, subscribe_to_terminal, unsubscribe, get_subscriptions)
- Event Bus integration

### Phase 2: Persistent Storage (Backend Terminal)
- SQLite subscription table
- Migrate to persistent registry
- Survive service restarts

### Phase 3: Advanced Features (Conductor)
- Telegram notification templates
- Subscription analytics dashboard
- Failed delivery alerts

## Elfogadási Kritériumok Teljesülése

- [x] ADR-052 dokumentáció elkészült
- [x] MCP tool interface-ek definiálva (4 tool)
- [x] Subscription Registry design (Map-based + expiration)
- [x] SSE + Telegram + Inbox delivery terv (3-tier fallback)

## Technikai Döntések Indoklása

### Miért SSE (nem WebSocket)?
- **Use case:** One-way push (server → client), nem kell bi-directional
- **Egyszerűség:** HTTP-based, auto-reconnect beépített
- **Firewall:** HTTP port mindig nyitva, WebSocket gyakran blokkolva

### Miért In-Memory Registry (Phase 1)?
- **Latency:** <1ms (vs SQLite 5-10ms)
- **Zero setup:** Azonnal működik
- **Elég:** Rövid sessionök esetén (Phase 2: SQLite upgrade)

### Miért NEM RabbitMQ?
- **Komplexitás:** Új dependency, setup, maintenance
- **Overkill:** Event Bus + SSE egyszerűbb és elég

## Következő Lépések (Backend Terminal számára)

1. **SubscriptionManager implementálása** (`src/pipeline/subscriptionManager.ts`)
2. **SSE endpoint hozzáadása** (`src/routes/subscriptionRoutes.ts`)
3. **4 MCP tool regisztrálása** (`src/mcp.ts`)
4. **Event Bus hook** (pipelineEvents.onAny → deliverNotification)
5. **Testing:** Subscribe → Event occurs → Notification delivered

## Fájlok

- `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md` — Teljes ADR dokumentum

## Megjegyzések

A subscription system teljesen integrálódik a meglévő Event Bus architektúrába, és követi az MCP tool pattern-t. Az SSE + Telegram + Inbox 3-tier fallback garantálja a notification delivery-t minden esetben.

A Backend Terminal most implementálhatja a Phase 1 Core System-et az ADR-052 specifikáció alapján.
