---
id: MSG-JOINERY-051
from: root
to: joinery
type: task
priority: high
status: READ
ref: MSG-JOINERY-050-DONE
created: 2026-04-20
---

# JOINERY Phase 2 — Batch PDF + Anyaglista

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Timeline:** ~3 nap
> **Blokkoló:** JOINERY Phase 1 ✅ DONE (460fce9)

---

## Kontextus

A Phase 1-ben elkészült az egyedi gyártásilap PDF generálás. A Phase 2 két funkciót ad hozzá:

1. **Batch generálás** — egy rendeléshez tartozó összes gyártásilap egyszerre generálható (Doorstar portal "Összes PDF letöltése" gomb)
2. **Anyaglista PDF** — aggregált anyagfelhasználási lista egy rendeléshez (supplier/beszállítói nézet)

---

## Feladat 1 — Batch PDF generálás

### Domain

**Fájl:** `Domain/Core/GyartasilapBatch.cs`

```csharp
public sealed class GyartasilapBatch : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid TenantId { get; private set; }
    public IReadOnlyList<Guid> GyartasilapIds { get; private set; }
    public BatchStatus Status { get; private set; }  // Pending/Generating/Ready/Failed
    public string? ZipStoragePath { get; private set; }

    public static GyartasilapBatch Create(Guid orderId, Guid tenantId, IReadOnlyList<Guid> ids);
    public void MarkReady(string zipPath);
    public void MarkFailed();
}
```

### Application

- `GenerateBatchCommand` + handler — iterálja a gyártásilap ID-kat, ZIP-be tömöríti a PDF-eket, MinIO-ba menti
- `GetBatchStatusQuery` + handler — batch státusz lekérdezése

### API endpoints

```csharp
// POST /api/gyartasilap/batch
// GET  /api/gyartasilap/batch/{batchId}/status
// GET  /api/gyartasilap/batch/{batchId}/download  → redirect to MinIO presigned URL
```

### Migration

```bash
dotnet ef migrations add AddGyartasilapBatch \
  --project SpaceOS.Modules.Joinery.Infrastructure \
  --startup-project SpaceOS.Modules.Joinery.Api
```

---

## Feladat 2 — Anyaglista PDF

### Domain

**Fájl:** `Domain/Services/IAnyaglistaPdfBuilder.cs`

Az anyaglista egy rendelés összes termékéhez szükséges alapanyagokat összesíti:
- Fa alapanyagok (típus, vastagság, m²)
- Furatok, élek, szerelvények összesítve
- Szállítói kód + mennyiség

### Application

- `GenerateAnyaglistaCommand` + handler
- `GetAnyaglistaQuery` + handler

### Infrastructure

**Fájl:** `Infrastructure/Documents/AnyaglistaPdfBuilder.cs`

QuestPDF builder — táblázatos nézet, szállítói csoportosítás.

### API endpoints

```csharp
// POST /api/anyaglista/generate   → generál + MinIO-ba ment
// GET  /api/anyaglista/{orderId}  → letöltési link (presigned URL)
```

---

## Tesztek (min. 30 új)

- `GyartasilapBatch` FSM tesztek (Create, MarkReady, MarkFailed)
- `GenerateBatchCommandHandler` — sikeres batch, részleges hiba kezelés
- `GetBatchStatusQuery` — státusz lekérdezés
- `AnyaglistaPdfBuilder` — tartalom ellenőrzés
- `GenerateAnyaglistaHandler` — happy path
- Endpoint tesztek (201, 200, 404, 401)

---

## Definition of Done

- [ ] `GyartasilapBatch` aggregate + migration
- [ ] `GenerateBatchCommand` + handler + ZIP generálás
- [ ] `GetBatchStatusQuery` + download redirect
- [ ] `AnyaglistaPdfBuilder` QuestPDF
- [ ] `GenerateAnyaglistaCommand` + `GetAnyaglistaQuery`
- [ ] 5 új endpoint regisztrálva
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥374 pass (344 + 30)
- [ ] Outbox DONE üzenet küldve
