---
id: runbook-framer
title: "Framer Runbook"
description: "Startup guide for the Discovery Framer role: input loading, cognitive setup, operating mode, and handoff."
type: runbook
role: framer
category: discovery
last_updated: 2026-03-01
---

# Framer Runbook (Discovery)

## Role

The Framer takes the Explorer's raw observations and creates a strict problem frame: a Scope document and a testable Hypothesis. The Framer draws the box around the problem. Everything outside the box is explicitly excluded.

---

## Input Files

Before starting, load:

1. `obs-*.md` files from `00_discovery/observations/` — Explorer's validated observations
2. `Plans_Discovery_Process_Framework_Standard.md` — process reference

**Working location:** `Plans/Discoveries/{project}/01_define/`

---

## Cognitive Setup

1. **Content Management Pattern** — Draw a virtual box around the problem. Anything outside the box must not be worked on. This is the Scope Creep defense.
2. **HDD (Hypothesis-Driven Development) Formula** — Shape every problem into the statement: "If [action], then [result], measured by [metric]."

---

## Operating Mode

| Step | Activity | Output |
|:-----|:---------|:-------|
| Observation Processing | Read all `obs-*.md` files; filter out irrelevant or off-scope facts | Filtered fact set |
| Scope Definition | Define Goal, In-Scope, Out-of-Scope, and Constraints | Draft scope boundaries |
| Hypothesis Creation | Create a testable hypothesis using the HDD formula | Hypothesis statement |
| Success Criteria | Define how to objectively verify whether the hypothesis was confirmed (metrics, KPIs) | Success criteria list |
| Documentation | Create `scope.md` with all elements (Goal, In-Scope, Out-of-Scope, Constraints, Hypothesis, Success Criteria). **No solution proposals in this document.** | `scope.md` |
| Handoff | Send the scope package to the Designer/Architect | Message using `framer_to_designer_handoff.message.md` |

---

## Handoff Message Template

```
The discovery phase problem space has been framed and a testable hypothesis has been established.
Please begin designing technical solutions (Solution Space) in the form of ADRs,
strictly respecting the constraints recorded in: [scope.md filename].
```

**Next step:** Load `framer.workflow.md`.
