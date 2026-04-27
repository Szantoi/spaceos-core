---
id: 2026-04-27_004
from: partner
to: root
type: done
priority: high
status: READ
ref: 2026-04-27_004_partner-inbox4-security-deploy
---

# Inbox #4 DONE — Security Hardening + Deploy Readiness

## Elvégzett feladatok

### 1. EF Core Migration: P_0001_InitialPartnerSchema
- Migration generálva: `src/SpaceOS.PartnerTier.Infrastructure/Migrations/20260427092756_P_0001_InitialPartnerSchema.cs`
- Raw SQL hozzáadva a migrációhoz:
  - `CREATE SCHEMA IF NOT EXISTS spaceos_partner` (EnsureSchema via EF)
  - RLS ENABLE + FORCE + `partner_isolation` policy mind az 5 táblán (Partners, PartnerApiKeys, PartnerLeads, PartnerUpgradeAttributions, PartnerAuditLog)
  - COALESCE pattern: `COALESCE(current_setting('app.current_partner_id', true)::uuid, '00000000-...')`
  - GUC regisztráció: `set_config('app.current_partner_id', '', false)`
  - Append-only trigger: `prevent_audit_modification()` + `audit_log_immutable` a PartnerAuditLog-on
  - Performance indexek: `idx_partner_api_keys_key_prefix`, `idx_partner_leads_email_hash`, `idx_partner_upgrade_attributions_partner_converted`
  - Down() megfelelő DROP utasításokkal

### 2. ApiKeyExpiryWorker BackgroundService
- Fájl: `src/SpaceOS.PartnerTier.Infrastructure/Workers/ApiKeyExpiryWorker.cs`
- Napi futás, 365 napos lejárat (LastUsedAt NULL → CreatedAt alapján)
- Revoke() + PartnerAuditLog.Create(... "API_KEY_EXPIRED" ...) batch
- Regisztrálva: `Program.cs` + eltávolítva a test factory-kból

### 3. PartnerCorsMiddleware
- Fájl: `src/SpaceOS.PartnerTier.Api/Middleware/PartnerCorsMiddleware.cs`
- Csak `/api/partner/nest` és `/api/partner/leads` útvonalakra hat
- Bearer token `origin` claim vs request `Origin` header összehasonlítás
- Mismatch → 403 `{ "error": "Origin not allowed." }`
- Program.cs-ben LEGELŐRE regisztrálva (AdminAuth előtt)

### 4. appsettings.Production.json
- Fájl: `src/SpaceOS.PartnerTier.Api/appsettings.Production.json`
- Kestrel: `http://127.0.0.1:5011`
- ConnectionStrings, Redis, EmbedToken mind `FROM_ENV`

### 5. dotnet publish
- `dotnet publish src/SpaceOS.PartnerTier.Api -c Release -o /tmp/partner-publish/` ✓
- SpaceOS.PartnerTier.Api.dll + appsettings.Production.json jelen van

### 6. PartnerApiTestFactory javítás
- DB neve: `$"PartnerTestDb_{Guid.NewGuid()}"` - a Guid most a lambda ELŐTT kerül kiértékelésre (korábban minden DbContext-példányosításkor új Guid keletkezett, ami DB mismatch-et okozott)

## Teszt eredmény

| Kategória | Új tesztek | Fájl |
|---|---|---|
| Migration metadata | 18 | `Domain/MigrationMetadataTests.cs` |
| ApiKeyExpiryWorker | 10 | `Infrastructure/ApiKeyExpiryWorkerTests.cs` |
| CORS middleware | 6 | `Api/CorsMiddlewareTests.cs` |
| Security hardening | 12 | `Security/SecurityHardeningTests.cs` |
| Integration lifecycle | 15 | `Integration/PartnerLifecycleIntegrationTests.cs` |
| Deploy readiness | 8 | `Deploy/DeployReadinessTests.cs` |

**Összesen: 165 → 232 teszt (+67)**

```
dotnet build  → 0 error, 0 warning ✓
dotnet test   → 232 pass, 0 fail ✓
dotnet publish → /tmp/partner-publish/ ✓
```

## Megjegyzések

- `DisplayHint` a PartnerApiKey entitáson (nem `KeyPrefix`) — az index ennek megfelelően kerül létrehozásra
- A PartnerAuditLog tábla neve `PartnerAuditLog` (egyes szám, a konfiguráció szerint), az RLS/trigger is erre vonatkozik
- Az EF Tools (`dotnet-ef` 8.0.26) a `/opt/spaceos/spaceos-freetier-api/.tools/`-ból lett felhasználva `DOTNET_ROOT=/opt/dotnet` env-vel
