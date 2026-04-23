---
id: ORCH-061
title: proof.route.ts Content-Type normalizálás (415 fix)
status: active
priority: critical
assignee: orchestrator
epic: ORCH
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/orchestrator/inbox/2026-04-14_061_proof-content-type-normalize.md
  - docs/mailbox/e2e/outbox/2026-04-14_015_rerun-plus-37-tools-done.md
---

## Feladat

`proof.route.ts`: Kernel felé induló proxy hívásban `Content-Type` → `application/octet-stream`.

**Root cause:** Kernel `.Accepts<byte[]>("application/octet-stream")` — user `image/jpeg` → 415.
**Fix:** normalizáld a Content-Type-ot, őrizd meg `X-SpaceOS-Original-Content-Type`-ban.

**Blokkolja:** 36-proof teszt 1 valódi 200-as happy path.
