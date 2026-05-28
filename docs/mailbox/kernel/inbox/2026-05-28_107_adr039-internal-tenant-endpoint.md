---
id: MSG-KERNEL-107
from: root
to: kernel
type: task
priority: high
status: READ
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-KERNEL-107 — ADR-039: GET /api/internal/tenants/{id}

## Kontextus

A Sales modul (5009) blokkolója. A Sales `KernelActorDirectoryClient` szinkron HTTP loopback-on
hívja ezt az endpointot Customer→Platform Actor link validáláshoz.

**Teljes spec:** `docs/tasks/new/SpaceOS_Kernel_ADR039_InternalTenantEndpoint_v1.md`

## Összefoglaló

Új internal-only endpoint a Kernel-ben:
- `GET /api/internal/tenants/{id}` — loopback-only, `X-SpaceOS-Internal` secret header, audit log
- Auth: **NEM JWT** — `X-SpaceOS-Internal: {secret}` + `X-SpaceOS-TenantId: {caller}` header
- `UseWhen("/api/internal", ...)` — JWT middleware **nem** érinti
- `TenantActorResponse` DTO: **csak** id, name, status, tier — PII mezők (email, phone, address) TILOS
- `InternalAccessAuditEntry` — minden hívás naplózva (Found + NotFound egyaránt)
- `B2BHandshakeVerifier` — `TenantHandshakeAllowlist` bidirektív allowlist ellenőrzés
- `ExcludeFromDescription()` — nem jelenik meg public Swagger-ben
- nginx: `/api/internal/*` **nincs** kifelé exponálva (csak loopback)

## Track sorrend

```
A) Domain/Application: GetTenantActorQuery + DTO + Ports + Spec
B) Infrastructure/Persistence: InternalAccessAuditEntry + Config + Migration 0030
C) Infrastructure/Internal: AuditWriter + HandshakeVerifier
D) Infrastructure/Security: InternalHeaderMiddleware (ha még nincs)
E) API: Endpoints + Program.cs + DI
F) Tests: párhuzamosan A-E-vel (5 handler + 3 middleware teszt minimum)
```

## Indítás előtt ellenőrizd

```bash
ls Infrastructure/Persistence/Migrations/ | tail -3   # → következő migration szám
grep -r "InternalHeaderMiddleware" --include="*.cs"    # → ha létezik, §5.4 skip
grep -r "TenantHandshakeAllowlist" Infrastructure/Persistence/KernelDbContext.cs
grep -r "IDbContextFactory" --include="*.cs"          # → factory típus neve
```

## Definition of Done (összefoglaló)

- [ ] `GET /api/internal/tenants/{id}` → 200/400/401/404 helyes esetekben
- [ ] `TenantActorResponse`: nincs PII mező
- [ ] Migration `0030` alkalmazva VPS-en
- [ ] `SpaceOS:InternalSecret` env-varból jön (nem appsettings.json)
- [ ] `GetTenantActorQueryHandlerTests` 5 teszt + `InternalHeaderMiddlewareTests` 3 teszt zöld
- [ ] Meglévő 1178 Kernel teszt zöld
- [ ] 0 build warning

Teljes spec és kódminták: `docs/tasks/new/SpaceOS_Kernel_ADR039_InternalTenantEndpoint_v1.md`
