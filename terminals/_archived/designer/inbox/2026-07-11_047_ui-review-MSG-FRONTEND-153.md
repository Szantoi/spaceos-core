---
id: MSG-DESIGNER-REVIEW-47
from: system
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-FRONTEND-153
created: 2026-07-11
content_hash: 8b402b451b78a101da6f5fd76c19a0311b87506b4941ba5048555566201c8c38
---

# UI Review Required

Frontend completed: **MSG-FRONTEND-153**

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
   - If issues: Create feedback task f