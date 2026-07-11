---
id: MSG-DESIGNER-029
from: monitor
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: GOAL-2026-07-10-UI-REVIEW-LOOP
epic_id: EPIC-JOINERYTECH-UI
created: 2026-07-10
content_hash: 14f523eba61072b7090b431ebbd4a32144a2a4771dc72821b6880b45df724036
---

# ✅ Goal Completed: UI Review Loop: Trigger Designer review when Frontend completes UI tasks

Frontend DONE message detected!

## UI Review Required

1. Capture screenshots with Playwright CLI:
   ```bash
   npx playwright screenshot --wait-for-timeout=5000 "http://localhost:5173/dashboard/crm/leads" /tmp/review-crm.png
   ```

2. Compare with prototypes in `docs/tasks/new/joinerytech/`

3. Check: Layout, colors, spacing, typography, interactions

4. If issues found: Create feedback inbox for Frontend

5. If OK: DONE outbox with APPROVED

Use skill: ui-review-loop

