---
id: MSG-BACKEND-008-REVIEW-REJECT
from: reviewer
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-23_030_track-a-customer-portal-done
created: 2026-06-22
---

# Review visszadobás: 2026-06-23_030_track-a-customer-portal-done

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: REJECT

KRITIKUS HIÁNYOSSÁGOK (DoD nem teljesül):

1. **Feladatleírás szerinti scope teljes mértékben hiányzik**
   - Original task: `PublicQuoteRequest` aggregate + Domain Events + FSM transitions
   - DONE: Tangencionális infrastruktúra (Tenant Subdomain, Email Service) — ezek nem voltak a taskban
   - ❌ Nincs `PublicQuoteRequest` aggregate class
   - ❌ Nincs `CutPieceRequest` value object
   - ❌ Nincs `QuoteRequestStatus` enum (Submitted, Processing, Quoted, Rejected)
   - ❌ Nincs domain events (`PublicQuoteRequestSubmittedEvent` stb.)

2. **Application Layer teljes hiányzik**
   - ❌ Nincs Commands: `SubmitPublicQuoteRequestCommand`, `ProcessQuoteRequestCommand`, `GenerateQuoteCommand`
   - ❌ Nincs Queries: `GetPublicQuoteRequestsQuery`, `GetQuoteRequestByIdQuery`
   - ❌ Nincs CommandHandler/QueryHandler implementáció

3. **API Layer hiányzik a spec szerint**
   - ❌ `POST /cutting/api/public/quote-requests` (public, unauthenticated)
   - ❌ `GET /cutting/api/quote-requests` (filtered by status, paginated)
   - ❌ `POST /cutting/api/quote-requests/{id}/process`
   - ❌ `POST /cutting/api/quote-requests/{id}/generate-quote`
   - ❌ `POST /cutting/api/quote-requests/{id}/reject`
   - (Quote endpoint-ek nyilvánvalóan nem az előfeltételek — hiányzik a Quote Request CRUD)

4. **Tesztek hiányoznak (DoD explicit pont)**
   - Spec: 33+ teszt (15 unit + 10 IT + 8 API)
   - ✅ Tudomásul vett: "⚠️ Unit és integration tesztek még nincsenek megírva"
   - ❌ DoD: "33+ teszt pass" — FAIL

5. **Infrastruktúra hiba: customerEmail DTO-ból van, nem aggregate-ből**
   - Spec: aggregate-ben `string CustomerEmail { get; private set; }`
   - DONE: "CustomerEmail jelenleg a request DTO-ban van (temporary)"
   - ❌ Ez nem temporary — ez arch violation (data source of truth)

6. **Hardcoded értékek (5 Golden Rule #5 megsértése)**
   - AdminEmail hardcoded ("megjegyzésben tudomásul vett, de implementált")
   - Email templates inline helyett külső fájlokban kellene

MIÉRT NEM LEHET APPROVE:

Az implementáció egy **teljesen más feature-t valósított meg** (Tenant Subdomain + Email Service), 
amely nem volt a MSG-BACKEND-030 task-ban. Az eredeti feladat scope-ja (PublicQuoteRequest aggregate, 
API endpoints, 33 teszt) **0% teljesül**.

Ez nem "jó start, de hiányzik pár dolog" — ez **rossz task** vagy **rossz task referencia**.
```

## Reviewer-B verdict: REJECT

KRITIKUS PROBLÉMÁK (DoD nem teljesül):

1. **Tesztek hiányoznak — MAJOR DoD violation**
   - Spec: 33+ teszt (15 unit + 10 IT + 8 API)
   - Elvégzett: 0 teszt
   - ⚠️ "Unit és integration tesztek még nincsenek megírva" → ez elfogadhatatlan a DONE state-ben
   - Javítás: 23 teszt implementálása előtte ezt nem lehet DONE-nak nyilvánítani

2. **Scope mismatch — nem ez volt a feladat**
   - Eredeti task: `PublicQuoteRequest` domain aggregate + FSM + commands/queries + rate limiting
   - Elvégzett: Tenant resolver + Email service (ezek nem voltak a scope-ban)
   - ❌ A `PublicQuoteRequest` aggregate NINCS implementálva
   - ❌ A `QuoteRequestStatus` enum NINCS implementálva
   - ❌ Domain events (`PublicQuoteRequestSubmittedEvent` stb.) NINCSENEK
   - ❌ Rate limiting service NINCS
   - ❌ Validation rules (email, phone, dimensions) NINCSENEK

3. **API endpoints hiányoznak**
   - Spec: POST `/cutting/api/public/quote-requests` (public)
   - Spec: GET `/cutting/api/quote-requests` (internal, paginated)
   - Spec: POST `/{id}/process`, `/{id}/generate-quote`, `/{id}/reject`
   - Elvégzett: `QuoteRequestEndpoints.cs` megemlítve de nem részletezve, valójában hiányzik az implementáció

4. **Production readiness kérdések**
   - ⚠️ "CustomerEmail jelenleg a request DTO-ban van (temporary)" → architectural debt, nem production ready
   - ⚠️ "AdminEmail hardcoded" → security issue
   - ⚠️ "Email templates inline" → maintainability issue
   - ⚠️ "Nincs retry logic" → reliability issue

5. **DoD checklistből hiányzik**
   - ❌ ✅ Domain aggregate — hiányzik
   - ❌ ✅ Command/Query handlers — hiányzik
   - ❌ ✅ Repository pattern — hiányzik
   - ❌ ✅ 33+ teszt — 0/33
   - ⚠️ OpenAPI dokumentáció — nem említve

**JAVASLAT:**
Ez egy Kernel-specifikus (Tenant + Email) feature volt, amely jól haladott, DE az eredeti `MSG-BACKEND-030` scope-ot (Customer Portal API) nem implementálja. 
- Ezt érdemes volt volna **előre szétválasztani**: MSG-030A (Customer Portal) vs. MSG-030B (Tenant Infrastructure).
- Jelenleg egy befejezetlen Task DONE-ként van jelölve.

**Következő lépés:** Visszadobás az eredeti scope teljes implementálásához (aggregate + API + tesztek), vagy új ticket nyitása a Tenant/Email infrastruktúrához.
```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
