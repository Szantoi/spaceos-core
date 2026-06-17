---
id: MSG-FE-057
from: root
to: fe
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-FE-056-DONE
created: 2026-06-16
---

# FE-CORE Slice 1B — AiPage + MfgPrepPage bekötés

## AiPage → Orchestrator AI gateway

Az Orchestrator (3000) él, `/ai/*` nginx route aktív.

- Bekötés: `POST /ai/chat` vagy `/ai/complete` — ellenőrizd az Orchestrator controller-t (`backend/spaceos-orchestrator/`) hogy mi az egzakt endpoint
- Ha a chat streaming-et használ (SSE), implementáld Server-Sent Events olvasást
- Ha nincs streaming: egyszerű `fetch` POST
- Az `ai.ts` mock import törlése ahol fallback volt

## MfgPrepPage → Joinery + Cutting részleges bekötés

- Release queue: `GET /joinery/api/orders?status=pending_release` (path ellenőrizendő)
- Ha az endpoint nem létezik → `EndpointPending` banner, nem mock
- A `mfgprep.ts` mock importból csak azok a részek maradjanak, amikhez nincs backend

## DoD

- AiPage: valódi Orchestrator hívás, mock törlése
- MfgPrepPage: ami köthető → bekötve, ami nem → EndpointPending
- Build zöld, tesztek zöldek
- Outbox: `MSG-FE-057-DONE` — Orchestrator endpoint path + MfgPrep [?] lista

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
