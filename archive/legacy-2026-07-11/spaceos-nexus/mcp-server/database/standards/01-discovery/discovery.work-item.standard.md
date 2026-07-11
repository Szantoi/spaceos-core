---
id: discovery-work-item-standard-v1
title: "Discovery Work Item (DWI) Standard — Hybrid Tracking for the Discovery Track"
description: "Defines the lightweight, markdown-based work item format that bridges the file-driven Discovery track with the task-driven Delivery track, without violating either track's philosophy."
type: standard
scope: global
category: discovery-governance
version: 1.0
created: 2026-03-03
last_updated: 2026-03-03
complementary_standards:
  - docs/standards/00-foundation/two-track.meta-framework.md
  - docs/standards/01-discovery/discovery.methodology.md
  - docs/standards/01-discovery/discovery.process.md
  - docs/standards/02-delivery/epic.fsm-schema.md
---

# Discovery Work Item (DWI) Standard

## 1. Motivation — The Asymmetry Problem

The two tracks operate on fundamentally different philosophies:

| Dimension | Discovery Track | Delivery Track |
|:----------|:----------------|:---------------|
| **Nature of work** | Exploratory, iterative, fuzzy | Deterministic, executable, crisp |
| **Unit of progress** | Phase gate crossed | Task completed |
| **Content known upfront?** | No — emergent | Yes — specified in advance |
| **Storage** | `.md` files in folder hierarchy | `state.md` + `tasks/TASK-XX-YY.md` |
| **"What's next?"** | Hard to know without reading all files | Trivially readable from `state.md` |

The **Discovery Work Item (DWI)** solves the "what's next?" problem in Discovery without imposing the rigid Task-card model from Delivery. It introduces a single declarative state file per topic, making Discovery progress queryable.

---

## 2. Core Concepts

### 2.1 Discovery Work Item (DWI)

A **DWI** is the Discovery analog of an Epic — one per topic. It does not decompose into Tasks. Instead, it advances through **phase gates** (the Double Diamond phases 0–4).

> **Key rule:** A DWI is never about *what to do in detail* — it is about *where we are* and *what the single next action is*.

### 2.2 Hypothesis Card

A **Hypothesis Card** is the Discovery analog of a Task. It lives inside an existing `hyp-*.md` file as extended YAML frontmatter. It does not replace or restructure the file — it enriches it with trackable metadata.

### 2.3 Relationship

```
DiscoveryWorkItem  (1 per topic — loose Epic)
    └── HypothesisCard[]  (N per DWI — loose Task)
            └── artifact_path → existing .md files in discovery/<topic>/
```

The DWI tracks topic-level state. Hypothesis Cards track individual learning units. The actual content (observations, research, ADR drafts) remains in the existing file hierarchy untouched.

---

## 3. File Locations

| Artifact | Path | Description |
|:---------|:-----|:------------|
| DWI state file | `docs/joinerytech-flow/discovery/<topic>/dwi-state.md` | Single source of truth for topic progress |
| Hypothesis Card | Inside existing `<topic>/01_define/hyp-*.md` YAML frontmatter | Extended metadata on existing files |
| Topic archive | `docs/joinerytech-flow/discovery/_archive/<topic>/dwi-state.md` | On conclusion/archive |

---

## 4. DWI State File Format (`dwi-state.md`)

One file per discovered topic. Lives at the root of the topic folder.

### Template

```markdown
---
id: dwi-<topic-slug>
type: discovery_work_item
topic: "<human-readable topic name>"
status: open                   # open | in_progress | concluded | archived
current_phase: 0               # 0=discover 1=define 2=ideate 3=prototype 4=learn
next_action: "<single most important next step>"
verdict: null                  # null | validated | invalidated | pivoted
hypothesis_count: 0            # total hyp-*.md files linked
validated_count: 0             # count with status: validated
created: YYYY-MM-DD
updated: YYYY-MM-DD
---

# DWI: <topic>

## Active Phase

**Phase `<N>` — <phase name>** (see phase gate table below)

## Next Action

> <next_action value — free text, max 2 sentences, actor + artifact>

## Hypothesis Summary

| ID | Statement (short) | Status |
|:---|:-----------------|:-------|
| hyp-001 | ... | open |

## Phase Gate History

| Phase | Gate crossed | Date | Notes |
|:------|:------------|:-----|:------|
| 0 — Discover | ✅ | YYYY-MM-DD | Initial observations logged |
| 1 — Define | ⬜ | — | |
| 2 — Ideate | ⬜ | — | |
| 3 — Prototype | ⬜ | — | |
| 4 — Test & Learn | ⬜ | — | |

## Linked Artifacts

- `obs-*.md` — observations
- `hyp-*.md` — hypotheses
- `adrs/ADR-*.md` — architecture decisions
- `exp-*.md` — experiments
- `learn-*.md` — final learnings
```

### Field Definitions

| Field | Type | Description |
|:------|:-----|:------------|
| `id` | `dwi-<slug>` | Unique identifier for the DWI. Must be kebab-case. |
| `type` | fixed | Always `discovery_work_item` |
| `topic` | string | Human-readable topic name (matches folder name) |
| `status` | enum | `open` → `in_progress` → `concluded` \| `archived` |
| `current_phase` | int 0–4 | Active Double Diamond phase |
| `next_action` | string | **The single most important next step.** Free text. Actor + artifact. |
| `verdict` | enum/null | Set at Phase 4 close: `validated` \| `invalidated` \| `pivoted` \| `null` (open) |
| `hypothesis_count` | int | Total number of linked `hyp-*.md` files |
| `validated_count` | int | Count of hypotheses with `status: validated` |

---

## 5. Hypothesis Card Frontmatter Extension

Existing `hyp-*.md` files get two additional YAML fields. **No structural change to the file body.**

### Standard `hyp-*.md` frontmatter (before)

```yaml
---
id: hyp-001
statement: "..."
---
```

### Extended frontmatter (after — additions marked with ★)

```yaml
---
id: hyp-001
dwi_id: dwi-<topic-slug>    # ★ links back to parent DWI
statement: "If [action] then [measurable result] because [mechanism]"
status: open                 # ★ open | testing | validated | invalidated
phase: 1                     # ★ phase where this hypothesis was created (1–4)
created: YYYY-MM-DD
closed: null                 # ★ YYYY-MM-DD when status reached terminal state
---
```

### Hypothesis Status Values

| Status | Meaning |
|:-------|:--------|
| `open` | Formulated, not yet actively tested |
| `testing` | Active experiment running (exp-*.md linked) |
| `validated` | Experiment confirmed the hypothesis |
| `invalidated` | Experiment refuted — pivot or archive |

---

## 6. Phase Gate Table — "What is next_action?"

The `next_action` field should always reflect the **phase gate criterion** for the current phase.

| Phase | Gate criterion | Typical `next_action` examples |
|:------|:--------------|:-------------------------------|
| **0 — Discover** | ≥1 `obs-*.md` filed, all known problem signals captured | `"Explorer: log obs-002 on [signal X]"` |
| **1 — Define** | `scope.md` complete, ≥1 `hyp-*.md` with `status: open` | `"Framer: write hyp-001 for [core assumption]"` |
| **2 — Ideate** | ≥1 `ADR-*.md` draft in `adrs/`, alternatives documented | `"Designer: draft ADR-001 comparing [option A vs B]"` |
| **3 — Prototype** | `exp-*.md` created, experiment executed | `"Experimenter: run exp-001 and log results"` |
| **4 — Test & Learn** | `learn-*.md` filed, verdict set, DoR package assembled | `"Integrator: write learn-001 verdict and notify PO"` |

> **Rule:** `next_action` must name a **role** (Explorer, Framer, etc.) and an **artifact** (e.g. `obs-003.md`, `ADR-001`). Vague values like `"continue research"` are not acceptable.

---

## 7. Status State Machine

```
open ──► in_progress ──► concluded
                    └──► archived
```

| Transition | Trigger |
|:-----------|:--------|
| `open → in_progress` | First artifact created in the topic folder |
| `in_progress → concluded` | Phase 4 complete, verdict set, DoR package assembled |
| `in_progress → archived` | Topic deprioritised or superseded; no verdict required |
| `concluded → archived` | Historical cleanup after ≥90 days in `concluded` |

---

## 8. Agent Responsibilities

| Role | DWI Responsibility |
|:-----|:-------------------|
| **Explorer** | Updates `updated` date; does NOT change `current_phase` or `next_action` |
| **Framer** | Advances `current_phase` from 0→1, sets `next_action` for phase 1 |
| **Designer / Architect** | Advances `current_phase` from 1→2, updates `next_action` |
| **Experimenter** | Advances `current_phase` from 2→3, increments `hypothesis_count` |
| **Integrator** | Advances `current_phase` from 3→4, sets `verdict`, marks gate in Phase Gate History |
| **Orchestrator** | Reads `dwi-state.md` for dashboard aggregation; does NOT modify content |
| **Product Owner** | Sets `status: concluded` after approving the DoR package |

> **Rule:** Only the phase owner may change `current_phase` and `next_action`. No agent may modify another phase's gate entry retroactively.

---

## 9. Dashboard Query Pattern

Because all DWI state files share the same YAML frontmatter schema, a simple PowerShell or Python script can produce a cross-topic discovery dashboard without reading any body content.

### PowerShell example — topic status summary

```powershell
Get-ChildItem -Path "docs/joinerytech-flow/discovery" -Filter "dwi-state.md" -Recurse |
  ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match "status:\s*(\S+)") { $status = $Matches[1] }
    if ($content -match "current_phase:\s*(\d)") { $phase = $Matches[1] }
    if ($content -match "next_action:\s*`"(.+?)`"") { $next = $Matches[1] }
    [PSCustomObject]@{
      Topic  = $_.Directory.Name
      Status = $status
      Phase  = $phase
      Next   = $next
    }
  } | Format-Table -AutoSize
```

### Output example

```
Topic             Status        Phase  Next
-----             ------        -----  ----
ssot-memory       in_progress   2      Designer: draft ADR-001 comparing storage backends
mcp-rbac          in_progress   3      Experimenter: run exp-001 RBAC enforcement spike
workflow-protocol concluded     4      —
meta-security     open          0      Explorer: log obs-001 on threat surface
```

---

## 10. Relationship to Existing Standards

| Standard | Relationship |
|:---------|:-------------|
| `Discovery_&_Delivery_Meta_Framework.md` | DWI implements the "Discovery Track state" described in Section 1.1 |
| `Plans_Discovery_Framework_Standard.md` | DWI `current_phase` maps to the Double Diamond phases defined there |
| `Plans_Discovery_Process_Framework_Standard.md` | Phase gate criteria in Section 6 of this standard operationalize the process rules there |
| `fsm_schema.md` | DWI status (`open → in_progress → concluded`) is a simplified FSM; does NOT use the full agent FSM schema (intentional — Discovery is not task-deterministic) |
| `Operative_Process_Framework_Standard.md` | DWI `concluded` + `verdict: validated` is a prerequisite for the DoR gate defined there |

---

## 11. Non-Goals (Explicit Out of Scope)

- **DWI does not replace Tasks.** Delivery tasks remain in `tasks/TASK-XX-YY.md`.
- **DWI does not enforce agent assignments.** It declares `next_action` as guidance, not as a dispatch command. Actual dispatch is the Orchestrator's responsibility.
- **DWI does not enforce file creation order.** It reflects state, it does not block work.
- **DWI does not replace the DoR gate.** Marking `status: concluded` does not automatically grant Delivery access — the Product Owner still approves the DoR package.


---

## 12. How to Create & Maintain Topic `state.md`

### 12.1 Initialization Checklist

When starting a new Discovery topic:

1. **Create the topic folder** (if not already present)
   ```
   docs/joinerytech-flow/discovery/<topic-slug>/
     ├── 00_discovery/
     ├── 01_define/
     ├── 02_ideate/
     ├── 03_prototype/
     ├── 04_test-and-learn/
     └── dwi-state.md              ← create this
   ```

2. **Initialize `dwi-state.md`** using the template from Section 4
   - Set `id: dwi-<topic-slug>` (kebab-case of topic folder name)
   - Set `status: open` (will transition to `in_progress` when first artifact is created)
   - Set `current_phase: 0` (we are in the Discover phase)
   - Set `next_action` to the first Explorer task (e.g., `"Explorer: log obs-001 on [signal X]"`)
   - Leave `verdict: null`
   - Save with `created` and `updated` as today's date

3. **Commit and inform the Orchestrator** so it can aggregate into the Discovery dashboard

### 12.2 Phase Transitions — Who Changes What

The phase owner (see Section 8) is responsible for advancing the DWI state file when a gate is cleared.

**Phase 0 → Phase 1 transition** (Framer's responsibility):
- Gate criterion: ≥1 `obs-*.md` filed, all known problem signals captured
- Action: Framer updates `dwi-state.md`:
  ```yaml
  current_phase: 1
  updated: YYYY-MM-DD
  next_action: "Framer: write hyp-001 hypothesis for [core assumption]"
  ```
- Update Phase Gate History row: `1 — Define | ✅ | YYYY-MM-DD | Problem space closed`

**Phase 1 → Phase 2 transition** (Designer/Architect's responsibility):
- Gate criterion: `scope.md` complete with success criteria, ≥2 `hyp-*.md` with `status: open`
- Action: Designer/Architect updates `dwi-state.md`:
  ```yaml
  current_phase: 2
  updated: YYYY-MM-DD
  hypothesis_count: 2
  next_action: "Designer: draft ADR-001 comparing [option A vs B]"
  ```
- Update Phase Gate History row: `2 — Ideate | ✅ | YYYY-MM-DD | Problem frame sealed, solution space divergence begins`

**Phase 2 → Phase 3 transition** (Experimenter's responsibility):
- Gate criterion: ≥1 `ADR-*.md` draft, ≥1 `exp-*.md` (Minimum Viable Experiment) plan
- Action: Experimenter updates `dwi-state.md`:
  ```yaml
  current_phase: 3
  updated: YYYY-MM-DD
  next_action: "Experimenter: execute exp-001 and log results in 03_prototype/"
  ```

**Phase 3 → Phase 4 transition** (Experimenter → Integrator handoff):
- Gate criterion: `exp-*.md` executed, results logged
- Action: Integrator updates `dwi-state.md`:
  ```yaml
  current_phase: 4
  updated: YYYY-MM-DD
  next_action: "Integrator: write learn-001 verdict and assemble DoR package"
  ```

**Phase 4 conclusion** (Integrator + Product Owner):
- Integrator writes `learn-*.md` with final verdict
- Integrator updates `dwi-state.md`:
  ```yaml
  verdict: validated                    # or invalidated / pivoted
  updated: YYYY-MM-DD
  next_action: "PO: approval for Delivery track entry"
  ```
- Product Owner reviews and updates:
  ```yaml
  status: concluded
  updated: YYYY-MM-DD
  ```

### 12.3 Updating Hypothesis Count

Each time a new `hyp-*.md` file is created and linked (by adding `dwi_id: dwi-<topic-slug>` to its frontmatter):

- Increment `hypothesis_count`
- If the hypothesis is immediately validated, also increment `validated_count`
- Update the `Hypothesis Summary` table with a new row

Example:
```markdown
## Hypothesis Summary

| ID | Statement (short) | Status |
|:---|:-----------------|:-------|
| hyp-001 | RBAC schema supports 15 role profiles | validated |
| hyp-002 | MCP guardrails can be enforced per-tool | open |
```

### 12.4 Maintaining `next_action`

**`next_action` must always be:**
1. **Specific** — names a role and an artifact (e.g., `"Framer: write hyp-001"`, NOT `"continue research"`)
2. **Unambiguous** — the reader knows exactly what to do next without reading the body
3. **Actionable in the current phase** — pulls from the phase gate table (Section 6)
4. **Synchronous with `current_phase`** — the action describes work appropriate for that phase

**Bad examples:**
- `"more work on discovery"` ← vague
- `"run the experiment"` ← missing artifact name
- `"escalate to Architect"` ← not a work item, a meta-action

**Good examples:**
- `"Explorer: log obs-003 on role lifecycle during session handoff"`
- `"Framer: complete scope.md with success criteria for RBAC enforcement validation"`
- `"Designer: draft ADR-001 comparing in-memory vs persistent RBAC cache"`
- `"Experimenter: execute exp-001 spike on MCP tool guard feasibility and log results"`
- `"Integrator: write learn-001 verdict and assemble DoR for Delivery gate"`

### 12.5 Dashboard Maintenance

The Orchestrator (or a Knowledge Steward bot) regularly queries all `dwi-state.md` files to produce a cross-topic status dashboard.

Suggested monthly maintenance:
- Check all `dwi-state.md` files for stale `updated` dates (older than 30 days = at-risk)
- Verify `next_action` is current and role-specific
- Archive topics marked `status: concluded` that are >90 days old → move to `_archive/`

### 12.6 Practical Example: MCP RBAC Topic

**Initial state** (Day 1, phase 0):
```yaml
id: dwi-mcp-rbac
status: open
current_phase: 0
next_action: "Explorer: log obs-001 on current RBAC schema coverage"
hypothesis_count: 0
validated_count: 0
updated: 2026-03-03
```

**After Explorer files 3 observations** (Day 5, gate cleared → phase 1):
```yaml
id: dwi-mcp-rbac
status: in_progress
current_phase: 1
next_action: "Framer: write hyp-001 on schema scalability to 50 roles"
hypothesis_count: 1
updated: 2026-03-05
```

**After Framer defines scope** (Day 10, gate cleared → phase 2):
```yaml
id: dwi-mcp-rbac
status: in_progress
current_phase: 2
next_action: "Designer: draft ADR-001 comparing JSON vs YAML schema storage"
hypothesis_count: 2
updated: 2026-03-10
```

**After Designer drafts alternatives** (Day 15, gate cleared → phase 3):
```yaml
id: dwi-mcp-rbac
status: in_progress
current_phase: 3
next_action: "Experimenter: execute exp-001 load test on role binding queries"
hypothesis_count: 3
updated: 2026-03-15
```

**After Experimenter concludes** (Day 22, phase 4):
```yaml
id: dwi-mcp-rbac
status: in_progress
current_phase: 4
verdict: validated
next_action: "Integrator: write learn-001 verdict and assemble DoR package"
hypothesis_count: 3
validated_count: 2
updated: 2026-03-22
```

**After Integrator + PO approval** (Day 25, ready for Delivery):
```yaml
id: dwi-mcp-rbac
status: concluded
verdict: validated
next_action: "—"
updated: 2026-03-25
```

---

## 13. Relationship to Program Dashboard

The global Discovery dashboard (if one is created) would aggregate all `dwi-state.md` files to show:

```
Topic              Status        Phase  Validated  Next_Action
------             ------        -----  ---------  ---
mcp-rbac           concluded     4      2/3        —
role-protocol      in_progress   2      1/2        Designer: draft ADR-001
ssot-memory        in_progress   1      0/1        Framer: write hyp-001
workflow-protocol  open          0      n/a        Explorer: log obs-001
meta-security      open          0      n/a        Explorer: log obs-001
```

This dashboard would be maintained by the **Orchestrator** as part of their context hygiene responsibilities (Section 8 of `delivery.process.md`).
