---
id: tech-lead-signoff-template
title: "Tech Lead Sign-off Template"
description: "Template for the Tech Lead sign-off section at Epic closure. Add as a ## Tech Lead sign-off section in the Epic README or save as tech_lead_signoff.md under the epic directory."
type: template
scope: global
last_updated: 2026-03-01
---

# Tech Lead Sign-off Template

Use this template at Epic closure (after all tasks are complete and verified).

Example:

```markdown
## Tech Lead sign-off
- Sign-off by: <name>
- Date: YYYY-MM-DD
- Epic: {EPIC-ID or path}
- Verification performed:
  - [x] All tasks have Implementation Report
  - [x] All tasks have QA sign-off
  - [x] CI checks passed (unit/integration/E2E/SAST)
  - [x] No unresolved security findings (or security task created)
- Notes / follow-ups: <links to follow-up tasks or issues>
```

> Tip: Add this as a `## Tech Lead sign-off` section in the Epic README or save as `tech_lead_signoff.md` under the epic directory.
