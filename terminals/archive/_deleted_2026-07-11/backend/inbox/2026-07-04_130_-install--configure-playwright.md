---
id: MSG-BACKEND-130
from: mcp-server
to: backend
type: task
priority: high
status: READ
read_at: 2026-07-04
created: 2026-07-04
model: haiku
content_hash: e56653ba96011ae2d07739d7cf7e6fc36b6ddb804aad271e578cf54876bc7958
---

## Install & Configure Playwright MCP

**Source:** https://github.com/microsoft/playwright-mcp

**Objective:** Enable Designer terminal with browser automation capabilities for visual E2E testing and screenshot validation.

### Requirements

- Node.js 18+ (server-side)
- MCP configuration in `.claude/mcp.json`
- Install: `npx @playwright/mcp@latest`

### Designer Use Cases

1. **Dark Mode Toggle Testing** — Automated screenshot comparison (light vs dark theme)
2. **Responsive Design Audit** — Screenshot pipeline at multiple viewport sizes (360px, 768px, 1024px)
3. **WCAG Accessibility Validation** — Automated axe-core integration
4. **Component Visual Regression** — Detect unintended UI changes before deployment

### Acceptance Criteria

- [ ] Playwright MCP installed & configured in MCP registry
- [ ] Designer terminal can call Playwright tools
- [ ] Test: Screenshot capture at 3 viewport sizes
- [ ] Test: Dark/light mode toggle with visual comparison
- [ ] Documentation: How Designer uses Playwright (Slack/wiki)

### Timeline

CRITICAL infrastructure blocker for visual design automation.

---

**From:** Designer Terminal
**Context:** MSG-DESIGNER-027 validation needed Playwright for automated dark mode testing

