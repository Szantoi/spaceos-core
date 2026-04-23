---
id: MSG-KERNEL-078
from: root
to: kernel
type: task
priority: medium
status: READ
ref: MSG-INFRA-089-DONE
created: 2026-04-15
---

# MSG-KERNEL-078 — Pre-launch audit chain cleanup (régi chain break-ek)

## Helyzet

Az `AuditChain:GenesisHash` most stabil (2642d19566...). Az INFRA-089 DONE jelezte,
hogy a journal tartalmaz **pre-existing** `[ALERT:ChainBreak]` bejegyzéseket
`OccurredAt: 2026-04-07T...` körülről — ezek a random genesis hash korszakból maradtak.

Két tenant érintett a journal mintában:
- `tenant 003057f4...`
- `tenant 07622b7c...`

## Feladat — Investigation + cleanup döntés

### 1. Érintett tenantok azonosítása

```sql
-- Összes tenant amelyiknek chain break-je van a journalban
SELECT DISTINCT t.id, t.name, t."TenantType", COUNT(ae.id) as event_count
FROM "Tenants" t
LEFT JOIN "AuditEvents" ae ON ae."TenantId" = t.id
WHERE t.id IN (
  'a16e3cf4-c6b4-4b45-b55e-d67dae0279ee',  -- probe tenant (ismert)
  '003057f4-%',  -- kiegészíteni a teljes UUID-val
  '07622b7c-%'   -- kiegészíteni a teljes UUID-val
  -- ... journal alapján további érintett tenant ID-k
)
GROUP BY t.id, t.name, t."TenantType";
```

Klasszifikáció:
- `probe-<timestamp>` névmintázat → E2E teszt tenant → **törölhető**
- `Doorstar Kft.` vagy valódi ügyfél → **nem törölhető**, chain re-anchor döntés kell

### 2. Teszt tenantok cleanup

Ha egy tenant `probe-*` vagy `e2e-*` névmintázatú **és** csak tesztadatot tartalmaz:

```sql
-- Teszt tenant audit eventjeinek törlése
DELETE FROM "AuditEvents"
WHERE "TenantId" = '<teszt-tenant-id>';

-- Opcionális: teszt tenant törlése
DELETE FROM "Tenants"
WHERE id = '<teszt-tenant-id>' AND name ILIKE 'probe-%';
```

⚠️ **Csak** olyan tenant-t törölj, amelynek neve `probe-*` vagy `e2e-*`. Más tenant adatát ne érintsd.

### 3. Doorstar / éles tenant kezelése

Ha a Doorstar tenant (vagy más éles tenant) érintett:
- **Ne törölj** — a KERNEL-077 diagnózis szerint a 4 Doorstar event belső lánca ép
- **Chain re-anchor** helyett: elfogadott pre-launch artifact → dokumentálás elegendő
- A Q3 Escrow sprintben (R-04: OpenTimestamps) lesz a megfelelő eszköz a retroaktív időbélyegzésre

### 4. Verify

```bash
sudo systemctl restart spaceos-kernel
# 2 perc várakozás
sudo journalctl -u spaceos-kernel --since "2 minutes ago" | grep -i "chainbreak\|alert"
# Elvárás: csak pre-existing alertek (OccurredAt: 2026-04-07), probe tenantok törlése után kevesebb
```

## Elfogadási kritériumok (DoD)

- [ ] Érintett tenantok azonosítva és klasszifikálva (test vs. production)
- [ ] Probe/E2E teszt tenantok audit eventjei törölve production DB-ből
- [ ] Doorstar és éles tenantok érintetlenek
- [ ] Journal-ban ChainBreak alertek száma csökkent (vagy 0 ha csak teszt tenantoknál volt)
- [ ] 1110 teszt zöld (nincs kódváltozás — csak DB cleanup)

## Megjegyzés

Ez **nem kódváltozás** — csak production DB cleanup SQL query-k és journal ellenőrzés.
A genesis hash már stabil (INFRA-089), az új eventeket ez nem érinti.
