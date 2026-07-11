---
id: template-implementation_report
title: "Implementation Report Template (Frontend)"
description: "Technical evidence of Task completion for frontend development. Fill out after implementing a task; summarize changes using the Fact Summary Pattern."
type: template
scope: frontend
last_updated: 2026-03-01
---

# 📝 Implementation Report: [[ TASK_ID ]]

**Purpose:** This document is the technical evidence of Task completion. Summarize changes concisely and factually, following the **Fact Summary Pattern**.

## 📋 Frontmatter (Copy to the top of the file)

```yaml
---
id: implementation-[TASK_ID]
title: "Implementation Report - [TASK_ID]"
type: implementation
project: {project-slug}
task: [TASK_ID]
author: {Agent Name / Developer Name}
date: {YYYY-MM-DD}
status: review_needed
---
```

Suggested sections (use `##` headers):

## Summary

- Brief 1-2 sentence summary: what you achieved.

## Changes / Files Modified

- `src/xxx` - brief description
- PR: <https://github.com/.../pull/123>

## How I tested

- Commands (e.g. `npm run test`, `npm run build`)
- CI jobs / E2E runs (links)

## Issues found / Workarounds

- Issues found, what was resolved, what remains open (issue link)

## Remaining risks / Follow-ups

- Next steps (issue/PR numbers)

## Acceptance / DoD

- [ ] Unit tests added
- [ ] Integration/E2E passed
- [ ] QA verification or screenshots attached

> Tip: If the Task touched multiple files, provide the exact `git diff` reference or PR link. For detailed logs, attach as a separate artifact and link it.
