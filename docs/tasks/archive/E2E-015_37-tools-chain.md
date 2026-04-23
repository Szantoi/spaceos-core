---
id: E2E-015
title: 37-tools.chain.test.ts — LLM tool calling registry E2E
status: new
priority: high
assignee: e2e
epic: batch3-tools-chain
blocked_by: E2E-014 (36-proof DONE)
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/e2e/outbox/2026-04-13_011_coverage-gap-report.md
---

## Feladat

Írd meg: `37-tools.chain.test.ts` — az LLM tool calling registry E2E tesztje.

## Üzleti indok

Az AI-natív architektúra (5. Golden Rule: Walking Skeleton First) alapja.
A 14-chat.chain.test.ts csak felszínes coverage — a ToolEndpoints (`/api/tools/*`)
teljesen leteszteletlen. Nélküle az LLM tool dispatch nincs E2E-szinten verifikálva.

## Endpoint-ok

**Kernel:**
```
GET  /api/tools              → toolok listája
GET  /api/tools/:name        → tool metaadat
POST /api/tools/:name/invoke → LLM tool call dispatch
```

BFF proxyn át: `/bff/api/tools/*`

Forrás:
- `/opt/spaceos/SpaceOS.Kerner/SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs`
- Kernel tesztek: `/opt/spaceos/SpaceOS.Kerner/SpaceOS.Kernel.Api.Tests/Endpoints/ToolEndpointTests.cs`

## Tervezett chain

```
1. GET /bff/api/tools → toolok listája (200 vagy 404 ha nincs tool regisztrálva)
2. Ha van tool: GET /bff/api/tools/:name → metaadat
3. POST /bff/api/tools/:name/invoke → tool dispatch (mock payload)
4. Auth nélkül: 401
```

## Blokkoló feltétel

E2E-014 (36-proof.chain.test.ts) elfogadva a root által, és az összes meglévő teszt zöld.
