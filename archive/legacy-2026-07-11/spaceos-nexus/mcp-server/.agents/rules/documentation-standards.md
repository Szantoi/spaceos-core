---
trigger: always_on
description: Mandatory documentation patterns and file updates for all agents.
---

# Documentation Standards

## Process Awareness
- **State Management**: Always update `state.md` with progress after significant milestones or task completions.
- **Fact Summary**: Use the "Fact Summary Pattern" for status updates (concise, data-driven).

## Task Execution
- **Implementation Summary**: EVERY finished task must have an `Implementation Summary` in the task file (or as a separate artifact if required).
- **Decision Logs**: Track key choices and architectural deviations in `decision_logs/` within the epic or project structure.

## Templates
- **Standard Templates**: Use predefined templates for ADRs, Epic Plans, and Implementation Reports.
- **Lazy Loading**: Do not load entire documentation files into context; only read relevant sections.
