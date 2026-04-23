# Kernel Terminal — Hidegindítási Kontextus

> Stack: .NET 8, EF Core 8, PostgreSQL 16 (port 5433)
> Repo: `/opt/spaceos/SpaceOS.Kerner` (typo: "Kerner" nem "Kernel"!)
> Branch: `develop`

---

## Felelősség

- Auth (JWT ES256 → Keycloak JWKS validáció)
- Tenant izolálás (RLS + EF query filter + TenantSessionInterceptor)
- Audit event chain (SHA-256 hash chain, advisory lock)
- FlowEpic FSM (Pending → InDev → Review → Delivery → ClosedDone)
- Stage Registry (StageDefinition, StageChainTemplate, StageHandoff)
- Spatial BIM Core (PhysicalSpace, BvhNode, SpatialElement)
- B2B Handshake
- Escrow GA Foundation (Phase 3B)
- GDPR (adattörlés)

---

## Jelenlegi állapot (2026-04-20)

| Metrika | Érték |
|---------|-------|
| Commit | `46d64b5` (KERNEL-093: EnableRetryOnFailure removed) LIVE |
| Unit tesztek | **926 pass** |
| Integration tesztek | **108 pass** |
| API integration | **104 pass** |
| **Összesen** | **1138 pass, 0 fail** |
| VPS státusz | `DEPLOYED` ✅ — `systemctl is-active spaceos-kernel` → active |
| Port | **5000** (loopback-only) |
| Health | `GET http://127.0.0.1:5000/healthz` → 200 |
| Soft Launch | 🚀 **LIVE 2026-04-20** — `POST /api/tenants → 201` VPS-en ✅ |

---

## PostgreSQL séma

**DB neve:** `spaceos` (port 5433)

**Fő táblák:**
- `Tenants`, `Facilities`, `WorkStations`, `SpaceLayers`
- `FlowEpics` (CurrentStageCode, StageChainTemplateId — Migration 0028)
- `BvhNodes`, `PhysicalSpaces`, `SpatialElements`, `SpatialTaskLinks`
- `StageDefinitions`, `StageChainTemplates`, `StageChainSteps`, `StageHandoffs`
- `AuditEvents` (owner: `spaceos_schema_owner`, RLS FORCE)
- `RefreshTokens`, `HashChainRecords`
- `AuditHashes` (WORM, Migration 0027)

**Migration history (utolsó 3):**
- `20260410130000_Migration_0028_StageRegistry` (stage registry + FlowEpics columns)
- `20260408120000_Migration_0027_AuditHashesWorm` (WORM hash sink)
- `20260409174236_Migration_0001_ProductConfigurationEngine` (Abstractions séma?)

---

## Fontos env var-ok (`/etc/spaceos/kernel.env`)

```ini
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=spaceos;...
ASPNETCORE_ENVIRONMENT=Development      # Rate limit + Crypto fallback
Crypto__SigningKey=<base64>             # Dev dummy
RateLimit__WritePerMinute=1000          # E2E-hez
JWT_AUTHORITY=https://joinerytech.hu/auth/realms/spaceos
JWT_AUDIENCE=kernel-api
```

---

## Kritikus kód helyek

| Komponens | Helyszín | Fontosság |
|-----------|----------|-----------|
| `ClaimsTenantResolver` | `SpaceOS.Infrastructure/Auth/` | spaceos_tenants claim parse, DenyWebRequestSentinel |
| `TenantSessionInterceptor` | `SpaceOS.Infrastructure/Persistence/` | GUC beállítás, pool cleanup |
| `AppDbContext` | `SpaceOS.Infrastructure/Persistence/` | 13 entitás query filter, null/sentinel/realGuid logika |
| `Program.cs` | `SpaceOS.Kernel.Api/` | `MapInboundClaims = false` kötelező! |
| `StageEndpoints.cs` | `SpaceOS.Kernel.Api/Endpoints/` | RBAC policy-k: SystemAdmin/TenantAdmin/StageOperator |
| `CreateStageHandoffCommandHandler` | `SpaceOS.Infrastructure/` (!) | ADR-023: Infrastructure rétegben, `pg_advisory_xact_lock` |

---

## Kritikus fixek — Soft Launch előtt (Sprint 4-9)

### EnableRetryOnFailure eltávolítva (KERNEL-090/091/093/099)

**Root cause:** `EnableRetryOnFailure` + explicit transaction = `InvalidOperationException` (NpgsqlRetryingExecutionStrategy.OnFirstExecution)

**Érintett DbContext-ek:**
- `AppDbContext` — `EnableRetryOnFailure` eltávolítva (KERNEL-093)
- `AuditDbContext` — `EnableRetryOnFailure` eltávolítva (KERNEL-093)
- `ModulesDbContext` — `EnableRetryOnFailure` eltávolítva (KERNEL-099)
- `HashSinkDbContext` — **MEGTARTVA** (nincs user transaction, helyes)

**Kapcsolódó:** `AuditEvent.Sequence GENERATED ALWAYS AS IDENTITY` fix (KERNEL-090) + transaction wrapper (KERNEL-091)

[MSG-KERNEL-100-DONE]

### MinIO DI conditional registration fix (KERNEL-089)

**Root cause:** `MinioClient().Build()` dobott `MinioException`-t, ha `AccessKey` üres → DI feloldás 500.

**Fix:** Conditional DI — csak ha `Enabled=true && AccessKey nem üres && SecretKey nem üres`.
```
if (escrowCfg.Enabled && !string.IsNullOrWhiteSpace(escrowCfg.AccessKey) && !string.IsNullOrWhiteSpace(escrowCfg.SecretKey))
  → IMinioClient + MinioStorageAdapter + MinioAuditEscrowWriter
else
  → NullAuditEscrowWriter (no-op)
```

**Default:** `MinioEscrow.Enabled: false` az `appsettings.json`-ban.
**Commit:** `7f8fd4c`

[MSG-K089-DONE]

---

## Ismert tech debt

1. **kernel.env jogosultság** — 644 (world-readable) → 640-re javítandó Q3 előtt
2. **Kernel port 5000 vs 5001** — `appsettings.Production.json` felülírja az env var-t; konzisztens állapot: minden kliens 5000-et használ
3. **`/api/nodes/register` 500** — NodeRegister endpoint intermittent 500 (E2E 15 örökség bug)
4. **FlowEpic /close FSM bug** — `PUT /close` Delivery→ClosedDone 500 (E2E 05 örökség bug)

---

## Indítás előtt

1. `MapInboundClaims = false` megvan-e a `Program.cs`-ben?
2. `DenyWebRequestSentinel` megvan-e a `ClaimsTenantResolver`-ben?
3. Migration history konzisztens-e? (`20260410130000_Migration_0028_StageRegistry`)
4. `dotnet build → 0 error, 0 warning`
5. `dotnet test → 1138 pass, 0 fail`

---

## Deploy parancs

```bash
cd /opt/spaceos/SpaceOS.Kerner
git pull origin develop
dotnet publish SpaceOS.Kernel.Api -c Release -o /tmp/kernel-publish
sudo systemctl stop spaceos-kernel
sudo rm -rf /opt/spaceos/spaceos-kernel/publish/*
sudo cp -r /tmp/kernel-publish/. /opt/spaceos/spaceos-kernel/publish/
sudo chown -R spaceos-deploy:spaceos-deploy /opt/spaceos/spaceos-kernel/publish/
sudo systemctl start spaceos-kernel
curl http://127.0.0.1:5000/healthz
```
