---
id: SPRINT4
title: Sprint 4 — Soft Launch Readiness
status: active
priority: high
assignee: kernel, procurement, portal, orch, infra, e2e
epic: soft-launch-readiness
blocked_by: ~
created: 2026-04-17
updated: 2026-04-17
docs:
  - docs/SpaceOS_Sprint4_Soft_Launch_Readiness.md
---

## Feladatok

1. **KERNEL-086** — Audit chain hash mismatch fix (P0) ✅ commit 82a849a · 1122/1122 · AuditDbContext advisory lock fix · 50 concurrent write teszt ✅
2. **PROCUREMENT-007** — /healthz endpoint (P1) ✅ commit 0382189 · 51/51
3. **PORTAL-011** — api-client tsconfig fix (P2) ✅ commit 4d88176 · turbo build 7/7 · 306/306
4. **ORCH-081** — Rate limit vizsgálat (P1) ✅ szándékos design (proxyLimiter minden metódus) · 218/218 · E2E 27-rate-limit 5/5 ✅
5. **INFRA-148/149** — post-fix deploy-ok 🔄 KERNEL-086 + PROCUREMENT-007 mind DONE → INFRA-149 kiadva
6. **E2E-049** — Full rerun Soft Launch gate (P0) ✅ **233/233** · 0 fail · 27-rate-limit 5/5 ✅ · Doorstar Q2 Soft Launch GO 🎉
