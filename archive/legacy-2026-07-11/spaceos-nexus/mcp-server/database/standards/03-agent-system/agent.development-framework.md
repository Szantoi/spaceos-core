---
id: agent-development-framework-standard
title: "Agent Development Framework Standard"
description: "Defines the trace-driven, iterative LLMOps validation pipeline every agent role must pass before reaching Production status."
type: standard
scope: global
category: agentops
last_updated: 2026-03-01
---

# Agent Development Framework Standard

## 1. Purpose

Every AI agent in the JoineryTech.Flow system (e.g. Backend Developer, Integrator, Orchestrator) must behave in a deterministic, strictly governed manner. Traditional software engineering (SWE) methodologies alone cannot achieve this.

This standard defines the **Trace-Driven, Iterative (LLMOps)** workflow that **every** newly created or modified agent role must pass before receiving Production status.

**Core principle:** Writing a system prompt or YAML definition is never sufficient on its own. Agent behaviour must be enforced against specifically curated datasets through Role Enforcement testing.

---

## 2. The Training and Evaluation Cycle (Evaluator-Optimizer Loop)

Introducing a new or changed role requires a three-phase test cycle (Validation Pipeline) executed against the definitions in `src/agent-system/database/roles/`.

### Phase 1: Dataset Definition (Test Cases)

A test scenario dataset must be created for each agent. The dataset must contain three types of test samples (traces):

**Happy Path**
Typical, correctly formatted tasks.
Expected outcome: Output produced in full compliance with all rules.

**Edge Cases (Incomplete context)**
Tasks where a required input is clearly missing.
Expected outcome: The agent recognises the gap and refuses to proceed — for example, a Knowledge Steward must not produce a report from invalid data.

**Red Teaming (Prompt Injection / Role Break)**
Deliberate provocation — explicitly asking the agent to cross its role boundaries (e.g. asking a QA Tester to write production code).
Expected outcome: Firm refusal and strict adherence to the role definition.

Test datasets are stored in the role's test folder or optionally under `.gemini/`.

---

### Phase 2: Simulation and Evaluation (Evaluator)

A dedicated Judge — either a human supervisor (Product Owner / Architect) or an automated evaluation process — tests the role:

1. **Input:** The Judge receives the system prompt, the `role.schema.yaml`, and the dataset items.
2. **Output generation:** The agent (or LLM) generates responses.
3. **Scoring:** The Judge evaluates compliance against the "Limitations" and "Responsibilities" defined in the role. (Violated? Fulfilled?)

---

### Phase 3: Optimisation (Optimizer)

When a test — especially Red Teaming — produces a **FAIL** result, immediate intervention is required:

- Rewrite the `description` or `limitations` rules.
- Embed **N-shot (Few-Shot) examples** in the prompt — provide the agent with concrete, unambiguous examples of correct behaviour (e.g. refine the `messages/` templates).
- Repeat the cycle (Phase 2 → Phase 3) until the agent profile passes all tests with 100% reliability.

---

## 3. Integration with the Discovery Framework

The Agent Development Cycle is tightly coupled to the Discovery Framework workflow.

During the research and development of every new role, the LLMOps Evaluator cycle **MUST** be executed during the **`03_prototype`** (MVE design) or **`04_test-and-learn`** (testing) phase. A Discovery is only considered **Validated** once the role has passed these rigorous LLM tests.

> **Architectural answer:** This framework addresses the question "How do we enforce agent constraints?" — Answer: Through iterative Red Teaming and instruction + Schema YAML design optimisation.

---

## 4. Referenced Documents

- `Operative_Process_Framework_Standard.md`
- `Plans_Discovery_Framework_Standard.md`
- Discovery learning: `agent-system-v2/workflow-protocol/04_test-and-learn/learn-004-agent-dev-workflow-validated.md`
