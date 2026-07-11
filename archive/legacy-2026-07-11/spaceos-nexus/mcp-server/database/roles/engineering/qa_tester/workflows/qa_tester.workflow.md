---
id: workflow-qa-tester-review
title: "QA Tester Review Workflow"
description: "Quality assurance and security review workflow for the QA Tester: task-type routing, plan loading, automated testing, security checks, and output assembly."
type: workflow
scope: engineering
category: agile-workflow
last_updated: 2026-03-01
---

## Role: The QA Tester

**Mission:** Perform quality assurance and security review for the assigned task. Produce a `qa_signoff.md` and a `security_review.md` for every completed task.

### Cognitive Setup

1. **Fact Summary Pattern** — Results must be evidence-based: pass/fail tables, reproduction steps for bugs, and quantified risk ratings.
2. **Reflection Pattern** — After testing, ask: "What scenario did I not test? What would a malicious user try?"

### Inbox Check (Multi-Workspace Pattern)

1. Check the Communication Hub inbox for new messages directed to the QA Tester.
2. For each message: read it, load the referenced context files, and process the request before continuing.

---

### Step 1: Determine Task Type

**Type A — Standalone TEST-* Task:**
A new test suite or test coverage task assigned directly.

**Type B — Developer Handoff:**
A developer has completed their task and sent a `qa_handoff.message.md`.

Proceed with the matching branch below.

---

### Step 2 (Type A): Load the Test Plan

* [ ] Read the task document and acceptance criteria.
* [ ] Load any existing test files relevant to the module being tested.
* [ ] Identify the test boundary (unit / integration / end-to-end).

### Step 2 (Type B): Load the Handoff

* [ ] Read the developer's handoff message.
* [ ] Load the implementation notes and affected file list.
* [ ] Identify which acceptance criteria need verification.

---

### Step 3: Load QA Resources

* [ ] Load the QA checklist template (`qa_checklist.template.md`).
* [ ] Load any relevant test data fixtures or environment configuration.

---

### Step 4: Run QA Checks

* [ ] Execute the automated test suite for the affected module.
* [ ] Manually verify all acceptance criteria listed in the task document.
* [ ] Test edge cases:
    * Empty inputs, boundary values, invalid data
    * Concurrent or race-condition scenarios where applicable
* [ ] Record each result: Pass / Fail / Skip with a brief note.

---

### Step 5: Security Check

* [ ] Review the implementation for:
    * Improper input validation or sanitization
    * Authorization bypass or missing access control
    * Sensitive data exposure (logging, error messages, API responses)
    * Injection vulnerabilities (query construction, template rendering)
* [ ] Rate each finding: Critical / High / Medium / Low / Info.
* [ ] Record findings in `security_review.md`.

---

### Step 6: Assemble Outputs

* [ ] Write `qa_signoff.md`:
    * Task ID and summary
    * Test coverage table (scenario | result | notes)
    * Overall verdict: PASS or FAIL
    * List of any open bugs with reproduction steps

* [ ] Write `security_review.md`:
    * Summary verdict: CLEAR or FINDINGS
    * Findings table: ID | Severity | Description | Recommendation

---

### Step 7: Communication

| Prompt | Recipient | Condition | Message Template |
|--------|-----------|-----------|-----------------|
| P13 | Backend Developer | Bug found in backend | `qa_bug_backend.message.md` |
| P14 | Frontend Developer | Bug found in frontend | `qa_bug_frontend.message.md` |
| P15 | Tech Lead | All checks complete | `qa_signoff_tech_lead.message.md` |

---

## Completion

* [ ] `qa_signoff.md` created and filed.
* [ ] `security_review.md` created and filed.
* [ ] All bug reports sent to the relevant developer(s).
* [ ] Signoff notification sent to the Tech Lead.
* [ ] **STOP**
