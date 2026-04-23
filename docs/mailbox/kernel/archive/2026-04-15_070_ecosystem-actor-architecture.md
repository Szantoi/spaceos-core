---
id: MSG-KERNEL-070
from: root
to: kernel
type: task
priority: high
status: READ
ref: SpaceOS_Ecosystem_Actor_Architecture_v4
created: 2026-04-15
---

# MSG-KERNEL-070 — Ecosystem Actor Architecture v4 (Migration 0029)

## Összefoglaló

**Sprint 4 fő feladata.** Teljes specifikáció: `docs/tasks/new/SpaceOS_Ecosystem_Actor_Architecture_v4.md`

TenantType expansion + ModuleRegistry + B2B Service Graph implementálása a Kernel-ben.

**Becsült effort:** ~10 fejlesztői nap  
**Migration:** 0029  
**Test baseline:** 1077 pass (0 fail)

---

## 1. Adatbázis — Migration 0029

### Tenants tábla bővítése

```sql
-- TenantType: varchar(32) + CHECK constraint (nem PG native enum — DB-01 finding)
ALTER TABLE "Tenants" ADD COLUMN "TenantType" varchar(32) NOT NULL DEFAULT 'Manufacturer';
ALTER TABLE "Tenants" ADD CONSTRAINT "CK_Tenants_TenantType_Valid"
    CHECK ("TenantType" IN ('Manufacturer','PanelCutter','Trader','Installer','Designer','SystemOperator'));

-- EnabledModules: JSONB array (pl. ["door","cutting","cabinet"])
ALTER TABLE "Tenants" ADD COLUMN "EnabledModules" jsonb NOT NULL DEFAULT '[]'::jsonb;
```

### Biztonsági triggerek (SEC-01, SEC-02 findings)

```sql
-- SEC-01: TenantType immutability
CREATE OR REPLACE FUNCTION prevent_tenant_type_change()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD."TenantType" != NEW."TenantType" THEN
        RAISE EXCEPTION 'TenantType is immutable after creation';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_prevent_tenant_type_change
BEFORE UPDATE ON "Tenants"
FOR EACH ROW EXECUTE FUNCTION prevent_tenant_type_change();

-- SEC-02: ModuleRegistry DB-level validation
CREATE OR REPLACE FUNCTION validate_enabled_modules_for_type()
RETURNS TRIGGER AS $$
DECLARE allowed_modules text[];
BEGIN
    CASE NEW."TenantType"
        WHEN 'Manufacturer'  THEN allowed_modules := ARRAY['door','cabinet','window','cutting','spatial'];
        WHEN 'PanelCutter'   THEN allowed_modules := ARRAY['cutting','inventory','procurement'];
        WHEN 'Trader'        THEN allowed_modules := ARRAY['inventory','procurement'];
        WHEN 'Installer'     THEN allowed_modules := ARRAY['spatial'];
        WHEN 'Designer'      THEN allowed_modules := ARRAY['spatial','door','cabinet'];
        WHEN 'SystemOperator' THEN allowed_modules := ARRAY['door','cabinet','window','cutting','spatial','inventory','procurement'];
        ELSE allowed_modules := ARRAY[]::text[];
    END CASE;
    IF EXISTS (
        SELECT 1 FROM jsonb_array_elements_text(NEW."EnabledModules") m
        WHERE m != ALL(allowed_modules)
    ) THEN
        RAISE EXCEPTION 'EnabledModules contains modules not allowed for TenantType %', NEW."TenantType";
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_validate_modules
BEFORE INSERT OR UPDATE ON "Tenants"
FOR EACH ROW EXECUTE FUNCTION validate_enabled_modules_for_type();
```

### TenantHandshakeAllowlist tábla (SEC-03)

```sql
-- Partner keresés CSAK ezen keresztül lehetséges (GET /api/tenants/by-type TÖRÖLVE)
CREATE TABLE "TenantHandshakeAllowlist" (
    "Id"             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "InitiatorId"    uuid NOT NULL REFERENCES "Tenants"("Id"),
    "PartnerId"      uuid NOT NULL REFERENCES "Tenants"("Id"),
    "PartnerType"    varchar(32) NOT NULL,
    "Status"         varchar(16) NOT NULL DEFAULT 'Pending',  -- Pending/Approved/Rejected
    "CreatedAt"      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "CK_Allowlist_Status" CHECK ("Status" IN ('Pending','Approved','Rejected')),
    CONSTRAINT "UQ_Allowlist_Pair" UNIQUE("InitiatorId","PartnerId")
);
```

---

## 2. Domain Model — C# változások

### TenantType enum (BE-02 finding)

```csharp
public enum TenantType
{
    Manufacturer,
    PanelCutter,
    Trader,
    Installer,
    Designer,
    SystemOperator
}
```

EF Core mapping — explicit conversion (nem `HasConversion<string>()`):
```csharp
.HasConversion(v => v.ToString(), v => Enum.Parse<TenantType>(v))
.HasColumnType("varchar(32)")
```

### Tenant entity bővítés

```csharp
public TenantType TenantType { get; private set; }
public IReadOnlyList<string> EnabledModules { get; private set; }

// Register() signature (BE-01: nem breaking — default paraméter)
public static Tenant Register(
    string name,
    TenantType tenantType = TenantType.Manufacturer,
    IEnumerable<string>? enabledModules = null)
```

---

## 3. API Végpontok

### Új végpontok

```
POST   /api/tenants                     → Register (TenantType + EnabledModules)
GET    /api/tenants/{id}/modules        → Tenant engedélyezett moduljai
GET    /api/handshakes/allowed-partners → Partner keresés (TenantHandshakeAllowlist)
POST   /api/handshakes                  → Új handshake kérelem
PUT    /api/handshakes/{id}/approve     → Jóváhagyás
PUT    /api/handshakes/{id}/reject      → Elutasítás
```

### Törölt végpont (SEC-03)

```
DELETE GET /api/tenants/by-type/{tenantType}  ← TÖRÖLVE tenant-izolációs sérelemért
```

---

## 4. Tesztek

- Minden új trigger tesztelése (immutability, module validation)
- `Register()` backward compatibility (default TenantType = Manufacturer)
- Handshake CRUD tesztek (approve/reject state machine)
- Negatív tesztek: TenantType módosítási kísérlet, invalid module

**Gate:** ≥1077 teszt pass (0 fail) a régi tesztek sértetlenek maradnak.

---

## DoD

- [ ] Migration 0029 (`dotnet ef migrations add EcosystemActorV4`)
- [ ] TenantType immutability trigger
- [ ] ModuleRegistry validation trigger
- [ ] TenantHandshakeAllowlist tábla + CRUD
- [ ] `GET /api/tenants/by-type` törölve
- [ ] `Tenant.Register()` bővítve (nem breaking)
- [ ] ≥1077 teszt pass
- [ ] Commit + push develop
- [ ] DONE outbox: commit hash + teszt count
