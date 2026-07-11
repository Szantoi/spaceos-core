# SpaceOS Kernel — Threat Model v1.0

> STRIDE analysis for Sprint D Phase 2. All BE-P2-01..BE-P2-08 findings included.
> Date: 2026-04-07 | Author: kernel-team | Review: architect

---

## Components in Scope

| Component | Role | Trust Boundary |
|---|---|---|
| **Nginx** | Reverse proxy — TLS termination, X-Forwarded-For | Public internet → VPS loopback |
| **Orchestrator** | Node.js LLM agent — JWT forwarding, tool calls | Loopback → Kernel API |
| **Kernel API** | ASP.NET Core 8 — CQRS, JWT auth, RLS | Loopback (Orchestrator) + PostgreSQL |
| **PostgreSQL** | Primary data store — RLS enforced | Localhost only |
| **Redis** | Distributed cache — RL token backing (future) | Localhost only (bind 127.0.0.1) |

---

## STRIDE Matrix

### Nginx

| Threat | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| IP spoofing via X-Forwarded-For | ✅ KnownProxies whitelist (BE-P2-08) | ✅ TLS 1.3 only | ✅ access.log | ⚠️ Brand header spoof possible | — | ✅ CSP headers |
| Mitigation (BE-P2-08) | `UseForwardedHeaders` + `KnownProxies.Add(Loopback)` only | `ssl_protocols TLSv1.3` | — | Rate-limit by real IP | — | — |

### Orchestrator

| Threat | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| JWT forward to Kernel | ✅ Sub-claim validates identity | ✅ HMAC-signed tool results | ✅ ToolQueryExecuted audit | ✅ `sanitizeToolResultForLlm()` (BE-P2-04) | ✅ SSE RL + AbortController | ✅ 401 → structured error (BE-P2-04) |
| Prompt injection via tool result | — | — | — | ✅ `wrapToolResult()` escapes content | — | — |
| KernelClient timeout / unavailability | ✅ — | — | — | — | ✅ `AbortSignal.timeout(10_000)` | ✅ `KernelClientError` enum (BE-P2-04) |

### Kernel API

| Threat | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| Forged JWT | ✅ ES256 asymmetric (Phase 1.5) | ✅ ClockSkew=0 | ✅ AuthEndpointTests | — | — | — |
| Cross-tenant data access | ✅ TenantId from JWT, not header | ✅ HMAC hash chain | ✅ AuditEvent per mutation | ✅ PostgreSQL RLS (Phase 1.5 + T-01) | — | — |
| Cartesian product SQL (BE-P2-02) | — | — | — | ✅ Scalar subqueries only | — | — |
| Silent audit failure (BE-P2-03) | — | — | ✅ `FireAndForget` logs errors | — | — | — |
| Oversized payload | — | — | — | — | — | ✅ Kestrel 64 KB limit (T-06) |
| BuildServiceProvider anti-pattern | — | ✅ Fixed (BE-P2-01) | — | — | — | — |

### PostgreSQL

| Threat | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| Tenant cross-contamination | ✅ RLS on AuditEvents + query tables | ✅ HMAC hash chain | ✅ pg_log enabled | ✅ Role separation (spaceos_audit_writer) | ⚠️ No HA (single VPS) | ✅ FORCE ROW LEVEL SECURITY |
| SQL injection | ✅ EF Core parameterised queries only | — | — | ✅ No raw string interpolation | — | — |
| Backup encryption | — | ⚠️ Backup encryption operator responsibility | — | — | — | — |

### Redis

| Threat | S | T | R | I | D | E |
|---|---|---|---|---|---|---|
| Unauthorized access | ✅ AUTH requirepass (config/redis-spaceos.conf) | ✅ bind 127.0.0.1 loopback only | ✅ Redis logs | ✅ IP-keyed RL (BE-P2-08) | ✅ In-memory (single instance, ADR-007) | — |
| Rate limit bypass (proxy) | ✅ Real IP via ForwardedHeaders (BE-P2-08) | — | — | — | — | — |
| BuildServiceProvider multiplexer | — | ✅ Fixed (BE-P2-01): singleton IConnectionMultiplexer | — | — | — | — |

---

## Finding → Mitigation Map

| Finding | Severity | Component | Mitigation | Status |
|---|---|---|---|---|
| BE-P2-01 | CRITICAL | Redis DI | `IConnectionMultiplexer` singleton (no `BuildServiceProvider()`) | T-07 |
| BE-P2-02 | CRITICAL | Kernel SQL | Scalar subqueries in `GetTenantSummaryQueryHandler` | T-01 |
| BE-P2-03 | HIGH | Kernel Audit | `TaskExtensions.FireAndForget` — no silent failures | T-01 |
| BE-P2-04 | HIGH | Orchestrator | `KernelClientError` enum — exhaustive HTTP status map | Orchestrator (T-02) |
| BE-P2-05 | HIGH | Orchestrator SSE | `AbortController` + `req.on('close', abort)` | Orchestrator (T-03) |
| BE-P2-06 | MEDIUM | Tooling | Standalone `dotnet run` console project (not `dotnet script`) | T-05 |
| BE-P2-07 | MEDIUM | Redis RL | ADR-007 documents in-memory RL backing store limitation | T-07 |
| BE-P2-08 | MEDIUM | Nginx/RL | `UseForwardedHeaders` + real client IP for RL key | T-07 |

---

## Residual Risks

| Risk | Severity | Owner | Phase |
|---|---|---|---|
| No PostgreSQL HA — single VPS | Medium | Ops | Phase 3 |
| Backup encryption not enforced | Medium | Ops | Phase 3 |
| RL state resets on process restart | Low | Accepted (ADR-007) | Phase 3 (multi-instance) |
| P1-3 AggregateSnapshot | Medium | kernel-team | Phase 3B |
| P1-4 Outbox Pattern | Medium | kernel-team | Phase 3B |
| P1-8 ProofHash + WORM | High | kernel-team | Phase 3B |

---

## References

- `docs/adr/ADR-005-advisory-lock-audit-chain.md`
- `docs/adr/ADR-007-rl-backing-store.md`
- `scripts/db/fix-audit-ownership.sql`
- `scripts/db/init-roles.sql`
- `scripts/db/init-query-rls.sql`
- `config/redis-spaceos.conf`
