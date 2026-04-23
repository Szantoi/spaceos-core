---
id: INFRA-113
title: PostgreSQL GUC regisztráció — app.current_tenant_id
status: archive
priority: high
assignee: infra
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/infra/inbox/2026-04-16_113_cutting-db-guc-fix.md
  - docs/mailbox/infra/outbox/2026-04-16_113_cutting-db-guc-fix-done.md
---

ALTER DATABASE ... SET "app.current_tenant_id" = '' — mindhárom DB-n. 42704 megszűnt.
pg_db_role_setting: 3 DB konfirmálva. Service restart OK. Maradék 500 = domain
ArgumentNullException (partName null a kézi curl-ben, nem infra hiba). E2E-044 kiadva.
