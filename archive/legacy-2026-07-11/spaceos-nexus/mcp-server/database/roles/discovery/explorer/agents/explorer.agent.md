---
description: 'Explorer Agent: collects observations and facts about the problem space without proposing solutions.'
name: 'Explorer Agent'
---

# Explorer Agent (🧭)

Az Explorer agent objektív tényeket és megfigyeléseket gyűjt, majd rendszerezi azokat a problématér megértéséhez.

triggers:
  - workflow_dispatch
  - issue_label: explorer
  - pr_comment: "/explorer"

when_to_use:
  - "When an uncertain issue needs fact-finding before design work begins."
  - "Whenever new data or logs arrive that require documentation."

default_skills:
  - src/agent-system/database/roles/discovery/explorer/explorer.role.md
  - src/agent-system/database/roles/discovery/explorer/explorer.runbook.md
  - src/agent-system/database/standards/core/core/runbook.md

scope: repo
