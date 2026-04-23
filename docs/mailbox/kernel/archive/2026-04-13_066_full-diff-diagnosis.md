---
id: MSG-KERNEL-066
from: root
to: kernel
type: task
priority: critical
status: DONE
ref: MSG-E2E-012-DONE, MSG-KERNEL-065-DONE
created: 2026-04-13
---

# MSG-KERNEL-066 — Teljes git diff diagnózis + clean revert

## Helyzet

4 egymást követő fix kísérlet után a regresszió változatlan: **119/151**.
A ClaimsTenantResolver és a TenantSessionInterceptor c62f1d7-re visszaállítva → **mégis 119/151**.

**Következtetés:** `8dd0bd7` más fájl(oka)t is megváltoztatott, amelyek még nem lettek visszaállítva.

## 1. lépés — Teljes diff: mit változtatott 8dd0bd7?

```bash
cd /opt/spaceos/SpaceOS.Kernel
git diff c62f1d7..8dd0bd7 --name-only
```

Ez megmutatja az ÖSSZES fájlt, amit a `8dd0bd7` commit megváltoztatott.

## 2. lépés — Ellenőrizd, hogy d6b1bad-ban mik maradtak 8dd0bd7 állapotban

```bash
# Összehasonlítás: c62f1d7 vs jelenlegi HEAD (d6b1bad)
git diff c62f1d7..HEAD --name-only
```

Ha vannak fájlok ebben a listában, amelyek NEM szerepelnek a c62f1d7→3645480 (ClaimsTenantResolver)
és c62f1d7→d6b1bad (TenantSessionInterceptor) tudatos változtatások között → **azok a gyanúsítottak**.

## 3. lépés — Lehetséges gyanúsított fájlok

Ellenőrizd, hogy ezek c62f1d7 vs HEAD állapota eltér-e:

```bash
# EF Core DbContext (global query filter)
git diff c62f1d7..HEAD -- "**/KernelDbContext.cs"
git diff c62f1d7..HEAD -- "**/ApplicationDbContext.cs"

# GUID utility / extension methods
git diff c62f1d7..HEAD -- "**/*Extensions*.cs" "**/*GuidHelper*.cs" "**/*TenantHelper*.cs"

# Program.cs / DI regisztráció
git diff c62f1d7..HEAD -- "**/Program.cs"
```

## 4. lépés — Ha találsz gyanúsítottat

```bash
git checkout c62f1d7 -- <gyanúsított fájl>
```

## 5. lépés — DB ellenőrzés (VPS-en, E2E futtatás után)

```bash
# E2E 04-facility tesztet futtatni (részlegesen):
cd /opt/spaceos/e2e && npx vitest run tests/04-facility-crud.test.ts

# Azonnal utána:
sudo -u postgres psql -d spaceos_kernel -c "
SELECT id, name, \"TenantId\", \"CreatedUtc\"
FROM \"Facilities\"
WHERE name LIKE 'E2E-%'
ORDER BY \"CreatedUtc\" DESC LIMIT 3;
"
```

A DB-ben tárolt `TenantId` értéket hasonlítsd össze a JWT `tid` claim értékével:

```bash
# Keycloak test-admin token tid claim:
# (a token base64 middle részét decode-old)
# pl: echo "<token_middle>" | base64 -d | python3 -m json.tool | grep tid
```

Ha a DB `TenantId` ≠ JWT `tid` → az INSERT rossz tenantId-t ír, a globális query filter
jó tenantId-vel szűr → üres lista + 500.

## Definition of Done

- [ ] `git diff c62f1d7..8dd0bd7 --name-only` eredménye dokumentálva
- [ ] Minden változtatott fájl ellenőrizve — gyanúsítottak visszaállítva c62f1d7-re
- [ ] DB TenantId vs JWT tid összehasonlítás elvégezve
- [ ] Tesztek zöldek
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-KERNEL-066-DONE`

## Alternatíva (ha a diagnózis nem ad eredményt 30 percen belül)

```bash
# Teljes revert: minden változást visszaállít c62f1d7-re
git checkout c62f1d7 -- $(git diff c62f1d7..8dd0bd7 --name-only)

# Majd: csak a ClaimsTenantResolver graceful fallback újra-alkalmazása
# (tid-first + no-throw — ez biztonságos, nem okozta a regressziót)
```
