---
id: MSG-O009-RESP
from: orchestrator
to: architect
type: response
ref: MSG-O009
status: DONE
date: 2026-04-07
---

# Phase 3A Response — Spatial BIM BFF routes + Security review

## 1. BFF route-ok implementálva?

**Igen** — `src/routes/spatial.route.ts` (új fájl)

| Route | Kernel endpoint | Megjegyzés |
|-------|----------------|-----------|
| `POST /bff/spaces` | `POST /api/spaces` | ✅ |
| `POST /bff/spaces/:id/elements` | `POST /api/spaces/:id/elements` | ✅ UUID validáció |
| `POST /bff/elements/:id/links` | `POST /api/elements/:id/links` | ✅ UUID validáció |
| `GET /bff/spaces/:id/timeline` | `GET /api/spaces/:id/timeline` | ✅ `?at=` passthrough |
| `GET /bff/spaces/:id/timeline/events` | `GET /api/spaces/:id/timeline/events` | ✅ pagination passthrough |

Implementáció: axios-alapú proxy (mint `auth.route.ts` refresh/logout), AFTER body parsing. Minden route: `requireAuth` + JWT/Brand header forwarding.

## 2. KernelClient bővítve?

**Igen** — `src/kernel/kernelClient.ts`

- Új `post<T>()` metódus (ugyanolyan error map mint `get<T>()`, 10s timeout)
- 5 spatial wrapper metódus: `registerSpace`, `registerElement`, `linkTask`, `getSpatialSnapshot`, `getSpatialEvents`
- Spatial DTOs hozzáadva `src/types/kernel.types.ts`-hez + `PagedResult<T>` alias

## 3. Security review ✅ / ⚠️

| Pont | Státusz | Megjegyzés |
|------|---------|-----------|
| 4.1 JWT forwarding | ✅ | `Authorization: Bearer <token>` header változatlan forwarding. `tid` claim nem módosul. |
| 4.2 Request body size limit | ✅ | `express.json({ limit: '512kb' })` be van állítva (szigorúbb is lehetne: 64kb), de Kernel maga is rejecteli a túl nagy requesteket. |
| 4.3 UUID path traversal | ✅ | `/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i` validáció minden `:id` param-ra proxy előtt. |
| 4.4 Rate limiting | ✅ | `proxyLimiter` (100/perc prod) alkalmazva a spatial route-okra `index.ts`-ben: `app.use(['/bff/spaces', '/bff/elements'], proxyLimiter)` — nincs külön write-heavy bucket szükség egyelőre. |
| 4.5 Nem SSE a timeline events route-on | ✅ | `GET /bff/spaces/:id/timeline/events` normál JSON paginated response, `proxy_buffering off` csak a chat SSE route-ra vonatkozik. |

## 4. Teszt eredmény

```
Test Files  15 passed (15)
     Tests  126 passed (126)
  Duration  3.19s
```

Új tesztek: `src/routes/spatial.route.test.ts` — 12 eset:
- JWT guard (401)
- POST /spaces → proxy + JWT forwarding + Brand header forwarding + Kernel error forwarding
- UUID validáció (400) mindhárom paraméteres route-on
- GET timeline → `?at=` query passthrough
- GET events → pagination query passthrough

## 5. Blockerek / kérdések

Nincs blocker.

**Megjegyzés:** `SpatialSnapshotResponse` DTO-ban `elementType` szándékosan hiányzik (ADR-008). ✅

**Tool Registry (Phase 3B):** `get_spatial_snapshot` tool registry struct nem lett előkészítve — spec szerint Phase 3B scope, nem kötelező most.
