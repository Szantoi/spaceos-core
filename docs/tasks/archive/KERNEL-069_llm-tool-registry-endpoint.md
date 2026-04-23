---
id: KERNEL-069
title: LLM Tool Registry endpoint — GET /api/llm-tools
status: new
priority: medium
assignee: kernel
epic: AI-NATIVE
blocked_by: BATCH-0-CLEANUP + Doorstar pilot feedback
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/e2e/outbox/2026-04-14_015_rerun-plus-37-tools-done.md
---

## Háttér

Az E2E-015 diagnózisa alapján a jelenlegi `/api/tools/*` endpointok **adat-projekciók** (flow-epics, workstations, facilities paginated queries), nem LLM tool descriptors.

Ha az AI-natív tool calling architektúra (5 Golden Rule #1) megköveteli egy valódi tool registry-t, új endpoint szükséges:

```
GET /api/llm-tools   → [{ name, description, parameters: JSONSchema }]
```

Ez az Orchestrator LLM Tool Calling-hoz kell (Phase 3C+ LLM integration).

## Prioritás

Nem blokkoló Doorstar Soft Launch-hoz. 2026 Q3 scope (2. ügyfél + LLM feature mélység).
