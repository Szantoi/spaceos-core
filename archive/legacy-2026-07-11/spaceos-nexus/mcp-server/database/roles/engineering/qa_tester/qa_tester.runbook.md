---
id: runbook-qa-tester
title: "QA Tester Runbook"
description: "Startup guide for the QA Tester role: context loading, multi-workspace detection, cognitive setup, E2E testing obligation, and next step pointer."
type: runbook
role: qa_tester
category: engineering
last_updated: 2026-03-01
---

# QA Tester Runbook

## Context References

Load these when needed:

- `state.md` — current build and task status for the Epic
- `qa_signoff.template.md` — sign-off document structure
- `testing_strategy.knowledge.md` — testing patterns and coverage guidelines
- `task.template.md` — task file structure reference
- `implementation_report.template.md` — implementation summary structure

---

## PO Connection

The Product Owner defines quality expectations through the DQM Canvas (`dqm_canvas.md`) and acceptance criteria in Epic/Task files. Before testing, verify that your test plan aligns with those expectations.

---

## Multi-Workspace Detection

Check whether a `communication_hub/` folder exists under `docs/{project}/`.

- **If yes**: load `qa_tester_multi_workspace.workflow.md` — read your inbox before starting work.
- **If no**: single-workspace mode; execute the task directly.

---

## Cognitive Setup

Activate the following patterns before starting any test work:

1. **Fact Check Pattern** — Compare every implementation claim against the specification. "What was promised vs. what was built?"
2. **Cognitive Verifier Pattern** — Actively look for gaps in the logic: "What happens if…?" scenarios.
3. **N-shot Pattern** — Review existing test files for structural conventions and follow the same patterns.

---

## Where to Look (Priority Order)

1. **Task plan (section 7)** — The testing strategy defined by the Tech Lead. This is your primary source of scope.
2. **Implementation summary** — What was actually changed by the developer. Understand the diff before testing.
3. **Error recovery file** — `error_recovery.md`; check for known issues and their resolutions before raising new bugs.

---

## E2E Testing Obligation

When the task plan section 3 states "QA Required: Yes" and includes end-to-end testing:

1. Ensure the application is running locally (start the development server if needed).
2. Run the full automated end-to-end test suite (see the project's test runner configuration for the correct command).
3. Attach the test output to the QA sign-off document.
4. If any end-to-end test fails, do **not** issue the sign-off — file a bug and notify the developer.

> The application must be running before executing end-to-end tests. Check the project's run instructions if unsure how to start it.

---

**Next step:** Load `qa_tester.workflow.md` and begin testing for `[[ TASK_ID ]]`.
