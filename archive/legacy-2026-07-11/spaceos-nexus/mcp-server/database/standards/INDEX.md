---
id: standards-index
title: "Standards Index"
description: "Navigation map for all standards. Machine-readable YAML catalog + human-readable tables. Read this first. File naming convention: <topic>.<type>.md"
type: index
scope: global
version: 2.0
created: 2026-03-03
last_updated: 2026-03-03
---

# Standards Index

> **Start here.** Single entry point for all standards.
>
> **File naming convention:** `<topic>.<type>.md`
> Types: `.meta-framework` · `.methodology` · `.process` · `.standard` · `.constraints` · `.glossary` · `.folder-structure` · `.development-framework` · `.protocol` · `.error-recovery` · `.workflow-diagram` · `.definition-of-done` · `.fsm-schema`

---

## YAML Catalog (machine-readable)

```yaml
standards:
  - id: two-track-meta-framework
    path: database/standards/00-foundation/two-track.meta-framework.md
    title: "Discovery & Delivery Two-Track Meta-Framework"
    type: meta-framework
    group: "00-foundation"
    read_order: 1
    summary: "Top-level two-track paradigm. Read before everything else."

  - id: system-constraints
    path: database/standards/00-foundation/system.constraints.md
    title: "System Constraints"
    type: constraints
    group: "00-foundation"
    read_order: 2
    summary: "Agent behavioral constraints and architectural principles. NOT app tech stack."

  - id: system-glossary
    path: database/standards/00-foundation/system.glossary.md
    title: "Agent System Terminology Glossary"
    type: glossary
    group: "00-foundation"
    read_order: 3
    summary: "Canonical definitions for all terms used across the system."

  - id: project-folder-structure
    path: database/standards/00-foundation/project.folder-structure.md
    title: "Project Folder Structure"
    type: folder-structure
    group: "00-foundation"
    read_order: 4
    summary: "Epic-First hierarchy: Program → Project → Milestone → Epic → Task."

  - id: discovery-methodology
    path: database/standards/01-discovery/discovery.methodology.md
    title: "Discovery Methodology (Double Diamond + HDD)"
    type: methodology
    group: "01-discovery"
    read_order: 5
    summary: "Double Diamond + HDD rationale, phase schemas, sizing rules, artifact templates."

  - id: discovery-process
    path: database/standards/01-discovery/discovery.process.md
    title: "Discovery Process"
    type: process
    group: "01-discovery"
    read_order: 6
    summary: "CANONICAL: Discovery roles, phase transitions, handoff protocol, anti-patterns."

  - id: discovery-work-item-standard
    path: database/standards/01-discovery/discovery.work-item.standard.md
    title: "Discovery Work Item (DWI) Standard"
    type: standard
    group: "01-discovery"
    read_order: 7
    summary: "Hybrid tracking for Discovery: dwi-state.md + HypothesisCard frontmatter."

  - id: delivery-process
    path: database/standards/02-delivery/delivery.process.md
    title: "Delivery Process"
    type: process
    group: "02-delivery"
    read_order: 8
    summary: "Epic lifecycle, role structure, implementation evidence rules."

  - id: epic-definition-of-done
    path: database/standards/02-delivery/epic.definition-of-done.md
    title: "Epic Definition of Done"
    type: definition-of-done
    group: "02-delivery"
    read_order: 9
    summary: "Mandatory acceptance criteria for every backlog item."

  - id: epic-fsm-schema
    path: database/standards/02-delivery/epic.fsm-schema.md
    title: "Epic FSM Schema"
    type: fsm-schema
    group: "02-delivery"
    read_order: 10
    summary: "CANONICAL: All FSM state definitions, transitions, retry rules, YAML schema."

  - id: agent-development-framework
    path: database/standards/03-agent-system/agent.development-framework.md
    title: "Agent Development Framework"
    type: development-framework
    group: "03-agent-system"
    read_order: 11
    summary: "Trace-driven LLMOps validation pipeline for agent roles."

  - id: agent-messaging-protocol
    path: database/standards/03-agent-system/agent.messaging.protocol.md
    title: "Agent Messaging Protocol v2.0"
    type: protocol
    group: "03-agent-system"
    read_order: 12
    summary: "CANONICAL: Date-sharded single-file-per-message protocol."

  - id: agent-error-recovery
    path: database/standards/03-agent-system/agent.error-recovery.md
    title: "Agent Error Recovery"
    type: error-recovery
    group: "03-agent-system"
    read_order: 13
    summary: "Retry logic, STOP criteria, required report templates on failure."

  - id: agent-workflow-diagram
    path: database/standards/03-agent-system/agent.workflow-diagram.md
    title: "Agent Workflow Diagram"
    type: workflow-diagram
    group: "03-agent-system"
    read_order: 14
    status: partial-deprecated
    summary: "Workflow diagrams valid. P1-P18 prompt catalog superseded by messaging protocol."

adrs:
  - id: ADR-003
    path: database/standards/adrs/ADR-003-knowledge-base-extraction.md
    title: "ADR-003: Extracting the Agent Knowledge Base"
    status: accepted
    date: 2026-02-24
    summary: "Move agent role definitions from database/ to src/agent-system/database/."
```

---

## 00-foundation — Read First

| File | Type | Purpose |
|:-----|:-----|:--------|
| [two-track.meta-framework.md](00-foundation/two-track.meta-framework.md) | `.meta-framework` | **Top-level.** Two-track paradigm, DoR gate, artifact lifecycle |
| [system.constraints.md](00-foundation/system.constraints.md) | `.constraints` | Agent behavioral constraints, architectural principles |
| [system.glossary.md](00-foundation/system.glossary.md) | `.glossary` | Canonical term definitions |
| [project.folder-structure.md](00-foundation/project.folder-structure.md) | `.folder-structure` | Epic-First folder hierarchy |

## 01-discovery — Discovery Track

| File | Type | Purpose |
|:-----|:-----|:--------|
| [discovery.methodology.md](01-discovery/discovery.methodology.md) | `.methodology` | Double Diamond + HDD rationale, phase schemas, Fast Track templates |
| [discovery.process.md](01-discovery/discovery.process.md) | `.process` | **CANONICAL roles.** Phase transitions, handoffs, anti-patterns |
| [discovery.work-item.standard.md](01-discovery/discovery.work-item.standard.md) | `.standard` | DWI hybrid tracking (`dwi-state.md` format + HypothesisCard) |

## 02-delivery — Delivery Track

| File | Type | Purpose |
|:-----|:-----|:--------|
| [delivery.process.md](02-delivery/delivery.process.md) | `.process` | Epic lifecycle, role structure, implementation evidence rules |
| [epic.definition-of-done.md](02-delivery/epic.definition-of-done.md) | `.definition-of-done` | DoD checklist |
| [epic.fsm-schema.md](02-delivery/epic.fsm-schema.md) | `.fsm-schema` | **CANONICAL FSM.** All state definitions, transitions, YAML schema |

## 03-agent-system — Agent Infrastructure

| File | Type | Purpose |
|:-----|:-----|:--------|
| [agent.development-framework.md](03-agent-system/agent.development-framework.md) | `.development-framework` | Agent validation pipeline |
| [agent.messaging.protocol.md](03-agent-system/agent.messaging.protocol.md) | `.protocol` | **CANONICAL messaging.** Date-sharded message protocol |
| [agent.error-recovery.md](03-agent-system/agent.error-recovery.md) | `.error-recovery` | Retry logic, STOP criteria |
| [agent.workflow-diagram.md](03-agent-system/agent.workflow-diagram.md) | `.workflow-diagram` | ⚠️ Diagrams valid; P1–P18 catalog superseded |

## adrs — Architecture Decision Records

| File | Status | Summary |
|:-----|:-------|:--------|
| [ADR-003-knowledge-base-extraction.md](adrs/ADR-003-knowledge-base-extraction.md) | accepted | KB extraction to `src/agent-system/database/` |

---

## Reading Order by Role

| Role | Read in this order |
|:-----|:-------------------|
| **New to the system** | `two-track.meta-framework` → `system.glossary` → `project.folder-structure` |
| **Discovery agent** (Explorer / Framer / Designer / Experimenter / Integrator) | `two-track.meta-framework` → `discovery.methodology` → `discovery.process` → `discovery.work-item.standard` |
| **Delivery agent** (Orchestrator / Tech Lead / Developer / QA) | `two-track.meta-framework` → `delivery.process` → `epic.definition-of-done` → `epic.fsm-schema` |
| **Agent author** | `agent.development-framework` → `agent.messaging.protocol` → `agent.error-recovery` |
| **Architect / Product Owner** | `two-track.meta-framework` → `system.constraints` → all ADRs |

---

## Quick-find by type suffix

```powershell
# All process files
Get-ChildItem "database/standards" -Recurse -Filter "*.process.md"

# All canonical FSM/messaging sources
Get-ChildItem "database/standards" -Recurse -Filter "*.fsm-schema.md","*.protocol.md"

# By group
Get-ChildItem "database/standards/01-discovery" -File
```
