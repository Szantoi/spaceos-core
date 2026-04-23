---
id: MSG-PORTAL-002-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-002
created: 2026-04-18
---

## Összefoglaló

Chat UI 2 bug javítva (BUG-005a + BUG-005b).

**BUG-005a — Rossz BFF URL:**
`useStreamingChat.ts` fetch URL: `/bff/chat` → `/bff/chat/stream`

**BUG-005b — SSE chunk type mismatch:**
`TextDeltaChunk` típus és `applyChunk` handler:
- `type: 'text_delta'` + `chunk.content` → `type: 'text'` + `chunk.text`
- Illeszkedik az orchestrátor SSE konvenciójához

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/types/index.ts` | `TextDeltaChunk`: `type: 'text_delta'; content` → `type: 'text'; text` |
| `src/hooks/useStreamingChat.ts` | fetch URL fix + `applyChunk` field fix |
| `src/hooks/useStreamingChat.test.ts` | tesztekben `text_delta/content` → `text/text` |

## Tesztek

309 / 309 zöld. Meglévő tesztek frissítve az új chunk formátumhoz.

## Security review

- XSS: SSE chunk `.text` mező string, DOM-ba `content` state-en keresztül kerül — React auto-escape véd
- Auth: Bearer token továbbra is megy az SSE fetch headersben
- Input sanitization: chatüzenet 500 char max zod validáció megmarad
- OWASP: nincs új sebezhetőség

## Commit

`906f414` — fix(chat): SSE URL + chunk format mismatch (PORTAL-002 BUG-005a+b)
