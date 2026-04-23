---
id: KERNEL-062
title: E2E maradék Kernel hibák — FlowEpic Close + Node Register + TenantSummary
status: active
priority: high
assignee: kernel
epic: e2e-stabilization
blocked_by: ""
created: 2026-04-12
updated: 2026-04-12
docs:
  - docs/mailbox/kernel/inbox/2026-04-12_062_e2e-remaining-failures.md
---

# KERNEL-062 — E2E maradék Kernel hibák (115/120 után)

## Kontextus

Az E2E-006 rerun 115/120 eredményt hozott. 4 maradék fail Kernel-oldali.

## Feladatok

| # | Teszt | Hiba | Root cause |
|---|---|---|---|
| 1 | `05-flowepic-lifecycle` | PUT /close → 500 | Stage transition null ref (StageChainTemplate) |
| 2 | `15-nodes-sync` | POST /nodes/register → 500 | Korábban masked regresszió |
| 3 | `24-tenant-summary` | `flowEpicCount: 0` | RLS / tenantId mismatch GetSummary-ban |
| 4 | `24-tenant-summary` | `activeWorkstationCount: 0` | Ugyanaz mint #3 |

## DoD

- [ ] PUT /close → 200
- [ ] POST /nodes/register → 201/409
- [ ] `flowEpicCount` és `activeWorkstationCount` helyes értékek
- [ ] 1075 teszt zöld
- [ ] Commit + push

## Mailbox

MSG-KERNEL-062 (kiadva: 2026-04-12)
