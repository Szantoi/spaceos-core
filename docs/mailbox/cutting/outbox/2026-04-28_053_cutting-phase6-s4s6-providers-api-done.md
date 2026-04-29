---
id: MSG-CUTTING-053-DONE
from: cutting
to: root
type: done
status: READ
---

## Összefoglaló

Cutting Phase 6 S4-S6 implementálva — Adapter Providers + Admin API + Hardening.

### S4: Adapter Implementations
- `BuiltinCuttingProvider` — wraps `CuttingProviderAdapter`, backwards compatible (BE-02)
- `OptiCutAdapter` + `OptiCutFormatConverter` — file-based XML, SEC-02 XXE hardening (DtdProcessing.Prohibit, XmlResolver=null, entity cap 0)
- `CutRiteAdapter` + `CutRiteFormatConverter` — CLI subprocess CSV format
- `ManualAdapter` — submit-only, delegates persistence to BuiltinCuttingProvider
- `ServiceCollectionExtensions` frissítve: mind a 4 adapter, `IAdapterFactory`, `ICuttingProviderResolver`, `IAdapterCallAuditWriter`, `IDistributedCache` (in-memory fallback), background services regisztrálva

### S5: Admin API
- `ConfigureAdapterCommand` + `ConfigureAdapterCommandHandler` — create/reconfigure optimistic concurrency, SEC-06 secret check, cache invalidation
- `TestAdapterCommand` + `TestAdapterCommandHandler` — adapter connectivity test
- `GetAdapterConfigQuery` + `GetAdapterConfigQueryHandler` — config read
- `GetAdapterHealthQuery` + `GetAdapterHealthQueryHandler` — health record read
- `AdapterAdminEndpoints` (POST /api/cutting/adapters/config, GET /api/cutting/adapters/config, POST /api/cutting/adapters/config/test, GET /api/cutting/adapters/health)
- `Program.cs` kiegészítve: `app.MapAdapterAdminEndpoints()`

### S6: Hardening
- `operations/db_jobs/audit_retention.sql` — pg_cron partition cleanup procedure (`fn_drop_old_audit_partitions`)
- `IConfigSecretDetector.ValidateConfigJson()` integrálva a `ConfigureAdapterCommandHandler`-be

## Tesztek

| Suite | Előtte | Utána | Új tesztek |
|---|---|---|---|
| Main | 851 | 921 | +70 |
| Contracts | 10 | 10 | 0 |
| **Összesen** | **861** | **931** | **+70** |

Minden teszt zöld, 0 hiba, 0 figyelmeztetés.

Új tesztfájlok:
- `Adapters/Providers/BuiltinCuttingProviderTests.cs` (3)
- `Adapters/Providers/OptiCutFormatConverterTests.cs` (9)
- `Adapters/Providers/OptiCutAdapterTests.cs` (9)
- `Adapters/Providers/CutRiteFormatConverterTests.cs` (8)
- `Adapters/Providers/CutRiteAdapterTests.cs` (6)
- `Adapters/Providers/ManualAdapterTests.cs` (7)
- `Adapters/Application/Commands/ConfigureAdapterCommandHandlerTests.cs` (6)
- `Adapters/Application/Commands/TestAdapterCommandHandlerTests.cs` (5)
- `Adapters/Application/Queries/GetAdapterConfigQueryHandlerTests.cs` (2)
- `Adapters/Application/Queries/GetAdapterHealthQueryHandlerTests.cs` (3)
- `Adapters/Api/AdapterAdminEndpointsTests.cs` (10)

## Security review

- [x] SEC-02: XXE hardening — `OptiCutFormatConverter` DtdProcessing.Prohibit, XmlResolver=null, MaxCharactersFromEntities=0; tesztelve XXE + billion-laughs payloaddal
- [x] SEC-06: plaintext secret rejection — `ConfigureAdapterCommandHandler` elsőként hívja `IConfigSecretDetector.ValidateConfigJson()`; tesztelve
- [x] Authorization — minden endpoint `[RequireAuthorization("ManufacturerOnly")]`; 401-es tesztek megírva
- [x] Layer boundaries — Infrastructure csak Application-t és Domain-t referál; nincs körkörös függőség
- [x] Public setters — nincsenek aggregátokon
- [x] Immutability — `TenantCuttingProviderConfig` csak `Reconfigure()`/`Disable()` metódusokon át változik

## Kockázatok / kérdések

Nincs blokkoló probléma. Az `AdapterConfigInvalidationListener` background service stub marad (Redis PubSub implementáció nem scope-ja ennek az epic-nek). A DI Redis `IDistributedCache`-re cserélhető production konfigban.
