# Orchestrator Terminal — Hidegindítási Kontextus

> Stack: Node.js 22, Express, TypeScript
> Repo: `/opt/spaceos/spaceos.orchestrator`
> Branch: `develop`
> Runtime: PM2 (`spaceos-orchestrator`)

---

## Felelősség

- BFF (Backend-for-Frontend) — minden `/bff/*` route
- JWT validáció (Keycloak JWKS RS256 / dev ES256)
- LLM Tool Calling (Gemini 2.0 Flash)
- Modul proxy-k (Kernel, Joinery, Abstractions, Cutting, Inventory)
- Rate limiting minden külső route-on
- Internal service-to-service guard (`X-SpaceOS-Internal`)
- Auth/Me endpoint (tenant payload visszaadás)

---

## Jelenlegi állapot (2026-04-20)

| Metrika | Érték |
|---------|-------|
| Commit | `7b16acb` (chat 422 fix — ORCH-082) |
| Unit tesztek | **219 pass** (25 test file) |
| VPS státusz | `DEPLOYED` — `pm2 status` → online |
| Port | **3000** (loopback-only) |
| Health | `GET http://127.0.0.1:3000/bff/health` → 200 |

---

## Env config (`/etc/spaceos/orchestrator.env`)

```ini
AUTH_PROVIDER=keycloak
JWKS_URI=https://joinerytech.hu/auth/realms/spaceos/protocol/openid-connect/certs
JWT_ISSUER=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=kernel-api
KERNEL_BASE_URL=http://127.0.0.1:5000
JOINERY_BASE_URL=http://127.0.0.1:5002
ABSTRACTIONS_BASE_URL=http://127.0.0.1:5003
CUTTING_BASE_URL=http://127.0.0.1:5005
```

---

## Kritikus kód helyek

| Komponens | Helyszín | Fontosság |
|-----------|----------|-----------|
| `src/index.ts` | Route regisztrációs sorrend | `requireAuth` MINDIG `chatLimiter`/`proxyLimiter` előtt! |
| `src/middleware/auth.middleware.ts` | JWT validáció | `AUTH_PROVIDER=keycloak` → JWKS RS256; `dev` → ES256 |
| `src/routes/auth.route.ts` | `/bff/auth/me` | tenantId, tenants[], activeTenantId, roles |
| `src/routes/abstractions.route.ts` | Pass-through proxy | `pathRewrite '^/': '/api/'` |
| `src/middleware/internal.middleware.ts` | SEC-01 guard | `X-SpaceOS-Internal: true` kötelező |

---

## Route sorrend kritikalitása

```typescript
// HELYES sorrend (src/index.ts):
app.use('/bff/chat', requireAuth, chatLimiter, chatRouter);   // ← requireAuth ELŐBB
// HIBÁS:
// app.use('/bff/chat', chatLimiter, chatRouter);  ← 429 unauthentikált esetén is!
```

[MSG-ORCH-058-DONE]

---

## PM2 parancsok

```bash
pm2 status
pm2 restart spaceos-orchestrator
pm2 logs spaceos-orchestrator --lines 30
pm2 reload spaceos-orchestrator  # zero-downtime reload
```

---

## Chat 422 fix — Zod schema (ORCH-082, commit 7b16acb)

**Root cause:** `useStreamingChat.ts` üres assistant content-et küldött vissza (`content: ''`), amit a Zod `z.string().min(1)` elutasított → 422 loop.

**Fix:**
```typescript
// chat.route.ts — discriminated union:
messages: z.array(
  z.discriminatedUnion('role', [
    z.object({ role: z.literal('user'),      content: z.string().min(1) }),
    z.object({ role: z.literal('assistant'), content: z.string() }),  // üres engedett
  ])
).min(1)

// interpreter.service.ts — filter:
const messages = request.messages
  .filter((m) => !(m.role === 'assistant' && m.content === ''))
  .map(...)
```

[MSG-ORCH-082-DONE]

---

## SSE vs JSON endpoint

- `/bff/chat` — JSON endpoint (reply, toolsUsed, iterations)
- `/bff/chat/stream` — SSE endpoint (data: chunks `{ type: 'text', text: '...' }`)

Portal-nak a `/bff/chat/stream`-et kell hívnia, nem a `/bff/chat`-et!
SSE chunk format: `{ type: 'text', text: '...' }` (nem `{ type: 'text_delta', content: '...' }`)

---

## Ismert tech debt

1. Seed profil env var-ok (KC_TOKEN_URL, TEST_RUNNER_CLIENT) dokumentálása az orchestrator.env-ben
2. JWKS cache TTL finomhangolás (jelenlegi: 5 perc)
3. LLM Tool Registry — `/api/tools/*` endpoint teljes coverage
4. PM2 process root-ként fut (PID ownership) — `pm2 restart` nem érhető el `gabor` userrel

---

## Indítás előtt

1. `npm run build → 0 TypeScript error`
2. `npm test → 219 pass, 0 fail`
3. `pm2 status → spaceos-orchestrator online, 0 restart`
4. `AUTH_PROVIDER=keycloak` és `JWKS_URI` beállítva az env-ben
