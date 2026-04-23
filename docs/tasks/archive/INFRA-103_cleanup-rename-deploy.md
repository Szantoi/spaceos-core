---
id: INFRA-103
title: Cleanup sprint — systemd fix + NuGet feed + Kernel/Orch rename
status: active
priority: high
assignee: infra
epic: cleanup-sprint
blocked_by: —
created: 2026-04-16
updated: 2026-04-16
docs:
  - docs/mailbox/infra/inbox/2026-04-16_103_cleanup-rename-deploy.md
---

1. Systemd service fájlok: inventory/procurement → saját repo publish/ útvonal
2. NuGet feed: Inventory.Contracts → inventory/nupkg/, Procurement.Contracts → procurement/nupkg/
3. SpaceOS.Kerner → spaceos-kernel (typo + konszolidáció)
4. spaceos.orchestrator → spaceos-orchestrator (névkonvenció)
5. cutting-publish-staging törlése
