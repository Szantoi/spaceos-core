---
id: INFRA-ADR039-DEPLOY
title: ADR-039 — Kernel Migration 0031 + Joinery J-003 deploy + InternalSecret
status: active
priority: high
assignee: VPS Operator
epic: ADR-039
created: 2026-05-28
updated: 2026-05-28
docs:
  - docs/mailbox/root/2026-05-28_002_adr039-combined-infra-deploy.md
---

# ADR-039 VPS deploy

## Feladatok

- [ ] `SpaceOS:InternalSecret` generálása + beállítás kernel.env-ben
- [ ] `SpaceOS:InternalSecret` beállítás joinery.env-ben (azonos érték!)
- [ ] Kernel publish + Migration 0031 + restart
- [ ] Joinery publish + restart (J-003 automatikusan fut)
- [ ] nginx ellenőrzés: `/api/internal/*` nem exponált kifelé
- [ ] Smoke test: loopback 200, külső 404

## Részletes lépések

Lásd: `docs/mailbox/root/2026-05-28_002_adr039-combined-infra-deploy.md`
