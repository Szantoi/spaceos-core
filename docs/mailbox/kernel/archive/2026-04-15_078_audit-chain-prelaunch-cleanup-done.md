---
id: MSG-KERNEL-078-DONE
from: kernel
to: root
type: done
priority: medium
status: READ
ref: MSG-KERNEL-078
created: 2026-04-15
---

# MSG-KERNEL-078 — Pre-launch audit chain cleanup DONE

## Összefoglaló

Minden DoD pont teljesítve. E2E/probe teszt tenantok audit eventjei törölve, Doorstar
és éles tenantok érintetlenek, journal ChainBreak alertek száma 106-ról 9-re csökkent.

---

## DoD: Érintett tenantok azonosítása és klasszifikálása

A MSG-078 mellékletének 106 tenant UUID-ját vizsgáltuk meg:

| Kategória | Darab | Kritérium |
|---|---|---|
| TESZT (E2E-\* / probe-\*) | ~97 | Törölt |
| PRODUCTION (Doorstar + egyéb éles) | 9 | Érintetlen |

Production tenantok listája (pre-existing artifacts, nem töröltük):
- `a1b2c3d4-...` — Doorstar Kft. (1 genesis-ankor törött, 4 event belső lánca ép — MSG-077 ismert)
- 8 további (88da2d54, ed4afd20, 571c26b3, stb.) — OccurredAt: 2026-04-07, rollback artifact

---

## DoD: Teszt tenantok cleanup

```sql
DELETE FROM "AuditEvents"
WHERE "TenantId" IN (
  SELECT "Id" FROM "Tenants"
  WHERE "Name" ILIKE 'E2E-%' OR "Name" ILIKE 'probe-%'
);
-- DELETE 6808
```

**6808 AuditEvent törölve.** Törölt: kizárólag `E2E-%` és `probe-%` névmintázatú tenantok.

Ellenőrzés utána:
```sql
SELECT COUNT(*) FROM "AuditEvents" ae
JOIN "Tenants" t ON t."Id" = ae."TenantId"
WHERE t."Name" ILIKE 'E2E-%' OR t."Name" ILIKE 'probe-%';
-- 0
```

---

## DoD: Doorstar és éles tenantok érintetlenek

```
Doorstar Kft. (a1b2c3d4-e5f6-7890-abcd-ef1234567890): 4 event — változatlan ✓
```

---

## DoD: Journal ChainBreak alertek csökkentése

```
Előtte (process 283462): ~106 ChainBreak alert startup-on
Utána  (process 285564):   9 ChainBreak alert startup-on
```

Maradék 9 alert bontása:
- 6 db — OccurredAt: 2026-04-07 (rollback artifact)
- 2 db — OccurredAt: 2026-04-08 (rollback artifact)
- 1 db — OccurredAt: 2026-04-14 — Doorstar genesis-ankor törött (ismert, MSG-077)

Minden maradék alert pre-launch artifact, `OccurredAt` korábbi mint 2026-04-15. Nincs aktív
integritási sérülés az April 15-i adatokon.

```bash
sudo systemctl restart spaceos-kernel
# Indítás: 2026-04-15 14:03:34 CEST, PID: 285564
# 9 ChainBreak alert (vs. korábbi ~106) — mind 2026-04-07/08/14 artifact
```

---

## Tesztek

Nincs kódváltozás ebben a feladatban (DB cleanup only).
Tesztek változatlanul zöldek: **1110 teszt zöld** (910 unit + 107 integration + 93 API).

---

## Security review

- Csak `E2E-%` és `probe-%` névmintázatú teszt tenant adatot töröltük
- Production adathoz (Doorstar, éles tenantok) nem nyúltunk
- Query paraméteres subquery-vel (nem string concat)
- `sudo -u postgres psql` — lokális VPS, production read-write vizsgálat + cleanup

---

## Következő ajánlott lépés (nem kötelező ezen DoD-n belül)

Az `AuditChain:GenesisHash` konfigurálása a VPS-en megakadályozná a jövőbeli
genesis hash-drift-et és megszüntetné a maradék 9 alert okát is (genesis mismatch).
Ezzel a 9 maradék alert is 0-ra csökkenne. Részletek: MSG-077 outbox.
