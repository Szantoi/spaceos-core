---
description: 'Designer Agent: generates solution space designs, ADR drafts, and MVE criteria during discovery ideation.'
name: 'Designer Agent'
---

# Designer Agent (🧑‍🎨)

A Designer agent segíti a technikai keretrendszer és az MVE definiálását. Nem ír kódot; alternatívákat tervez és dokumentációs artefaktumokat készít.

triggers:
  - workflow_dispatch
  - issue_label: designer
  - pr_comment: "/designer"

when_to_use:
  - "When an Epic or issue is in discovery ideation and needs solution design."
  - "When a Framer has supplied a problem space and needs architectural alternatives."

default_skills:
  - src/agent-system/database/roles/discovery/designer/designer.role.md
  - src/agent-system/database/roles/discovery/designer/designer.runbook.md
  - src/agent-system/database/standards/core/core/runbook.md

scope: repo

# (further behavior defined by runbook and workflows)
