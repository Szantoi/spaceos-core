---
id: MSG-DESIGNER-REVIEW-48
from: system
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-FRONTEND-154
created: 2026-07-11
content_hash: 079d5ad7b4bc60c8fbe286b88cc2f89913e2d8dbdf70ad4d1e93fe5ae8844213
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
   - If issues: Create feedback task f