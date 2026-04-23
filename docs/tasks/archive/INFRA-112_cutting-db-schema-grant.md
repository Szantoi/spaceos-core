---
id: INFRA-112
title: PostgreSQL schema GRANT — spaceos_cutting / inventory / procurement
status: archive
priority: high
assignee: infra
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/infra/inbox/2026-04-16_112_cutting-db-schema-grant.md
  - docs/mailbox/infra/outbox/2026-04-16_112_cutting-db-schema-grant-done.md
---

USAGE + SELECT/INSERT/UPDATE/DELETE + SEQUENCES grant mindhárom sémán. ALTER DEFAULT PRIVILEGES
is beállítva (jövőbeli migrációk is hozzáférhetők). 42501 megszűnt. Cutting POST → 500 domain hiba
(nem 42501) ✅. Minimal privilege (nem ALL PRIVILEGES). E2E-043 kiadva nesting verifikációhoz.
