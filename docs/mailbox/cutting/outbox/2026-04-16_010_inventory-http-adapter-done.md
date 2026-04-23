---
id: MSG-CUTTING-010-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-010
created: 2026-04-16
---

## Összefoglaló

`InventoryProviderHttpAdapter` implementálva — a Stub-ot felváltja a valódi HTTP hívás az inventory service-re.

### Új fájlok

**`src/SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryProviderHttpAdapter.cs`**

Mind a 6 `IInventoryProvider` metódus implementálva:
- `GetStockAsync` → GET `/api/inventory/stock?materialType=...` → `StockApiResponse` → `PanelStockDto`
- `GetOffcutsAsync` → GET `/api/inventory/offcuts?materialType=...` → `OffcutApiResponse[]` → `IReadOnlyList<OffcutDto>`
- `RecordConsumptionAsync` → POST `/api/inventory/movements/consumption` (per item)
- `RecordInboundAsync` → POST `/api/inventory/movements/inbound`
- `RecordOffcutAsync` → POST `/api/inventory/movements/offcut`
- `GetConsumptionTrendAsync` → GET `/api/inventory/trend?from=...&to=...` → `ConsumptionTrendDto`

Minden metódus `try/catch + LogWarning` graceful degradation-nel — az inventory kiesése nem omlik le a cutting service-t.

Privát belső response record-ok (`StockApiResponse`, `OffcutApiResponse`, `TrendApiResponse`, `TrendEntry`) az API JSON → contract DTO mappinghoz, mivel az inventory API más response shape-et ad vissza, mint a contract DTO-k.

**`src/SpaceOS.Modules.Cutting.Infrastructure/AssemblyInfo.cs`**
- `[InternalsVisibleTo("SpaceOS.Modules.Cutting.Tests")]` — tesztek elérhetik az internal típusokat

**`tests/SpaceOS.Modules.Cutting.Tests/Infrastructure/InventoryProviderHttpAdapterTests.cs`**
- 7 új teszt: happy path + graceful degradation (503, network throw) GetStock, GetOffcuts, RecordConsumption, GetConsumptionTrend esetekre
- `MockHttpMessageHandler` és `ThrowingHttpMessageHandler` test double-ok

### Módosított fájlok

**`ServiceCollectionExtensions.cs`**:
- `InventoryProviderStub` regisztráció eltávolítva
- `AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>` hozzáadva
- `IConfiguration configuration` paraméter hozzáadva (`InventoryService:BaseUrl` config kulcs)

**`Program.cs`** (cutting Api):
- `AddCuttingInfrastructure(connectionString, builder.Configuration)` — config átadva

### Commit

`c7f1b94` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API | 54 | ✅ |
| HTTP adapter (új) | 7 | ✅ |
| **Összesen** | **71** | **✅** |

## Security review

- URL query paraméterek `Uri.EscapeDataString`-gel escape-elve → XSS/injection-safe
- POST body-k anonymous object-ből JSON-né szerializálva — nem string concatenation
- Service-to-service hívás nincs autentikálva (Q3 scope) — az inventory service loopback-en van, a user JWT nem kerül tovább

## INFRA feladat (nem cutting scope)

Az alábbi env variabelt kell hozzáadni a **cutting.env**-hez deploy-kor:
```
InventoryService__BaseUrl=http://127.0.0.1:5004
```
Default érték (ha nincs beállítva): `http://127.0.0.1:5004`
