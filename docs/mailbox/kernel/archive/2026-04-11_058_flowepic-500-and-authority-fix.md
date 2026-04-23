---
id: MSG-KERNEL-058
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-E2E-001-DONE
created: 2026-04-11
---

# MSG-KERNEL-058 — VPS deploy + FlowEpic 500 + Authority commit + ADR

## Kontextus (frissítve 2026-04-11 — INFRA-056 + ORCH-056 DONE)

MSG-KERNEL-054-DONE elfogadva ✅ — 1068 teszt.
MSG-INFRA-056-DONE: Keycloak hostname beállítva, VPS-en **99/120 E2E pass** (volt ~55).
MSG-ORCHESTRATOR-056-DONE: /bff/auth/me tenantId fix kész — 177 teszt.

**15 maradék E2E hiba mind Kernel scope.** Kérünk VPS deploy-t + 3 kódhiba javítást.

---

## 1. feladat — VPS deploy (Migration 0028 + új build)

```bash
dotnet publish -c Release -o /opt/spaceos-publish/kernel
dotnet ef database update   # Migration 0028
systemctl restart spaceos-kernel
```

---

## 2. feladat — Authority config commit (repóban)

Az INFRA terminál a VPS publish mappájában már javította az Authority-t.
A **source repóban** commitálandó:

```diff
// appsettings.Development.json
- "Authority": "http://localhost:8080/realms/spaceos"
+ "Authority": "http://localhost:8080/auth/realms/spaceos"

// appsettings.Production.json
- "Authority": "http://localhost:8080/auth/realms/spaceos"   (vagy korábbi érték)
+ "Authority": "https://joinerytech.hu/auth/realms/spaceos"
```

Development = lokális HTTP backchannel · Production = HTTPS publikus URL.

---

## 3. feladat — FlowEpic 500 (E2E 05 + 10, 5 fail)

```
POST /api/flow-epics → 500
```

Valószínű ok: Migration 0028 nem alkalmazva (deploy után eltűnhet).
Ha nem tűnik el: ellenőrizd a `FlowEpics` tábla NOT NULL oszlopait psql-ben.

---

## 4. feladat — TenantSessionInterceptor tid claim (E2E 24, 4 fail)

```
24-tenant-summary: tid claim → 401 Unauthorized
```

A `TenantSessionInterceptor` a `tid` JWT claim alapján szűr. A Keycloak token ezt a claim-et nem `tid`-ként, hanem `spaceos_tenants` JSON tömbként adja vissza.

Ellenőrizd a `TenantSessionInterceptor` claim-olvasó logikáját:
- Melyik claim névvel keresi a tenant ID-t? (`tid`? `tenant_id`? `spaceos_tenants`?)
- A Keycloak Script Mapper `spaceos_tenants: [{"tenant_id": "..."}]` formátumot állít elő
- Ha a Interceptor `tid` stringet vár → nem egyezik → 401

Javítás: az Interceptor olvassa a `spaceos_tenants[0].tenant_id`-t, vagy frissítsd a Script Mapppert, hogy `tid` string claim-et is adjon.

---

## 5. feladat — Workstation timeout (E2E 08, 5 fail)

```
08-workstation: PUT/POST/DELETE → 15s timeout
```

Valószínű ok: Migration 0028 alkalmazása előtti állapot, vagy az advisory lock blokkolás (pg_advisory_xact_lock hosszú várakozás).
Deploy + migration alkalmazás után futtasd újra. Ha a timeout megmarad, ellenőrizd a workstation endpoint-ok tranzakció kezelését.

---

## 6. feladat — ADR-023

```
docs/adr/ADR-023_stage-handoff-handler-in-infrastructure.md
```
Dokumentáld: `CreateStageHandoffCommandHandler` Infrastructure rétegben van — `pg_advisory_xact_lock` közvetlen `AppDbContext` hozzáférés miatt.

---

## Definition of Done

- [ ] Migration 0028 alkalmazva VPS-en + kernel restart
- [ ] `appsettings.Development.json` Authority = `http://localhost:8080/auth/realms/spaceos` commitálva
- [ ] `appsettings.Production.json` Authority = `https://joinerytech.hu/auth/realms/spaceos` commitálva
- [ ] E2E 05 + 10 — FlowEpic POST → 201 ✅
- [ ] E2E 24 — tenant-summary → 200 (tid claim fix) ✅
- [ ] E2E 08 — workstation timeout → zöld ✅
- [ ] ADR-023 létrehozva
- [ ] Meglévő **1068** teszt zöld · 0 build warning

## Visszajelzés

Outboxba: `MSG-KERNEL-058-DONE`
