---
id: CUTTING-009
title: IInventoryProvider stub — cutting DI regisztráció
status: archive
priority: high
assignee: cutting
epic: sprint-8-q3
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/cutting/inbox/2026-04-16_009_inventory-provider-stub.md
  - docs/mailbox/cutting/outbox/2026-04-16_009_inventory-provider-stub-done.md
---

InventoryProviderStub: 6 metódus, üres/default return. AddScoped<IInventoryProvider, InventoryProviderStub>.
64/64 ✅. commit 873ba39. Graceful degradation: panelAssignments=null, 200 grouping-only. INFRA-115 deploy kiadva.
