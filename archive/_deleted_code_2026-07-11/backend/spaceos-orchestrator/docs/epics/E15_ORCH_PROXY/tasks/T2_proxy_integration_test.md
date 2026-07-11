# T2 — Proxy Integration Test (Manual / Live Kernel Required)

**Epic:** E15 — Kernel Proxy & Auth Middleware
**Status:** `BACKLOG_READY`
**Test type:** Manual integration test — NOT automated in `npm test`

---

## Why This Test Is Manual

`kernel.proxy.ts` uses `http-proxy-middleware` to forward requests to a live C# Kernel at
`KERNEL_BASE_URL`. Automated unit tests cannot exercise this meaningfully because:

1. **No live Kernel in CI** — the Kernel is a separate .NET 8 process (`SpaceOS.Kernel.Api`)
   that is not spun up during `npm test`.
2. **Intercepting the proxy is non-trivial** — `http-proxy-middleware` creates real TCP
   connections. Mocking it would only test the mock, not the proxy configuration.
3. **Path rewrite verification requires a real response** — confirming that
   `/bff/api/tenants` → `/api/tenants` can only be observed at the Kernel's HTTP access log.

---

## Manual Test Procedure

### Prerequisites

- `SpaceOS.Kernel.Api` running on `http://localhost:5000`
- Orchestrator running on `http://localhost:3000` (`npm run dev`)
- A valid JWT signed with the shared `JWT_SIGNING_KEY`

### Steps

```bash
# 1. Obtain or generate a valid JWT
TOKEN=$(node -e "
  const jwt = require('jsonwebtoken');
  console.log(jwt.sign({ sub: 'manual-tester', role: 'admin' }, process.env.JWT_SIGNING_KEY));
")

# 2. Proxy passthrough — expect Kernel response forwarded as-is
curl -s -o /dev/null -w "%{http_code}" \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:3000/bff/api/tenants
# Expected: 200 (or whatever the Kernel returns for this tenant list)

# 3. Path rewrite — verify Kernel access log shows /api/tenants (no /bff prefix)
# Check Kernel stdout or access log for: GET /api/tenants

# 4. Missing JWT → 401 before proxy fires
curl -s -w "%{http_code}" http://localhost:3000/bff/api/tenants
# Expected: 401 {"error":"Missing or malformed Authorization header."}

# 5. Kernel down → 502
# Stop the Kernel, then:
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:3000/bff/api/tenants
# Expected: 502 {"error":"Kernel unavailable","detail":"..."}
```

---

## Acceptance Criteria (Manual)

- [ ] Valid JWT + `GET /bff/api/tenants` → Kernel response forwarded as-is (HTTP 200)
- [ ] Missing JWT → `401` (proxy never fires — confirmed by Kernel log showing no request)
- [ ] Invalid JWT → `401` (same as above)
- [ ] Kernel stopped → `502 { error: "Kernel unavailable" }`
- [ ] Kernel access log confirms path rewrite: `/bff/api/tenants` → `/api/tenants`

---

## Future Automation Note

If a test Kernel stub is introduced in a later epic (e.g., E16), this test can be automated
using a lightweight HTTP server (e.g., `nock` or an in-process Express stub) that records
inbound requests and validates the forwarded path and headers.
