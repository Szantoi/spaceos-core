---
id: PROCUREMENT-015
title: from-reorder-alert GUC fix — RLS RETURNING violation
status: active
priority: critical
assignee: PROCUREMENT
epic: procurement-v2-deploy
blocked_by: ""
created: 2026-05-29
updated: 2026-05-29
docs:
  - docs/mailbox/procurement/inbox/2026-05-29_015_reorder-alert-guc-fix.md
---

# from-reorder-alert GUC fix

`InternalEndpoints.cs` `from-reorder-alert` endpointban a GUC `app.current_tenant_id` nincs
beállítva a `mediator.Send` előtt. EF Core RETURNING-ja RLS USING check-et triggerel → 42501.

Fix: `dbContext.Database.OpenConnectionAsync` + `set_config` + mediator.Send, ugyanolyan mintával,
mint a meglévő `delete by tenant` endpoint.
