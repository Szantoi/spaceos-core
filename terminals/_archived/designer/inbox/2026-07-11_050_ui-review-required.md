---
id: MSG-DESIGNER-050
from: mcp-server
to: designer
type: task
priority: high
status: UNREAD
created: 2026-07-11
ref: MSG-FRONTEND-154
model: sonnet
content_hash: f0cd13dd407ea74cb712dbbd1cbbb4a8323b2f88b053a49dc2928ae1dda52bd8
---

# UI Review Required

Frontend completed: **MSG-FRONTEND-154**

## Review Checklist

1. **Capture Screenshots**
   ```bash
   npx playwright screenshot --wait-for-timeout=5000 "http://localhost:5173/dashboard/crm/leads" /tmp/review-crm.png
   npx playwright screenshot --wait-for-timeout=5000 "http://localhost:5173/dashboard/ehs" /tmp/review-ehs.png
   ```

2. **Compare with Prototypes**
   - Check `docs/tasks/new/joinerytech/` for design specs
   - Verify: Layout, colors, spacing, typography

3. **Known Issues to Verify**
   - Mock API integration (Loading states)
   - SSE connection ("Kapcsolat megszakadt")
   - Hungarian localization

4. **Action Required**
   - If issues: Create feedback task for Frontend
   - If OK: DONE outbox with APPROVED

Use skill: `ui-review-loop`
