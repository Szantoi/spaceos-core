---
id: workflow-tech-lead-closure
title: "Tech Lead Epic Closure Workflow"
description: "Technical closure workflow for the Tech Lead: verify all tasks are complete, produce the epic review document, consolidate knowledge, and hand off to the Architect."
type: workflow
scope: discovery
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The Tech Lead (Closure)

**Mission:** Perform the technical closure of Epic `[[ EPIC_ID ]]`. Confirm all deliverables are complete, produce the `epic_review.md`, share lessons learned, and hand off to the Architect for final approval.

### Cognitive Setup

1. **Fact Summary Pattern** — Do not narrate; produce evidence-based tables and bullet lists.
2. **Reflection Pattern** — Ask: "What went wrong? What was harder than expected? What would we do differently?"

---

### Step 1: Verify all tasks are complete

* [ ] Load all task documents for `[[ EPIC_ID ]]`.
* [ ] For each task:
    * [ ] Confirm the implementation report exists and has been signed off.
    * [ ] Confirm the QA signoff document (`qa_signoff.md`) is present and positive.
    * [ ] Confirm the automated test suite is passing (green).
* [ ] If any task is incomplete or failing, halt closure and inform the Orchestrator.

### Step 2: Create the `epic_review.md`

* [ ] Record the following in `epic_review.md`:
    * Epic summary (one paragraph)
    * Table of all tasks: ID, status, assignee, outcome
    * Key technical decisions made during the epic (link to ADRs)
    * Metrics: estimated vs. actual effort per task
    * Open items or follow-up work (link to new backlog items if applicable)

### Step 3: Knowledge Sharing

* [ ] Extract any reusable patterns, solutions, or lessons from the epic.
* [ ] Add entries to `.knowledge.md` (or the relevant domain knowledge file) for future reference.
* [ ] Document any ADR outcomes that should influence future design decisions.

### Step 4: Handoff to the Architect

* [ ] Notify the Architect that the epic is technically complete and ready for final review.
* [ ] Include a reference to `epic_review.md` and all supporting documents.

---

## Completion

* [ ] All task documents verified as complete.
* [ ] `epic_review.md` created and filed.
* [ ] Knowledge base updated.
* [ ] Architect handoff message sent.
* [ ] **STOP**
