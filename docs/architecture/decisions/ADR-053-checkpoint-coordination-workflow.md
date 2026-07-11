# ADR-053: Checkpoint-Based Coordination Workflow

**Status:** Accepted
**Date:** 2026-07-01
**Author:** Root
**Relates to:** ADR-049 (Dual Session), ADR-052 (FSM Subscription)

## Context

A terminálok gyakran "elakadnak" mert:
1. Nincs explicit visszajelzés a task felvételéről
2. Nincs meghatározott checkpoint ahol koordináció történik
3. A trigger/ébresztő mechanizmus nem megbízható

## Decision

### 1. Epic/Projekt Tervezési Fázis

Minden epic létrehozásakor **kötelező meghatározni a checkpoint-okat**:

```yaml
# EPICS.yaml példa
- id: EPIC-DATAHAVEN-UI
  name: Datahaven Dashboard UI Redesign
  checkpoints:
    - id: CP-1
      name: "Bento Grid Layout DONE"
      trigger_to: conductor
      condition: "MSG-FRONTEND-064 status=DONE"
    - id: CP-2
      name: "KPI Cards Implementation Start"
      trigger_to: root
      condition: "MSG-FRONTEND-065 status=READ"
    - id: CP-3
      name: "All UI Components Complete"
      trigger_to: [root, conductor]
      condition: "EPIC-DATAHAVEN-UI status=done"
```

### 2. Task Felvétel Protocol

Minden terminál **kötelezően** jelzi MCP-n keresztül:

```typescript
// Task felvétel
mcp__spaceos-knowledge__ack_task({
  terminal: "frontend",
  message_id: "MSG-FRONTEND-065"
});
// → Automatikusan: register_working + inbox READ

// Ha nem jön ACK 5 percen belül → alert
```

### 3. Subscription-Based Triggers (Ébresztő Óra)

A `subscribe_to_task` MCP tool-t használjuk checkpoint figyelésre:

```typescript
// Conductor feliratkozik a Frontend task-ra
mcp__spaceos-knowledge__subscribe_to_task({
  terminal: "conductor",
  task_id: "MSG-FRONTEND-065",
  events: ["done", "blocked"],
  delivery_method: "telegram",  // vagy "inbox"
  expires_in: 86400  // 24 óra
});

// Visszakapott subscription_id-t tárolni kell
// Ha trigger tüzelt → explicit unsubscribe szükséges
```

### 4. Explicit Unsubscribe (Kikapcsolás)

A trigger **nem tűnik el automatikusan** — explicit ki kell kapcsolni:

```typescript
// Miután feldolgoztuk a triggert
mcp__spaceos-knowledge__unsubscribe({
  subscription_id: "uuid-from-subscribe"
});
```

### 5. Checkpoint Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                    EPIC TERVEZÉS                                │
├─────────────────────────────────────────────────────────────────┤
│ 1. Epic létrehozás (EPICS.yaml)                                 │
│ 2. Checkpoint-ok definiálása (CP-1, CP-2, ...)                 │
│ 3. Trigger subscription minden checkpoint-ra                   │
│ 4. Task dispatch első terminálnak                              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    TASK VÉGREHAJTÁS                             │
├─────────────────────────────────────────────────────────────────┤
│ 1. Terminál kap inbox üzenetet                                 │
│ 2. ack_task() → MCP visszajelzés (5 perc timeout)             │
│ 3. register_working() → státusz frissítés                      │
│ 4. Dolgozik...                                                  │
│ 5. complete_task() → DONE/BLOCKED outbox                       │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    CHECKPOINT TRIGGER                           │
├─────────────────────────────────────────────────────────────────┤
│ 1. Subscription tüzel (done/blocked event)                     │
│ 2. Coordinator kap értesítést (Telegram/inbox/SSE)            │
│ 3. Coordinator feldolgoz:                                       │
│    - Review DONE                                                │
│    - Dispatch következő task                                   │
│    - Update epic status                                        │
│ 4. unsubscribe() — trigger kikapcsolása                        │
│ 5. Új subscription a következő checkpoint-ra                   │
└─────────────────────────────────────────────────────────────────┘
```

## MCP Tools Summary

| Tool | Mikor | Ki használja |
|------|-------|--------------|
| `ack_task` | Task inbox megérkezett | Terminál |
| `register_working` | Munka megkezdése | Terminál |
| `subscribe_to_task` | Checkpoint figyelés | Conductor/Root |
| `subscribe_to_terminal` | Terminál állapot figyelés | Conductor/Root |
| `unsubscribe` | Trigger feldolgozás után | Conductor/Root |
| `complete_task` | Munka befejezése | Terminál |

## Timeout és Alert Szabályok

| Esemény | Timeout | Akció |
|---------|---------|-------|
| Task inbox → nincs ACK | 5 perc | Alert Root + Conductor |
| ACK → nincs DONE | 24 óra | Stuck alert |
| Checkpoint trigger → nincs unsubscribe | 1 óra | Warning log |
| Subscription lejár | expires_in | Auto cleanup |

## Consequences

**Pozitív:**
- Nincs "elakadás" — minden lépés explicit visszajelzéssel
- Audit trail — ki, mikor, mit csinált
- Megbízható trigger — nem marad le semmi

**Negatív:**
- Több MCP hívás szükséges
- Termináloknak be kell tartani a protocolt

## Implementation

1. **Phase 1:** Update Conductor/Root CLAUDE.md
2. **Phase 2:** Add checkpoint support to EPICS.yaml schema
3. **Phase 3:** Auto-subscription on epic creation
4. **Phase 4:** Dashboard visualization (checkpoint progress)
