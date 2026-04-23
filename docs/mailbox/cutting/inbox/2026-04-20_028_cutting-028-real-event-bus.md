---
id: MSG-CUTTING-028
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-INVENTORY-057 (INVENTORY Phase 1 DONE)
created: 2026-04-20
---

# CUTTING-028 — Real Integration Event Bus: CuttingJobCompletedEvent

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → E2E → outbox
> **Agent eszközök:** engedélyezve
> **Sub-agent:** engedélyezett ha szükséges

## Kontextus

Az INVENTORY Phase 1 (150/150 teszt, DONE ✅) megvalósította a `CuttingJobCompletedEvent` handlert
az INVENTORY modulban — de **stub/mock** alapon: a teszt manuálisan publisholja az eventet.

CUTTING-028 célja: **valódi event publishing** a Cutting modulból, amikor egy `CuttingJob.Status → "Cut"` átmenet történik.

## Scope — 4 feladat

### 1. ICuttingEventPublisher interface (CUTTING modulban)

```csharp
// SpaceOS.Modules.Cutting/Application/Events/ICuttingEventPublisher.cs
public interface ICuttingEventPublisher
{
    Task PublishJobCompletedAsync(
        Guid jobId,
        Guid tenantId,
        Guid cuttingSheetId,
        DateTime completedAt,
        decimal yieldPct,
        decimal wasteM2,
        CancellationToken ct = default);
}
```

### 2. Status átmenet trigger — CuttingJob "Cut" státusz

Keresd meg ahol `CuttingJob.Status` → `"Cut"` átmenet történik (valószínűleg egy command handler).
Az átmenet után injektált `ICuttingEventPublisher` hívása:

```csharp
await _eventPublisher.PublishJobCompletedAsync(
    job.Id, job.TenantId, job.CuttingSheetId,
    DateTime.UtcNow, job.YieldPct, job.WasteM2, ct);
```

### 3. Implementáció — cross-service HTTP

Mivel CUTTING (:5005) és INVENTORY (:5004) külön .NET service-ek:

**A) INVENTORY-ban: integration event endpoint**

Ellenőrizd, hogy az INVENTORY Day 2 handler (`CuttingJobCompletedEvent`) elér-e HTTP-n keresztül.
Ha nincs dedikált integration endpoint, add hozzá:

```
POST /api/inventory/integration/cutting-job-completed
Body: { jobId, tenantId, cuttingSheetId, completedAt, yieldPct, wasteM2 }
Auth: Internal service-to-service (X-Internal-Service: cutting, vagy meglévő auth)
Response: 202 Accepted
```

**B) CUTTING-ban: HttpClient implementáció**

```csharp
// CuttingEventPublisher.cs — ICuttingEventPublisher implementation
public class CuttingEventPublisher : ICuttingEventPublisher
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    
    public async Task PublishJobCompletedAsync(...)
    {
        var url = _config["Inventory:BaseUrl"] + "/api/inventory/integration/cutting-job-completed";
        var payload = new { jobId, tenantId, cuttingSheetId, completedAt, yieldPct, wasteM2 };
        var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
        response.EnsureSuccessStatusCode();
    }
}
```

> **Fontos:** Ha a codebase-ben már van cross-module event/integration pattern (pl. shared MediatR, outbox table, vagy meglévő HTTP service client), azt kövesd — ne vezess be új patterneket szükségtelenül.

### 4. INVENTORY Day 2 stub eltávolítása

Az INVENTORY teszt-fájlban (Day 2, CuttingJobCompletedEvent handler tesztek) a manuálisan publisholt eventet
cseréld valódi integrációs tesztre — ahol a CUTTING HTTP endpoint meghívása triggereli a handlert.

Ha ez az INVENTORY repo-ban van, küldj egy kiegészítő üzenetet az INVENTORY terminálnak, hogy frissítse a Day 2 stub teszteket.

## Acceptance Criteria

| Kritérium | Pass |
|---|---|
| `ICuttingEventPublisher` interface definiálva CUTTING Application rétegben | ✅ |
| CuttingJob status → "Cut" trigger bekötve | ✅ |
| `POST /api/inventory/integration/cutting-job-completed` endpoint INVENTORY-ban (ha nem volt) | ✅ |
| `CuttingEventPublisher` HttpClient implementáció registered DI-ban | ✅ |
| Integrációs teszt: CuttingJob "Cut" → INVENTORY handler meghívódik (real HTTP, in-memory DB) | ✅ |
| Build: 0 error, 0 warning | ✅ |
| Minden meglévő teszt zöld marad | ✅ |

## Effort becslés

~2 fejlesztői nap

## Timeline

JOINERY Phase 1-gyel párhuzamosan futhat — különböző modul, nincs technikai függőség.

## DONE outbox

`docs/mailbox/cutting/outbox/YYYY-MM-DD_NNN_cutting-028-event-bus-done.md`

Tartalmazza:
- Commit hash
- Melyik fájlok módosultak
- Teszt count (cutting + inventory)
- Integrációs teszt eredmény: CuttingJob → INVENTORY handler ✅
- Ha INVENTORY stub tesztet módosítottad: INVENTORY teszt count is
