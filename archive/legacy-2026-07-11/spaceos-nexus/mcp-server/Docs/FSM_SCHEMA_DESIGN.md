# FSM Schema Design — Workflow State Tracking

## Purpose
This document defines the database schema and transition rules for the Finite State Machine (FSM) tracking system in EPIC-11.

## ER Diagram (FSM Schema)

```mermaid
erDiagram
    workflow_definitions ||--o{ workflow_states : "defines"
    roles ||--o{ agent_sessions : "has"
    workflow_definitions ||--o{ agent_sessions : "governs"
    agent_sessions ||--o{ session_history : "logs"

    workflow_definitions {
        string workflow_id PK
        string name
        string track "delivery | discovery"
        string states "JSON array"
        string version
    }

    workflow_states {
        string workflow_id FK, PK
        string state_name PK
        int state_order
        string valid_transitions "JSON array"
    }

    agent_sessions {
        string session_id PK "UUID"
        string domain FK
        string role_name FK
        string workflow_id FK
        string current_state
        string last_action
        datetime updated_at
        datetime created_at
    }

    session_history {
        int id PK
        string session_id FK
        string state_before
        string state_after
        string action
        string metadata "JSON"
        datetime timestamp
    }
```

## FSM State Diagram (7-State Standard)

```mermaid
stateDiagram-v2
    [*] --> INITIALIZED : bootstrap_agent()

    INITIALIZED --> IN_PROGRESS : start_work / action
    IN_PROGRESS --> IN_PROGRESS : intermediate_action

    IN_PROGRESS --> UNDER_REVIEW : submit_for_review

    UNDER_REVIEW --> NEEDS_REVISION : reject_review
    NEEDS_REVISION --> IN_PROGRESS : retry_work

    UNDER_REVIEW --> APPROVED : approve_review

    APPROVED --> READY_FOR_MERGE : prepare_merge

    READY_FOR_MERGE --> COMPLETED : finalize_merge

    COMPLETED --> [*]
```

## Migration Rationale
- **Decoupling**: Sessions are tracked separately from role definitions to allow multiple active sessions for the same role.
- **Auditability**: `session_history` provides a full audit trail of how an agent progressed through a task.
- **Flexibility**: `workflow_definitions` allow for different tracks (delivery vs discovery) without changing the core session logic.
- **Performance**: Composite indexes on `(domain, role_name)` and FKs ensure fast lookups during bootstrap and transitions.

## Implementation Details
- **Migration**: `src/metadata/migrations/004_epic11_fsm_schema.sql`
- **Típusok**: `src/metadata/FSMSchema.ts`
