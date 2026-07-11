---
trigger: always_on
description: Mandatory enforcement of the project's core process frameworks and organizational standards.
---

# Process & Framework Standards

All agents MUST strictly adhere to the following standards for any project activity.

## 1. Project Folder Structure
- **Reference**: [Project_Folder_Structure_Standard.md](file:///c:/Users/szant/Documents/Development/JoineryTech.Flow/src/agent-system/database/standards/Project_Folder_Structure_Standard.md)
- **Rule**: All file creation and organization must follow the `Program → Project → Milestone → Epic → Task` hierarchy.
- **Workflow**: Use `/project-setup` and `/epic-setup` for initializing structures.

## 2. Discovery Framework
- **Reference**: [Plans_Discovery_Framework_Standard.md](file:///c:/Users/szant/Documents/Development/JoineryTech.Flow/src/agent-system/database/standards/Plans_Discovery_Framework_Standard.md)
- **Rule**: For research and "discovery" tasks (Phase 0-4), the defined roles (Explorer, Framer, Experimenter, Integrator) and their respective workflow phases must be followed.
- **Workflows**: `/discovery-observation`, `/discovery-define`, `/discovery-ideate`, `/discovery-prototype`, `/discovery-test-and-learn`, `/discovery-fast-track`.

## 3. Operative Process Framework (OPF)
- **Reference**: [Operative_Process_Framework_Standard.md](file:///c:/Users/szant/Documents/Development/JoineryTech.Flow/src/agent-system/database/standards/Operative_Process_Framework_Standard.md)
- **Rule**: The complete lifecycle of an Epic (Phase 0-7) must be strictly managed through the defined personas and handoffs.
- **Workflows**: `/orchestrator-epic-execution`, `/architect-epic-planning`, `/tech-lead-task-planning`, `/developer-implementation`, `/qa-tester-testing`, `/tech-lead-epic-review`, `/architect-signoff`, `/epic-closure`.

## Enforcement
- Agents must NOT skip phases or bypass the defined roles in these standards.
- Every phase transition must be documented in the relevant `state.md` or task files.
- If a standard is violated, the Agent must immediately alert the user or the Orchestrator.
