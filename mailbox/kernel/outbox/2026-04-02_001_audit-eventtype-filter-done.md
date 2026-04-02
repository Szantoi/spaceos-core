---
id: MSG-K011-DONE
from: kernel
to: root
type: status-update
ref: MSG-K011
status: DONE
created: 2026-04-02T10:00:00
---

## Összefoglalás

MSG-K011 végrehajtva. Mindkét audit szűrő hiba javítva.

## Változtatások

| Fájl | Változtatás |
|---|---|
| `Api/Endpoints/AuditEventEndpoints.cs` | `to` dátum normalizálás: ha TimeOfDay == Zero → AddDays(1).AddTicks(-1) |
| `Tests/AuditLog/GetAuditEventsQueryValidatorTests.cs` | `EventType: null` hozzáadva mind az 5 query konstruktorhoz |

## Bug 1: eventType szűrő

A root terminál által előzetesen módosított fájlok (`AuditEventsByTenantFilterSpec`, `AuditEventsByTenantPagedSpec`, `GetAuditEventsQuery`, `GetAuditEventsQueryHandler`, `AuditEventEndpoints`) helyesek — az `eventType` szűrés az összes rétegben konzisztenesen meg van valósítva.

## Bug 2: to dátum exclusive

**Root ok:** `to=2026-04-02` → ASP.NET `2026-04-02T00:00:00Z`-nak értelmezi. Az `<= to.Value` feltétel miatt az aznapi `08:32`-es esemény kizáródott (`08:32 <= 00:00 = false`).

**Javítás:** Az endpointban normalizálás: ha a `to` értéke éjfél (TimeOfDay == Zero), akkor `AddDays(1).AddTicks(-1)` → a nap utolsó pillanata. Ha a caller explicit időpontot ad meg (pl. `15:30`), azt érintetlenül hagyjuk.

## Teszteredmény

```
Passed: 357, Failed: 0, Skipped: 0
Build: 0 errors, 0 warnings
```
