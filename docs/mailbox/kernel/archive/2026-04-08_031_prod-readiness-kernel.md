---
id: MSG-K031
from: architect
to: kernel
type: task
priority: P0
date: 2026-04-08
sprint: "Sprint D · Production Readiness"
---

# Production Readiness — Kernel: Track B (Audit Race Fix) + Track C (WORM) + Track A.4 (JWT config)

## Kontextus

Phase 3C+ DoD ✅ teljes. A Production Readiness Sprint három trackje közül kettő (B+C) és egy részfeladat (A.4) a Kernel felelőssége.

---

## Track B — Audit Race Fix

### B1 — `AuditEventDispatcher`: `BoundedChannelFullMode.Wait`

Fájl: `SpaceOS.Infrastructure/Events/AuditEventDispatcher.cs`

```csharp
// JELENLEGI (HIBÁS — SEC-01):
_channel = Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(512)
{
    FullMode = BoundedChannelFullMode.DropWrite  // ← silent loss!
});

// JAVÍTÁS:
_channel = Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(512)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true
});
```

Ha a channel tele van, `WriteAsync` vár. Ha 5s-on belül nem tud írni → `LogCritical` + dobja.

### B2 — `pg_advisory_xact_lock(bigint)` MD5 key

Fájl: `SpaceOS.Infrastructure/Events/PostgresAuditWriteLock.cs` (vagy ahol az advisory lock van)

```csharp
// JELENLEGI (HIBÁS — SEC-06):
await conn.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext(@tenantId))", new { tenantId });
// hashtext() → int4 → 50% kollízió esély 10k+ tenant felett!

// JAVÍTÁS:
var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(tenantId.ToString()));
var lockKey  = BitConverter.ToInt64(md5Bytes, 0);
await conn.ExecuteAsync("SELECT pg_advisory_xact_lock(@lockKey)", new { lockKey });
```

### B3 — `DisposeAsync()` graceful drain 30s

```csharp
public async ValueTask DisposeAsync()
{
    _channel.Writer.Complete();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try { await _processingTask.WaitAsync(cts.Token); }
    catch (OperationCanceledException) { _logger.LogWarning("Audit drain timeout after 30s"); }
}
```

### B tesztek (≥4 új)

- `Channel_WhenFull_DoesNotDropWrite` (kapacitás = 1, 2 párhuzamos WriteAsync)
- `LockKey_DifferentTenants_DifferentInt64Keys`
- `LockKey_SameTenant_SameInt64Key`
- `DisposeAsync_DrainCompletes_Within30s`

---

## Track C — PostgreSQL WORM Sink

### C1 — `AuditHashes` tábla + Migration 0027

```sql
CREATE TABLE IF NOT EXISTS "AuditHashes" (
    "TenantId"   uuid        NOT NULL,
    "BlockIndex" bigint      NOT NULL,
    "Hash"       varchar(64) NOT NULL,
    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "PK_AuditHashes" PRIMARY KEY ("TenantId", "BlockIndex")
);
ALTER TABLE "AuditHashes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AuditHashes" FORCE ROW LEVEL SECURITY;
CREATE POLICY "rls_audit_hashes_tenant"
    ON "AuditHashes" USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid,
            '00000000-0000-0000-0000-000000000001'::uuid));
```

Migration neve: `Migration_0027_AuditHashesWorm`, `suppressTransaction: true`.

### C2 — `spaceos_audit_worm` role (VPS SQL script)

```sql
CREATE ROLE spaceos_audit_worm NOLOGIN;
GRANT INSERT ON "AuditHashes" TO spaceos_audit_worm;
-- SEC-03: NO SELECT, NO UPDATE, NO DELETE
REVOKE ALL ON "AuditHashes" FROM spaceos_audit_worm;
GRANT INSERT ON "AuditHashes" TO spaceos_audit_worm;
```

**FONTOS:** `AUDIT_SINK_CONNECTION_STRING` env varban, NOT appsettings.json-ban (SEC-07).

### C3 — `PostgresWormStorageService`

```csharp
// SpaceOS.Infrastructure/Storage/PostgresWormStorageService.cs
// Implements IWormStorageService
// Connection string: IConfiguration["AUDIT_SINK_CONNECTION_STRING"]
// INSERT INTO "AuditHashes" VALUES (@tenantId, @blockIndex, @hash)
// spaceos_audit_worm role-lal csatlakozik (csak INSERT jog)
```

Regisztrálás DI-ban: `services.AddScoped<IWormStorageService, PostgresWormStorageService>()`
(csak ha `AUDIT_SINK_CONNECTION_STRING` env var be van állítva)

### C tesztek (≥6 új)

- `PostgresWormStorageService_Insert_Succeeds`
- `PostgresWormStorageService_SelectNotAllowed` (role nem tud SELECT-elni)
- `AuditHashes_RLS_TenantCannotSeeOtherTenantHashes`
- `Migration0027_Runs_Without_Error`

---

## Track A.4 — JWT Authority + Audience config-driven

Fájl: `SpaceOS.Kernel.Api/Program.cs` vagy `appsettings.json`

```csharp
// JELENLEGI (HIBÁS — BE-01): hardcoded issuer
.AddJwtBearer(opt => {
    opt.Authority = "https://spaceos-kernel"; // ← hardcoded!
    opt.Audience  = "spaceos-kernel-api";     // ← hardcoded!
});

// JAVÍTÁS:
.AddJwtBearer(opt => {
    opt.Authority = builder.Configuration["JWT_AUTHORITY"]
                   ?? "https://spaceos-kernel";  // dev fallback
    opt.Audience  = builder.Configuration["JWT_AUDIENCE"]
                   ?? "spaceos-kernel-api";
});
```

`appsettings.json`-ban NEM tároljuk — csak env var vagy `appsettings.Development.json`.

---

## DoD gate-ek (Kernel)

```bash
dotnet build  # 0 error, 0 warning
dotnet test   # ≥925 pass (915 + ≥10 új), 0 fail
```

Checklist:
- [ ] `Channel<T>` overflow → `LogCritical` tesztelhető
- [ ] `pg_advisory_xact_lock(bigint)` MD5 key — nem `hashtext()`
- [ ] `DisposeAsync()` graceful drain 30s timeout
- [ ] `AuditHashes` tábla + RLS + Migration 0027
- [ ] `PostgresWormStorageService` implementálva
- [ ] `AUDIT_SINK_CONNECTION_STRING` env varból (nem appsettings)
- [ ] JWT `Authority` + `Audience` config-driven

## Válaszban kérem

Mailbox outbox: `docs/mailbox/kernel/outbox/2026-04-08_031_prod-readiness-kernel-done.md`
