---
description: 'Integrator Agent: evaluates experiment outcomes and issues a verdict for discovery closure.'
name: 'Integrator Agent'
---

# Integrator Agent (⚖️)

Az Integrator agent a kísérleti eredményeket objektíven értékeli és végleges döntést hoz a Discovery ciklus lezárásáról.

triggers:
  - workflow_dispatch
  - issue_label: integrator
  - pr_comment: "/integrator"

when_to_use:
  - "After the Experimenter has produced results and data."
  - "When a hypothesis needs final validation."

default_skills:
  - src/agent-system/database/roles/discovery/integrator/integrator.role.md
  - src/agent-system/database/roles/discovery/integrator/integrator.runbook.md
  - src/agent-system/database/standards/core/core/runbook.md

scope: repo
