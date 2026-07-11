---
id: integrator-verdict-handoff
title: "Integrator → Dynamic Target: Verdict Handoff"
description: "Integrator routes the Discovery verdict to the correct next agent: Orchestrator (validated), Designer (solution invalid), or Framer (hypothesis invalid)."
type: message
scope: global
category: discovery
initiator: "integrator"
target: "orchestrator | designer | framer"
last_updated: 2026-03-01
---

# Integrator → Dynamic Target: Verdict Handoff

## Overview

After issuing the Verdict document, the Integrator routes the outcome to one of three possible targets depending on the verdict. Use the appropriate section below.

---

## Path A — SUCCESS: Hypothesis Validated → Orchestrator

**Condition:** ALL success criteria from `hyp-{ID}.md` were met by the MVE measurements.

**Action:** Discovery thread is proven and closed. Transfer findings to the project and request Epic planning.

---

### Path A — Message Template

> **From: Integrator → To: Orchestrator**
>
> **Discovery VALIDATED.**
>
> **Discovery ID:** {DISCOVERY_ID}
> **Hypothesis:** `hyp-{ID}.md`
> **Verdict:** `Verdict-{ID}.md` — VALIDATED
>
> **Evidence summary:**
> | Criterion | Target | Measured | Status |
> |-----------|--------|----------|--------|
> | {crit 1}  | {val}  | {val}    | ✅ Pass |
> | {crit 2}  | {val}  | {val}    | ✅ Pass |
>
> **Architecture confirmed:** `ADR-{ID}.md` — recommended solution is viable.
>
> **Recommended next step:**
> 1. Transfer validated architecture from `ADR-{ID}.md` to `docs/{project}/decisions/`
> 2. Update `docs/{project}/goal.md` with new validated capability
> 3. Launch Epic planning — request Architect to create Epic Plan based on the ADR
>
> Discovery thread `{DISCOVERY_ID}` is CLOSED.

---

## Path B — FAILURE: Solution Invalid → Designer

**Condition:** The success criteria were NOT met, but the root cause is the chosen architectural solution — NOT the hypothesis itself. The problem is real and the hypothesis may still be valid.

**Action:** Return to Designer to redesign the solution (go back to `02_ideate`).

---

### Path B — Message Template

> **From: Integrator → To: Designer**
>
> **Discovery FAILED — Solution Invalid.**
>
> **Discovery ID:** {DISCOVERY_ID}
> **Hypothesis:** `hyp-{ID}.md` — MAY still be valid
> **Verdict:** `Verdict-{ID}.md` — SOLUTION_INVALID
>
> **Failed criteria:**
> | Criterion | Target | Measured | Gap |
> |-----------|--------|----------|-----|
> | {crit 1}  | {val}  | {val}    | {delta} |
>
> **Root cause analysis:**
> The architectural approach in `ADR-{ID}.md` did not achieve the required thresholds.
> The hypothesis itself has NOT been invalidated — the problem is likely real,
> but the chosen solution was insufficient.
>
> **Recommended next step:**
> Redesign the solution. Re-enter at `{DISCOVERY_ROOT}/02_ideate/`:
> - Consider alternative architectural approaches
> - Explicitly address the failure point identified in Verdict-{ID}.md
> - Create a new `ADR-{ID+1}.md`
>
> Previous experiment archived at: `{DISCOVERY_ROOT}/03_prototype/experiments/poc-{ID}/`

---

## Path C — FAILURE: Hypothesis Invalid → Framer

**Condition:** The experiment failed AND the root cause is that the hypothesis itself was flawed — the assumed mechanism or root cause does not hold.

**Action:** Return to Framer for full hypothesis revision, or archive the Discovery thread entirely.

---

### Path C — Message Template

> **From: Integrator → To: Framer**
>
> **Discovery FAILED — Hypothesis Invalid.**
>
> **Discovery ID:** {DISCOVERY_ID}
> **Hypothesis:** `hyp-{ID}.md` — INVALIDATED
> **Verdict:** `Verdict-{ID}.md` — HYPOTHESIS_INVALID
>
> **Evidence:**
> | Criterion | Target | Measured | Gap |
> |-----------|--------|----------|-----|
> | {crit 1}  | {val}  | {val}    | {delta} |
>
> **Root cause analysis:**
> The assumed mechanism ("because {mechanism}") did not hold under test conditions.
> The hypothesis is structurally invalid — it is not a solution problem, it is a problem definition problem.
>
> **Options:**
> 1. **Re-hypothesis:** Return to `01_define` — revise hypothesis with new evidence from experiment
> 2. **Archive:** If the problem domain is not worth pursuing further, archive the Discovery thread
>
> **Recommendation:** {Integrator's recommendation based on evidence}
>
> All experiment artefacts archived at: `{DISCOVERY_ROOT}/`

---

## Routing Decision Table

| Verdict | Root cause | Target | Action |
|---------|-----------|--------|--------|
| VALIDATED | — | Orchestrator | Close Discovery, launch Epic |
| SOLUTION_INVALID | ADR failed | Designer | Redesign solution, new ADR |
| HYPOTHESIS_INVALID | Problem framing failed | Framer | Re-hypothesis or archive |

---

## Constraints

- 🚫 **One verdict per Discovery run** — do not issue multiple verdicts
- 🚫 **Cannot route to multiple targets** — pick exactly one path
- 🚫 **Cannot propose new experiments** after verdict is issued
- ✅ **Evidence must be cited** in every routing message — no verdict without data reference
- ✅ **Archive all artefacts** regardless of verdict outcome

---

**ROUTING:** Issue the Verdict document first, then send the appropriate Path A / B / C message.
