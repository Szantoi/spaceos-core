---
id: MSG-O009
from: architect
to: orchestrator
type: task
status: UNREAD
priority: P1
sprint: "Sprint D · Phase 3A"
---

# Phase 3A — Spatial BIM értesítés + BFF route-ok + Security review

A Kernel Phase 3A implementációja (Spatial BIM Core) kész. Az alábbi feladatokat kell elvégezni az Orchestrator oldalán.

---

## Kontextus — Új Kernel endpoint-ok

A Kernel API-ban 5 új endpoint érhető el:

| Method | Kernel route | Leírás |
|--------|-------------|--------|
| `POST` | `/api/spaces` | Fizikai tér regisztráció |
| `POST` | `/api/spaces/{id}/elements` | Spatial elem regisztráció (BVH insert) |
| `POST` | `/api/elements/{id}/links` | FlowTask–SpatialElement összerendelés |
| `GET`  | `/api/spaces/{id}/timeline` | 4D snapshot lekérdezés (`?at=` timestamp) |
| `GET`  | `/api/spaces/{id}/timeline/events` | 4D timeline események (`?from=&to=&page=`) |

Autentikáció: `RequireAuthorization()` — JWT `tid` claim alapján tenant scope.

---

## 1. BFF proxy route-ok hozzáadása

Add hozzá az alábbi route-okat a Kernel-proxy / federation-proxy mellé:

```typescript
// src/routes/spatial.proxy.ts (vagy meglévő kernel.proxy.ts bővítése)

router.post('/bff/spaces',
  requireAuth,
  (req, res) => kernelProxy.forward(req, res, 'POST', '/api/spaces'));

router.post('/bff/spaces/:id/elements',
  requireAuth,
  (req, res) => kernelProxy.forward(req, res, 'POST', `/api/spaces/${req.params.id}/elements`));

router.post('/bff/elements/:id/links',
  requireAuth,
  (req, res) => kernelProxy.forward(req, res, 'POST', `/api/elements/${req.params.id}/links`));

router.get('/bff/spaces/:id/timeline',
  requireAuth,
  (req, res) => kernelProxy.forward(req, res, 'GET', `/api/spaces/${req.params.id}/timeline`));

router.get('/bff/spaces/:id/timeline/events',
  requireAuth,
  (req, res) => kernelProxy.forward(req, res, 'GET', `/api/spaces/${req.params.id}/timeline/events`));
```

Követelmények:
- JWT Bearer token átadása a Kernel felé (`Authorization` header forwarding)
- `X-SpaceOS-Brand` header forwarding (SourceBrand) — mint a többi BFF route-nál
- Query string paraméterek (`?at=`, `?from=`, `?to=`, `?page=`, `?pageSize=`) passthrough

---

## 2. KernelClient bővítés

Ha létezik `KernelClient` típusos wrapper, add hozzá:

```typescript
// Spatial methods
async registerSpace(body: RegisterPhysicalSpaceRequest): Promise<SpaceResponse>
async registerElement(spaceId: string, body: RegisterSpatialElementRequest): Promise<ElementResponse>
async linkTask(elementId: string, body: LinkTaskToElementRequest): Promise<void>
async getSpatialSnapshot(spaceId: string, at: string): Promise<SpatialSnapshotResponse>
async getSpatialEvents(spaceId: string, params: TimelineQueryParams): Promise<PagedResult<SpatialTimelineEvent>>
```

**Fontos:** `SpatialSnapshotResponse` DTO-ban **nincs** `elementType` mező — ez szándékos (ADR-008, security döntés). Ne adjuk hozzá.

---

## 3. Tool Registry — Spatial tool-ok (opcionális, Phase 3B scope)

Ha a Gemini chat-nek szüksége van spatial tool-okra, az alábbiak regisztrálhatók a Tool Registry-ben:

```typescript
{
  name: "get_spatial_snapshot",
  description: "Get the spatial state of a physical space at a given point in time",
  parameters: {
    spaceId: { type: "string" },
    at: { type: "string", description: "ISO 8601 timestamp" }
  }
}
```

Ez Phase 3B scope — most nem kötelező implementálni, de a registry struct előkészíthető.

---

## 4. Security review

### 4.1 JWT forwarding ellenőrzés
```bash
# BFF spatial route-on a JWT Bearer token átadásra kerül-e a Kernel felé?
# Nincs-e olyan eset, hogy a BFF saját tenant kontextust használ a JWT helyett?
```
- [ ] JWT `tid` claim nem módosul a BFF-en áthaladva
- [ ] `Authorization: Bearer <token>` header változatlan marad

### 4.2 Request body méret limit
A Kernel oldali Kestrel limit 64KB (Phase 2, T-06). Az Orchestrator Express oldal:
- [ ] `express.json({ limit: '64kb' })` vagy szigorúbb van-e beállítva?
- Ha nincs explicit limit: `express.json({ limit: '64kb' })` hozzáadása ajánlott

### 4.3 Path traversal a proxy route-okban
```typescript
// Ellenőrizd: req.params.id-t közvetlenül a path-ba építed?
// Ha igen, sanitizálás szükséges (UUID formátum validáció)
```
- [ ] Space ID és Element ID UUID formátum validálva van (pl. uuid4 regex) mielőtt proxy path-ba kerül
- Ajánlott: `if (!/^[0-9a-f-]{36}$/i.test(req.params.id)) return res.status(400).json(...)`

### 4.4 Rate limiting — spatial endpoint-ok
- [ ] Spatial POST endpoint-ok (RegisterSpace, RegisterElement) az általános RL policy alá esnek?
- Ha külön RL bucket szükséges (pl. write-heavy), jelezd az outbox-ban

### 4.5 Nincs SSE stream a spatial route-okon
- GET `/bff/spaces/:id/timeline/events` egy normál JSON paginated response, **nem SSE**
- [ ] `proxy_buffering off` Nginx-en **nem szükséges** erre a route-ra (különbség a chat SSE-től)

---

## 5. Teszt feladatok

```bash
npm test
```

Elvárt: minden meglévő teszt zöld marad. Ha az új route-ok bevezetésre kerülnek, írj hozzájuk teszteket:

- `spatial.proxy.test.ts`:
  - POST /bff/spaces → Kernel POST /api/spaces (JWT forwarding, Brand header)
  - GET /bff/spaces/:id/timeline → query string passthrough
  - UUID validáció: `/bff/spaces/not-a-uuid/timeline` → 400

---

## Elvárt outbox üzenet

Küldj vissza `type: response` üzenetet `ref: MSG-O009` hivatkozással:
- BFF route-ok implementálva? igen/nem + fájlnév
- KernelClient bővítve? igen/nem
- Security review pontok ✅ / ⚠️ eltérés
- Teszt eredmény (pass/fail count)
- Bármilyen blocker vagy kérdés
