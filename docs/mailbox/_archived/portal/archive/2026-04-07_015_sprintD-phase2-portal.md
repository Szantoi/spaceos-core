---
id: MSG-P015
from: architect
to: portal
type: task
status: READ
priority: P1
sprint: "Sprint D · Phase 2"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md"
---

# Sprint D · Phase 2 — Portal: Chat UX (SSE Streaming + ToolResultCard)

## Összefoglaló

**1 feladat, 1 fejlesztői nap.** Track A vége — az Orchestrator T-03 SSE streaming elkészülte után startol.

**Implementációs sorrend (v4):**
```
Nap 9: T-04 — Portal Chat UX (ToolResultCard + streaming hook)
```

> ⚠️ MINDIG hivatkozd az eredeti dokumentumot: `/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md` — a teljes specifikáció ott van.

**Blokkolás:** Az Orchestrator T-03 SSE streaming elkészültéig az SSE reader integrációt nem lehet élesben tesztelni. Addig unit tesztekkel dolgozz (mock SSE stream).

---

## T-04 · Portal Chat UX — SSE streaming + ToolResultCard (Nap 9)

### SSE Streaming Reader

Az Orchestrator `POST /chat` endpoint SSE streamet küld. A Portal-nak:

1. **SSE reader hook** (pl. `useStreamingChat`):
   - `fetch` + `ReadableStream` / `EventSource` az Orchestrator SSE endpointjára
   - Chunk-onként frissíti a chat üzenet tartalmát (streaming UX)
   - `[DONE]` sentinel esetén lezárja a streamet
   - Hiba esetén (`{ error: 'stream_error' }`) felhasználóbarát hibaüzenet

2. **Chunk típusok kezelése:**
   - Text delta chunk → hozzáfűz a jelenlegi üzenethez
   - Tool result chunk → `isToolResult()` type guard → `ToolResultCard` komponens renderelése
   - Error chunk → inline hiba a chat-ben

### `ToolResultCard` komponens

Structured tool result megjelenítése kártyaként a chat folyamban:

```typescript
// isToolResult() type guard
function isToolResult(chunk: ChatChunk): chunk is ToolResultChunk {
  return chunk.type === 'tool_result';
}
```

`ToolResultCard.tsx` — minimális követelmény:
- Tool neve fejlécként
- Strukturált adat (JSON) olvasható formában (táblázat vagy lista)
- Betöltési állapot (amíg a tool hívás fut)
- Hibás tool result esetén hibaüzenet kártyán

### Koordináció

- **Orchestrator terminállal:** Az SSE chunk formátumát az Orchestrator `SseSerializer` határozza meg — egyeztess a pontos chunk struktúráról, ha szükséges.
- **E2E teszt:** chat → tool call → live Kernel adat → `ToolResultCard` megjelenik a Portalon

---

## DoD

- [ ] SSE streaming reader + chunk-onkénti render (text delta megjelenik gépelés közben)
- [ ] `ToolResultCard.tsx` komponens + `isToolResult()` type guard
- [ ] `[DONE]` sentinel → stream lezárása, befejezési állapot
- [ ] Hiba chunk → felhasználóbarát hibaüzenet (nem raw JSON)
- [ ] Portal unit teszt: streaming render — szöveg chunk-ok helyes sorrend
- [ ] Portal unit teszt: `ToolResultCard` megjelenik tool result chunk-ra
- [ ] Portal unit teszt: `isToolResult()` type guard — helyes és helytelen chunk-ok
- [ ] E2E (koordináció Orchestratorral): chat → tool call → live Kernel adat → `ToolResultCard`

---

## Összesített DoD (Portal)

- [ ] Streaming UX: felhasználó látja a szöveg generálódását real-time
- [ ] Tool result kártyák megjelennek a chat folyamban
- [ ] Meglévő tesztek zöld + Phase 2 új portal tesztek ≥ 5 db
- [ ] 0 TypeScript type error / lint warning
