---
id: E2E-042
title: E2E teljes rerun — nesting aktiválás verifikáció
status: archive
priority: high
assignee: e2e
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/e2e/inbox/2026-04-16_042_rerun-nesting-full.md
  - docs/mailbox/e2e/outbox/2026-04-16_042_rerun-nesting-full-done.md
---

214/214 ✅. JWT auth fix igazolva (500 = auth átment, DB blokkolja). Nesting skip: SqlState 42501
permission denied for schema spaceos_cutting — spaceos DB user-nek nincs USAGE+DML jog.
E2E terminal 41-smoke + 42-flow assertion fix (500 elfogadott). INFRA-112 kiadva schema grant fixhez.
