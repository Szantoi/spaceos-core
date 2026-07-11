---
id: MSG-DESIGNER-REVIEW-38
from: system
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-FRONTEND-001-DONE
created: 2026-07-11
content_hash: 9b27147f66aea34e8b14ddffd3756b2a9bce7f173462ef34e4bf4703443e6388
---

# UI Review Required

Frontend completed: **MSG-FRONTEND-001-DONE**

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
