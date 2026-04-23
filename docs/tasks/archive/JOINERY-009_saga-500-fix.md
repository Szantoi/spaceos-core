---
id: JOINERY-009
title: Saga 500 kivizsgálás — POST /bff/door-orders
status: active
priority: high
assignee: joinery
epic: cleanup-sprint
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/joinery/inbox/2026-04-16_009_saga-500-fix.md
---

E2E-033 futásakor 43-joinery-cutting-integration teszt POST /bff/door-orders 500-at kapott.
29-joinery-order (meglévő) zöld → sorrend/állapot függő hiba.
Root cause: CuttingProviderStub DI? Graceful degradation scope? Data state?
