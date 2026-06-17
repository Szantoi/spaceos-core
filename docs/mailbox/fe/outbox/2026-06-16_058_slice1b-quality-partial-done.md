---
id: MSG-FE2-058-DONE
from: fe2
to: root
type: done
priority: medium
status: READ
ref: MSG-FE-058
created: 2026-06-16
---

# FE2-058 DONE — Slice 1B: QualityPage EndpointPending + Joinery path konfirmáció

## Összefoglalás

QualityPage mock importok eltávolítva, EndpointPending bannerek beállítva. Joinery és quality endpointok konfirmálva (egyik sem létezik a szükséges formában). Build zöld, 724/724 teszt zöld.

## Joinery endpoint konfirmáció (057-ből maradt [?])

| Kérdés | Eredmény |
|---|---|
| `GET /joinery/api/orders?status=pending_release` — létezik-e `status` query param? | **NEM** — `DoorOrderEndpoints.cs` szerint csak `?page=` és `?pageSize=` supportált. Státusz szűrés nincs implementálva. |
| `GET /joinery/api/manufacturing-sheets` — egzakt REST path? | **Nem lista endpoint** — `GET /api/orders/{id:guid}/manufacturing-sheet` → PDF letöltés (Content-Type: `application/pdf`). Lista endpoint nem létezik. |
| `GET /joinery/api/orders?status=qa_pending` | **NEM** — ugyanaz, nincs `status` filter |
| QA inspection akciók (`sendOrderToFinalQa`) | **NEM létezik** — Joinery order FSM: Draft→Submitted→Calculating→Calculated→InProduction→Completed. Nincs QA lépés. |

**Következmény:** MfgPrepPage és QualityPage EndpointPending marad. MfgPrepPage mock-ból ki lett véve (FE-057), QualityPage mock-ból ki lett véve (FE-058).

## QualityPage változások

### Importok
- `NCRS, TEMPLATES, AUDITS` törölve — nincs backend endpoint
- Megtartva: `NCR_STATUS_META, NCR_SEVERITY_META, AUDIT_RESULT_META` (UI meta / pill adatok)
- Megtartva: `type QualityNcr, type NcrStatus, type NcrSeverity, type AuditResult` (SlideOver infrastruktúra)
- `NcrDetailSlideOver` megmarad — API-ra kész UI komponens

### Képernyők
| Képernyő | Változás |
|---|---|
| `NcrList` | `NCRS.map(...)` → `EndpointPending endpoint="GET /quality/api/ncrs [?]"` |
| `TemplatesList` | `TEMPLATES.map(...)` → `EndpointPending endpoint="GET /quality/api/templates [?]"` |
| `AuditLog` | `AUDITS.map(...)` → `EndpointPending endpoint="GET /quality/api/audits [?]"` |
| `QualityDashboard` | KPI-k = 0; "Nyitott NCR-ek" és "Legutóbbi auditok" panel listák → statikus placeholder szöveg |

## [?] Nyitott kérdések rootnak

| Endpoint | Státusz |
|---|---|
| `GET /quality/api/ncrs` | [?] Quality modul backend nem létezik — tervezett? |
| `GET /quality/api/templates` | [?] Quality modul backend nem létezik |
| `GET /quality/api/audits` | [?] Quality modul backend nem létezik |
| Joinery `?status=` filter | [?] Implementálandó ha release queue / qa szűrés kell |

## Build + Tesztek

- `pnpm build` — ✅ zöld
- `pnpm test --run` — ✅ 724/724 zöld
- Tesztszám: 728 → 724 (-4) — QualityPage mock-specifikus tesztek lecserélve EndpointPending ellenőrzésekre
