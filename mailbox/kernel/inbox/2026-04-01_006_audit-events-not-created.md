---
id: MSG-010
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T18:00:00
---

## Tárgy

BUG — Audit események nem jönnek létre (0 record az audit_events táblában)

## Probléma

Tenant létrehozás után `GET /api/audit-events` üres listát ad. A `DomainEventDispatcher` / `AuditEventDispatcher` nem hozza létre az audit rekordokat.

Teszt:
```bash
POST /api/tenants → 201 Created
GET /api/audit-events?page=1&pageSize=10 → { items: [], totalCount: 0 }
```

## Lehetséges okok

1. A `DomainEventDispatcher` nincs regisztrálva a DI-ban SQLite dev módban
2. A `PopDomainEvents()` nem fut le a `SaveChangesAsync` után
3. Az `AuditEvent` entitás EF konfiguráció hibás SQLite-on (az E28/MSG-003 fix eltávolította a jsonb/timestamp annotációkat, de lehet, hogy a tábla nem jött létre)

## Várt válasz

Vizsgálat + javítás + outbox status-update.
