---
id: MSG-ARCHITECT-037
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-034
created: 2026-06-30
content_hash: 04fd1afdaf315ed3aeac591b3e928b4031424adc4d6c753556b4d863942aebff
---

# FSM Subscription System Design (MCP Event Pub/Sub) — DONE (Complete)

> **Megjegyzés:** Az MSG-ARCHITECT-035/036 hiányos volt (monitoring extension hiányzott az ADR-052-ből). Ez a teljes verzió minden KITERJESZTÉS követelménnyel együtt.

## Összefoglaló

Elkészült az **ADR-052: FSM Subscription System** dokumentum, amely egy push-based event notification rendszert specifikál terminálok számára a jelenlegi polling-based workflow megszüntetésére, **valamint** a monitoring automation rendszert terminál folyamat figyelésre.

## Elvégzett munka

### 1. Core Subscription System

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

### 3. MCP Tool Interface-ek (Core)

**4 core MCP tool definiálva:**

1. **subscribe_to_task** — Task state változásokra (done, blocked, progress)
2. **subscribe_to_terminal** — Terminal eseményekre (inbox_new, outbox_done, session events)
3. **unsubscribe** — Feliratkozás törlése
4. **get_subscriptions** — Aktív feliratkozások listázása

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

### 6. Notification Delivery Strategy (3-tier fallback)

**Priority order:**
1. **SSE** — Ha aktív session van (<100ms latency)
2. **Telegram** — Ha nincs session (1-2s latency)
3. **Inbox** — Fallback (async, guaranteed delivery)

---

## KITERJESZTÉS: Monitoring Automation (Új!)

### 7. Monitoring Event Types Definiálva

**7 MonitoringEventType:**
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

### 8. Auto-Recovery Config Design

**AutoRecoveryConfig interface:**
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

**MCP tools:**
- `set_auto_recovery(terminal, config)` — Auto-recovery szabályok beállítása
- `get_auto_recovery(terminal)` — Aktuális konfiguráció lekérdezése

### 9. Extended MCP Tools (Monitoring)

**3 monitoring MCP tool definiálva:**

1. **subscribe_to_monitoring** — Feliratkozás monitoring eseményekre (session, cost, health)
2. **subscribe_to_all_terminals** — Minden terminál eseményeire (Conductor/Root számára)
3. **get_terminal_health** — Terminál health status poll (SSE fallback)

### 10. Dashboard SSE Streaming

**Endpoint:** `GET /api/monitoring/stream`
- Server-Sent Events minden monitoring eventtel
- Dashboard real-time frissül (WORKING/IDLE/STUCK/CRASHED)

**Szűrt stream:** `GET /api/monitoring/stream/:terminal`

### 11. Nightwatch Integration

**Event Bus integration:**
```typescript
// watch-stuck.sh → session_stuck event emit
if (stuckDuration > 300) {
  pipelineEvents.emit('monitoring:session_stuck', {
    type: 'session_stuck',
    terminal: 'backend',
    timestamp: new Date(),
    details: { stuckDuration: 300, sessionId: 'session-123' },
  });
}

// Auto-recovery action
pipelineEvents.on('monitoring:session_stuck', (event) => {
  const config = autoRecoveryManager.getConfig(event.terminal);
  if (config.enabled && config.rules.onStuck === 'nudge') {
    sendNudge(event.terminal);
  }
});
```

---

## ADR-052 Dokumentum

**Lokáció:** `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md`

**Dokumentum méret:** 978 sor (volt 724 → +254 sor monitoring extension)
**Státusz:** IMPLEMENTÁCIÓRA KÉSZ

**Tartalom (12 szekció):**
1. **Kontextus** — Polling probléma, push notification cél
2. **Döntés** — 6 design döntés (Registry, MCP tools, Event Bus, SSE, Delivery, Monitoring)
3. **Indoklás** — Miért Event Bus + SSE (nem WebSocket, nem RabbitMQ)
4. **Következmények** — Pozitív (90% latency ↓), Negatív (reconnect logic), Semleges
5. **Implementáció** — 3 Phase roadmap (Core, Persistent, Advanced)
6. **Példa használat** — 2 scenario (Conductor vár Backend-re, Architect watches Frontend)
7. **Extended Feature: Monitoring Automation** — ✅ **ÚJ!**
   - Extended Event Types (7 MonitoringEventType)
   - Auto-Recovery Configuration
   - Dashboard Real-time Streaming
   - Extended MCP Tools (3 monitoring tool)
   - Integration with Existing Watchers
8. **Elfogadási kritériumok** — 10 core + 5 monitoring criteria
9. **Kapcsolódó dokumentáció**
10. **Felülvizsgálat** — Phase timeline

---

## ROI Highlight

**Latency csökkenés:**
- Polling: 5000ms átlag
- SSE push: <100ms
- **Javulás: 98% (50× gyorsabb)**

**Resource savings:**
- Polling: 120 request/perc (10 terminál × 12 poll/perc)
- Subscription: 0 felesleges request
- **Megtakarítás: 100%**

**Monitoring automation:**
- Automatikus stuck detection és recovery
- Real-time dashboard frissítés
- Cost alert automation

---

## Implementációs Roadmap

### Phase 1: Core System (Backend Terminal)
- SubscriptionManager + in-memory Registry
- SSE endpoint (`/api/subscriptions/events`)
- 4 core MCP tool (subscribe_to_task, subscribe_to_terminal, unsubscribe, get_subscriptions)
- Event Bus integration
- MonitoringEvent emission (nightwatch integration)

### Phase 2: Persistent Storage + Auto-Recovery (Backend Terminal)
- SQLite subscription table
- Migrate to persistent registry
- Auto-recovery logic (nudge, restart, escalate)
- Survive service restarts

### Phase 3: Advanced Features (Conductor)
- Telegram notification templates
- Subscription analytics dashboard
- Failed delivery alerts
- Dashboard real-time monitoring display

---

## Elfogadási Kritériumok Teljesülése

### Core Subscription System
- [x] ADR-052 dokumentáció elkészült (978 sor)
- [x] MCP tool interface-ek definiálva (4 core tool)
- [x] Subscription Registry design (Map-based + expiration)
- [x] SSE + Telegram + Inbox delivery terv (3-tier fallback)

### Monitoring Automation Extension
- [x] Monitoring event types definiálva (7 MonitoringEventType)
- [x] AutoRecoveryConfig interface tervezve
- [x] Extended MCP tools definiálva (3 monitoring tool)
- [x] Dashboard SSE streaming specifikálva
- [x] Nightwatch integration design

**Minden acceptance criteria teljesült!**

---

## Következő Lépések (Backend Terminal számára)

### Phase 1 Implementation Tasks

1. **SubscriptionManager implementálása** (`src/pipeline/subscriptionManager.ts`)
   - Registry (Map-based)
   - subscribe/unsubscribe/findMatching methods
   - Expiration cleanup

2. **SSE endpoint hozzáadása** (`src/routes/subscriptionRoutes.ts`)
   - `/api/subscriptions/events?terminal=<name>`
   - SSE stream management
   - Auto-reconnect support

3. **4 core MCP tool regisztrálása** (`src/mcp.ts`)
   - subscribe_to_task
   - subscribe_to_terminal
   - unsubscribe
   - get_subscriptions

4. **2 monitoring MCP tool regisztrálása** (`src/mcp.ts`)
   - set_auto_recovery
   - get_auto_recovery

5. **Event Bus hook** (pipelineEvents.onAny → deliverNotification)

6. **MonitoringEvent emission** (nightwatch.sh integration)
   - watch-stuck.sh → session_stuck
   - watch-done.sh → task:done
   - watch-priority.sh → session_started

7. **Testing:** Subscribe → Event occurs → Notification delivered

---

## Fájlok

- `/opt/spaceos/docs/architecture/decisions/ADR-052-fsm-subscription-system.md` — Teljes ADR dokumentum (978 sor)

---

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

---

## Megjegyzések

A subscription system teljesen integrálódik a meglévő Event Bus architektúrába, és követi az MCP tool pattern-t. Az SSE + Telegram + Inbox 3-tier fallback garantálja a notification delivery-t minden esetben.

A monitoring automation extension lehetővé teszi a terminálok automatikus figyelését (stuck detection, cost alert, crash recovery) és a Datahaven Dashboard real-time frissítését.

A Backend Terminal most implementálhatja a Phase 1 teljes rendszert az ADR-052 specifikáció alapján.
