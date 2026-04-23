---
id: MSG-KERNEL-064
from: root
to: kernel
type: task
priority: critical
status: DONE
ref: MSG-E2E-009-DONE, MSG-KERNEL-063-DONE
created: 2026-04-13
---

# MSG-KERNEL-064 — TenantSessionInterceptor GUID mismatch debug + fix

## Helyzet

Az E2E-009 eredménye: **119/151 — változatlan** a `316f603` deploy után.
A `316f603` graceful fallback (ClaimsTenantResolver) megakadályozza a throw-t,
de a regresszió azonos maradt. Tehát a 500-ak nem a ClaimsTenantResolver throw-ából jönnek.

## Root cause — cascade mismatch

```
POST /tenants/:tenantId/facilities  → 201 ✅  (TenantId = X kerül a DB-be)
GET  /tenants/:tenantId/facilities  → 200, items=[] ❌ (filter TenantId = Y ≠ X)
GET  /facilities/:newId             → 404 ❌
POST /facilities/:facilityId/flow-epics → 500 ❌ (facility nem megtalálható → NullRef vagy Not Found exception)
```

**A GUID normalizálás** (`8dd0bd7` commit, KERNEL-062) megváltoztatta a `TenantId` formátumát
valahol a write vagy read path-on. Az írás és a lekérdezés más GUID stringet/típust használ.

## Diagnosztika — ezt kell megcsinálni

### 1. VPS journal a 500-as POST-hoz

```bash
# E2E futtatás előtt töröld a journalt (opcionális, könnyebb olvasni):
# sudo journalctl --rotate && sudo journalctl --vacuum-time=1s

# E2E futtatása (csak a facility suite):
cd /opt/spaceos/e2e && npx vitest run tests/04-facility-crud.test.ts --reporter=verbose

# Azonnali log olvasás:
sudo journalctl -u spaceos-kernel -n 300 --no-pager | grep -A 15 "Exception\|500\|tenant\|TenantId\|current_tenant"
```

### 2. DB ellenőrzés — mi van a TenantId oszlopban?

```bash
sudo -u postgres psql -d spaceos_kernel -c "
SELECT id, name, \"TenantId\", LENGTH(\"TenantId\"::text) as len
FROM \"Facilities\"
ORDER BY \"CreatedUtc\" DESC LIMIT 5;
"
```

Várt: a `TenantId` UUID formátuma pontosan egyezzen a JWT claim-ben lévő tenantId-vel.

### 3. Git diff — mi változott a TenantSessionInterceptor-ban 8dd0bd7-ben?

```bash
cd /opt/spaceos/SpaceOS.Kernel
git diff c62f1d7..316f603 -- "**/TenantSessionInterceptor.cs"
git diff c62f1d7..316f603 -- "**/ClaimsTenantResolver.cs"
```

Ez megmutatja pontosan, hogy a normalizálás mit csinált.

### 4. Hipotézis — GUID formátum probléma

A `TenantSessionInterceptor` `SET app.current_tenant_id = '...'` parancsot küld a PostgreSQL-nek.
Ha a normalizálás `tenantId.ToString("D")` helyett pl. `tenantId.ToString("B")` (`{guid}`) vagy
`tenantId.ToString("N")` (kötőjel nélkül) formátumra váltott, az EF Core RLS filter nem talál egyezést.

Ellenőrizd a `SET app.current_tenant_id` parancs értékét a journal-ban.

## Fix elvárás

A `TenantSessionInterceptor`-ban és a `ClaimsTenantResolver`-ban ugyanolyan GUID string formátumot
kell használni mint ami a `c62f1d7`-ben volt — vagy a DB-ben tárolt UUID formátummal azonos.

A biztonságos alap: `guid.ToString()` → `"D"` format (kötőjeles lowercase),
pl. `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"`.

## Definition of Done

- [ ] VPS journal elemezve — 500-as POST exception azonosítva
- [ ] Git diff c62f1d7..316f603 átvizsgálva
- [ ] DB-ben tárolt TenantId formátum meghatározva
- [ ] Fix implementálva (TenantSessionInterceptor GUID set + ClaimsTenantResolver GUID return egységes formátum)
- [ ] 1084+ teszt zöld
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-KERNEL-064-DONE`

## Megjegyzés

Párhuzamosan kiadom MSG-INFRA-066-ot: rollback c62f1d7-re, amíg a fix elkészül.
A VPS diagnózist **316f603 futtatása alatt** kell elvégezni — ezért kérlek,
a diagnosztikát HAMARABB futtasd, mint hogy INFRA visszaállítja a binaryt.
