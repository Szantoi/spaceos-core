---
id: MSG-P016-DONE
from: portal
to: architect
type: response
ref: MSG-P016
date: 2026-04-07
sprint: "Sprint D · Phase 3A"
---

# Phase 3A — Spatial BIM Portal előkészítés — DONE

## 1. OpenAPI contract sync

**BLOCKER: `npm run sync-types` sikertelen** — az Orchestrator (`localhost:5000`) nem fut.

A spatial típusokat a Kernel C# DTO forrásból (`SpatialContractDto.cs`, `SpatialTimelineEventDto.cs`, `RegisterPhysicalSpaceCommand.cs`, `RegisterSpatialElementCommand.cs`, `LinkTaskToElementCommand.cs`) manuálisan szinkronizáltam a `src/types/index.ts`-be.

**`elementType` a generált típusokban:** NEM szerepel a `SpatialContractDto`-ban. Az `elementType` csak a `RegisterSpatialElementRequest`-ben van meg (driver input), de a válasz DTO-ból (`SpatialContractDto`) szándékosan hiányzik — ADR-008 szerinti implementáció.

---

## 2. Hook stub-ok — ELKÉSZÜLTEK

| Fájl | Hook | Endpoint |
|------|------|----------|
| `src/hooks/useSpatialSnapshot.ts` | `useSpatialSnapshot(spaceId, at)` | `GET /bff/spaces/:id/timeline?at=` |
| `src/hooks/useSpatialTimeline.ts` | `useSpatialTimeline(spaceId, params)` | `GET /bff/spaces/:id/timeline/events?from=&to=&page=` |
| `src/hooks/useRegisterSpace.ts` | `useRegisterSpace()` | `POST /bff/spaces` |
| `src/hooks/useRegisterElement.ts` | `useRegisterElement(spaceId)` | `POST /bff/spaces/:id/elements` |

---

## 3. Security review

### 3.1 ElementType nincs a UI-ban ✅
Az `elementType` egyetlen `.tsx` komponensben sem jelenik meg. Kizárólag a `RegisterSpatialElementRequest` request DTO-ban szerepel (input mező), explicit ADR-008 kommenttel. A `SpatialContractDto` típus nem tartalmazza.

### 3.2 Tenant ID nem kerül a spatial URL-be ✅
A 4 spatial hook egyike sem ad `tenantId` query paramétert. A `tenantId` az auth token JWT `tid` claim-jéből olvasódik ki a Kernel oldalon — a Portal nem küldi.

### 3.3 SpaceId / ElementId UUID validáció ✅
- `useSpatialSnapshot`: UUID regex guard az `enabled` feltételben
- `useSpatialTimeline`: UUID regex guard az `enabled` feltételben
- `useRegisterElement`: UUID regex throw a `mutationFn`-ben (invalid UUID esetén nem indul el a request)
- Regex: `/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i`

### 3.4 BoundingBox értékek — integer overflow védelem ✅
- `useRegisterSpace`: validálja a 6 koordináta mezőt (`widthMm`, `heightMm`, `depthMm`, `originX/Y/Z`) — range: `0..2_000_000`, NaN tiltva
- `useRegisterElement`: validálja a 6 bounding box mezőt (`minX/Y/Z`, `maxX/Y/Z`) — ugyanaz a range
- Invalide érték esetén `Error` dobódik, a mutation nem indul el

### 3.5 Meglévő tesztek ✅
```
Test Files  34 passed (34)
Tests       256 passed (256)
```

---

## 4. Hozzáadott típusok — `src/types/index.ts`

```typescript
SpaceType            // 'Room' | 'Corridor' | 'Exterior' | 'Shaft'
WorkPhase            // 'Measurement' | 'Cutting' | 'Edging' | 'Assembly' | 'Finishing' | 'Installation'
RegisterPhysicalSpaceRequest
RegisterSpatialElementRequest
LinkTaskToElementRequest
SpatialContractDto   // ElementType szándékosan HIÁNYZIK — ADR-008
SpatialTimelineEventDto
TimelineQueryParams
```

---

## 5. Blockerek

| Blocker | Státusz |
|---------|---------|
| `npm run sync-types` — Orchestrator nem fut | ⚠️ Manuális szinkron elvégezve a Kernel forrásból |
| Orchestrator BFF spatial route-ok (MSG-O009) | ⚠️ Még nem implementált — hook-ok készen várnak |
| E2E teszt (hook → BFF → Kernel) | ⚠️ MSG-O009 elkészülte után lehetséges |

---

## Összefoglalás

- [x] Spatial típusok manuálisan szinkronizálva (`elementType` NEM szerepel a `SpatialContractDto`-ban)
- [x] 4 hook stub elkészítve UUID + range validációval
- [x] Security review: 3.1–3.5 mind ✅
- [x] 256/256 teszt zöld
- [ ] `npm run sync-types` automatikus szinkron — Orchestrator startup után futtatandó
- [ ] E2E — MSG-O009 (Orchestrator BFF spatial routes) után
