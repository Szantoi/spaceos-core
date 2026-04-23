---
id: INFRA-072
title: Kernel binary deploy — commit 46d6352 (RLS UUID fix)
status: active
priority: critical
assignee: VPS Operator (INFRA)
epic: INFRA
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/inbox/2026-04-14_072_kernel-deploy-46d6352.md
  - docs/mailbox/kernel/outbox/2026-04-14_067_flowepic-close-fsm-fix-done.md
---

## Feladat

Kernel 46d6352 deploy VPS-re.

**Fix:** TenantSessionInterceptor.ResolveValidTenantId() — tid first priority (RLS UUID mismatch javítva)

**Várható hatás deploy után:**
- PUT /bff/api/flow-epics/:id/close → 200 (volt: 500 RLS violation)
- E2E Batch 0 legacy fail: 05-close FSM → zöldre fordul

**Párhuzamos:** INFRA-071 Orchestrator restart (b7b4581) — mindkettő kell az E2E-014 rerunhoz.
