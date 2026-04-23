# SpaceOS — Ecosystem Actor Architecture
## TenantType Expansion + ModuleRegistry + B2B Service Graph

> Verzió: v4.0 — 2026-04-11
> Státusz: **IMPLEMENTÁCIÓRA KÉSZ — minden döntés lezárva**
> Blokkoló feltétel: Stage Registry DoD (MSG-057 tesztek)
> Referencia: `SpaceOS_Ecosystem_Module_Architecture_v1.md` (tervezési döntések)
> Érintett repo: `spaceos-kernel`
> Migration sorszám: 0029
> Becsült effort: **10 fejlesztői nap** (v1: 8 → v4: +2 review findings)
> Test baseline: 1570+ pass (Kernel 933 + Orchestrator 176 + Portal 291 + Joinery 109 + Abstractions 61)
> Kumulált review: `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | 0 CRITICAL · 1 HIGH · 2 MEDIUM | Native PG enum → varchar+CHECK · Transaction guard | +0.5 nap |
| v2 → `/senior-security` → v3 | 1 CRITICAL · 2 HIGH · 1 MEDIUM | TenantType immutability trigger · DB-level module validation · Cross-tenant partner isolation | +1 nap |
| v3 → `/senior-backend` → v4 | 0 CRITICAL · 1 HIGH · 2 MEDIUM | Register() breaking change mitigation · Ardalis.Spec · Conversion guard | +0.5 nap |
| **Összesen** | **1 CRITICAL · 4 HIGH · 5 MEDIUM** | | **~10 fejlesztői nap** |

### Finding részletek

| ID | Súly | Terület | Probléma | v_ javítás |
|----|------|---------|----------|------------|
| DB-01 | 🟠 HIGH | PG enum | `CREATE TYPE tenant_type AS ENUM` — EF Core nem kezeli natívan, migration rollback problémás, enum bővítés `ALTER TYPE ADD VALUE` nem transactionable | v2: `varchar(32)` + `CK_Tenants_TenantType_Valid` CHECK constraint native enum helyett |
| DB-02 | 🟡 MEDIUM | Seed UUID | Demo seed UUID-ok (`...0010`–`...0014`) kiszámítható pattern | v2: Elfogadható demo-hoz; prod tenant-ek valódi UUID-ot kapnak; dokumentált |
| DB-03 | 🟡 MEDIUM | DDL atomicity | `DROP CONSTRAINT` + `ADD CONSTRAINT` nem atomikus implicit transaction nélkül | v2: Egyetlen `DO $$` blokk, atomic CHECK swap |
| SEC-01 | 🔴 CRITICAL | TenantType immutability | API-n nincs endpoint TenantType módosításra, de direkt DB UPDATE lehetséges — rossz TenantType → ModuleRegistry bypass | v3: `prevent_tenant_type_change()` trigger: UPDATE-kor `OLD."TenantType" != NEW."TenantType"` → `RAISE EXCEPTION` |
| SEC-02 | 🟠 HIGH | Module validation bypass | `ModuleRegistryService` csak Application-szinten validál — direkt DB INSERT/UPDATE megkerüli | v3: `validate_enabled_modules_for_type()` trigger: INSERT/UPDATE-kor ellenőrzi hogy az EnabledModules kompatibilis a TenantType-pal |
| SEC-03 | 🟠 HIGH | Partner isolation | `GET /api/tenants/by-type/{tenantType}` — az összes tenant listázása sérti a tenant-izolációt; nem minden PanelCutter partner mindenkinek | v3: Endpoint törlése; partner keresés kizárólag `TenantHandshakeAllowlist`-en keresztül (`GET /api/handshakes/allowed-partners`) |
| SEC-04 | 🟡 MEDIUM | Seed UUID range | Seed UUID-ok `00000000-...-0010` nem ütközhetnek `CK_Tenants_NoSystemId` constraint-tel | v3: Explicit CHECK: `CK_Tenants_NoSystemId` kizárólag `...0001`-et blokkolja — `...0010`+ OK |
| BE-01 | 🟠 HIGH | Breaking change | `Tenant.Register()` paraméter bővítés — meglévő tesztek és seed hívások frissítése szükséges | v4: `TenantType tenantType = TenantType.Manufacturer` default paraméter → nem breaking; új tesztek explicit TenantType-ot adnak |
| BE-02 | 🟡 MEDIUM | Conversion | `HasConversion<string>()` + globális `JsonStringEnumConverter` → dupla conversion risk | v4: `HasColumnType("varchar(32)")` explicit; `HasConversion(v => v.ToString(), v => Enum.Parse<TenantType>(v))` explicit mapping |
| BE-03 | 🟡 MEDIUM | Spec pattern | `GetTenantsByTypeQuery` raw repository hívás → Golden Rule #5 sértés | v4: **Endpoint törölve** (SEC-03); marad `AllowedPartnersSpec` az allowlist lekérdezéshez |

---

## 2. Architekturális döntések (ADR)

### ADR-018: Ecosystem Actor Types

**Döntés:** Explicit `TenantType` varchar (nem PG native enum — DB-01) a Kernel Tenants táblában. 6 aktor-típus.

### ADR-019: Module Decomposition

**Döntés:** Közös modulok (Cutting, Spatial) és trade-specifikus modulok (Door, Cabinet, Window) szétválasztása. `EnabledModules` CHECK bővül.

### ADR-020: Kontextus-alapú UI szétválás

**Döntés:** Portal UI kontextus-alapú nézetek — ebben a fázisban csak Kernel struktúra.

---

## 3. Domain modell

### 3.1 TenantType enum

```csharp
// SpaceOS.Kernel.Domain/Enums/TenantType.cs
public enum TenantType
{
    Manufacturer = 0,
    PanelCutter = 1,
    Trader = 2,
    Logistics = 3,
    Installer = 4,
    EndCustomer = 5
}
```

### 3.2 ModuleType enum

```csharp
// SpaceOS.Kernel.Domain/Enums/ModuleType.cs
public enum ModuleType
{
    Door = 0,
    Cabinet = 1,
    Window = 2,
    Cutting = 10,
    Spatial = 11,
    Trading = 20,
    Delivery = 21,
    Installation = 22,
    Orders = 23
}
```

### 3.3 ModuleRegistryService (Domain Service)

```csharp
// SpaceOS.Kernel.Domain/Services/IModuleRegistryService.cs
public interface IModuleRegistryService
{
    Result ValidateModulesForTenantType(TenantType tenantType, IReadOnlyList<string> enabledModules);
    IReadOnlyList<string> GetRequiredModules(TenantType tenantType);
    IReadOnlyList<string> GetAllowedModules(TenantType tenantType);
}
```

```csharp
// SpaceOS.Kernel.Domain/Services/ModuleRegistryService.cs
public sealed class ModuleRegistryService : IModuleRegistryService
{
    // Statikus konfiguráció — NEM DB-ből olvasott (SEC-02 defense-in-depth)
    private static readonly Dictionary<TenantType, (string[] Required, string[] Optional)> Registry = new()
    {
        [TenantType.Manufacturer]  = (new string[0], new[] { "door", "cabinet", "window", "cutting", "spatial" }),
        [TenantType.PanelCutter]   = (new[] { "cutting" }, new string[0]),
        [TenantType.Trader]        = (new[] { "trading" }, new[] { "delivery" }),
        [TenantType.Logistics]     = (new[] { "delivery" }, new string[0]),
        [TenantType.Installer]     = (new[] { "installation" }, new string[0]),
        [TenantType.EndCustomer]   = (new[] { "orders" }, new string[0]),
    };

    public Result ValidateModulesForTenantType(TenantType tenantType, IReadOnlyList<string> enabledModules)
    {
        if (!Registry.TryGetValue(tenantType, out var config))
            return Result.Error($"Unknown TenantType: {tenantType}");

        var allowed = new HashSet<string>(config.Required.Concat(config.Optional));
        var invalid = enabledModules.Where(m => !allowed.Contains(m)).ToList();
        if (invalid.Count > 0)
            return Result.Invalid($"Modules not allowed for {tenantType}: {string.Join(", ", invalid)}");

        var missingRequired = config.Required.Where(r => !enabledModules.Contains(r)).ToList();
        if (missingRequired.Count > 0)
            return Result.Invalid($"Required modules missing for {tenantType}: {string.Join(", ", missingRequired)}");

        return Result.Success();
    }

    public IReadOnlyList<string> GetRequiredModules(TenantType tenantType)
        => Registry.TryGetValue(tenantType, out var c) ? c.Required : Array.Empty<string>();

    public IReadOnlyList<string> GetAllowedModules(TenantType tenantType)
        => Registry.TryGetValue(tenantType, out var c)
            ? c.Required.Concat(c.Optional).ToArray()
            : Array.Empty<string>();
}
```

### 3.4 Tenant aggregate bővítés

```csharp
// Tenant.cs — bővítés
public TenantType TenantType { get; private set; }

// Factory — default paraméter (BE-01: nem breaking)
public static Result<Tenant> Register(
    string name, string? brandSkinId,
    TenantType tenantType = TenantType.Manufacturer,
    string[]? enabledModules = null)
{
    var tenant = new Tenant
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(), // self-reference
        Name = name,
        BrandSkinId = brandSkinId,
        TenantType = tenantType,
        EnabledModules = enabledModules ?? Array.Empty<string>(),
        IsArchived = false
    };
    tenant.AddDomainEvent(new TenantRegistered(tenant.Id, name, tenantType));
    return Result.Success(tenant);
}

// Modul-módosítás validációval
public Result UpdateEnabledModules(string[] modules, IModuleRegistryService registry)
{
    var validation = registry.ValidateModulesForTenantType(TenantType, modules);
    if (!validation.IsSuccess) return validation;

    EnabledModules = modules;
    AddDomainEvent(new TenantModulesUpdated(Id, modules));
    return Result.Success();
}
```

### 3.5 Domain events

```csharp
// TenantRegistered bővítés
public sealed record TenantRegistered(Guid TenantId, string Name, TenantType TenantType) : IDomainEvent;

// Új event
public sealed record TenantModulesUpdated(Guid TenantId, string[] EnabledModules) : IDomainEvent;
```

---

## 4. DB schema (DDL)

### Migration 0029 — Ecosystem Actor Types

```sql
-- ============================================================
-- Migration 0029: Ecosystem Actor Types
-- TenantType column + CHECK expansion + demo seed + DB triggers
-- ============================================================

-- 1. Add TenantType column (varchar, NOT native enum — DB-01)
ALTER TABLE "Tenants"
  ADD COLUMN IF NOT EXISTS "TenantType" varchar(32) NOT NULL DEFAULT 'Manufacturer';

-- 2. TenantType CHECK constraint
ALTER TABLE "Tenants"
  ADD CONSTRAINT "CK_Tenants_TenantType_Valid"
  CHECK (
    "TenantType" IN (
      'Manufacturer', 'PanelCutter', 'Trader',
      'Logistics', 'Installer', 'EndCustomer'
    )
  );

-- 3. Expand EnabledModules CHECK (atomic swap — DB-03)
DO $$
BEGIN
  ALTER TABLE "Tenants"
    DROP CONSTRAINT IF EXISTS "CK_Tenants_EnabledModules_Valid";

  ALTER TABLE "Tenants"
    ADD CONSTRAINT "CK_Tenants_EnabledModules_Valid"
    CHECK (
      "EnabledModules" <@ ARRAY[
        'door','cabinet','window',
        'cutting','spatial',
        'trading','delivery','installation','orders'
      ]::varchar(32)[]
    );
END $$;

-- 4. Expand AllowedTradeTypes CHECK (atomic swap)
DO $$
BEGIN
  ALTER TABLE "TenantHandshakeAllowlist"
    DROP CONSTRAINT IF EXISTS "CK_TenantHandshakeAllowlist_TradeTypes";

  ALTER TABLE "TenantHandshakeAllowlist"
    ADD CONSTRAINT "CK_TenantHandshakeAllowlist_TradeTypes"
    CHECK (
      "AllowedTradeTypes" <@ ARRAY[
        'door','cabinet','window',
        'cutting','delivery','installation'
      ]::varchar(32)[]
    );
END $$;

-- 5. Index on TenantType
CREATE INDEX IF NOT EXISTS "IX_Tenants_TenantType"
  ON "Tenants" ("TenantType");

-- 6. TRIGGER: TenantType immutable after creation (SEC-01 CRITICAL)
CREATE OR REPLACE FUNCTION prevent_tenant_type_change()
RETURNS TRIGGER AS $$
BEGIN
  IF OLD."TenantType" IS DISTINCT FROM NEW."TenantType" THEN
    RAISE EXCEPTION 'TenantType is immutable after creation. Current: %, Attempted: %',
      OLD."TenantType", NEW."TenantType";
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Tenants_ImmutableTenantType" ON "Tenants";
CREATE TRIGGER "TR_Tenants_ImmutableTenantType"
  BEFORE UPDATE ON "Tenants"
  FOR EACH ROW
  EXECUTE FUNCTION prevent_tenant_type_change();

-- 7. TRIGGER: EnabledModules must be valid for TenantType (SEC-02)
CREATE OR REPLACE FUNCTION validate_enabled_modules_for_type()
RETURNS TRIGGER AS $$
DECLARE
  allowed_modules varchar(32)[];
BEGIN
  -- Build allowed module list per TenantType
  CASE NEW."TenantType"
    WHEN 'Manufacturer' THEN
      allowed_modules := ARRAY['door','cabinet','window','cutting','spatial'];
    WHEN 'PanelCutter' THEN
      allowed_modules := ARRAY['cutting'];
    WHEN 'Trader' THEN
      allowed_modules := ARRAY['trading','delivery'];
    WHEN 'Logistics' THEN
      allowed_modules := ARRAY['delivery'];
    WHEN 'Installer' THEN
      allowed_modules := ARRAY['installation'];
    WHEN 'EndCustomer' THEN
      allowed_modules := ARRAY['orders'];
    ELSE
      RAISE EXCEPTION 'Unknown TenantType: %', NEW."TenantType";
  END CASE;

  -- Check all enabled modules are in the allowed list
  IF NOT (NEW."EnabledModules" <@ allowed_modules) THEN
    RAISE EXCEPTION 'EnabledModules % not allowed for TenantType %. Allowed: %',
      NEW."EnabledModules", NEW."TenantType", allowed_modules;
  END IF;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Tenants_ValidateModulesForType" ON "Tenants";
CREATE TRIGGER "TR_Tenants_ValidateModulesForType"
  BEFORE INSERT OR UPDATE ON "Tenants"
  FOR EACH ROW
  EXECUTE FUNCTION validate_enabled_modules_for_type();

-- 8. Update existing tenants (Doorstar = Manufacturer, already default)
UPDATE "Tenants"
  SET "TenantType" = 'Manufacturer'
  WHERE "TenantType" = 'Manufacturer';  -- no-op, ensures column populated

-- 9. Demo seed tenants (idempotens — DB-02 documented)
INSERT INTO "Tenants" ("Id", "Name", "TenantType", "EnabledModules", "IsArchived")
VALUES
  ('00000000-0000-0000-0000-000000000010', 'Demo Szekrénygyártó Bt.',
   'Manufacturer', ARRAY['cabinet','cutting','spatial'], false),
  ('00000000-0000-0000-0000-000000000011', 'Demo Lapszabász Kft.',
   'PanelCutter', ARRAY['cutting'], false),
  ('00000000-0000-0000-0000-000000000012', 'Demo Anyagkereskedő Kft.',
   'Trader', ARRAY['trading'], false),
  ('00000000-0000-0000-0000-000000000013', 'Demo Fuvarozó Kft.',
   'Logistics', ARRAY['delivery'], false),
  ('00000000-0000-0000-0000-000000000014', 'Demo Beszerelő Kft.',
   'Installer', ARRAY['installation'], false)
ON CONFLICT ("Id") DO UPDATE SET
  "Name" = EXCLUDED."Name",
  "TenantType" = EXCLUDED."TenantType",
  "EnabledModules" = EXCLUDED."EnabledModules";

-- 10. Demo B2B connections
INSERT INTO "TenantHandshakeAllowlist" ("GuestTenantId", "HostTenantId", "AllowedTradeTypes")
VALUES
  ('00000000-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000011', ARRAY['cutting']),
  ('00000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000013', ARRAY['delivery'])
ON CONFLICT ("GuestTenantId", "HostTenantId") DO UPDATE SET
  "AllowedTradeTypes" = EXCLUDED."AllowedTradeTypes";

-- 11. RLS: nem szükséges új policy — a meglévő tenant_isolation policy a Tenants táblán érvényes
-- A TenantType oszlop nem igényel külön RLS-t
```

---

## 5. API surface

### 5.1 Módosított endpoint-ok

| Endpoint | Változás |
|---|---|
| `POST /api/tenants` | + `tenantType` mező (kötelező, FluentValidation) |
| `GET /api/tenants` | + `tenantType` query param szűrő (opcionális) |
| `GET /api/tenants/{id}` | + `tenantType` a response DTO-ban |
| `PUT /api/tenants/{id}/modules` | + `ModuleRegistryService` validáció a handler-ben |

### 5.2 Új endpoint-ok

| Endpoint | Metódus | Leírás |
|---|---|---|
| `GET /api/module-registry/{tenantType}` | GET | Adott TenantType kötelező és engedélyezett moduljai |
| ~~`GET /api/tenants/by-type/{tenantType}`~~ | ~~GET~~ | ~~Törölve (SEC-03)~~ — partner keresés kizárólag allowlist-en |

### 5.3 CQRS

```
RegisterTenantCommand          — + TenantType paraméter (default Manufacturer — BE-01)
UpdateTenantModulesCommand     — + ModuleRegistryService injektálás
GetModuleRegistryQuery         — TenantType → { required, allowed } response
```

### 5.4 FluentValidation

```csharp
// RegisterTenantCommandValidator
RuleFor(x => x.TenantType)
    .IsInEnum()
    .WithMessage("Invalid TenantType. Allowed: Manufacturer, PanelCutter, Trader, Logistics, Installer, EndCustomer");

// UpdateTenantModulesCommandValidator
RuleFor(x => x)
    .Must(cmd => /* injected ModuleRegistryService.Validate */ true)
    .WithMessage("Modules not allowed for this TenantType");
```

---

## 6. EF Core konfiguráció

```csharp
// TenantConfiguration.cs
builder.Property(t => t.TenantType)
    .HasConversion(
        v => v.ToString(),
        v => Enum.Parse<TenantType>(v))   // BE-02: explicit conversion
    .HasColumnType("varchar(32)")          // BE-02: no double conversion
    .HasColumnName("TenantType")
    .IsRequired();

builder.HasIndex(t => t.TenantType)
    .HasDatabaseName("IX_Tenants_TenantType");
```

---

## 7. Definition of Done

### Migration gates
- [ ] Migration 0029 alkalmazva: `SELECT "TenantType" FROM "Tenants"` → 'Manufacturer' meglévő tenant-eknél
- [ ] `CK_Tenants_TenantType_Valid` CHECK: `'InvalidType'` → INSERT rejected
- [ ] `CK_Tenants_EnabledModules_Valid` CHECK: `ARRAY['cutting']` → INSERT sikeres
- [ ] `CK_Tenants_EnabledModules_Valid` CHECK: `ARRAY['invalid']` → INSERT rejected
- [ ] `CK_TenantHandshakeAllowlist_TradeTypes` CHECK: `ARRAY['cutting']` → INSERT sikeres
- [ ] `TR_Tenants_ImmutableTenantType` trigger: UPDATE TenantType → EXCEPTION (SEC-01)
- [ ] `TR_Tenants_ValidateModulesForType` trigger: Manufacturer + `ARRAY['trading']` → EXCEPTION (SEC-02)
- [ ] `TR_Tenants_ValidateModulesForType` trigger: PanelCutter + `ARRAY['cutting']` → OK
- [ ] `IX_Tenants_TenantType` index létezik
- [ ] Demo seed: 5 tenant (`...0010`–`...0014`) + 2 allowlist rekord

### Domain gates
- [ ] `TenantType` enum: 6 érték
- [ ] `ModuleType` enum: 9 érték
- [ ] `ModuleRegistryService.ValidateModulesForTenantType()` — 6 TenantType × valid/invalid = 12 teszt
- [ ] `Tenant.Register()` — default `TenantType.Manufacturer` → meglévő hívók nem törnek (BE-01)
- [ ] `Tenant.UpdateEnabledModules()` — invalid modul → `Result.Invalid`
- [ ] `TenantRegistered` event tartalmazza `TenantType`
- [ ] `TenantModulesUpdated` event

### API + validation gates
- [ ] `POST /api/tenants` — `tenantType: "PanelCutter"` → 201 Created
- [ ] `POST /api/tenants` — `tenantType: "InvalidType"` → 422
- [ ] `POST /api/tenants` — `tenantType: "PanelCutter", enabledModules: ["door"]` → 422
- [ ] `GET /api/tenants?tenantType=PanelCutter` → szűrt lista
- [ ] `GET /api/module-registry/Manufacturer` → `{ required: [], allowed: ["door","cabinet","window","cutting","spatial"] }`
- [ ] `GET /api/module-registry/PanelCutter` → `{ required: ["cutting"], allowed: ["cutting"] }`
- [ ] `PUT /api/tenants/{id}/modules` — `["trading"]` Manufacturer-re → 422
- [ ] `PUT /api/tenants/{id}/modules` — `["cutting"]` PanelCutter-re → 200

### Security gates (deployment blockers)
- [ ] `TR_Tenants_ImmutableTenantType` trigger aktív — UPDATE TenantType → EXCEPTION
- [ ] `TR_Tenants_ValidateModulesForType` trigger aktív — invalid modul kombináció → EXCEPTION
- [ ] Demo seed UUID-ok NEM ütköznek `CK_Tenants_NoSystemId`-vel
- [ ] ~~`GET /api/tenants/by-type`~~ endpoint NEM létezik (SEC-03)
- [ ] `ModuleRegistryService` DI: Singleton (statikus konfig, thread-safe)
- [ ] Meglévő RLS policy tesztek továbbra is zöldek

### Összesített
- [ ] Meglévő 1570+ teszt zöld (Kernel 933 + Orchestrator 176 + Portal 291 + Joinery 109 + Abstractions 61)
- [ ] Ecosystem Architecture új tesztek: ≥ 30 db
- [ ] 0 build warning
- [ ] `ConfigureAwait(false)` minden production async call-ban
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `EXPLAIN ANALYZE`: `IX_Tenants_TenantType` Index Scan
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat

---

## 8. Security adósság státusz

| ID | Tétel | Previous phase | This phase | Marad |
|----|-------|---------------|------------|-------|
| D-P15-T01 | Concurrent hash chain fix | ❌ NYITOTT | ❌ | Még nyitott |
| D-P15-T02 | RLS policy enforcement | ❌ NYITOTT | ❌ | Még nyitott |
| SEC-01 | TenantType immutability | — | ✅ DB trigger | — |
| SEC-02 | Module validation bypass | — | ✅ DB trigger | — |
| SEC-03 | Partner isolation | — | ✅ Endpoint törölve | — |

---

## 9. Mi jön utána

| Sorrend | Fázis | Blokkoló |
|---------|-------|----------|
| 1 | Modules.Cutting kiemelés | Ecosystem Architecture DoD |
| 2 | Modules.Door átnevezés | Modules.Cutting DoD |
| 3 | Modules.Cabinet v1 | Modules.Cutting DoD |
| 4 | Modules.Spatial v1 | Ecosystem Architecture DoD |

---

## 10. Claude Code implementációs csomag

### Végrehajtási sorrend

| Nap | Feladat | Track | Függőség |
|-----|---------|-------|----------|
| 1 | Domain: `TenantType` + `ModuleType` enums | Domain | — |
| 1 | Domain: `IModuleRegistryService` + `ModuleRegistryService` | Domain | Enums |
| 2 | Domain: `Tenant` aggregate bővítés (TenantType prop, factory default, UpdateEnabledModules) | Domain | ModuleRegistryService |
| 2 | Domain: Events (`TenantRegistered` bővítés, `TenantModulesUpdated`) | Domain | — |
| 3 | Application: `RegisterTenantCommand` + Validator bővítés | Application | Nap 2 |
| 3 | Application: `UpdateTenantModulesCommand` + Handler + Validator (ModuleRegistryService inject) | Application | Nap 1 |
| 4 | Application: `GetModuleRegistryQuery` + Handler | Application | Nap 1 |
| 4 | Infrastructure: `TenantConfiguration` bővítés (HasConversion explicit — BE-02) | Infra | Nap 2 |
| 5 | Infrastructure: Migration 0029 DDL (TenantType + CHECK + triggers + seed) | Infra | Nap 4 |
| 5 | API: Endpoint bővítések (`POST /api/tenants`, `GET` szűrő, `GET /api/module-registry/{type}`) | Api | Nap 3-4 |
| 6 | Tests: `ModuleRegistryServiceTests` (12+ teszt: 6 TenantType × valid/invalid) | Tests | Nap 1 |
| 6 | Tests: `TenantAggregateTests` (Register + UpdateEnabledModules) | Tests | Nap 2 |
| 7 | Tests: `RegisterTenantCommandValidatorTests` (TenantType validation) | Tests | Nap 3 |
| 7 | Tests: `UpdateTenantModulesCommandHandlerTests` | Tests | Nap 3 |
| 8 | Tests: API integration tesztek (module-registry endpoint, tenant creation with type) | Tests | Nap 5 |
| 8 | Tests: Migration 0029 trigger tesztek (immutability + module validation) | Tests | Nap 5 |
| 9 | Tests: Security gate tesztek (SEC-01/02/03 grep + reflection + trigger) | Tests | Nap 8 |
| 9 | Meglévő teszt-javítások (Tenant.Register() hívók frissítése — BE-01) | Tests | Nap 2 |
| 10 | VPS: Migration 0029 alkalmazás + seed data verifikáció | Infra | Nap 9 |

### Agent utasítás

> "Implementáld a `SpaceOS_Ecosystem_Actor_Architecture_v4.md` alapján:
>
> **Domain:** TenantType enum (6 érték), ModuleType enum (9 érték), ModuleRegistryService (statikus registry — SEC-02 defense-in-depth, NEM DB-ből olvas), Tenant aggregate bővítés (TenantType property, Register() default paraméter — BE-01, UpdateEnabledModules validáció)
>
> **Application:** RegisterTenantCommand bővítés (TenantType kötelező), UpdateTenantModulesCommand (ModuleRegistryService inject), GetModuleRegistryQuery
>
> **Infrastructure:** TenantConfiguration (HasConversion explicit — BE-02, HasColumnType varchar(32)), Migration 0029 (TenantType varchar + CHECK + immutability trigger SEC-01 + module validation trigger SEC-02 + demo seed)
>
> **API:** POST /api/tenants + tenantType, GET /api/tenants?tenantType szűrő, GET /api/module-registry/{tenantType}. NEM implementálni: GET /api/tenants/by-type (SEC-03 — törölve)
>
> **FONTOS:** Tenant.Register() — default TenantType.Manufacturer paraméter, meglévő hívók NEM törhetnek (BE-01)
>
> **ELSŐ LÉPÉS:** Futtasd a Section 11 (Implementation Context) felderítési parancsait:
> 1. `grep -rn 'Tenant\.Register\(' --include='*.cs'` → hatáselemzés (BE-01)
> 2. `cat .../TenantConfiguration.cs` → meglévő EF config
> 3. `cat .../Tenant.cs` → meglévő aggregate
> 4. `grep -rn 'Doorstar' --include='*.sql' --include='*.cs'` → Doorstar UUID
> 5. `grep -rn 'EnabledModules' --include='*.sql' .../Migrations/` → meglévő EnabledModules
> 6. `ls -la .../Migrations/ | tail -5` → migration sorrend
>
> A felderítési eredmények alapján hozd létre az implementációt. A Doorstar UUID-t a seed SQL-be helyettesítsd be.
> Ha a meglévő Doorstar EnabledModules üres ('{}'), UPDATE-eld ARRAY['door']-ra a trigger ELŐTT.
>
> DoD: #7 · Blokkolók: Migration 0029, SEC-01 trigger, SEC-02 trigger
> Gate: `dotnet test && dotnet build`"

### Kockázatok

| Kockázat | P | Hatás | Mitigáció |
|----------|---|-------|-----------|
| `Tenant.Register()` meglévő hívók — compilation error | Közepes | Build fail | Default paraméter (BE-01) |
| `TR_Tenants_ValidateModulesForType` — meglévő Doorstar tenant `EnabledModules = '{}'` | Közepes | Trigger exception seed-nél | UPDATE Doorstar `EnabledModules` BEFORE trigger creation |
| EF Core `HasConversion` + PostgreSQL varchar mismatch | Alacsony | Runtime | Explicit conversion (BE-02) |
| Demo seed INSERT — `ON CONFLICT` UPDATE overrides TenantType trigger | Közepes | `ON CONFLICT DO UPDATE` → trigger fires | `ON CONFLICT` update NEM módosítja TenantType-ot — csak Name, EnabledModules |

---

## 11. Implementation Context — Claude Code felderítési utasítások

Ez a szekció azokat az információkat tartalmazza, amelyeket a Claude Code agentnek **futásidőben kell felderítenie** a repo-ból, mert a tervdokumentumban nem rögzíthetők elavulás kockázata nélkül.

### 11.1 Doorstar Tenant UUID felderítése

A Doorstar tenant UUID nincs hardcoded-olva a tervdokumentumban — az implementáció során az agentnek meg kell keresnie.

```bash
# Lehetséges helyek a repo-ban:
grep -r "Doorstar" --include="*.cs" --include="*.sql" -l
grep -r "DOORSTAR_TENANT_UUID" --include="*.cs" --include="*.sql" --include="*.ts"
grep -r "tenant_id" --include="*.json" --include="*.env*" -l

# Keycloak group attribute-ból:
grep -r "tenant_id" --include="*.sh" --include="*.json" spaceos-kernel/
grep -r "doorstar" --include="*.sql" spaceos-kernel/src/SpaceOS.Infrastructure/Migrations/
```

**Ha nem található:** az agent kérdezze meg az operátort, vagy használja a VPS-en futó `psql -c "SELECT \"Id\",\"Name\" FROM \"Tenants\" WHERE \"Name\" LIKE '%Doorstar%'"` parancsot.

A felderített UUID-ot a Migration 0029 seed SQL-jébe kell behelyettesíteni a B2B allowlist rekordokhoz:
```sql
-- Doorstar ↔ Demo Szekrénygyártó B2B kapcsolat
INSERT INTO "TenantHandshakeAllowlist" ("GuestTenantId", "HostTenantId", "AllowedTradeTypes")
VALUES
  ('00000000-0000-0000-0000-000000000010', '<DOORSTAR_UUID>', ARRAY['door']),
  ('<DOORSTAR_UUID>', '00000000-0000-0000-0000-000000000010', ARRAY['cabinet']);
```

### 11.2 Tenant.Register() hatáselemzés

Az agent az implementáció **ELŐTT** futtassa le:

```bash
# Hány helyen hívják a Tenant.Register()-t?
grep -rn "Tenant\.Register\(" --include="*.cs"

# Hány tesztben szerepel?
grep -rn "Tenant\.Register\(" --include="*.cs" spaceos-kernel/tests/
```

A `TenantType tenantType = TenantType.Manufacturer` default paraméter (BE-01) biztosítja, hogy a meglévő hívók nem törnek. **DE**: ha bármelyik hívó explicit nevesített paramétereket használ (`name:`, `brandSkinId:`), a pozíció-eltolás compilation errort okozhat. Az agent ellenőrizze ezt.

### 11.3 Meglévő Tenant EF Core konfiguráció

```bash
# Jelenlegi TenantConfiguration.cs tartalma:
cat spaceos-kernel/src/SpaceOS.Infrastructure/Persistence/Configurations/TenantConfiguration.cs

# Meglévő Tenant.cs aggregate:
cat spaceos-kernel/src/SpaceOS.Kernel.Domain/Aggregates/Tenant.cs
```

Az agent a **meglévő property mapping-ek mellé** adja hozzá a TenantType property-t — ne írja felül a teljes fájlt.

### 11.4 Meglévő EnabledModules állapot

**KRITIKUS — Migration 0029 sorrend:**

A `TR_Tenants_ValidateModulesForType` trigger a meglévő Doorstar tenant `EnabledModules`-ét is validálja. Ha a Doorstar tenant `EnabledModules = '{}'` (üres), a trigger exception-t dob INSERT/UPDATE-nál.

Az agent ellenőrizze:
```bash
# Mi a meglévő Doorstar EnabledModules értéke?
grep -rn "EnabledModules" --include="*.sql" spaceos-kernel/src/SpaceOS.Infrastructure/Migrations/
```

Ha üres → **a Migration 0029-ben először UPDATE-elni kell** a Doorstar EnabledModules-t `ARRAY['door']`-ra, **MIELŐTT** a trigger létrejön:

```sql
-- BEFORE trigger creation:
UPDATE "Tenants" SET "EnabledModules" = ARRAY['door']
  WHERE "EnabledModules" = '{}' AND "BrandSkinId" = 'doorstar';
```

### 11.5 RegisterTenantCommand jelenlegi paraméterek

```bash
# Command fájl:
cat spaceos-kernel/src/SpaceOS.Kernel.Application/Tenants/Commands/RegisterTenantCommand.cs

# Handler fájl:
cat spaceos-kernel/src/SpaceOS.Kernel.Application/Tenants/Commands/RegisterTenantCommandHandler.cs

# Validator fájl:
cat spaceos-kernel/src/SpaceOS.Kernel.Application/Tenants/Commands/RegisterTenantCommandValidator.cs

# Endpoint registration:
grep -rn "RegisterTenant\|MapPost.*tenants" --include="*.cs" spaceos-kernel/src/SpaceOS.Kernel.Api/
```

### 11.6 Meglévő teszt pattern

```bash
# Teszt projekt struktúra:
find spaceos-kernel/tests/ -name "*Tenant*" -type f

# Teszt pattern vizsgálat (assertion stílus, fixture-ök):
head -50 spaceos-kernel/tests/SpaceOS.Kernel.Tests/Domain/TenantTests.cs 2>/dev/null || \
head -50 spaceos-kernel/tests/SpaceOS.Kernel.Tests/Application/RegisterTenantCommandHandlerTests.cs 2>/dev/null
```

Az agent a meglévő teszt pattern-t kövesse: ugyanaz az assertion stílus, ugyanaz a mock setup, ugyanaz a naming convention.

### 11.7 Migration sorrend ellenőrzés

```bash
# Legutolsó migration sorszám:
ls -la spaceos-kernel/src/SpaceOS.Infrastructure/Migrations/ | tail -5

# Stage Registry migration (0028) létezik-e:
find spaceos-kernel/ -name "*0028*" -type f
```

Migration 0029 csak 0028 UTÁN jöhet. Ha 0028 nincs commitolva (CODE_COMPLETE, de tesztek hiányoznak), az agent konzultáljon az operátorral.

---

*SpaceOS — Ecosystem Actor Architecture v4.0 · `/database-designer` + `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-11*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 10 finding beépítve, minden döntés lezárva*
