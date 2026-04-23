---
id: MSG-CUTTING-010
from: root
to: cutting
type: task
priority: medium
status: READ
ref: MSG-CUTTING-009-DONE
created: 2026-04-16
---

# MSG-CUTTING-010 — IInventoryProvider HTTP adapter (stub → valódi cross-service hívás)

## Háttér

A CUTTING-009-ben `InventoryProviderStub` lett regisztrálva — ez üres adatot ad vissza,
a nesting handler graceful degradation-nel 200-t ad (grouping-only, `panelAssignments: null`).

Q3 scope: a cutting service-nek valódi lapkészlet-adatot kell kapnia az inventory service-től,
hogy a Nesting L1 FFDH algoritmus valódi panelekre optimalizáljon.

## Feladat — InventoryProviderHttpAdapter

### 1. HTTP adapter létrehozása

```
SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryProviderHttpAdapter.cs
```

Az adapter az inventory service-t hívja HTTP-n:
- Base URL: `http://127.0.0.1:5004` (config-ból olvasva)
- Autentikáció: a cutting service a saját JWT tokenét **nem** adja tovább
  (service-to-service hívás, nincs user token a context-ben) —
  az inventory service-t egyelőre **nyitottnak kell kezelni** a loopback-en
  (vagy `AllowAnonymous` a belső route-on, ha szükséges)

```csharp
// SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryProviderHttpAdapter.cs

internal sealed class InventoryProviderHttpAdapter : IInventoryProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryProviderHttpAdapter> _logger;

    public InventoryProviderHttpAdapter(HttpClient http, ILogger<InventoryProviderHttpAdapter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<PanelStockDto>(
                $"/api/inventory/stock?materialType={Uri.EscapeDataString(materialType)}", ct)
                .ConfigureAwait(false);
            return response ?? new PanelStockDto(materialType, 0, 0, 0, []);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory service unavailable for materialType {MaterialType}", materialType);
            return new PanelStockDto(materialType, 0, 0, 0, []);
        }
    }

    public async Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<OffcutDto>>(
                $"/api/inventory/offcuts?materialType={Uri.EscapeDataString(materialType)}", ct)
                .ConfigureAwait(false);
            return response ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory service unavailable for offcuts {MaterialType}", materialType);
            return [];
        }
    }

    // RecordConsumptionAsync, RecordInboundAsync, RecordOffcutAsync, GetConsumptionTrendAsync
    // → POST/GET hívások az inventory API-ra, ugyanolyan try/catch + log mintával
}
```

### 2. DI regisztráció — ServiceCollectionExtensions.cs

```csharp
// InventoryProviderStub helyett:
services.AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>(client =>
{
    client.BaseAddress = new Uri(configuration["InventoryService:BaseUrl"]
        ?? "http://127.0.0.1:5004");
});
```

### 3. Config — cutting.env (INFRA fogja hozzáadni deploy-kor)

```
InventoryService__BaseUrl=http://127.0.0.1:5004
```

Ez az INFRA feladat, nem a te dolgod — csak dokumentáld az outbox-ban.

### 4. Inventory API endpoint ellenőrzés

Ellenőrizd, hogy az inventory service `GET /api/inventory/stock` és `GET /api/inventory/offcuts`
endpoint-ok milyen query paramétereket fogadnak:

```bash
grep -rn "MapGet\|stock\|offcuts" /opt/spaceos/spaceos-modules-inventory/src/SpaceOS.Modules.Inventory.Api/
```

Ha az URL eltér, igazítsd az adaptert.

## Tesztek

- Az `InventoryProviderStub` maradhat a tesztekben (`TestAuthHandler` + unit tesztek) —
  a HTTP adapter csak production kódban kerül regisztrálásra
- Írj legalább 1 unit tesztet az adapter-re (mock HttpClient):
  ha az inventory elérhető → adatot ad vissza
  ha az inventory nem elérhető (exception) → üres listát/default-ot ad vissza (graceful)

## DoD

- [ ] `InventoryProviderHttpAdapter` létrehozva, minden interfész metódus implementálva
- [ ] DI: `AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>` regisztrálva
- [ ] `InventoryService:BaseUrl` config kulcs dokumentálva az outbox-ban (INFRA fogja hozzáadni)
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → legalább 64 teszt zöld (új tesztek hozzáadva)
- [ ] Commit
- [ ] Outbox: `MSG-CUTTING-010-DONE`
