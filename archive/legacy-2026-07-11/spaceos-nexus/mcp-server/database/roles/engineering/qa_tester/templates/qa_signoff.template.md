---
id: qa-signoff-template
title: "QA Sign-off Template"
description: "Snippet for the required QA sign-off section in a Task file. Paste as a ## QA sign-off section or save as qa.md next to the task."
type: template
scope: global
last_updated: 2026-03-01
---

# QA Sign-off Template

Use this snippet as the required QA sign-off section to paste into a Task file (or save as `qa.md` next to the task).

Example:

```markdown
## QA sign-off
- Reviewer: <name>
- Date: YYYY-MM-DD
- Checks performed:
  - [x] Unit tests ran (link)
  - [x] Integration tests ran (link)
  - [x] Acceptance criteria verified
- Notes / artifacts: <linkek, screenshotok, CI jobok>
```

> Tip: rövid, pontszerű bejegyzés legyen; a CI most figyeli a jelenlétet és warningot ad, ha hiányzik.
