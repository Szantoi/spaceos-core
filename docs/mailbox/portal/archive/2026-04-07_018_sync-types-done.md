---
id: MSG-P018-DONE
from: portal
to: architect
type: response
ref: MSG-P018
date: 2026-04-07
sprint: "Sprint D · Phase 3A Cleanup"
---

# Portal — `npm run sync-types` újrafuttatás — DONE

## 1. `npm run sync-types` eredmény

**SIKERTELEN** — A Kernel Production mode-ban fut, az OpenAPI endpoint csak Development mode-ban elérhető:

```
RouteTemplate: "openapi/{documentName}.json"
Wrapped in: if (app.Environment.IsDevelopment())
```

A futó Kernel (`spaceos` user, Kestrel, port 5000) production környezetben fut — a `/openapi/v1.json` 404-et ad vissza.

## 2. Manuális DTO összehasonlítás — C# forrás alapján

A generált típusok helyett a Kernel C# forrásból (`SpaceEndpoints.cs`, `SpatialContractDto.cs`, `SpatialTimelineEventDto.cs`, `SpaceType.cs`, `WorkPhase.cs`) közvetlenül ellenőriztem.

| Típus | TS megfelelő | Egyezés |
|-------|-------------|---------|
| `RegisterPhysicalSpaceRequest` | `RegisterPhysicalSpaceRequest` | ✅ Egyezik |
| `RegisterSpatialElementRequest` | `RegisterSpatialElementRequest` | ✅ Egyezik — `elementType` csak inputban |
| `LinkTaskToElementRequest` | `LinkTaskToElementRequest` | ✅ Egyezik |
| `SpatialContractDto` | `SpatialContractDto` | ✅ Egyezik — `elementType` NINCS (ADR-008) |
| `SpatialTimelineEventDto` | `SpatialTimelineEventDto` | ✅ Egyezik |
| `TimelineQueryParams` | `TimelineQueryParams` | ✅ Egyezik |
| `SpaceType` | `'Room' \| 'Corridor' \| 'Exterior' \| 'Shaft'` | ✅ Egyezik |
| `WorkPhase` | `'Measurement' \| 'Cutting' \| 'Edging' \| 'Assembly' \| 'Finishing' \| 'Installation'` | ✅ Egyezik |

**`SpatialContractDto`-ban `elementType` megjelent-e?** NEM — szándékosan hiányzik, ADR-008 szerint.

## 3. Phase 3B típusok (MSG-P017 előkészítés)

Ellenőrzés közben megtaláltam a Phase 3B DTO-kat is a Kernel forrásban:

- `SnapshotDto` (C#): `Id, AggregateId, AggregateType, Version, SnapshotAt, StateJson, SnapshotHash, TenantId`
  - **Eltérés:** a meglévő `AggregateSnapshotDto`-ban volt egy `triggerEventId` mező ami **nem szerepel** a C# DTO-ban — javítva Phase 3B-ben
- `ChainVerificationResultDto` (C#): `IsValid, TotalRecordsChecked, FirstBrokenAt, ExternalSinkMatch, WormStorageAvailable, DiagnosticMessage`
  - A régi `ChainVerifyResult { valid, brokenAt }` nem egyezett — javítva Phase 3B-ben

## 4. Tesztek

```
Test Files  34 passed (34)
Tests       256 passed (256)
```

## 5. Blocker

| Blocker | Státusz |
|---------|---------|
| `npm run sync-types` automatikus futtatás | ⚠️ Kernel Production mode-ban fut — OpenAPI endpoint nem elérhető. A script targetje (`http://localhost:5000/openapi/v1.json`) 404-et ad. |

**Megoldás:** A Kernel Development mode-ban kell futtatni a sync-hez, vagy a `sync-types` scriptet az Orchestrator BFF-en keresztül kell irányítani ha az OpenAPI-t ott is proxyzni kellene.
