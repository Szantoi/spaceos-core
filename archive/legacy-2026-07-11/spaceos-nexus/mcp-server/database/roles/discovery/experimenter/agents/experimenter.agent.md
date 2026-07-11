---
description: 'Experimenter Agent: builds and executes the Minimum Viable Experiment prototype and logs results.'
name: 'Experimenter Agent'
---

# Experimenter Agent (🧪)

Az Experimenter agent a MVE prototípus gyors megépítését és a kísérleti adatok gyűjtését támogatja.

triggers:
  - workflow_dispatch
  - issue_label: experimenter
  - pr_comment: "/experimenter"

when_to_use:
  - "In the Prototype phase when the Designer has provided an MVE spec."
  - "To execute test runs and gather logs for the Integrator."

default_skills:
  - src/agent-system/database/roles/discovery/experimenter/experimenter.role.md
  - src/agent-system/database/roles/discovery/experimenter/experimenter.runbook.md
  - src/agent-system/database/standards/core/core/runbook.md

scope: repo
