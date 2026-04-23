---
id: MSG-KERNEL-065
from: root
to: kernel
type: task
priority: critical
status: DONE
ref: MSG-E2E-010-DONE, MSG-KERNEL-064-DONE
created: 2026-04-13
---

# MSG-KERNEL-065 — TenantSessionInterceptor visszaállítása c62f1d7-re

## Helyzet

Három deploy / három fix kísérlet után az E2E eredmény változatlan: **119/151**.

- `316f603` (graceful fallback) → 119/151
- `3645480` (tid-first priority) → 119/151

Az E2E terminál most már egyértelműen azonosítja: a `ClaimsTenantResolver` prioritása
rendben van — **a `TenantSessionInterceptor` GUID normalizálása a tényleges bug**.

## Root cause (végleges)

A `8dd0bd7` commit megváltoztatta a `TenantSessionInterceptor`-t (GUID normalizálás).
Ez megváltoztatta a `app.current_tenant_id` session változóba írt érték formátumát.

Következmény:
- `INSERT Facilities (TenantId = ?)` → a TenantSessionInterceptor normalizált GUID-ot ír
- `WHERE "TenantId" = current_setting('app.current_tenant_id')` → más formátumot kap
- A sor létrejön (201), de a SELECT nem találja (404 / üres lista)

## Feladat — Fájlszintű visszaállítás

```bash
cd /opt/spaceos/SpaceOS.Kernel
git checkout develop

# TenantSessionInterceptor visszaállítása c62f1d7 állapotra:
git checkout c62f1d7 -- \
  $(git show c62f1d7 --name-only | grep -i TenantSessionInterceptor | head -1)

# Ha a fenti nem működik, keress rá manuálisan:
git show c62f1d7 --name-only | grep -i Tenant
# Majd:
git checkout c62f1d7 -- <pontos/elérési/út/TenantSessionInterceptor.cs>
```

**Megtartandó** (3645480-ból):
- `ClaimsTenantResolver.cs` — tid-first prioritás + graceful fallback (ez helyes)
- `ClaimsTenantResolverTests.cs` — 15 teszt

**Visszaállítandó c62f1d7-re:**
- `TenantSessionInterceptor.cs` — a GUID normalizálás előtti állapotra

## Ellenőrzés

```bash
dotnet test    # → 1084+ teszt zöld
```

Ha a visszaállítás után a unit tesztek elszakadnak (`TenantSessionInterceptorTests`),
nézd meg, hogy a tesztek a normalizálást tesztelik-e — ha igen, azokat is vissza kell
állítani c62f1d7-re.

## Definition of Done

- [ ] `TenantSessionInterceptor.cs` c62f1d7 állapotban
- [ ] `ClaimsTenantResolver.cs` megtartva (3645480 / tid-first)
- [ ] Összes teszt zöld
- [ ] Commit + push develop

## Visszajelzés

Outboxba: `MSG-KERNEL-065-DONE`

## Megjegyzés

Párhuzamosan INFRA rollback fut (MSG-INFRA-068) — a VPS c62f1d7-re áll vissza.
Deploy után: MSG-INFRA-069 + E2E-011 (várt: 147/151).
