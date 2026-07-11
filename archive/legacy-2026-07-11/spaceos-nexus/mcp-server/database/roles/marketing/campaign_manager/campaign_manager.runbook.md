---
id: runbook-campaign-manager
title: "Campaign Manager Runbook"
description: "Quick start guide for Campaign Managers on how to work with MCP tasks and run the right workflows."
type: runbook
role: campaign_manager
category: marketing
last_updated: 2026-03-13
---

# Campaign Manager Runbook

## Starting a Task

1. **Locate the task**: Open the task file at `docs/mcp-pm-engine/delivery/pm-engine/milestones/milestone_04/epic_17/tasks/{TASK_ID}.md`.
2. **Review acceptance criteria**: Ensure you understand what "done" looks like.
3. **Load relevant skills**: Tasks may reference skills such as `marketing-strategy`, `analytics`, or `copywriting`. Load them before proceeding.

---

## Working with MCP Tools

* Use **Templates** to standardize campaign briefs and measurement plans:
  * `list_templates` to discover available templates.
  * `get_template` to fetch the template content.
* Use **Artifacts** to submit campaign assets and reports:
  * `artifact_submit` to upload campaign briefs, audience definitions, and results summaries.
* Use **Workflows** to track campaign progress:
  * `get_workflow_state` to see current state and next steps.
  * `request_workflow_transition` to move the workflow forward when ready.

---

## Key References

* `state.md` — current epic/task status and launch readiness.
* `implementation_report.template.md` — structure for final summary.
* `testing_strategy.knowledge.md` — how to define test coverage and validation.

---

## Handoff Notes

* When the campaign is ready for execution, notify the **Marketing Operations** or **Analytics** team.
* If the campaign requires legal or privacy approval, escalate early and include measurement plans.
