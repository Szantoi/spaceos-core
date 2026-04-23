# SpaceOS — Deployment Known Gotchas

> Minden csapda amit átéltünk, tanulsággal. Időrendben.

---

## GOTCHA-001 — rm -rf publish kötelező deploy előtt

**Mikor:** Minden inkrementális .NET publish előtt.

**Tünet:** `StackExchangeRedis DLL mismatch` — régi assembly fájlok maradnak a publish mappában, az új build más verzióra hivatkozik.

**Fix:**
```bash
sudo rm -rf /opt/spaceos/[repo]/publish/*
# UTÁNA: sudo cp -r /tmp/[service]-publish/. /opt/spaceos/[repo]/publish/
```

**Referencia:** [MSG-INFRA-062-DONE]

---

## GOTCHA-002 — GenesisHash env deploy-invariáns

**Mikor:** Ha az audit chain `GenesisHash`-t futás közben számítja ki, minden deploy után más hash jön → chain megszakad.

**Megoldás:** Rögzíteni az első hash értékét env var-ban:
```ini
# kernel.env
AuditChain__GenesisHash=<hash-érték-production-DB-ból>
```

Ezzel a hash deploy-invariáns lesz. [MSG-INFRA-096 pattern]

---

## GOTCHA-003 — Migration bypass procedúra

**Mikor:** Az EF Core `MigrateAsync()` startup-kor nem futotta le a migration-t, vagy a regenerált migration veszélyes SQL-t tartalmaz (DropTable).

**Eset (MSG-INFRA-060-DONE):** Migration 0028 `StageRegistry` — az EF Core regenerált migration `DropTable("AuditEvents")` sort tartalmazott SQLite ModelSnapshot alapján. Közvetlen `dotnet ef database update` futtatása katasztrofális lett volna.

**Bypass procedúra:**
1. Kinyerni az eredeti, authorizált SQL-t a git history-ból (`git show COMMIT:file.cs > /tmp/migration.sql`)
2. Futtatni psql-lel:
   ```bash
   PGPASSWORD=*** psql -U spaceos -h localhost -p 5433 -d spaceos \
     -v ON_ERROR_STOP=1 -f /tmp/migration.sql
   ```
3. EF history rekord manuális beillesztése:
   ```sql
   INSERT INTO "__EFMigrationsHistory" ("MigrationId","ProductVersion")
   VALUES ('20260410130000_Migration_0028_StageRegistry','8.0.11')
   ON CONFLICT DO NOTHING;
   ```

**Figyelem:** Ha ezt csináljuk, az EF Core migration history eltérhet a kód-szintű migration fájl ID-tól → következő `dotnet ef database update` veszélyes lehet. Kernel reconciliation task szükséges.

**Referencia:** [MSG-INFRA-060-DONE], [MSG-KERNEL-061-DONE]

---

## GOTCHA-004 — Keycloak KC_HOSTNAME_ADMIN fix

**Tünet:** Keycloak `iss` claim értéke `http://localhost:8080/...` → Kernel `RequireHttpsMetadata` hibát dob, JWT Authority mismatch.

**Root cause:** `KC_HOSTNAME` és `hostname-strict` nem volt megfelelően beállítva.

**Fix (keycloak.conf):**
```ini
hostname=joinerytech.hu
hostname-strict=false
hostname-strict-https=true
hostname-backchannel-dynamic=true
http-relative-path=/auth
proxy=edge
```

**Utána:** Kernel `appsettings.Production.json` Authority is frissítendő:
```json
{
  "Jwt": {
    "Authority": "https://joinerytech.hu/auth/realms/spaceos"
  }
}
```

**Referencia:** [MSG-INFRA-056-DONE]

---

## GOTCHA-005 — spaceos_tenants claim értelmezés

**Probléma:** A Keycloak Script Mapper kimenete **double-serialized** JSON string (nem natív JSON array):
```json
"spaceos_tenants": "[{\"tenant_id\":\"a1b2c3d4-...\"}]"
```

ASP.NET Core `JsonWebTokenHandler` ezt **szét is bontja** — minden array elem külön `Claim` objektum lesz azonos névvel. Tehát `user.FindFirst("spaceos_tenants")` az ELSŐ elemet adja vissza mint JSON object string: `{"tenant_id":"..."}`.

**Következmény:** Ha `[...]` array wrapperre tesztelünk, az else ágon fogunk menni!

**Helyes parse:**
```csharp
var tenantClaims = user.FindAll("spaceos_tenants");
foreach (var claim in tenantClaims)
{
    try
    {
        using var doc = JsonDocument.Parse(claim.Value);
        if (doc.RootElement.TryGetProperty("tenant_id", out var idEl))
            if (Guid.TryParse(idEl.GetString(), out var g)) return g;
    }
    catch { /* continue */ }
}
```

**Referencia:** [MSG-E2E-001-DONE v3, MSG-KERNEL-060-DONE]

---

## GOTCHA-006 — tid vs spaceos_tenants claim naming

**Összefoglalás:** Két claim rendszer él párhuzamosan:

| Claim | Forrás | Formátum | Hol használják |
|-------|--------|----------|----------------|
| `tid` | Régi SpaceOS auth | UUID string | Kernel backup, TenantSessionInterceptor fallback |
| `spaceos_tenants` | Keycloak Script Mapper | Double-serialized JSON (snake_case) | ClaimsTenantResolver elsődleges |

**Keycloak token tartalma (snake_case!):**
```json
{
  "tenant_id": "a1b2c3d4-...",
  "tenant_type": "Manufacturer",
  "enabled_modules": ["door"],
  "brand_skin": "doorstar"
}
```

**Veszély:** Ha kódban `tenantId` (camelCase) property-t keresünk a `spaceos_tenants` claim-ben → null. Mindig `tenant_id` (snake_case).

**Referencia:** [MSG-E2E-001-DONE v3, MSG-INFRA-060-DONE]

---

## GOTCHA-007 — MapInboundClaims = false nélkül az ASP.NET átnevezi a tid claimet

**Tünet:** `ClaimsTenantResolver` null-t kap a `"tid"` claim-re → `DenyWebRequestSentinel` → minden tenant-scoped kérés üres választ kap.

**Root cause:** `MapInboundClaims = true` (ASP.NET default) a `"tid"` → `http://schemas.microsoft.com/identity/claims/tenantid` URI-ra mappeli.

**Fix (minden .NET service Program.cs-ben):**
```csharp
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    // ...
});
```

**Referencia:** [MSG-KERNEL-082-DONE]

---

## GOTCHA-008 — GUC regisztráció ALTER DATABASE-szel

**Tünet:** `42704: unrecognized configuration parameter "app.current_tenant_id"`

**Root cause:** A PostgreSQL nem ismeri a custom GUC paramétert — explicit regisztráció szükséges.

**Fix (egyszeri init, nem migration!):**
```sql
ALTER DATABASE spaceos SET "app.current_tenant_id" TO '';
ALTER DATABASE spaceos_joinery SET "app.current_tenant_id" TO '';
ALTER DATABASE spaceos_abstractions SET "app.current_tenant_id" TO '';
ALTER DATABASE spaceos_cutting SET "app.current_tenant_id" TO '';
-- ... minden SpaceOS DB-re
```

**Megjegyzés:** A Kernel ezt Migration-ban csinálja. Új modul esetén el kell végezni a DB init scriptben.

**Referencia:** [MSG-E2E-044-DONE]

---

## GOTCHA-009 — USAGE + DML + SEQUENCES + DEFAULT PRIVILEGES grant új DB-nél

**Tünet:** Új séma + tábla létrehozás után `42501: permission denied for schema` — még ha a user tábla ownere is.

**Fix (teljes privilege set új DB-n):**
```sql
GRANT USAGE ON SCHEMA spaceos_modules TO spaceos;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA spaceos_modules TO spaceos;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA spaceos_modules TO spaceos;

-- Jövőbeli objektumokra:
ALTER DEFAULT PRIVILEGES IN SCHEMA spaceos_modules
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO spaceos;
ALTER DEFAULT PRIVILEGES IN SCHEMA spaceos_modules
    GRANT USAGE, SELECT ON SEQUENCES TO spaceos;
```

**Referencia:** [MSG-INFRA-112 pattern, E2E cutting module setup]

---

## GOTCHA-010 — Rate limit window E2E-ban (85 másodperc wait)

**Tünet:** E2E tesztek `429 Too Many Requests` hibával megállnak a `seedPOST` helper során.

**Root cause:** Production rate limit (20 write/perc) az E2E gyors POST láncolatot túllépi.

**Megoldások:**
1. `ASPNETCORE_ENVIRONMENT=Development` → Dev rate limit (500/perc) aktiválódik [MSG-INFRA-059]
2. `RateLimit__WritePerMinute=1000` env var override [MSG-INFRA-062-DONE]
3. E2E `seedPOST` helper-ben 85 másodperc cooldown az exhaustion után

**Rate limit defaults:**
- Development: 500 write/perc
- Production: 20 write/perc (E2E-t blokkolja!)

**Referencia:** [MSG-E2E-001-DONE v3, MSG-KERNEL-060-DONE]

---

## GOTCHA-011 — Portal CSP connect-src Nginx fix

**Tünet:** Portal nem tud Keycloak-ra csatlakozni — CSP header blokkolja.

**Fix (Nginx config):**
```nginx
add_header Content-Security-Policy "
    default-src 'self';
    connect-src 'self' https://joinerytech.hu/auth/;
    ...
";
```

**Referencia:** [MSG-INFRA-KC01, Portal security review]

---

## GOTCHA-012 — Keycloak user profile (VERIFY_PROFILE hiba)

**Tünet:** Direct Access Grant (DAG) login `"Account is not fully set up"` hibával megáll.

**Root cause:** Keycloak 24 VERIFY_PROFILE: `firstName` + `lastName` kötelező, különben a token exchange megtagadva.

**Fix:** Minden teszt user létrehozásakor `firstName` + `lastName` kitöltése kötelező.

**Referencia:** [MSG-INFRA-KC01-DONE]

---

## GOTCHA-013 — Docker → Bare-metal Keycloak (CPU inkompatibilitás)

**Tünet:** `quay.io/keycloak/keycloak:24.0.5` image nem indul: `CPU does not support x86-64-v2`.

**Megoldás:** Bare-metal Keycloak 24.0.0 systemd service-ként. [MSG-INFRA-KC01-DONE]

Különbségek:
| | Docker terv | Megvalósítás |
|---|---|---|
| Bináris | docker-compose.yml | `/opt/keycloak-app/bin/kc.sh` |
| Service | `docker compose up -d` | `sudo systemctl restart keycloak` |
| Konfig | docker-compose.yml env | `/opt/keycloak-app/conf/keycloak.conf` |
| Logs | `docker logs` | `sudo journalctl -u keycloak` |

---

## GOTCHA-014 — OpenConnectionAsync affinity (cutting internal endpoint)

**Tünet:** `22P02: invalid input syntax for type uuid: ""` — GUC érték üres.

**Root cause:** Az internal DELETE endpoint-on az `InternalEndpoints` közvetlenül hívja a `DbContext`-et, de a `TenantSessionInterceptor` nem fut le (nincs HTTP kérés pipeline) → GUC üres marad.

**Fix:** Explicit connection pin + GUC beállítás az endpoint kódban:
```csharp
if (dbContext.Database.IsRelational())
{
    await dbContext.Database.OpenConnectionAsync(ct);
    await dbContext.Database.ExecuteSqlAsync(
        $"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct);
}
try { counts = await repo.DeleteByTenantAsync(tenantGuid, ct); }
finally
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync();
}
```

**Referencia:** [MSG-CUTTING-015-DONE]

---

## GOTCHA-015 — Kernel port 5000 vs 5001 eltérés

**Helyzet:** Az env var `ASPNETCORE_URLS=http://127.0.0.1:5001`, de a Kernel valójában 5000-en hallgat — az `appsettings.Production.json` `"Urls": "http://127.0.0.1:5000"` felülírja az env var-t.

**Konzisztens állapot:** Orchestrator `.env`-ben `KERNEL_BASE_URL=http://127.0.0.1:5000` → minden kliens 5000-en szól a Kernelhez.

**Ha valaki 5001-re akar váltani:**
- A) `appsettings.Production.json`-ből `"Urls"` kulcs eltávolítása (Kernel PR)
- B) Orchestrator `.env` átírása 5001-re

**Referencia:** [MSG-INFRA-062-DONE]

---

## GOTCHA-016 — DateTime.SpecifyKind kötelező Npgsql timestamptz-nél

**Mikor:** Bármely .NET service PostgreSQL `timestamp with time zone` oszlopba ír dátumot.

**Tünet:**
```
System.ArgumentException: Cannot write DateTime with Kind=Unspecified to PostgreSQL type
'timestamp with time zone', only UTC is supported.
```

**Gyökérok:** `DateTime.TryParse("2026-04-18")` `Kind=Unspecified`-et ad. Az Npgsql **csak `Kind=Utc`-t** fogad el `timestamptz`-nél.

**Miért nem bukott meg unit teszten:** InMemory EF nem ellenőrzi Kind-et — csak PostgreSQL-en robban.

**Fix:**
```csharp
if (!DateTime.TryParse(request.Date, out var rawDate))
    return Results.BadRequest(...);
var planDate = DateTime.SpecifyKind(rawDate.Date, DateTimeKind.Utc);  // ← kötelező!
```

**Referencia:** [MSG-CUTTING-018-DONE, BUG-004]

---

## GOTCHA-017 — EnableRetryOnFailure + explicit transaction = InvalidOperationException

**Mikor:** Bármely EF DbContext, ahol `EnableRetryOnFailure` be van kapcsolva, és a handler explicit `DbContext.Database.BeginTransaction()`-t nyit.

**Tünet:**
```
InvalidOperationException: The configured execution strategy 'NpgsqlRetryingExecutionStrategy'
does not support user-initiated transactions.
```

**Gyökérok:** A `NpgsqlRetryingExecutionStrategy.OnFirstExecution()` dobja, ha `Database.CurrentTransaction != null` — azaz ha a DbContext retry stratégiával van konfigurálva ÉS már aktív egy tranzakció.

**Fix:** `EnableRetryOnFailure` eltávolítása minden DbContext-ből, amely explicit tranzakciót használ.
```csharp
// TILOS explicit transaction mellé:
options.UseNpgsql(connStr, o => o.EnableRetryOnFailure(5));

// Csak read-only, tranzakciómentes DbContext-nél elfogadható (pl. HashSinkDbContext)
```

**Referencia:** [MSG-KERNEL-100-DONE, KERNEL-090/091/093/099, ADR-037]

---

## GOTCHA-018 — MinIO DI regisztráció: üres credentials exception DI feloldáskor

**Mikor:** `IMinioClient` singleton factory regisztrálva, de az `appsettings.json` nem tartalmaz MinIO konfigurációt (üres `AccessKey`/`SecretKey`).

**Tünet:** Minden request 500 → `MinioException` DI feloldáskor → `AuditEventDispatcher` nem oldható fel.

**Fix:**
```csharp
if (escrowCfg.Enabled
    && !string.IsNullOrWhiteSpace(escrowCfg.AccessKey)
    && !string.IsNullOrWhiteSpace(escrowCfg.SecretKey))
    services.AddSingleton<IMinioClient>(...);
else
    services.AddSingleton<IAuditEscrowWriter, NullAuditEscrowWriter>();
```

**Default:** `MinioEscrow.Enabled: false` az `appsettings.json`-ban.

**Referencia:** [MSG-K089-DONE, KERNEL-089]
