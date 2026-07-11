# ORCH Terminál

> Node.js BFF (Backend For Frontend) - LLM Tool Calling, API Gateway

## Gyors Info

| | |
|---|---|
| **Terminál** | orch (orchestrator) |
| **Port** | 3000 |
| **Típus** | on-demand |
| **Könyvtár** | `/opt/spaceos/backend/spaceos-orchestrator/` |
| **Mailbox** | `/opt/spaceos/docs/mailbox/orchestrator/` |
| **Memory** | `/opt/spaceos/docs/memory/orch.md` |

## Session Indítás

```bash
# 1. Memory olvasás
cat /opt/spaceos/docs/memory/orch.md

# 2. Inbox ellenőrzés
grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/orchestrator/inbox/

# 3. Build és teszt
cd /opt/spaceos/backend/spaceos-orchestrator
npm run build
npm test
```

## Build & Test Parancsok

```bash
# Install
npm install

# Build (TypeScript)
npm run build

# Tesztek
npm test

# Dev futtatás
npm run dev

# Health check
curl http://localhost:3000/bff/health
```

## Architektúra

```
src/
├── routes/              ← Express routes
├── services/            ← Business logic
├── llm/                 ← LLM Tool Calling
│   ├── tools/           ← Tool definitions
│   └── prompts/         ← System prompts
├── middleware/          ← Auth, logging
└── types/               ← TypeScript types
```

## Fontos Endpointok

```
GET  /bff/health                    Health check
GET  /bff/api/v1/tenants            Tenant proxy → Kernel
POST /bff/api/v1/chat               LLM chat endpoint
POST /bff/api/v1/tools/invoke       Tool invocation
```

## LLM Tool Calling

Az Orch kezeli az LLM-kel való kommunikációt:
- Tool definitions: `src/llm/tools/`
- System prompts: `src/llm/prompts/`
- Claude API integráció

## Környezeti Változók

```bash
# .env
PORT=3000
KERNEL_URL=http://localhost:5000
ANTHROPIC_API_KEY=sk-...
```

## DONE Outbox Sablon

```yaml
---
id: MSG-ORCH-NNN-DONE
from: orch
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-ORCH-NNN
created: YYYY-MM-DD
---

## Összefoglaló
Mit implementáltam, commit hash.

## Tesztek
npm test eredmény.

## Security review
JWT validation, input sanitization ellenőrizve.
```

## Kapcsolódó Dokumentáció

- CLAUDE.md: `/opt/spaceos/backend/spaceos-orchestrator/CLAUDE.md`
- Knowledge: `/opt/spaceos/docs/knowledge/context/ORCH_CONTEXT.md`
