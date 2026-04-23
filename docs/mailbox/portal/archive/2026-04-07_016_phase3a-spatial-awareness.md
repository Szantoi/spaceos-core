---
id: MSG-P016
from: architect
to: portal
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3A"
---

# Phase 3A — Spatial BIM értesítés + contract sync + Security review

A Spatial BIM Core (Phase 3A) implementáció a Kernel oldalán kész. Az Orchestrator BFF route-ok szintén készülnek (MSG-O009). Az alábbi feladatokat kell elvégezni a Portal oldalán.

---

## Kontextus — Új endpoint-ok (BFF-en keresztül)

| BFF route | Leírás | Auth |
|-----------|--------|------|
| `POST /bff/spaces` | Fizikai tér regisztráció | Bearer JWT |
| `POST /bff/spaces/:id/elements` | Spatial elem hozzáadás | Bearer JWT |
| `POST /bff/elements/:id/links` | FlowTask–Spatial összerendelés | Bearer JWT |
| `GET /bff/spaces/:id/timeline?at=` | 4D snapshot (adott időpillanat) | Bearer JWT |
| `GET /bff/spaces/:id/timeline/events?from=&to=&page=` | 4D timeline események | Bearer JWT |

---

## 1. OpenAPI contract sync

Futtasd az OpenAPI sync-et, hogy a TypeScript típusok naprakészek legyenek az új spatial endpoint-okhoz:

```bash
npm run sync-types
```

Ellenőrizd, hogy az alábbi típusok megjelennek a generált fájlokban:
- `RegisterPhysicalSpaceRequest`
- `RegisterSpatialElementRequest`
- `LinkTaskToElementRequest`
- `SpatialSnapshotResponse` / `SpatialContractDto`
- `SpatialTimelineEventDto`
- `PagedResult<SpatialTimelineEventDto>`

**Fontos:** `SpatialContractDto`-ban **nincs** `elementType` mező — ez szándékos (ADR-008, security döntés). Ha a generált típusban megjelenne, jelezd — ez hibát jelent.

---

## 2. Előkészítés — SpaceViewer (Phase 3B scope)

A Phase 3A nem tartalmaz Portal oldali UI implementációt — ez Phase 3B feladata. De az előkészítés most elvégezhető:

### API hook előkészítés
Hozd létre (vagy tervezd meg) az alábbi React Query hook-okat:

```typescript
// src/hooks/useSpatialSnapshot.ts
export function useSpatialSnapshot(spaceId: string, at: string)

// src/hooks/useSpatialTimeline.ts
export function useSpatialTimeline(spaceId: string, params: TimelineQueryParams)

// src/hooks/useRegisterSpace.ts
export function useRegisterSpace()

// src/hooks/useRegisterElement.ts
export function useRegisterElement(spaceId: string)
```

Ezek a hook-ok legyenek elkészítve (akár stub implementációval), hogy Phase 3B-ben közvetlenül a komponensekbe lehessen kötni.

### Router előkészítés
Tervezd meg a route struktúrát (még nem kell implementálni):
```
/spaces                     → SpaceListPage (Phase 3B)
/spaces/:id                 → SpaceDetailPage (Phase 3B)
/spaces/:id/timeline        → SpatialTimelinePage (Phase 3B)
```

---

## 3. Security review

### 3.1 ElementType nincs a UI-ban
Az API response (`SpatialContractDto`) szándékosan nem tartalmaz `elementType` mezőt (ADR-008).
- [ ] Ellenőrizd: a Portal sehol nem próbálja megjeleníteni az `elementType` értéket spatial context-ben
- [ ] Ha van placeholder vagy TODO comment ami `elementType`-ra vár: távolítsd el vagy jelezd

### 3.2 Tenant ID nem kerül a URL-be
A spatial endpoint-ok JWT `tid` claim alapján szűrik a tenant adatokat — a Portal nem kell hogy a tenant ID-t a request body-ba vagy URL-be tegye (azt a Kernel kinyeri a JWT-ből).
- [ ] Ellenőrizd: a tervezett hook-ok nem adnak `tenantId` query paramétert a spatial route-okhoz

### 3.3 SpaceId / ElementId UUID validáció
Mielőtt bármilyen spatial API hívás megvalósításra kerül:
- [ ] A `spaceId`, `elementId` paraméterek UUID formátumra validálva legyenek a hook-okban (ne kerüljön üres string vagy `undefined` az API hívásba)
- Ajánlott: `if (!spaceId || !/^[0-9a-f-]{36}$/i.test(spaceId)) return` pattern

### 3.4 BoundingBox értékek — integer overflow
A `BoundingBox` koordinátái `int` típusúak (mm-ben). A Portal oldalon:
- [ ] Ha a user manuálisan adja meg a koordinátákat, legyen range validáció: `min: 0, max: 2_000_000` (2 km — ésszerű épület méret)
- [ ] Ne lehessen negatív vagy NaN értéket elküldeni

### 3.5 Meglévő tesztek zöldek maradnak
```bash
npm test
```
- [ ] Minden meglévő teszt pass marad a contract sync után
- [ ] Ha a generált típusok megváltoznak és megtörnek meglévő teszteket: jelezd az outbox-ban

---

## 4. Meglévő tesztek ellenőrzése

```bash
npm test -- --reporter=verbose
```

Elvárt: `256 pass / 0 fail` (Phase 3A nem érint Portal funkcionalitást).

Ha az OpenAPI sync megváltoztat meglévő típusokat és ez teszttörést okoz:
- Fix a típushasználatban szükséges
- Jelezd az outbox-ban hogy melyik fájlban

---

## 5. Kapcsolódó ADR-ok — ismerd meg

- **ADR-008** — `SpatialContractsView` ElementType absent: a view szándékosan kizárja az elemtípust a 4D timeline view-ból. Indok: az elemtípus a BVH fastruktúrából következtethető, de a timeline snapshot context-ben nem releváns és information leakage kockázatot jelent.

---

## Elvárt outbox üzenet

Küldj vissza `type: response` üzenetet `ref: MSG-P016` hivatkozással:
- `npm run sync-types` sikerült? + generált típusokban van-e `elementType`?
- Meglévő tesztek: pass/fail count
- Hook stub-ok elkészültek? igen/nem + fájllistával
- Security review pontok ✅ / ⚠️ eltérés
- Bármilyen blocker vagy kérdés
