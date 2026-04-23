---
id: INFRA-062
title: Kernel VPS deploy post-KERNEL-060 — binary + env fix
status: archive
priority: high
assignee: infra
epic: e2e-stabilization
created: 2026-04-12
updated: 2026-04-12
---

Kernel binary c62f1d7 deployed. Port 5000 canonical (appsettings.Production.json). RateLimit__WritePerMinute=1000. kernel.env ASPNETCORE_URLS=5001 no-op (tech debt). MSG-INFRA-062-DONE elfogadva 2026-04-12.
