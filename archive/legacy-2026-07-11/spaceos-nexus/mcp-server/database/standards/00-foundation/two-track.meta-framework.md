---
title: "Discovery & Delivery Meta-Framework Standard"
description: "A general meta-framework defining the separation and integration of the problem space (learning) and the solution space (execution) for JoineryTech.Flow projects."
type: reference_guide
scope: global
version: "1.0"
date: 2026-03-03
methodology:
  - Dual-Track Agile
  - PDCA (Plan-Do-Check-Act)
  - Lean Startup
complementary_standards:
  - docs/standards/01-discovery/discovery.methodology.md
  - docs/standards/01-discovery/discovery.process.md
  - docs/standards/01-discovery/discovery.work-item.standard.md
  - docs/standards/02-delivery/delivery.process.md
  - docs/standards/02-delivery/epic.fsm-schema.md
  - docs/standards/02-delivery/epic.definition-of-done.md
  - docs/standards/00-foundation/project.folder-structure.md
revision_history:
  - version: "1.0"
    date: 2026-03-03
    author: "GitHub Copilot"
    change: "Initial version — imported into JoineryTech.Flow system"
---

## **Discovery & Delivery Meta-Framework Standard**

This document defines the **Discovery & Delivery** paradigm, which serves as the foundation for all projects and development cycles within the system.

The goal of this framework is the systematic reduction of risks (business, user, and technological) by splitting the process into two parallel but complementary tracks. This approach guarantees that we are "building the right thing" (Discovery) and "building the thing right" (Delivery).

## **1\. System Architecture: The Two Tracks**

The workflow is based on a physical and logical separation. The two tracks demand different mental models (focus modes) and measurement metrics.

> **Note:** This document is the top-level meta-framework. Detailed process rules are in `docs/standards/01-discovery/discovery.methodology.md` (Discovery) and `docs/standards/02-delivery/delivery.process.md` (Delivery). In case of conflict, the detailed standards take precedence.

### **1.1. The Discovery Track (The Problem Space)**

* **Location:** `docs/joinerytech-flow/discovery/`
* **Primary Goal:** Reduce uncertainty, maximize learning, and validate hypotheses as quickly and cheaply as possible.
* **Core Question:** *What should we build and why?*
* **Managed Risks:**
  * *Value Risk* (Will the user/business find it valuable?)
  * *Usability Risk* (Can users figure out how to use it?)
  * *Viability Risk* (Does it work for the business?)
* **Output:** Validated plans, formalized hypotheses, and rejected ideas (pivots/archives).
* **Progress Tracking:** Each topic maintains a `dwi-state.md` file (Discovery Work Item) that captures current phase, next action, and verdict — without imposing Delivery-style task cards. See `docs/standards/01-discovery/discovery.work-item.standard.md`.

### **1.2. The Delivery Track (The Solution Space)**

* **Location:** `docs/joinerytech-flow/delivery/<sub-project>/milestones/` and `docs/joinerytech-flow/delivery/epics/`
* **Primary Goal:** Deliver scalable, high-quality, and maintainable systems/products.
* **Core Question:** *How do we build it robustly?*
* **Managed Risks:**
  * *Feasibility Risk* (Can we build it with the chosen architecture?)
  * *Quality Risk* (Is it reliable, secure, and bug-free?)
  * *Scalability Risk* (Is it sustainable and scalable?)
* **Output:** Working product increments, clean code, standardized processes.
* **FSM Lifecycle:** All Epics follow a deterministic state machine. Canonical state definitions and transitions: → `docs/standards/02-delivery/epic.fsm-schema.md`

## **2\. The Interface: Definition of Ready (DoR)**

The transition between Discovery and Delivery is a strict quality gate. Its purpose is to prevent unvalidated or poorly specified tasks from entering the Delivery track (where execution costs are high).

Every Epic or Task must meet the following machine-verifiable criteria before crossing over into the `docs/joinerytech-flow/delivery/<project>/goal.md` file:

| Criterion (INVEST-based) | Machine/Operational Check (Checklist) | Required Discovery Artifact |
| :---- | :---- | :---- |
| **Independent** | No unresolved external blockers or missing dependencies. | dependency\_map.md |
| **Negotiable** | The goal is fixed, but technical implementation details are flexible (leaves room for engineering decisions). | Fixed scope.md |
| **Valuable** | The business goal and problem existence are proven by data or experimentation. | learn-\*.md (Validated status) |
| **Estimable** | Technical complexity is known; foundational architecture principles are established. | ADR-\*.md (Draft/Accepted) |
| **Feasibility Proven** | For high-risk items, a technical experiment (Spike/PoC) has proven feasibility. | exp-\*.md |

## **3\. Artifact Lifecycle and Flow**

Knowledge sharing between the two tracks occurs via standardized documents. The table below defines the transformation from the problem space to the solution space.

| Discovery Phase (Learning) | Generated Artifact | Expected Delivery Input (Execution) |
| :---- | :---- | :---- |
| Observation (Understand) | obs-\*.md (Observation) | \- (Context only) |
| Hypothesis (Define) | hyp-\*.md (Hypothesis) \+ scope.md | goal.md (Project Vision & Scope) |
| Concept (Ideate) | ADR-\*.md (Architecture Decision) | plan.md (Architectural blueprint) |
| Experiment (Prototype) | exp-\*.md (Experiment results) | Implementation specifications |
| Evaluation (Learn) | learn-\*.md (Handoff/Translation) | epics/ (Actionable Backlog) |

## **4\. Roles**

Each track has dedicated roles. Role definitions are the canonical source — this document only maps them to tracks.

| Track | Canonical source |
|:------|:----------------|
| **Discovery roles** (Explorer, Framer, Designer, Architect, Experimenter, Integrator, Product Owner) | `docs/standards/01-discovery/discovery.process.md` |
| **Delivery roles** (Orchestrator, Tech Lead, Developers, QA, Knowledge Steward) | `.github/agents/*.agent.md` |

> **Feedback loop:** Delivery generates real-world data → new `obs-*.md` → new Discovery iteration. The two tracks are never one-way (waterfall).
