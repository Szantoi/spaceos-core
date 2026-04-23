---
id: MSG-O008
from: architect
to: orchestrator
type: task
status: DONE
priority: P0
sprint: "Sprint D · Phase 2"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md"
---

# Sprint D · Phase 2 — Orchestrator: Tool Registry + KernelClient + SSE Streaming

## Összefoglaló

**2 feladat, ~5 fejlesztői nap.** Track A — párhuzamosan futhat a Kernel T-07 és T-05 munkáival.

**Implementációs sorrend (v4):**
```
Nap 6:   T-02 — KernelClient teljes error map + prompt inject guard
Nap 7:   T-02 — Tool Registry live wiring (4 tool)
Nap 8:   T-03 — SSE streaming + AbortController + SseSerializer + Nginx config
```

> ⚠️ MINDIG hivatkozd az eredeti dokumentumot: `/opt/spaceos/docs/SpaceOS_Sprint_D_Phase2_v4.md` — a teljes kódspecifikáció ott van.

**Blokkolás:** T-02 Tool Registry live tesztelése blokkolva a Kernel T-01 query endpoint-ok elkészültéig. Addig unit tesztekkel dolgozz.

---

## T-02 · Tool Registry + KernelClient (Nap 6–7)

### BE-P2-04 FIX — Teljes KernelClient error map (HIGH)

**Probléma:** Csak a 401 volt kezelve. 429/503/timeout esetén raw 500 kerül a chat response-ba — stack trace a LLM-nek.

**Javítás:** `src/kernel/kernelClient.ts`

**`KernelErrorCode` enum** (const enum):
```typescript
AuthExpired    = 'ERR_TOOL_AUTH_EXPIRED'
RateLimited    = 'ERR_TOOL_RATE_LIMITED'
Unavailable    = 'ERR_KERNEL_UNAVAILABLE'
Timeout        = 'ERR_KERNEL_TIMEOUT'
BadRequest     = 'ERR_TOOL_BAD_REQUEST'
Unknown        = 'ERR_KERNEL_UNKNOWN'
```

**`KernelClientError` class:** `code: KernelErrorCode`, `httpStatus: number`, `message: string`

**`get<T>()` metódus:**
- `AbortSignal.timeout(10_000)` — 10s kernel timeout
- Network error → `ERR_KERNEL_UNAVAILABLE`
- `DOMException('TimeoutError')` → `ERR_KERNEL_TIMEOUT`
- HTTP státusz switch: 400→BadRequest, 401→AuthExpired, 429→RateLimited, 503→Unavailable, default→Unknown
- **Nincs raw HTTP státuszkód a chat response-ban**

**Interpreter service** (`src/interpreter/interpreterService.ts`):
```typescript
} catch (err) {
  if (err instanceof KernelClientError) {
    toolResultContent = buildToolErrorResult(toolUseBlock.id, err.code, err.message);
    continue;  // agentic loop folytatódik — az LLM dönt
  }
  throw err;  // ismeretlen hiba → loop megszakad
}
```

### Prompt injection guard + JWT 401 wrapper (v3-ból változatlan)

`wrapToolResult()` + `sanitizeToolResultForLlm()` — minden tool result-re alkalmazni.

### Tool Registry live wiring

4 új live tool — `KernelClient` wrapper, JWT forward:
- `list_flow_epics`, `get_workstation_summary`, `list_facilities`, `get_tenant_summary`
- Minden tool result: `wrapToolResult()` → `sanitizeToolResultForLlm()`

**DoD:**
- [ ] `KernelClientError` enum: AuthExpired / RateLimited / Unavailable / Timeout / BadRequest / Unknown
- [ ] Interpreter: `KernelClientError` → `buildToolErrorResult()`, nem throw → agentic loop folytatódik
- [ ] `wrapToolResult()` + `sanitizeToolResultForLlm()` minden tool result-re
- [ ] 4 live tool: `KernelClient` wrapper, JWT forward
- [ ] Unit teszt: 401 → `ERR_TOOL_AUTH_EXPIRED`
- [ ] Unit teszt: 429 → `ERR_TOOL_RATE_LIMITED`
- [ ] Unit teszt: 503 → `ERR_KERNEL_UNAVAILABLE`
- [ ] Unit teszt: timeout (10s) → `ERR_KERNEL_TIMEOUT`
- [ ] Unit teszt: prompt injection tartalmú tool result → `[REDACTED]`
- [ ] E2E: chat → tool call → live Kernel adat → LLM válasz

---

## T-03 · SSE Streaming + BE-P2-05 AbortController (Nap 8)

### BE-P2-05 FIX — Client disconnect abort (HIGH)

**Probléma:** Ha a felhasználó bezárja a böngésző tabját, a `for await` loop fut tovább — LLM API billing + connection resource leak.

**Javítás:** `src/routes/chat.route.ts`

```typescript
const abortController = new AbortController();
req.on('close', () => { abortController.abort(); });

for await (const chunk of interpreterService.streamChat(req.body, abortController.signal)) {
  if (abortController.signal.aborted) break;
  SseSerializer.write(res, chunk);
}
// finally: res.write('data: [DONE]\n\n'); res.end();
```

**Interpreter service** (`src/interpreter/interpreterService.ts`):
```typescript
async *streamChat(body: ChatRequest, signal: AbortSignal): AsyncGenerator<ChatChunk> {
  const stream = await this.llmProvider.stream(messages, { signal });
  for await (const event of stream) {
    signal.throwIfAborted();  // explicit check minden iteráción
    yield mapEvent(event);
  }
}
```

- `AbortSignal` propagálva az Anthropic Node.js SDK-ba (v0.20+ natívan kezeli)
- `KernelClient.fetch()` hívásai szintén az `AbortSignal`-t kapják
- `AbortError` → silent (expected disconnect), minden más error → `logger.error` + `{ error: 'stream_error' }`

### SSE headers + RL + sanitization

- SSE headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`, `flushHeaders()`
- `sseChatRateLimit`: 10 req/perc/user → 429 + retryAfter
- `SseSerializer.sanitize()`: `\r`, `\n` → escaped (prompt injection megelőzés)
- `[DONE]` sentinel a stream végén

### Nginx konfiguráció

Az SSE route-ra:
```nginx
proxy_buffering off;
proxy_read_timeout 300s;
```

**DoD:**
- [ ] `Content-Type: text/event-stream`, `[DONE]` sentinel
- [ ] `AbortController` + `req.on('close', abort)` — disconnect cleanup
- [ ] `AbortSignal` propagálva az Anthropic SDK-ba és `KernelClient.fetch()`-be
- [ ] `SseSerializer.sanitize()` — `\r`, `\n` → escaped
- [ ] `sseChatRateLimit`: 10 req/perc/user → 429 + retryAfter
- [ ] Nginx: `proxy_buffering off; proxy_read_timeout 300s;` SSE route-ra
- [ ] Unit teszt: client disconnect szimulál → generator leáll, LLM hívás abortál

---

## Összesített DoD (Orchestrator)

- [ ] `KernelClientError` minden HTTP státuszra → nincs raw 500 a chat response-ban
- [ ] Agentic loop `KernelClientError` esetén folytatódik (nem megszakad)
- [ ] AbortController disconnect cleanup — nincs resource leak
- [ ] SSE sanitization minden chunk-on
- [ ] Meglévő tesztek zöld + Phase 2 új orchestrator tesztek ≥ 15 db
- [ ] 0 build warning / lint error
- [ ] E2E: chat → live Kernel adat → ToolResultCard a Portalon (koordináció a Portal terminállal)
