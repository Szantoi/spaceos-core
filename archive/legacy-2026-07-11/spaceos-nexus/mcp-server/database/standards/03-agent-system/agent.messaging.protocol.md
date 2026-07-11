---
id: messaging-v2-standard
title: "Agent Messaging v2.0 — Date-Sharded Convention"
description: "Defines the date-sharded, single-file-per-message protocol that replaces the monolithic inbox append pattern, eliminating race conditions in parallel workspace usage."
type: standard
scope: global
status: active
created: 2026-02-23
supersedes: communication_hub flat _inbox.md pattern
---

# Agent Messaging v2.0 — Date-Sharded Convention

> **Why v2.0?**
> The old `communication_hub/*_inbox.md` monolithic append pattern caused race conditions during parallel workspace usage. This standard switches to unique, timestamped individual files.

---

## Core Principle

Every message is an **independent `.md` file** in the target agent's `messages/<target-role>/` folder.
No more appending — write and forget. The aggregator collects them.

---

## Folder Structure

```text
messages/
  orchestrator/        ← Incoming messages for Orchestrator
  tech_lead/
  architect/
  backend_developer/
  frontend_developer/
  qa_tester/
  knowledge_steward/
  product_owner/
  security_engineer/
  ui_ux_designer/
  devils_advocate/
```

---

## Filename Format

```
<ISO-timestamp>_from-<sender>_<subject-slug>.md
```

**Exact format:** `YYYY-MM-DDTHH-MM-SS` (hyphens instead of colons — filename safe)

**Examples:**

```
messages/orchestrator/2026-02-23T10-30-00_from-architect_epic-02-review-complete.md
messages/tech_lead/2026-02-23T14-15-00_from-orchestrator_task-03-assignment.md
messages/architect/2026-02-23T09-00-00_from-orchestrator_epic-01-planning-request.md
```

---

## Required Message File Structure

```yaml
---
id: msg-<ISO-timestamp>-<sender>-to-<target>
from: <sender-role>
to: <target-role>
timestamp: <YYYY-MM-DDTHH:MM:SS>
priority: high | medium | low
category: task-assignment | review-request | status-update | blocker | handoff | epic-planning
subject: "Short, descriptive subject"
status: pending | read | processed
---
```

```markdown
# <Subject>

## Context
<Brief context — 1-3 sentences>

## Required Action
<Exactly what needs to be done>

## Deliverables
- [ ] <artifact 1>
- [ ] <artifact 2>

## Acceptance Criteria
- [ ] <AC 1>
- [ ] <AC 2>

## References
- [Relevant file](../../path/to/file.md)
```

---

## Sending a Message (Manual)

1. Create the file: `messages/<target-role>/<timestamp>_from-<you>_<subject>.md`
2. Fill in the front-matter and content using the template above.
3. Save. The target agent will see it when `orchestrator-inbox-aggregator.ps1` is run.

## Sending a Message (PowerShell Script)

```powershell
# Send a message to the Orchestrator inbox
.\scripts\orchestrator-inbox-aggregator.ps1 -SendMessage `
  -From "architect" `
  -To "orchestrator" `
  -Subject "epic-02-review-complete" `
  -Priority "high" `
  -Category "status-update" `
  -Body "EPIC-02 review completed. All ACs satisfied."
```

---

## Reading / Aggregating Messages

```powershell
# Collect all pending messages into a snapshot
.\scripts\orchestrator-inbox-aggregator.ps1 -Role orchestrator

# Result: messages/orchestrator/_inbox_snapshot_temp.md
```

---

## Message Lifecycle

| Status | Meaning |
|:--------|:---------|
| `pending` | Received, not yet read |
| `read` | Agent has read the message |
| `processed` | Agent has completed the required action |

> The aggregator only includes `pending` and `read` messages in the snapshot.
> Do **not** delete `processed` messages — they are archived.

---

## Related Documents

- [messages/README.md](../../../messages/README.md) — Folder documentation
- [scripts/orchestrator-inbox-aggregator.ps1](../../../scripts/orchestrator-inbox-aggregator.ps1) — Aggregator script
- [scripts/state-summarizer.ps1](../../../scripts/state-summarizer.ps1) — State summarizer script
