---
id: MSG-DESIGNER-052
from: mcp-server
to: designer
type: task
priority: high
status: READ
created: 2026-07-11
ref: MSG-FRONTEND-881-DONE
model: sonnet
content_hash: 8da01b83f689150eeb0b34db1f0929c94f51628239b6c4742165fbd852ee0169
---

# UI Review Required

Frontend completed: **MSG-FRONTEND-881-DONE**

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
