---
id: MSG-K030
from: architect
to: kernel
type: task
priority: P1
date: 2026-04-08
sprint: "Sprint D · Phase 3C+"
---

# Phase 3C+ — Kernel Tasks: Migration 0025+0026 + Token Claims

## Kontextus

A Phase 3C+ három területe közül a Kernel felelős:
- `Tenants.EnabledModules` oszlop (Migration 0025)
- `TenantHandshakeAllowlist` tábla (Migration 0026)
- JWT token claims bővítése (`enabled_modules`, `allowed_hosts`)
- B2BHandshake endpoint ellenőrzése / implementálása

## Feladatok

### T1 — Tenant entity: EnabledModules property
`Tenant.cs`: `IReadOnlyList<string> EnabledModules` + `SetEnabledModules()` factory.
`TenantConfiguration.cs`: `HasColumnType("varchar(32)[]")`.

### T2 — Migration 0025: EnabledModules
`suppressTransaction: true`. CHECK constraint: `<@ ARRAY['door','cabinet','window']`. Seed Doorstar → `ARRAY['door']`.

### T3 — TenantHandshakeAllowlist domain entity
`Domain/Entities/TenantHandshakeAllowlist.cs` + `Domain/DTOs/AllowedHostDto.cs` + `Domain/Repositories/ITenantHandshakeAllowlistRepository.cs`.

### T4 — Migration 0026: TenantHandshakeAllowlist
PK: (GuestTenantId, HostTenantId). FK CASCADE mindkét irányba. CHECK: NoSelfLink + TradeTypes valid + NotEmpty.
RLS FORCE. Seed: cabinetmaker → doorstar, ARRAY['door'], ON CONFLICT DO NOTHING.

### T5 — Infrastructure: TenantHandshakeAllowlistRepository
`GetAllowedHostsAsync`: JOIN Tenants for name, Take(20). `IsAllowedAsync`: AnyAsync.
Register in DI. AppDbContext DbSet hozzáadása.

### T6 — CreateTokenCommandHandler: new claims
```csharp
claims.Add(new Claim("enabled_modules", JsonSerializer.Serialize(tenant.EnabledModules ?? [])));
var hosts = await _allowlistRepo.GetAllowedHostsAsync(tenant.Id, ct).ConfigureAwait(false);
claims.Add(new Claim("allowed_hosts", JsonSerializer.Serialize(hosts.Take(20))));
```
Constructor inject: `ITenantHandshakeAllowlistRepository`.

### T7 — B2BHandshake endpoint ellenőrzés
`find /opt/spaceos/SpaceOS.Kerner -name "*Handshake*" | grep -v bin`
Ha nincs → `HandshakeEndpoints.cs` és domain entity implementálandó (külön MSG).

## DoD

```bash
dotnet build  # 0 error, 0 warning
dotnet test   # ≥915 pass, 0 fail
```

## Válaszban kérem

Mailbox outbox: `docs/mailbox/kernel/outbox/2026-04-08_030_phase3cplus-migration-0025-0026-done.md`
- Migration 0025+0026 implementálva?
- Token claims bővítve?
- B2BHandshake endpoint státusza?
- Build + test eredmény
