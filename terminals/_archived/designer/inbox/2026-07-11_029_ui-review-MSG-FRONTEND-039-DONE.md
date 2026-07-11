---
id: MSG-DESIGNER-REVIEW-29
from: system
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-FRONTEND-039-DONE
created: 2026-07-11
content_hash: fe0dbc7b9ef886631d42305f0519b7cc985a07fdd9c603b1923127604b46a646
---

# UI Review Required

Frontend completed: **MSG-FRONTEND-039-DONE**

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
