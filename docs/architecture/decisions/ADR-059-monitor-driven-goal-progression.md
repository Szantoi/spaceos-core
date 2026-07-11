# ADR-059: Monitor-Driven Goal Progression System

**Status:** Accepted
**Date:** 2026-07-04
**Author:** Root Terminal
**Deciders:** Root, Conductor, Monitor

## Context

Mode #4 (Structured Program Execution) requires continuous progress toward project goals. The previous approach of keeping Conductor (Sonnet model) running continuously is cost-inefficient:

- Sonnet costs ~10x more than Haiku
- Large context window maintained even when idle
- No proactive work detection

### Research Findings (2026-07-04)

Web research revealed several 2026 AI agent orchestration patterns:

1. **Heartbeat + Plateau Detection** (CORAL Framework)
   - Polling every 5 seconds with plateau triggers
   - Source: [arxiv.org/pdf/2604.01658](https://arxiv.org/pdf/2604.01658)

2. **Event-Triggered Activation**
   - Agents monitor external signals and initiate workflows proactively
   - Source: [arxiv.org/pdf/2606.24937](https://arxiv.org/pdf/2606.24937)

3. **3-Tier Severity System** (AI SRE Pattern)
   - INFO (logged) → WARNING (next cycle) → CRITICAL (immediate action)
   - Source: [arxiv.org/pdf/2604.03933](https://arxiv.org/pdf/2604.03933)

4. **Task-Decoupled Planning (TDP)**
   - DAG-based sub-goals with scoped contexts
   - 82% token reduction
   - Source: [arxiv.org/html/2604.11378v1](https://arxiv.org/html/2604.11378v1)

5. **Proactive vs Reactive Priority** (Agent.xpu)
   - Reactive: immediate, user-initiated
   - Proactive: background, system-initiated
   - Source: [arxiv.org/html/2506.24045v1](https://arxiv.org/html/2506.24045v1)

## Decision

Implement a **Monitor-Driven Goal Progression** system where:

1. **Conductor** (Sonnet) defines goals and completion criteria, then goes idle
2. **Monitor** (Haiku) continuously watches for criteria fulfillment
3. **Monitor** triggers Conductor when goals complete, providing full context

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CONDUCTOR (Sonnet)                        │
│  1. Dispatch task to terminal                                │
│  2. Create goal with completion_criteria                     │
│  3. Go idle (cost STOP)                                      │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼ Goal handoff via MCP
┌─────────────────────────────────────────────────────────────┐
│                     MONITOR (Haiku)                          │
│  • Watch goals every Nightwatch cycle (2 min)                │
│  • Check completion criteria:                                │
│    - DONE outbox patterns                                    │
│    - Checkpoint status in EPICS.yaml                         │
│    - Terminal states                                         │
│  • On completion → Trigger Conductor with context            │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼ Trigger when criteria met
┌─────────────────────────────────────────────────────────────┐
│                 CONDUCTOR (Sonnet) - Resumed                 │
│  • Receives goal completion notification                     │
│  • Continues toward next milestone                           │
│  • Creates new goal → cycle repeats                          │
└─────────────────────────────────────────────────────────────┘
```

### Cost Comparison

| Approach | Model | Runtime | Est. Cost/Hour |
|----------|-------|---------|----------------|
| Conductor always-on | Sonnet | Continuous | $3-5/hour |
| Monitor watches + Trigger | Haiku + Sonnet on-demand | Haiku continuous, Sonnet bursts | $0.50-1/hour |

**Estimated savings: 70-80%**

## Implementation

### 1. Goal Store

Location: `/opt/spaceos/store/goals/`

```yaml
# GOAL-{timestamp}-{sequence}.yaml
id: GOAL-2026-07-04-001
created: 2026-07-04T14:30:00Z
created_by: conductor
epic_id: EPIC-JT-CRM

goal:
  description: "JoineryTech CRM Backend API kész"
  checkpoint_id: CP-CRM-BACKEND

completion_criteria:
  - type: done_outbox
    terminal: backend
    message_pattern: "*crm*done*"

  - type: checkpoint_status
    checkpoint_id: CP-CRM-BACKEND
    expected_status: done

on_complete:
  trigger_terminal: conductor
  next_goal: "CP-CRM-FRONTEND indítása"
  prompt: |
    ✅ GOAL TELJESÜLT: {{goal.description}}

    Teljesült criteria:
    {{completed_criteria}}

    Következő lépés: {{on_complete.next_goal}}

status: watching  # watching | triggered | completed | expired
triggered_at: null
completed_at: null
```

### 2. MCP Tools

| Tool | Purpose |
|------|---------|
| `create_goal` | Conductor creates goal with criteria |
| `list_goals` | List goals by status |
| `get_goal` | Read specific goal |
| `complete_goal` | Mark goal as completed |
| `trigger_goal` | Monitor triggers Conductor |

### 3. Nightwatch Integration

New module: `watchGoals.ts`

```typescript
// Called every Nightwatch cycle (2 min)
export async function watchGoals(): Promise<WatchGoalsResult> {
  const activeGoals = await listGoals({ status: 'watching' });

  for (const goal of activeGoals) {
    const criteriaResults = await checkCriteria(goal.completion_criteria);

    if (criteriaResults.allMet) {
      await triggerGoalCompletion(goal, criteriaResults);
    }
  }

  return { checked: activeGoals.length, triggered: triggeredCount };
}
```

### 4. Criteria Types

| Type | What it checks |
|------|----------------|
| `done_outbox` | Terminal outbox matches pattern |
| `checkpoint_status` | EPICS.yaml checkpoint status |
| `terminal_idle` | Terminal session idle for X minutes |
| `message_status` | Specific message ID status |
| `all_tasks_done` | All tasks in list completed |

## Consequences

### Positive

- **Cost reduction**: 70-80% savings on model costs
- **Proactive execution**: Mode #4 continues without manual intervention
- **Clear handoff**: Goals are explicit, auditable, logged
- **Separation of concerns**: Monitor watches, Conductor decides

### Negative

- **Latency**: Up to 2 min delay between completion and trigger (Nightwatch cycle)
- **Complexity**: New goal management layer
- **Monitor dependency**: Monitor must be reliable

### Mitigations

- Critical goals can have shorter check intervals
- Goal expiration prevents stale goals
- Fallback: Manual trigger via MCP if Monitor fails

## References

- [ADR-053: Checkpoint-Based Coordination](./ADR-053-checkpoint-coordination-workflow.md)
- [CORAL Framework](https://arxiv.org/pdf/2604.01658)
- [Hitchhiker's Guide to Agentic AI](https://arxiv.org/pdf/2606.24937)
- [Autonomous AI SRE Agent](https://arxiv.org/pdf/2604.03933)
- [Scheduler-Theoretic Framework for LLM Agents](https://arxiv.org/html/2604.11378v1)
