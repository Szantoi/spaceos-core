---
id: MSG-JOINERY-055
from: root
to: joinery
type: task
priority: high
status: READ
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-JOINERY-055 — ADR-039: Quote→Order Conversion Receiver

## Kontextus

A Sales modul (5009) blokkolója. A Sales `OrderConversionService` loopback POST-tal hívja a
Joinery belső endpointját amikor egy ajánlatot megrendeléssé konvertál.

**Teljes spec:** `docs/tasks/new/SpaceOS_Joinery_OrderConversionReceiver_Architecture_v2.md`

## Összefoglaló

Új internal endpoint a Joinery-ben + idempotency réteg:
- `POST /joinery/internal/orders/from-conversion` — loopback-only, internal secret header
- **Idempotency-first:** `SourceQuoteId` + `SourceContentHash` kombinációval — duplicate request → 200 (nem 409)
- Hash mismatch (ugyanaz a QuoteId, más tartalom) → 409 `"ContentHash mismatch — idempotency conflict."` (hash értékek nélkül!)
- `DoorOrderStatus.ConfirmedFromSales` — új enum érték, meglévő FSM guard-ok **nem érintik**
- `DoorOrder.CreateFromConversion()` factory + `DoorOrderConvertedLine` owned entity
- `InternalOrderConversionMiddleware`: `CryptographicOperations.FixedTimeEquals` (timing-safe), loopback assert
- `X-SpaceOS-TenantId` header strict-equal body TenantId → 400 ha eltér (SEC-S-01)
- `ExcludeFromDescription()` — nem jelenik meg public Swagger-ben

## DB változások (Migration J-003)

```sql
ALTER TABLE "DoorOrders" ADD COLUMN "SourceQuoteId" UUID NULL,
  ADD COLUMN "SourceContentHash" VARCHAR(64) NULL,
  ADD COLUMN "ConfirmedFromSalesAt" TIMESTAMPTZ NULL,
  ... (pénzügyi mezők)
CREATE UNIQUE INDEX "UX_DoorOrders_TenantId_SourceQuoteId" ... WHERE "SourceQuoteId" IS NOT NULL
CREATE TABLE "DoorOrderConvertedLines" ... (RLS + FORCE ROW LEVEL SECURITY)
```

## Indítás előtt ellenőrizd

```bash
ls src/SpaceOS.Modules.Joinery.Infrastructure/Migrations/ | tail -3   # → következő migration szám
grep -r "DoorOrderStatus" --include="*.cs" | grep -v ".test."          # → enum helye
grep -r "InternalOrderConversionMiddleware" --include="*.cs"           # → ha létezik, skip
grep -r "spaceos_joinery_app\|JoineryAppRole" --include="*.cs"        # → DB role neve
```

## Track sorrend

```
A) Domain: DoorOrderStatus bővítés + DoorOrderConvertedLine + DoorOrder factory + domain event
B) Application: Command + Handler (idempotency-first) + Validator + Repository interface
C) Infrastructure/Persistence: Entity config + Migration J-003 (VPS-en alkalmazni!)
D) Infrastructure/Security: InternalOrderConversionMiddleware
E) API: DTO + Endpoint + Program.cs UseWhen + DI
F) Tests: ≥25 teszt (5 domain + 8 application + 5 API + 2 concurrency + 5 integration)
```

## Definition of Done (összefoglaló)

- [ ] Migration J-003 alkalmazva VPS-en (`spaceos_joinery` schema)
- [ ] Duplicate request (same QuoteId + hash) → 200 (idempotent)
- [ ] Hash mismatch → 409, body csak `"error": "ContentHash mismatch — idempotency conflict."`
- [ ] `X-SpaceOS-TenantId` ≠ body TenantId → 400
- [ ] `CryptographicOperations.FixedTimeEquals` a middleware-ben
- [ ] Loopback assert (`IPAddress.IsLoopback`) aktív
- [ ] Meglévő 389 Joinery teszt zöld
- [ ] ≥25 új teszt zöld
- [ ] 0 build warning

Teljes spec és kódminták: `docs/tasks/new/SpaceOS_Joinery_OrderConversionReceiver_Architecture_v2.md`
