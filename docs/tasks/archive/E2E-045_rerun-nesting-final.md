---
id: E2E-045
title: E2E rerun — nesting végső verifikáció (TenantSessionInterceptor után)
status: archive
priority: high
assignee: e2e
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/e2e/inbox/2026-04-16_045_rerun-nesting-final.md
  - docs/mailbox/e2e/outbox/2026-04-16_045_rerun-nesting-final-done.md
---

213/214 — 1 fail. cuttingSheetId DEFINIÁLT (POST 201 ✅). Nesting teszt fut (nem skip).
Fail: 500 InvalidOperationException — IInventoryProvider nincs regisztrálva a cutting DI-ban.
GetNestingResultQueryHandler dependency-je. CUTTING-009 kiadva stub regisztrációhoz.
