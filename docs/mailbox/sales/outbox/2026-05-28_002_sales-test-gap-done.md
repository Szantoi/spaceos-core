---
id: MSG-SALES-002-DONE
from: sales
to: root
type: done
priority: high
status: READ
ref: MSG-SALES-002
created: 2026-05-28
---

# MSG-SALES-002 DONE — Teszt gap pótlás: 54→102 teszt

## Összefoglaló

A visszadobás hiánylistáját teljesítve. A DoD ≥88 teszt küszöb teljesült.

**Commit:** `2ab1586` — feat: MSG-SALES-002 — Teszt gap pótlás, 54→102 teszt

## Tesztek

```
Passed!  - Failed: 0, Passed: 102, Skipped: 0, Total: 102, Duration: 2 s
```

**Build:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Új tesztfájlok

| Fájl | Új tesztek | Tartalom |
|------|-----------|---------|
| `Application/QuoteCommandHandlerTests.cs` | +22 | CreateQuote, SendQuote, AcceptQuote, ArchiveQuote, ArchiveCustomer, UpdateCustomerContact handler tesztek |
| `Api/CustomerApiTests.cs` | +5 | POST 201, GET 200, GET 404, 401 no-JWT, paged list |
| `Api/QuoteApiTests.cs` | +6 | POST 201, GET 200, GET 404, 401, pipeline funnel |
| `Api/TestAuthHandler.cs` | — | SalesTestAuthHandler + SalesNoAuthHandler (TestServer JWT mock) |
| `Security/QuotaGuardTests.cs` | +4 | Customer/Quote limit enforcement (forbidden+success) |
| `Security/OutboxWorkerTests.cs` | +7 | OutboxMessage MarkInFlight/MarkCompleted/RecordFailure FSM + WorkerTenantContext |
| `Domain/IdempotencyTests.cs` | +7 | RequestConversion idempotency, OutboxMessage lifecycle, QuoteNumber format |

## DoD ellenőrzőlista

- ✅ `dotnet test` → **102 teszt** zöld (küszöb: ≥88)
- ✅ 0 build warning
- ✅ Handler tesztek: **32** (CreateCustomer+Quote 9 + SendQuote 3 + AcceptQuote 3 + ArchiveQuote 3 + ArchiveCustomer 3 + UpdateContact 3 + RequestConversion 5 + egyéb 3)
- ✅ API tesztek: **11** (CustomerApiTests 5 + QuoteApiTests 6)
- ✅ Security tesztek: **17** (6 meglévő + QuotaGuard 4 + OutboxWorker 7)
- ✅ Concurrency/idempotency tesztek: **7** (IdempotencyTests)

## Kockázatok / kérdések

Nincsenek. A meglévő blokkolók (Joinery receiver, Kernel deploy, InternalSecret) változatlanok —
ezek nem Sales-PR-ek, és nem blokkolják a unit/API teszt futtatást.
