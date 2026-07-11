# CLAUDE.md — src/proxy/

**Module:** Transparent Kernel proxy
**Rule:** This module knows the Kernel's internal URL. It must never be called for LLM chat — only for direct API passthrough.

---

## What lives here

| File | Purpose |
|---|---|
| `kernel.proxy.ts` | `http-proxy-middleware`: `/bff/api/*` → `KERNEL_BASE_URL/api/*` (120s timeout for uploads) |
| `federation.proxy.ts` | Federation endpoints: `/api/nodes/*`, `/api/sync/*` — injects `SpaceOS-SIP-Version: 1.0` header |

## Rules

- Path rewrite: `/bff/api` → `/api` — the frontend never knows the internal port
- JWT is forwarded as-is — the Kernel validates it independently
- On proxy error: return `502` with JSON — never let the raw error bubble to the client
- SSE-compatible headers (for future streaming): `proxy_buffering off` is set in nginx — the proxy itself is stateless
- This middleware is mounted AFTER `requireAuth` in `index.ts` — unauthenticated requests never reach the Kernel

## nginx note

```nginx
location /bff/ {
  proxy_pass http://127.0.0.1:3000/bff/;
  proxy_buffering off;       # required for SSE
  proxy_cache off;
  chunked_transfer_encoding on;
}
```

The Kernel (port 5001) is **firewall-blocked** externally.
Only this proxy (via nginx → port 3000) can reach it.
