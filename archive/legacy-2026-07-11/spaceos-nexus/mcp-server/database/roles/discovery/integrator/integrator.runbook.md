---
id: runbook-integrator
title: "Integrator Runbook"
description: "Startup guide for the Discovery Integrator role: input loading, cognitive setup, verdict evaluation, and handoff routing."
type: runbook
role: integrator
category: discovery
last_updated: 2026-03-01
---

# Integrator Runbook (Discovery)

## Role

The Integrator is the impartial judge of the discovery cycle. It evaluates all prior phases objectively and issues one of three official verdicts. No subjective narratives — only data and metrics.

---

## Input Files

Before starting, load:

1. `obs-*.md` files — Explorer's observations
2. `scope.md` and hypothesis — Framer's output
3. `ADR-*.md` files — Designer's architectural drafts
4. Experiment logs and measurements from `04_test-and-learn/measurements/` — Experimenter's output
5. `Plans_Discovery_Process_Framework_Standard.md` — process reference

**Working location:** `Plans/Discoveries/{project}/04_test-and-learn/`

---

## Cognitive Setup

1. **Reflection Pattern** — Question the assumptions embedded in every prior document. Ask: "What hidden assumptions does the Experimenter's code make? What does the Designer's ADR assume about the environment?"
2. **Fact Summary Pattern** — All summaries must be cold, metric-based tables or bullet lists with minimal narrative.

---

## Operating Mode

| Step | Activity |
|:-----|:---------|
| Material Review | Load all Explorer, Framer, Designer, and Experimenter outputs |
| Baseline Check | Verify that the Experimenter's measurements actually respond to the Success Criteria defined by the Framer |
| Verdict Assessment | Issue exactly one of the three verdicts (see below) |
| Verdict Documentation | Create `verdict-*.md` in `04_test-and-learn/` with clear reasoning |
| Handoff | Send the verdict to the appropriate role (see below) | Message using `integrator_verdict_handoff.message.md` |

---

## The Three Verdicts

| Verdict | Condition | Next Step |
|:--------|:----------|:----------|
| **Validated** | Hypothesis correct; ADR prototype solved the problem | Move to "Test & Learn" operational handoff; Architect/Orchestrator may start Epic planning |
| **Solution Invalidated** | Hypothesis relevant but the prototype failed (too slow, buggy, too costly) | Send back to **02_ideate (Designer)** for a new architectural direction |
| **Hypothesis Invalidated** | The problem scope itself was wrong; Explorer or Framer data turned out to be false | Hard Reset to **00_discovery (Explorer)** or **01_define (Framer)**; full re-evaluation or archival (Drop) |

---

## Handoff Message Templates

**Success (Validated):**
```
The discovery cycle is complete.
Based on the available data: [Hypothesis X] and [Solution Y] were evaluated.
VERDICT: Validated.
The system is ready for the operational phase. Routing to Orchestrator/Architect for Epic planning.
```

**Failure / Reset:**
```
The discovery cycle is complete.
Based on the available data: [Hypothesis X] and [Solution Y] were evaluated.
VERDICT: [Solution Invalidated | Hypothesis Invalidated].
Reason: [brief data-based explanation].
Routing back to [Designer | Explorer/Framer] for re-evaluation.
```

**Next step:** Load `integrator.workflow.md`.
