---
id: MSG-ARCH-009-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-006
created: 2026-04-24
---

## Összefoglalás

Elkészült a `docs/architecture/SpaceOS_Joinery_Phase3_Architecture_v1.md` — Joinery Phase 3 Portal integráció spec. Mind a 8 kért pont lefedve.

## Kulcsdöntések

| Kérdés | Döntés |
|--------|--------|
| Melyik portálon? | **Doorstar Portal** (`portal.joinerytech.hu`) — az aktív gyártói felület |
| Hol a UI-ban? | **OrderDetailPage** — "Dokumentumok" szekció a meglévő gombsor alatt |
| BFF route? | **4 új Orchestrator proxy route** (gyartasilap batch + anyaglista) |
| Auth? | **ManufacturerOnly** — már enforced a Joinery API-ban, BFF JWT-t forward-ol |
| Batch UX? | **Polling 2s/max 60** — SSE overkill <10s batch generálásra |
| Presigned URL? | **Nginx MinIO proxy** szükséges — MinIO localhost-on van, presigned URL-t a böngésző nem éri el közvetlenül |
| Anyaglista UX? | **Szinkron** — QuestPDF <1s, blob download |

## Effort

**Phase 3 MVP: 4.5 nap** (ORCH 0.5 + INFRA 0.25 + JOINERY 0.25 + PORTAL 3.0 + E2E 0.5)

**Végrehajtási sorrend:** JOINERY (MinIO host fix) → INFRA (Nginx proxy) → ORCH (BFF route-ok) → PORTAL (gombok) → E2E

## Fontos finding: MinIO presigned URL elérhetőség

A MinIO `127.0.0.1:9000`-on hallgat. A presigned URL `localhost:9000`-re mutat — a böngészőből NEM érhető el. **Nginx reverse proxy szükséges** (`/minio/` location → `127.0.0.1:9000`), és a Joinery MinIO client endpoint konfigurációját módosítani kell hogy a presigned URL a publikus domain-re mutasson.

## Nyitott kérdések

1. Joinery MinIO client endpoint konfigurációja — hogyan van az appsettings-ben?
2. `POST /api/gyartasilap/batch` body: `orderIds` array vagy egyetlen `orderId`?
3. Design Portal Joinery route-ok: Phase 3 vagy 3.5?
