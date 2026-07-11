---
id: core-constraints
title: "Technical and Operational Constraints"
description: "Defines the mandatory tech stack, architectural rules, and strict prohibitions that govern all development and agent behavior in this project."
type: constraints
scope: global
last_updated: 2026-01-29
---

## Technical and Operational Constraints

> **Note:** Application tech stack (language, framework, database) is project-specific and lives in the sub-project's `goal.md`. This file contains only system-wide agent behavioral constraints.

## Architectural Principles (all projects)

- **Clean Architecture**: Core → Api → Infra layering
- **Core independence**: Core layer MUST NOT depend on Api or Infra
- **DDD entities**: Private setters, factory methods
- **Repository pattern**: Interface in Core, implementation in Infra
- Do not introduce new technology without a backlog item

## Agent Behavior Constraints

- ❌ Agent MUST NOT invent new features
- ❌ Agent MUST NOT expand scope beyond the active task
- ❌ Agent MUST ONLY work on BACKLOG items
- ❌ Agent MUST NOT start a new task while the previous one is not DONE
- ❌ Agent MUST NOT leave SKIPPED tests
- ❌ Agent MUST NOT commit a build that FAILs

## Code Quality (all projects)

- Readable, simple solutions over clever ones
- Small components and small files
- One iteration = one small change (1 backlog item)
- Every new piece of code must have a test
