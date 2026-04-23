---
id: ORCH-CLEANUP-01
title: npm audit fix — Orchestrator 3 dependency vulnerability
status: new
priority: low
assignee: ORCH
epic: ORCH-CLEANUP
blocked_by:
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/infra/outbox/2026-04-14_071_orch-deploy-b7b4581-done.md
---

## Háttér

Az INFRA-071 deploy során `npm ci` futtatáskor az alábbi audit eredmény jelent meg:

- 1 critical / 1 high / 1 moderate — preexisting vulnerabilities
- Nem az ORCH-060 / b7b4581 hozta be

## Feladat

```bash
cd /opt/spaceos/spaceos.orchestrator
npm audit
npm audit fix --dry-run
# Ha safe (nem breaking): npm audit fix
# Ha breaking: manual package.json review szükséges
```

Orchestrator terminálon: `npm audit` kimenet vizsgálat + fix PR, 183 teszt zöld marad.

## Prioritás

Nem blokkoló. Batch 3 után, de Doorstar Soft Launch (2026 Q2) előtt elvégzendő.
