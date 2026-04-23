---
id: MSG-KERNEL-077
from: root
to: kernel
type: task
priority: medium
status: READ
ref: KERNEL-070
created: 2026-04-15
---

# MSG-KERNEL-077 — Audit chain break investigation (KERNEL-070)

## Háttér

INFRA-074 (2026-04-14) VPS journalban preexisting alert:

```
[ALERT:ChainBreak] Audit chain broken for tenant a16e3cf4-c6b4-4b45-b55e-d67dae0279ee
   at event 4bbd17e9-... (OccurredAt: 2026-04-14T03:52:56)
   Expected PreviousHash 'e9728cbde1...' but found 'e92ace49e3...'
```

Timestamp: `2026-04-14T03:52` — ez a 2026-04-13-i rollback ciklus idejére esik
(b270ccf → 8dd0bd7 → c62f1d7 → d6b1bad → b270ccf sorozatos deploy + rollback).

## Feladat — Diagnózis

Ez egy **investigation** feladat — kódot csak ha szükséges.

### 1. Tenant azonosítás

```sql
SELECT id, name, type FROM "Tenants"
WHERE id = 'a16e3cf4-c6b4-4b45-b55e-d67dae0279ee';
```

Kérdés: Ez az E2E teszt tenant, vagy egy valódi Doorstar tenant?

### 2. Chain break root cause

```sql
-- Az érintett event és szomszédai
SELECT id, "OccurredAt", "PreviousHash", "StateHash", sequence
FROM "AuditEvents"
WHERE "TenantId" = 'a16e3cf4-c6b4-4b45-b55e-d67dae0279ee'
ORDER BY "OccurredAt", sequence
LIMIT 20;
```

Kérdés: Az `e9728cbde1` PreviousHash melyik event StateHash-ére mutat?
Kérdés: Van-e "eltűnt" event a sorban (ugrás a sequence-ben)?

### 3. Doorstar prod adat érintett-e?

```sql
-- Doorstar tenant ID lekérdezés
SELECT id, name, type FROM "Tenants"
WHERE name ILIKE '%doorstar%' OR type = 'Manufacturer';

-- Ha megvan, ellenőrzés az ő chain-jére
SELECT COUNT(*) FROM "AuditEvents"
WHERE "TenantId" = '<doorstar-tenant-id>';
```

### 4. Teljes chain re-verify (ha szükséges)

Ha az `AuditChainIntegrityTest` mintájára egy production chain verify fut, az egyértelműen megmondja, hány break van és melyik tenant-nál.

## Elfogadási kritériumok (DoD)

- [ ] Tenant `a16e3cf4` azonosítva: E2E teszt tenant VAGY valódi ügyfél
- [ ] Root cause: rollback ciklus okozta VAGY valódi integrity probléma
- [ ] Doorstar tenant chain-je érintett-e? (igen/nem + bizonyíték)
- [ ] Javasolt akció (ha van): fix terv VAGY "nincs teendő, csak rollback artifact"

## Megjegyzés

Ha a break csak az E2E teszt tenant-nál van és a rollback ciklusból ered → elfogadható,
dokumentáljuk és lezárjuk. Ha Doorstar prod adatot érint → kritikus, azonnal jelezz.
