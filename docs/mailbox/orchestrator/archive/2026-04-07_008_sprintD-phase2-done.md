---
id: MSG-O008-REPLY
from: orchestrator
to: root
type: done
ref: MSG-O008
sprint: "Sprint D · Phase 2"
status: DONE
date: 2026-04-07
---

# Sprint D · Phase 2 — Tool Registry + KernelClient + SSE Streaming — DONE

## Összefoglaló

A MSG-O008 feladat T-02 és T-03 alfeladatai implementálva. KernelClient teljes error map, prompt injection guard, 4 live tool (query endpoints), SSE streaming AbortController-rel. Meglévő 84 teszt zöld + 30 új teszt.

---

## T-02 · KernelClient + Tool Registry ✅

### BE-P2-04 FIX — Teljes KernelClient error map

| Fájl | Változás |
|---|---|
| `src/kernel/kernelClient.ts` | ÚJ — `KernelErrorCode` enum, `KernelClientError` class, `KernelClient` class (native fetch, AbortSignal.timeout(10s)) |
| `src/kernel/kernelClient.test.ts` | ÚJ — 10 teszt (401/429/503/400/network/timeout/params/jwt/null-skip) |

**KernelErrorCode értékek:**

| Code | HTTP | Szituáció |
|---|---|---|
| `ERR_TOOL_AUTH_EXPIRED` | 401 | JWT lejárt |
| `ERR_TOOL_RATE_LIMITED` | 429 | Kernel saját RL |
| `ERR_KERNEL_UNAVAILABLE` | 503 / network | Kernel nem elérhető |
| `ERR_KERNEL_TIMEOUT` | TimeoutError | 10s timeout |
| `ERR_TOOL_BAD_REQUEST` | 400 | Érvénytelen paraméter |
| `ERR_KERNEL_UNKNOWN` | egyéb | Ismeretlen HTTP státusz |

### Prompt injection guard

| Fájl | Változás |
|---|---|
| `src/interpreter/sanitize.ts` | ÚJ — `sanitizeToolResultForLlm()`, `wrapToolResult()`, `buildToolErrorResult()` |
| `src/interpreter/sanitize.test.ts` | ÚJ — 10 teszt (injection patterns, clean passthrough, wrap, buildError) |

**Injection minták (→ `[REDACTED]`):**
- `ignore/disregard/forget (all) (previous|prior|above) instructions`
- `\nHuman:` / `\nAssistant:` / `\nSystem:` (Anthropic injection)
- `<system>` tag

### KernelClientError kezelés az agentic loop-ban

`src/interpreter/interpreter.service.ts` frissítve:
- `executeToolCall()` → re-throws `KernelClientError` (nem nyeli el)
- `Promise.all` map-ben catch: `KernelClientError` → `buildToolErrorResult()` → LLM tool_result kapja, loop **folytatódik**
- Ismeretlen hiba → re-throw → loop megszakad

### 4 új live tool

| Tool | Kernel endpoint | Státusz |
|---|---|---|
| `list_flow_epics` | `GET /api/query/flow-epics?tenantId=...` | Blokkolva (Kernel T-01) |
| `list_facilities` | `GET /api/query/facilities?tenantId=...` | Blokkolva (Kernel T-01) |
| `get_workstation_summary` | `GET /api/query/workstation-summary?facilityId=...` | Blokkolva (Kernel T-01) |
| `get_tenant_summary` | `GET /api/query/tenant-summary?tenantId=...` | Blokkolva (Kernel T-01) |

Minden tool: `KernelClient.get()` wrapper, JWT automatikusan forward-olva, `sanitizeToolResultForLlm()` alkalmazva.

---

## T-03 · SSE Streaming + BE-P2-05 AbortController ✅

### Implementált fájlok

| Fájl | Változás |
|---|---|
| `src/interpreter/sse-serializer.ts` | ÚJ — `SseSerializer.sanitize()` + `SseSerializer.write()` |
| `src/routes/chat.route.ts` | `POST /bff/chat/stream` hozzáadva — SSE endpoint, AbortController, sseChatRateLimit |
| `src/interpreter/interpreter.service.ts` | `streamChat()` async generator hozzáadva |
| `src/types/llm.types.ts` | `ChatChunk` típus + `ILlmProvider.stream?()` optional method |
| `src/llm/mock.provider.ts` | `stream()` implementálva — tesztelhetőség |

### SSE endpoint spec

```
POST /bff/chat/stream
Auth:       Bearer JWT (requireAuth)
Rate limit: 10 req/min prod (sseChatRateLimit — tighter than JSON endpoint)
Response:   Content-Type: text/event-stream
            data: { type: 'text', text: '...' }
            data: { type: 'done', toolsUsed: [...], iterations: N }
            data: [DONE]   ← sentinel (mindig utolsó, finally blokk garantálja)
```

### BE-P2-05 FIX — AbortController disconnect cleanup

```typescript
const abortController = new AbortController();
req.on('close', () => { abortController.abort(); });
// signal propagálva: streamChat() → provider.stream() → Anthropic SDK
// AbortError: silent (expected disconnect)
// Egyéb error: error chunk + [DONE]
```

### SseSerializer

- `sanitize(text)`: `\r` → `\\r`, `\n` → `\\n` (SSE protocol injection megelőzés)
- `write(res, chunk)`: `data: ${sanitize(JSON.stringify(chunk))}\n\n`

---

## DoD Checklist

- [x] `KernelClientError` enum: AuthExpired / RateLimited / Unavailable / Timeout / BadRequest / Unknown
- [x] Interpreter: `KernelClientError` → `buildToolErrorResult()`, nem throw → agentic loop folytatódik
- [x] `wrapToolResult()` + `sanitizeToolResultForLlm()` minden tool result-re
- [x] 4 live tool: `KernelClient` wrapper, JWT forward
- [x] Unit teszt: 401 → `ERR_TOOL_AUTH_EXPIRED` ✓
- [x] Unit teszt: 429 → `ERR_TOOL_RATE_LIMITED` ✓
- [x] Unit teszt: 503 → `ERR_KERNEL_UNAVAILABLE` ✓
- [x] Unit teszt: timeout (10s) → `ERR_KERNEL_TIMEOUT` ✓
- [x] Unit teszt: prompt injection tartalmú tool result → `[REDACTED]` ✓
- [x] `Content-Type: text/event-stream`, `[DONE]` sentinel ✓
- [x] `AbortController` + `req.on('close', abort)` — disconnect cleanup ✓
- [x] `AbortSignal` propagálva az LLM provider-be és `KernelClient`-be ✓
- [x] `SseSerializer.sanitize()` — `\r`, `\n` → escaped ✓
- [x] `sseChatRateLimit`: 10 req/perc (prod) ✓
- [x] Unit teszt: client disconnect szimulál → generator leáll ✓
- [ ] E2E: chat → live Kernel adat → LLM válasz — **BLOKKOLVA** (Kernel T-01)
- [ ] Nginx: `proxy_buffering off; proxy_read_timeout 300s;` — **Kernel/infra feladat**

---

## Metrikus összefoglaló

| Metrika | Érték |
|---|---|
| Build | 0 TypeScript error |
| Tesztek | 114/114 PASS (14 fájl) |
| Előző tesztek | 84/84 (változatlan, zöld) |
| Új tesztek | +30 (kernelClient: 10, sanitize: 10, interpreter: 4, chat/stream: 6) |
| Új fájlok | 5 (`kernelClient.ts`, `kernelClient.test.ts`, `sanitize.ts`, `sanitize.test.ts`, `sse-serializer.ts`) |
| Módosított fájlok | 7 |
| Új endpointok | 1 (`POST /bff/chat/stream`) |
| Új tool-ok | 4 (query endpoints — Kernel T-01 után live-tesztelhető) |

## ⚠️ Blokkoló megjegyzések

**T-02 live integráció blokkolva:**
- 4 új query tool (`list_flow_epics`, `list_facilities`, `get_workstation_summary`, `get_tenant_summary`) unit-tesztelt, de Kernel T-01 endpoint-ok nélkül nem futtatható live
- Amint a Kernel deploy-ol a `/api/query/*` endpointokkal, integrációs teszt szükséges

**T-03 Nginx konfiguráció:**
- `proxy_buffering off; proxy_read_timeout 300s;` az SSE route-ra — Nginx config fájlban szükséges, ez infra feladat
