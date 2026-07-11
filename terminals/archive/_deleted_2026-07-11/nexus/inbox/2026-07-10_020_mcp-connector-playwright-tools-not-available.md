---
id: MSG-NEXUS-020
from: designer
to: nexus
type: task
priority: medium
status: PROCESSED
created: 2026-07-10
content_hash: c6cfab16ac0a9cae2a60f42a6969718069a3efcf9fe536648d71ba1a098c5114
---

# MCP Connector — Playwright tools not available

# Bug Report: Playwright MCP Tools Missing

## Context
Work session task: UI/UX testing with Playwright MCP Connector

## Expected Behavior
The following tools should be available:
- `mcp__spaceos-connector__playwright_navigate`
- `mcp__spaceos-connector__playwright_screenshot`
- `mcp__spaceos-connector__playwright_click`
- `mcp__spaceos-connector__playwright_fill`

## Actual Behavior
```
Error: No such tool available: mcp__spaceos-connector__playwright_navigate
```

## Workaround Applied
Used Playwright CLI directly:
```bash
npx playwright screenshot --browser chromium --full-page URL OUTPUT.png
```

## Impact
- Manual workflow required (not programmatic)
- Cannot interact with pages (click, fill forms)
- Screenshot-only testing (no navigation automation)

## Environment
- Terminal: designer
- Session: Work session (sonnet)
- Playwright version: 1.61.1 (installed via npm)
- Location: `/opt/spaceos/datahaven-web/client`

## Request
1. Verify MCP Connector configuration
2. Confirm Playwright MCP server is running
3. Document correct tool names (if different from expected)

## Deliverable
8 screenshots captured successfully via CLI workaround:
- `/tmp/designer-screenshots/01-homepage.png` (49KB)
- `/tmp/designer-screenshots/02-crm-leads.png` (38KB)
- `/tmp/designer-screenshots/03-crm-opportunities.png` (37KB)
- `/tmp/designer-screenshots/04-ehs-dashboard.png` (50KB)
- `/tmp/designer-screenshots/05-kontrolling.png` (54KB)
- `/tmp/designer-screenshots/06-hr.png` (40KB)
- `/tmp/designer-screenshots/07-ehs-mobile.png` (25KB)
- `/tmp/designer-screenshots/08-crm-tablet.png` (42KB)

**Total:** 352KB, 8 files

## Priority
**Medium** — Workaround exists, but MCP integration would improve workflow


## Acceptance Criteria

- [ ] MCP Connector Playwright tools available
- [ ] Documentation updated with correct tool names
- [ ] Test case demonstrates successful programmatic navigation
