# CLAUDE.md — src/middleware/

**Module:** Express middleware — auth and error handling

---

## What lives here

| File | Purpose |
|---|---|
| `auth.middleware.ts` | `requireAuth` — verifies JWT, attaches `req.jwtToken` + `req.jwtPayload` |
| `error.middleware.ts` | `errorHandler` — catches all `next(err)` calls, returns uniform JSON |

## auth.middleware.ts rules

- Delegates JWT verification to `jwtVerify.ts` (`verifyToken()`) — JWKS RS256, no static keys
- All environments (dev + prod) use Keycloak JWKS — D-04 (no dev ES256 fallback)
- On success: attaches `req.jwtToken` (raw string) for the proxy to forward
- On failure: `401` with JSON — no stack trace, no internal detail
- `AuthenticatedRequest` interface extends `Request` — import this in route files

## jwtVerify.ts rules

- Creates a JWKS client pointing at `env.JWKS_URI`
- BE-03: on signature failure, flushes JWKS cache and retries once
- Rate limiting: `jwksRequestsPerMinute: 10` prevents burst on key rotation

## error.middleware.ts rules

- Must be registered **last** in `index.ts` after all routes
- Never exposes stack traces in production (`NODE_ENV === 'production'` check)
- Always returns `{ error: string, message: string }` shape
- Logs with `console.error` — replace with a proper logger (e.g. pino) in E17

## Security (OWASP A02 / Zero Trust)

```typescript
// ✅ correct — JWKS RS256 via verifyToken()
await verifyToken(token)  // issuer + audience validated, BE-03 retry included

// ❌ never — skips signature verification
jwt.decode(token)

// ❌ never — static symmetric key (removed in Keycloak migration)
jwt.verify(token, secret, { algorithms: ['HS256'] })
```
