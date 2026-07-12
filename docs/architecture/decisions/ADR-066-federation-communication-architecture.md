# ADR-066: Federation Communication Architecture

**Status:** Proposed
**Date:** 2026-07-12
**Author:** Root Terminal
**Deciders:** Root, Conductor, Nexus

## Context

A 4-island architektúra (SpaceOS, Nexus, JoineryTech, Doorstar) több szerverre is skálázható.
A jelenlegi szkript-alapú irányítás (nightwatch.sh, pipeline.sh, stb.) nem működik multi-server környezetben:

**Problémák:**
1. Shell szkriptek csak lokálisan futnak
2. Tmux session-ök nem érhetők el más szerverről
3. File-based inbox/outbox nem skálázódik
4. Nincs audit trail a cross-island kommunikációra
5. Nem üzemeltethető federált környezetben

## Decision

**Minden irányítási funkció MCP API-ra migrál.**

### Architektúra

```
┌─────────────────────────────────────────────────────────────┐
│                    SpaceOS (Kontroll)                       │
│                  Federation Control Center                   │
│                                                              │
│   root ────────── stratégiai döntések, felügyelet           │
│   conductor ───── koordináció, task dispatch                │
│   monitor ─────── health check, alert, metrikák             │
│   librarian ───── cross-island tudás szinkron               │
│   explorer ────── cross-island kutatás                      │
└──────────────────────────┬──────────────────────────────────┘
                           │ Federation API (HTTP/MCP)
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
    ┌──────────┐     ┌──────────┐     ┌──────────┐
    │  Nexus   │     │JoineryTech│    │ Doorstar │
    │  :3456   │     │  :3458   │     │  :3460   │
    │  (Infra) │     │   (Dev)  │     │(Customer)│
    └──────────┘     └──────────┘     └──────────┘
```

### Migrálandó szkriptek → MCP Tools

| Szkript | Új MCP Tool | Hol fut |
|---------|-------------|---------|
| `nightwatch.sh` | `mcp__federation__health_check` | SpaceOS |
| `watch-done.sh` | `mcp__federation__subscribe_done` | SpaceOS |
| `watch-inbox.sh` | `mcp__federation__inbox_watcher` | Minden island |
| `pipeline.sh` | `mcp__federation__dispatch` | SpaceOS |
| `reviewer.sh` | `mcp__federation__review_request` | SpaceOS |
| `session/*.sh` | `mcp__federation__session_*` | Minden island |

### Federation MCP API Specifikáció

#### 1. Health Check

```typescript
// GET /api/federation/health
interface FederationHealthResponse {
  islands: {
    id: string;
    url: string;
    status: 'healthy' | 'degraded' | 'offline';
    terminals: {
      name: string;
      status: 'working' | 'idle' | 'stuck';
      lastActivity: string;
    }[];
    latency_ms: number;
  }[];
  timestamp: string;
}
```

#### 2. Cross-Island Message Dispatch

```typescript
// POST /api/federation/dispatch
interface FederationDispatchRequest {
  from_island: string;
  from_terminal: string;
  to_island: string;
  to_terminal: string;
  message: {
    type: 'task' | 'done' | 'blocked' | 'query';
    priority: 'critical' | 'high' | 'medium' | 'low';
    content: string;
    ref?: string;
  };
}
```

#### 3. Subscription (Event-Driven)

```typescript
// POST /api/federation/subscribe
interface FederationSubscription {
  subscriber_island: string;
  subscriber_terminal: string;
  target_island: string;
  target_terminal: string;
  events: ('done' | 'blocked' | 'health')[];
  delivery: 'webhook' | 'polling' | 'sse';
  callback_url?: string;
}
```

#### 4. Session Management (Cross-Island)

```typescript
// POST /api/federation/session/start
interface FederationSessionStart {
  target_island: string;
  target_terminal: string;
  model: 'sonnet' | 'opus' | 'haiku';
  prompt: string;
  from_terminal: string;
  from_island: string;
}
```

### Kommunikációs Protokoll

```
SpaceOS                    JoineryTech
   │                           │
   │  POST /api/federation/dispatch
   │ ─────────────────────────>│
   │                           │
   │  { "ack": true, "id": "..." }
   │ <─────────────────────────│
   │                           │
   │      ... work happens ... │
   │                           │
   │  POST /api/federation/callback (DONE)
   │ <─────────────────────────│
   │                           │
```

### Security

1. **Island-to-Island Authentication:**
   - Minden island saját token-nel rendelkezik
   - Token rotation: 24 óránként automatikus
   - Token tárolás: `.mcp-tokens` fájl (gitignored)

2. **Authorization Matrix:**
   | From \ To | SpaceOS | Nexus | JoineryTech | Doorstar |
   |-----------|---------|-------|-------------|----------|
   | SpaceOS   | ✅ | ✅ | ✅ | ✅ |
   | Nexus     | ⚠️ health only | ✅ | ❌ | ❌ |
   | JoineryTech | ⚠️ done/blocked | ❌ | ✅ | ❌ |
   | Doorstar  | ⚠️ done/blocked | ❌ | ❌ | ✅ |

3. **Audit Trail:**
   - Minden cross-island hívás naplózva
   - `logs/federation/YYYY-MM-DD.jsonl` formátum

### Implementációs Terv

**Phase 1: Foundation (1-2 hét)**
- [ ] Federation API endpoints a knowledge-service-ben
- [ ] Island discovery és registration
- [ ] Health check endpoint

**Phase 2: Messaging (1 hét)**
- [ ] Cross-island dispatch
- [ ] Subscription management
- [ ] Callback handling

**Phase 3: Session Management (1 hét)**
- [ ] Remote session start/stop
- [ ] Session status query
- [ ] Prompt injection cross-island

**Phase 4: Migration (2 hét)**
- [ ] nightwatch.sh → MCP
- [ ] pipeline.sh → MCP
- [ ] Szkriptek deprecation

### Backwards Compatibility

**Átmeneti időszak:**
- Szkriptek továbbra is működnek lokálisan
- Új MCP toolok párhuzamosan elérhetők
- Fokozatos migráció island-onként

**Deprecation schedule:**
- Phase 1-2: Szkriptek + MCP párhuzamosan
- Phase 3-4: Szkriptek soft-deprecation (warning log)
- +1 hónap: Szkriptek hard-deprecation (eltávolítás)

## Consequences

### Pozitív

1. **Multi-server deployment** lehetséges
2. **Audit trail** minden cross-island akcióra
3. **Egységes API** minden island-on
4. **Üzemeltethető** federált környezetben
5. **Skálázható** új island-ok hozzáadhatók

### Negatív

1. **Komplexitás növekedés** — HTTP helyett file-based
2. **Latency** — network overhead
3. **Migration effort** — 2-4 hét munka

### Kockázatok

1. **Network partition** — island offline → queue-ing szükséges
2. **Token security** — rotation és tárolás kritikus
3. **Breaking changes** — terminálok CLAUDE.md frissítése szükséges

## Alternatives Considered

1. **RabbitMQ/Kafka** — túl komplex ehhez a mérethez
2. **gRPC** — nincs szükség bináris protokollra
3. **WebSocket only** — nem elég robusztus
4. **File-sync (rsync)** — nem real-time, nem auditálható

## References

- ADR-041: Graph-Based Workflow
- ADR-053: Checkpoint-Based Coordination
- docs/knowledge/patterns/MESSAGING_ARCHITECTURE.md
