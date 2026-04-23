---
id: ORCH-059
title: Abstractions BFF proxy (/bff/abstractions/*) + VPS deploy
status: active
priority: high
assignee: orchestrator
epic: e2e-expansion
blocked_by: ""
created: 2026-04-12
updated: 2026-04-12
docs:
  - docs/mailbox/orchestrator/inbox/2026-04-12_059_abstractions-proxy-and-deploy.md
---

# ORCH-059 — Abstractions BFF proxy + VPS deploy

## Feladatok

1. `src/routes/abstractions.route.ts` létrehozása (minta: joinery.route.ts)
2. `ABSTRACTIONS_BASE_URL=http://127.0.0.1:5003` env var
3. `app.use('/bff/abstractions', requireAuth, abstractionsRouter)` regisztráció
4. VPS deploy: `git pull develop` + `pm2 restart spaceos-orchestrator`

## DoD

- [ ] GET /bff/abstractions/health → 200
- [ ] no-auth → 401
- [ ] GET /bff/chat no-auth → 401 (ORCH-058 VPS-en él)
- [ ] 178 teszt zöld

## Mailbox

MSG-ORCH-059 (kiadva: 2026-04-12)
