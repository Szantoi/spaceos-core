---
id: ADR-007
title: Rate Limiting Backing Store — Single Instance Constraint
status: Accepted
date: 2026-04-07
deciders: architect, kernel-team
---

# ADR-007: Rate Limiting Backing Store — Single Instance Constraint

## Context

SpaceOS Kernel uses ASP.NET Core's built-in `AddRateLimiter` (introduced in .NET 7) for
identity-partitioned rate limiting. The question is: what backing store does it use?

## Decision

ASP.NET Core's `AddRateLimiter` uses **in-process memory** as its backing store. It does
NOT use Redis or any distributed cache for the rate limiter window state, regardless of
whether `IDistributedCache` (Redis) is configured.

This is a **documented limitation** in Phase 2, accepted because:
- SpaceOS runs on a **single VPS instance** — in-process state == global state
- Process restarts reset the RL counters, which is acceptable (brief window widening)
- Redis is registered for future `IDistributedCache` consumers (session, output cache)

## Consequences

**Positive:**
- No distributed state coordination overhead
- Zero latency RL decisions (no Redis round-trip per request)
- Works correctly on single-instance deployments

**Negative:**
- RL counters reset on process restart (brief burst window possible)
- Cannot enforce global rate limits across multiple instances

## Upgrade Gate

When horizontal scaling is introduced (load balancer + multiple instances):
1. Replace `AddRateLimiter` with `AspNetCoreRateLimit` (NuGet: `AspNetCoreRateLimit`)
2. Configure `IDistributedCache` (Redis) as the backing store
3. Trigger: first Kubernetes/Docker Swarm deployment OR load balancer introduction

## References

- BE-P2-07 finding — Sprint D Phase 2 review
- `SpaceOS.Kernel.Api/Program.cs` — rate limiter policy definitions
- `config/redis-spaceos.conf` — Redis hardening config
