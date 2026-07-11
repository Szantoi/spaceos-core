# Goal Store

**Location:** `/opt/spaceos/store/goals/`
**Purpose:** Monitor-Driven Goal Progression (ADR-059)

## Overview

This directory contains active goals that Monitor watches for completion.
When all completion criteria are met, Monitor triggers Conductor to continue.

## File Naming

```
GOAL-{YYYY-MM-DD}-{NNN}.yaml
```

Example: `GOAL-2026-07-04-001.yaml`

## Goal Lifecycle

```
watching → triggered → completed
              ↓
           expired (if timeout)
```

## YAML Schema

```yaml
id: GOAL-2026-07-04-001          # Unique ID
created: 2026-07-04T14:30:00Z    # ISO timestamp
created_by: conductor             # Terminal that created it
epic_id: EPIC-JT-CRM             # Related epic (optional)

goal:
  description: "Human-readable goal"
  checkpoint_id: CP-CRM-BACKEND  # Related checkpoint (optional)

completion_criteria:              # ALL must be satisfied
  - type: done_outbox
    terminal: backend
    message_pattern: "*crm*"

  - type: checkpoint_status
    checkpoint_id: CP-CRM-BACKEND
    expected_status: done

on_complete:
  trigger_terminal: conductor    # Who to notify
  next_goal: "Next step description"
  prompt: |
    Template for trigger message.
    Supports {{variables}}:
    - {{goal.description}}
    - {{completed_criteria}}
    - {{on_complete.next_goal}}

status: watching                 # watching | triggered | completed | expired
expires_at: 2026-07-05T14:30:00Z # Optional timeout
triggered_at: null
completed_at: null
trigger_message_id: null         # MSG-CONDUCTOR-XXX when triggered
```

## Criteria Types

| Type | Parameters | Checks |
|------|------------|--------|
| `done_outbox` | terminal, message_pattern | Outbox file matches pattern |
| `checkpoint_status` | checkpoint_id, expected_status | EPICS.yaml checkpoint |
| `message_status` | message_id, expected_status | Specific message status |
| `terminal_idle` | terminal, min_idle_minutes | Terminal idle time |
| `all_of` | criteria[] | All nested criteria met |
| `any_of` | criteria[] | Any nested criteria met |

## MCP Tools

```bash
# Create goal (Conductor uses this)
mcp__spaceos-knowledge__create_goal

# List goals by status
mcp__spaceos-knowledge__list_goals

# Get specific goal
mcp__spaceos-knowledge__get_goal

# Trigger goal completion (Monitor uses this)
mcp__spaceos-knowledge__trigger_goal

# Mark goal completed
mcp__spaceos-knowledge__complete_goal
```

## Logs

Goal events are logged to:
- `/opt/spaceos/logs/dispatcher/goals.log`
- Nightwatch cycle results (SSE events)

## Related

- ADR-059: Monitor-Driven Goal Progression
- ADR-053: Checkpoint-Based Coordination
- EPICS.yaml: Epic and checkpoint definitions
