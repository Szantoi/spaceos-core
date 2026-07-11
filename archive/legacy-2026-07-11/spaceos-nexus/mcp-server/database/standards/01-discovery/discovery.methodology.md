---
title: "Discovery Methodology Standard (Double Diamond + HDD)"
description: "Defines the Double Diamond + Hypothesis-Driven Development methodology for structured research, decision-making, and evidence-based planning before any operational project begins."
type: reference_guide
scope: global
date: 2026-02-23
last_updated: 2026-03-01
methodology:
  - Double Diamond
  - Hypothesis-Driven Development (HDD)
  - Architecture Decision Records (ADR)
---

# Discovery Methodology Standard (Double Diamond + HDD)

This guide defines how to move from a vague idea to an actionable, evidence-based plan — before it enters the operational `docs/<project>/` project structure.

**The final output is always an actionable `Plan`** that can be transferred to the operational project hierarchy in the form of `goal.md`, `state.md`, and ADRs.

---

## Why This Combination Wins

### Double Diamond

```
Problem space                      Solution space
────────────────────────────────────────────────────
  Diverge         Converge     Diverge        Converge
  [Discovery]  →  [Definition]-[Ideation]  →  [Delivery]
```

The power of the Double Diamond lies in **prohibiting premature convergence**. If the problem space is incorrectly defined, even the best solution leads to the wrong outcome.

- **1st diamond (left):** Open → Close → *Understand the real problem*
- **2nd diamond (right):** Open → Close → *Find the right solution*

### Hypothesis-Driven Development (HDD)

HDD brings the scientific method into software development. Instead of "building features," you **run experiments**:

> *"If [we take this action] [for these users/systems], then [this measurable result] will follow, because [this mechanism]."*

This aligns with Lean thinking: **do not build what has not first been validated by a smaller experiment.**

### Why Together is Better Than Separately

| Design Thinking Only | HDD Only | **Double Diamond + HDD** |
|:---|:---|:---|
| Creative but not always measurable | Measurable but skips deep empathy | Empathy-based AND measurable |
| Vague "aha!" moments | Tests — but what? Wrong question = wrong test | Tests the right question with the right method |

---

## Discovery Roles

Each phase is owned by a dedicated role. One role = one hat at a time (no premature convergence).
Canonical role definitions and handoff protocol: → `docs/standards/01-discovery/discovery.process.md`

---

## Folder Structure

Each discovery topic lives under `docs/joinerytech-flow/discovery/<topic>/`.

```text
docs/joinerytech-flow/discovery/
└── <topic>/                           # e.g. mcp-rbac, ssot-memory, workflow-protocol
    ├── dwi-state.md                   # DWI: topic status, current phase, next_action
    ├── 00_discovery/                  # Phase 0 — Explorer: observations, research
    ├── 01_define/                     # Phase 1 — Framer: hypotheses, scope, constraints
    │   └── hyp-*.md
    ├── 02_ideate/                     # Phase 2 — Designer/Architect: concepts, ADR drafts
    │   └── adrs/ADR-*.md
    ├── 03_prototype/                  # Phase 3 — Experimenter: MVE, PoC, spikes
    │   └── exp-*.md
    ├── 04_test-and-learn/             # Phase 4 — Integrator: results, verdict, learn-*.md
    └── 06_fast-tracks/                # S-size: single-file discoveries
        └── fast-track-XXX.md
```

> **DWI tracking:** Every topic has a `dwi-state.md` file that tracks `current_phase`, `next_action`, and `verdict` without imposing Delivery-style task cards. See `docs/standards/01-discovery/discovery.work-item.standard.md`.

---

## Fast Track vs. Full Track (T-shirt Sizing)

Not every idea requires the full multi-file structure. Determine the sizing before starting:

| Size | When? | Structure |
|:-----|:------|:----------|
| **S — Small / Fast Track** | Validating a small change (e.g. script refactor, minor configuration tweak) | Single `fast-track-XXX.md` that runs through all phases in one file |
| **L — Large / Full Track** | Complex, system-level change (e.g. new agent role, new integration) | Full multi-file structure (`obs → hyp → exp → learn`) |

### Fast Track file template (`06_fast-tracks/fast-track-XXX.md`)

Fast Track files live in their **own folder** (`06_fast-tracks/`) — a single fast-track file covers all phases, cleanly separated from large multi-file Epic-level discoveries.

```markdown
---
id: ft-XXX
size: S
date: YYYY-MM-DD
verdict: open | validated | invalidated
---
# Fast Track: [Short title]

## Observation
[What did I see? What is the problem?]

## Hypothesis
If [I do X], then [Y follows], because [Z mechanism].

## Experiment & Result
[What did I do? What was the result? Max X hours effort.]

## Conclusion
- **Validated / Invalidated**
- Next step: [ ] start project / [ ] standard / [ ] new Fast Track
```

> **Golden rule:** If a Fast Track result is generally applicable — elevate it to a Full Track `learn-XXX.md` and standardise it. Preserving knowledge is mandatory.

---

## Phases in Detail

### PHASE 0 — Entry Point: Observation

**Location:** `00_discovery/observations/`
**Dedicated Role:** The Explorer

Every Discovery begins by explicitly describing **what you see** — not what you think. Observation file format:

```markdown
---
id: obs-YYYY-MM-DD-XXX
type: observation
source: user_feedback | own_experience | analysis
date: YYYY-MM-DD
---
# Observation: [Short title]

## What did I see / experience?
[Factual description, no interpretation]

## What questions does this raise?
- [Question 1]
- [Question 2]

## Related files
- [link to research/ or analyses/]
```

---

### PHASE 1 — Define: Hypothesis Formalisation

**Location:** `01_define/hypotheses/`
**Dedicated Role:** The Framer

The hypothesis is the most critical file in the entire Discovery process. If the hypothesis is wrong, every subsequent step is also wrong.

**HDD Hypothesis template:**

```markdown
---
id: hyp-XXX
status: active | validated | invalidated | archived
created: YYYY-MM-DD
scope: 01_define/constraints/scope.md  # mandatory reference!
epic-link: [if project already exists: link to epic_state.md]
---
# Hypothesis: [Short title]

## Statement
If [we take this action] [for these users/systems],
then [this measurable result] will follow,
because [this mechanism].

## Measurable Success Criteria
- [ ] [1st measurable condition]
- [ ] [2nd measurable condition]

## Out of Scope (what we are NOT examining in this experiment)
- [...]
> Detailed scope: see `01_define/constraints/scope.md`

## In Case of Failure
If the hypothesis is not validated → [new hypothesis ID] or [project halted]

## Related ADR draft (if a software decision is expected)
- [Reference the ADR to be created under `02_ideate/adrs/` — only boundary conditions go here!]
```

**Scope Definition — `01_define/constraints/scope.md` (required for every Full Track)**

Out-of-Scope items must be locked **in the Define phase**, not at the end of testing. This dramatically reduces scope creep risk:

```markdown
---
hypothesis: hyp-XXX
date: YYYY-MM-DD
---
# Scope: [Hypothesis short name]

## In Scope (what we are examining)
- [...]

## Out of Scope (what we are NOT examining — in this experiment)
- [...]

## Technology-independent boundary conditions
- [...]
```

> **Premature Convergence prohibition:** In the `01_define` phase it is **forbidden** to make technology decisions (e.g. "we will use PostgreSQL"). This violates the Double Diamond logic — ADRs are created inside the 2nd diamond, in the `02_ideate` phase.

---

### PHASE 2 — Ideate: Brainstorming with Decision Log

**Location:** `02_ideate/`
**Dedicated Role:** The Framer (and partially The Explorer)

The `concepts/` folder is where ideas run freely. The `decision-log/` file captures why the team converged in a particular direction. This prevents the question "why did we decide this?" from surfacing six months later.

**ADR draft — `02_ideate/adrs/`** (status: `draft`)

ADRs are born here, once solution space exploration has begun. They become final (`accepted`) only after validation in `04_test-and-learn`.

```markdown
---
id: ADR-XXX
status: draft | accepted | rejected | superseded
date: YYYY-MM-DD
validated-by: learn-XXX   # filled in after 04_test-and-learn
---
# ADR-XXX: [Decision short name]

## Context
[Why does a decision need to be made? What is the situation?]

## Proposed Decision
[What do we propose? — not yet accepted!]

## Consequences
**Positive:** ...
**Negative / Trade-off:** ...

## Rejected Alternatives
- [Alt 1] — Why not this?
- [Alt 2] — Why not this?
```

---

### PHASE 3 — Prototype: The Minimum Viable Experiment

**Location:** `03_prototype/experiments/`
**Dedicated Role:** The Experimenter

The prototype is not the final product — it is an **instrument of the experiment**. MVE (Minimum Viable Experiment) template:

```markdown
---
id: exp-XXX
hypothesis: hyp-XXX
type: user_test | spike | PoC | a_b_test
effort_estimate: S | M | L
---
# Experiment: [Short title]

## What are we testing?
[Which exact hypothesis element is being tested?]

## Method
[How do we measure? What tool?]

## Success threshold
[When do we say the hypothesis is validated?]

## Planned effort
[Max X hours / X days — then STOP and evaluate]

## Integration Check — System-Level Impact Analysis
[Which existing standards, systems, or processes does this change affect?]
- **Affected standards:** [...]
- **Affected components/processes:** [...]
- **Where might existing architecture break?** [...]
```

> **Reversible Prototype Rule:**
> Every prototype must be built **in isolation and fully reversible**. The experiment MUST NOT modify existing Core/Domain entities, database schemas, or production code in a way that persists after the experiment closes. The prototype code must be removable via a single `git revert` or file deletion — without a trace. If this is not achievable → escalate to an operative project (see `rollback_safe: true` rule).

---

### PHASE 4 — Test & Learn: Conclusion and Feedback

**Location:** `04_test-and-learn/`
**Dedicated Role:** The Integrator

This phase determines the "fate" of the idea. **After the decision, notifying the Product Owner is mandatory.**

```
Experiment result
      │
      ├─ ✅ Validated  → learnings/ → Plan → docs/<project>/goal.md
      │                              → If proven method: 05_reference/standards/
      │
      └─ ❌ Invalidated → new-hypotheses/ → back to 01_define/hypotheses/
                          (new iteration starts — this is NOT failure, this is learning)
```

**Learning file template — `04_test-and-learn/learnings/`:**

```markdown
---
id: learn-XXX
hypothesis: hyp-XXX
experiment: exp-XXX
verdict: validated | invalidated
date: YYYY-MM-DD
promoted-to-standard: false | [standard filename]
---
# Learning: [Short title]

## What did we test?
[Brief summary]

## What actually happened?
[Objective result, with numbers where possible]

## Conclusion
**Hypothesis:** [validated / invalidated]

## Next steps
- [ ] Transfer to operative project — see Handoff block below
- [ ] Standardised → `05_reference/standards/<filename>.md`
- [ ] New hypothesis → `01_define/hypotheses/hyp-XXX+1.md`

## Operative Handoff — Translation Layer (only if `validated`!)

> This block translates the learning into an actionable operative project goal.
> Without it, `docs/<project>/goal.md` remains an empty slogan — not an executable plan.

**Project goal (1-2 sentence objective for `goal.md`):**
[...]

**Scope:**
- **In Scope:** [...]
- **Out of Scope:** [...]

**Success Criteria (carried over from learnings):**
- [ ] [...]

**Epics to launch:**
- EPIC-01: [...]

**ADR status update:**
- [ ] `02_ideate/adrs/ADR-XXX` → status: `accepted`

**PO Notification:**
- [ ] Product Owner notified of results and proposed next steps (Handoff).
```

---

### PHASE 5 — Reference / Standards: Preserving Knowledge

**Location:** `docs/joinerytech-flow/discovery/reference/standards/`

> **Golden rule:** If you "discovered" something twice, it should have been a Standard the first time.

When `04_test-and-learn` produces a `validated` conclusion and the result is **generally applicable** (not only to this specific project), the learning must be preserved **as a Standard** under the global `05_reference/standards/`.

This way, the next project does not need to rediscover the same problem — it simply references the Standard.

---

## The Complete Process Summary

```
Observation              Hypothesis                  Prediction
    🧭                       📐                          🏛️
00_discovery/    →    01_define/             →    02_ideate/
observations/         hypotheses/                 concepts/
research/             constraints/                decision-log/
analyses/             success-criteria/            adrs/ (DRAFT)
                      [No ADR here!]                   ↓ accepted only →
        Testing                       Conclusion + Handoff
           🧪                                  ⚖️
   03_prototype/              →       04_test-and-learn/
   experiments/                        results/
   + Integration Check!                learnings/ + Translation Layer
   prototypes/                         new-hypotheses/ (→ back)
         │
         ↓
   ADR: draft → accepted ──────────────────────────┘

                    Standards (if proven)
                           ↓
             05_reference/standards/
                           ↓
              docs/<project>/goal.md   → Operative project starts
```

---

## Traceability

The system relies on manual IDs (`hyp-XXX`, `exp-XXX`, `learn-XXX`). As document count grows, maintaining manual synchronisation becomes overhead. First steps toward **Digitise → Automate**:

| Tool | What it adds |
|:-----|:-------------|
| **VS Code + Foam** | Wikilink-based (`[[hyp-XXX]]`) bidirectional references, automatic backlink graph |
| **Obsidian** | Graph View — visualises the `obs → hyp → exp → learn → standard` chain |
| **Simple approach** | Add `traces: [hyp-XXX, exp-XXX]` YAML field in every file header — grep-searchable |

> Goal: from any Standard, trace backwards to the original observation in the full chain.
> The ID conventions (`obs-`, `hyp-`, `exp-`, `learn-`, `ft-`, `ADR-`) make this graph machine-readable.

---

## Quick Reference: Which File, When?

| Situation | File type | Folder |
|:----------|:----------|:-------|
| Noticed a problem | `obs-XXX.md` | `00_discovery/observations/` |
| Conducting research | Free format | `00_discovery/research/` |
| Formulating a hypothesis | `hyp-XXX.md` | `01_define/hypotheses/` |
| Locking scope and Out of Scope | `scope.md` | `01_define/constraints/` |
| Drafting an architectural decision | `ADR-XXX.md` (status: `draft`) | `02_ideate/adrs/` |
| Closing ADR (successful) | status → `accepted` | `02_ideate/adrs/` + learn Handoff |
| Closing ADR (failed) | status → `rejected` | `02_ideate/adrs/` (cannot be deleted!) |
| Gathering ideas | Free format | `02_ideate/concepts/` |
| Planning an experiment | `exp-XXX.md` | `03_prototype/experiments/` |
| Evaluating results | `learn-XXX.md` | `04_test-and-learn/learnings/` |
| Preserving a proven method | Standard file | `05_reference/standards/` |
| Project launch | `goal.md` | `docs/<project>/` |
