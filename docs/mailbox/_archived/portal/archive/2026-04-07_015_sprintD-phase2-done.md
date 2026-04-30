---
id: MSG-P015-DONE
from: portal
to: architect
type: response
ref: MSG-P015
status: DONE
date: 2026-04-07
---

# Sprint D · Phase 2 — Portal T-04 DONE

## Elvégzett munka

### T-04 · Portal Chat UX — SSE streaming + ToolResultCard

**Új fájlok:**
- `src/hooks/useStreamingChat.ts` — SSE reader hook (`fetch` + `ReadableStream`)
- `src/features/chat/ToolResultCard.tsx` — tool result kártya komponens
- `src/hooks/useStreamingChat.test.ts` — 8 unit teszt (mock SSE stream)
- `src/features/chat/ToolResultCard.test.tsx` — 8 komponens teszt

**Módosított fájlok:**
- `src/types/index.ts` — `ChatChunk`, `TextDeltaChunk`, `ToolResultChunk`, `ErrorChunk`, `DoneChunk`, `ChatMessage` (+ `toolResults`, `isStreaming`), `isToolResult()` type guard
- `src/features/chat/ChatBubble.tsx` — `ToolResultCard` + streaming cursor render
- `src/features/chat/ChatPage.tsx` — `useStreamingChat` hookra cserélve (`useSendChatMessage` eltávolítva)
- `src/features/chat/ChatPage.test.tsx` — 17 teszt, frissítve az új hook API-ra

## DoD állapot

- [x] SSE streaming reader + chunk-onkénti render (text delta megjelenik)
- [x] `ToolResultCard.tsx` komponens + `isToolResult()` type guard
- [x] `[DONE]` sentinel → stream lezárása, `isStreaming=false`
- [x] Error chunk → felhasználóbarát hibaüzenet (nem raw JSON)
- [x] Portal unit teszt: streaming render — szöveg chunk-ok helyes sorrendben
- [x] Portal unit teszt: `ToolResultCard` megjelenik tool result chunk-ra
- [x] Portal unit teszt: `isToolResult()` type guard — helyes és helytelen chunk-ok
- [ ] E2E (koordináció Orchestratorral): T-03 SSE backend elkészülte után

## Tesztek

**256/256 zöld** (volt: 239) — +17 új teszt

## Blokkolás

E2E teszt az Orchestrator T-03 SSE streaming elkészultéig nem lehetséges.
Unit tesztek mock SSE stream alapján teljes körűek.

## Megjegyzés

A `ChatPage` a `useSendChatMessage` (REST mutation) helyett most `useStreamingChat`-et használ.
Az SSE chunk formátum a spec szerint: `data: <JSON>\n\n` sorok, `data: [DONE]\n\n` sentinel.
Ha az Orchestrator SseSerializer eltér, csak a `useStreamingChat` hook parseolási logikáját kell frissíteni.
