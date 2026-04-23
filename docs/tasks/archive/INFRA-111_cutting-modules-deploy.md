---
id: INFRA-111
title: Cutting/Inventory/Procurement modulok deploy — CUTTING-007 fix live
status: archive
priority: high
assignee: infra
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/infra/inbox/2026-04-16_111_cutting-modules-deploy.md
  - docs/mailbox/infra/outbox/2026-04-16_111_cutting-modules-deploy-done.md
---

3 modul deploy: spaceos-modules-cutting (79d16a2), spaceos-modules-inventory (b67d9bc),
spaceos-modules-procurement (1fba5e2). JWT auth OK — cutting 500, inventory 404, procurement 405
(mind nem 401). CUTTING-007 GetTenantId("tid") + MapInboundClaims=false live.
