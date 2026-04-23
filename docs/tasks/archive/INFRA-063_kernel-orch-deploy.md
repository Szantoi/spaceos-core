---
id: INFRA-063
title: Kernel + Orchestrator VPS deploy (KERNEL-062 + ORCH-059 fix)
status: active
priority: high
assignee: infra
epic: e2e-stabilization
blocked_by: ""
created: 2026-04-13
updated: 2026-04-13
docs:
  - docs/mailbox/infra/inbox/2026-04-13_063_kernel-orch-deploy.md
---

# INFRA-063 — Kombinált VPS deploy

## Feladatok

1. **Kernel** — `git pull develop` (8dd0bd7) + `dotnet publish` + binary csere + `systemctl restart`
2. **Orchestrator** — `ABSTRACTIONS_BASE_URL=http://127.0.0.1:5003` env var + `git pull develop` (4a96e3c) + `pm2 restart`

## DoD

- [ ] Kernel health 200
- [ ] GET /bff/abstractions/health → 401
- [ ] GET /bff/chat no-auth → 401 (nem 429)

## Mailbox

MSG-INFRA-063 (kiadva: 2026-04-13)
