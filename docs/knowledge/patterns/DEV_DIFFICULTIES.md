# SpaceOS — Fejlesztési Nehézségek Timeline

> Visszatérő problémák és tanulságaik. Minden egyes iteráció dokumentálva.

---

## DEV-001 — ClaimsTenantResolver debug saga (4+ iteráció)

**Időszak:** 2026-04-11 – 2026-04-16

**Tünet:** E2E tesztek tenant-scoped GET endpointokra üres listát / 404-et adnak, pedig az adat létezik.

### Iteráció 1 — spaceos_tenants vs tid claim prioritás

**Diagnózis (MSG-E2E-001-DONE v3):** `TenantSessionInterceptor` a `spaceos_tenants` claim-et olvasta GUC-hoz, de a globális EF filter a `tid` claim-et. Két különböző UUID → RLS mismatch.

**Fix (MSG-KERNEL-062):** `ClaimsTenantResolver` most elsőként `spaceos_tenants` JSON array-t olvas, fallback `tid`. Mindkét (GUC + filter) azonos UUID-t kap.

### Iteráció 2 — DenyWebRequestSentinel bevezetés szándékos regressziót okoz

**Diagnózis (MSG-E2E-008-DONE):** A `DenyWebRequestSentinel` bevezetése (KERNEL-081) helyes volt, de feltárta a `MapInboundClaims = true` problémát. A `tid` claim MS URI-ra volt mappelve → `ClaimsTenantResolver` null-t kapott → sentinel → üres lista.

**Fix (MSG-KERNEL-082):** `MapInboundClaims = false` hozzáadva.

### Iteráció 3 — TenantSessionInterceptor GUC key naming mismatch

**Diagnózis:** Különböző modulokban különböző GUC kulcs:
- Kernel: `app.current_tenant_id`
- Egyes modulok (kezdetben): `app.tenant_id`

**Fix:** Egységesítve `app.current_tenant_id`-ra minden modulban.

### Iteráció 4 — Cutting modul `app.tenant_id` vs `app.current_tenant_id`

**Diagnózis (MSG-CUTTING-008-DONE):** A cutting modul TenantSessionInterceptor `app.tenant_id`-t használt (Kernel-től eltérő kulcs). Az RLS policy `app.current_tenant_id`-t vár.

**Fix:** Egységesítés `app.current_tenant_id`-ra a cutting, inventory, procurement modulokban.

**Root cause (végső):** Nincs egységes SpaceOS contract a GUC kulcs nevére — minden modul külön definiálta. Tanulság: a GUC kulcs nevet contracts-ban/dokumentumban kell rögzíteni.

---

## DEV-002 — Migration reconcile procedúra

**Időszak:** 2026-04-12

**Tünet:** Migration 0028 nem volt alkalmazva a production DB-n, holott a kódban megvolt. Az EF Core regenerált migration `DropTable("AuditEvents")` sort tartalmazott — katasztrofális lett volna.

**Root cause:** A Kernel terminál SQLite ModelSnapshot alapján regenerálta a migration-t (SQLite `TEXT`/`INTEGER` típusokkal) — az EF nem detektálta a PostgreSQL-specifikus típusokat.

**Mikor kell migration reconcile:**
- Ha `dotnet ef migrations list` `Pending`-et mutat egy korábban raw SQL-lel alkalmazott migrationre
- Ha a history táblában lévő ID eltér a kód-szintű migration fájl ID-tól

**Procedure:**
```bash
# 1. Ellenőrzés
psql -U spaceos -p 5433 -d spaceos -c \
  'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 3;'

# 2. Ha a DB-beli ID eltér a kódtól, manuális UPDATE:
psql -U spaceos -p 5433 -d spaceos -c \
  "UPDATE \"__EFMigrationsHistory\" 
   SET \"MigrationId\" = '20260410130000_Migration_0028_StageRegistry'
   WHERE \"MigrationId\" = '20260412060341_Migration_0028_StageRegistry';"

# 3. Verifikáció
dotnet ef migrations list --project SpaceOS.Infrastructure --startup-project SpaceOS.Kernel.Api
# Elvárás: Applied (nem Pending)
```

**Tanulság:** Raw SQL bypass után MINDIG kézzel kell az EF history-t szinkronban tartani. [MSG-INFRA-060-DONE, MSG-KERNEL-061-DONE]

---

## DEV-003 — TenantSessionInterceptor GUC key naming mismatch

**Összefoglaló táblázat (jelenlegi állapot):**

| Modul | GUC kulcs | Interceptor típus |
|-------|-----------|-------------------|
| Kernel | `app.current_tenant_id` | `DbConnectionInterceptor` |
| Joinery | `app.current_tenant_id` | `DbConnectionInterceptor` |
| Abstractions | `app.tenant_id` → javítva → `app.current_tenant_id` | `SaveChangesInterceptor` + `DbCommandInterceptor` |
| Cutting | `app.current_tenant_id` (CUTTING-008 után) | `SaveChangesInterceptor` |
| Inventory | `app.current_tenant_id` (CUTTING-008 után) | `SaveChangesInterceptor` |
| Procurement | `app.current_tenant_id` (CUTTING-008 után) | `SaveChangesInterceptor` |

**KRITIKUS:** Az RLS policy és az interceptor UGYANAZT a GUC kulcsot kell használja. Ha eltér → `22P02` vagy üres eredmény.

---

## DEV-004 — InventoryProviderStub vs HttpAdapter váltás

**Időszak:** 2026-04-16

**Kontextus:** A cutting modul kezdetben `InventoryProviderStub` implementációt használt (hardcoded empty returns), majd `InventoryProviderHttpAdapter`-re váltott.

**Tünet a váltás előtt:** `GetNestingResultQueryHandler` 500-at dobott — `IInventoryProvider` DI container-ben nem volt regisztrálva (a Stub csak tesztkonfigurációban volt).

**Fix (MSG-CUTTING-010-DONE):**
- `ServiceCollectionExtensions.cs`-ből eltávolítva `InventoryProviderStub` regisztrációja
- `AddHttpClient<IInventoryProvider, InventoryProviderHttpAdapter>` hozzáadva
- `InventoryService__BaseUrl=http://127.0.0.1:5004` env var szükséges

**Tanulság:** Stub implementáció regisztrálása production DI container-ben → service indulása után is stub marad.

---

## DEV-005 — Nesting activation sprint lépései

**Időszak:** 2026-04-16 (MSG-E2E-041 – MSG-E2E-045)

A cutting modul nesting funkció aktiválásának teljes debug lánca:

| Lépés | Hiba | Root cause | Fix |
|-------|------|------------|-----|
| 1 | `401 Unauthorized` | JWT `tid` claim nem talált | CUTTING-007: `MapInboundClaims=false` |
| 2 | `42501 permission denied` | Séma GRANT hiányzik | INFRA-112: `GRANT USAGE ON SCHEMA` |
| 3 | `42704 unrecognized GUC` | GUC nincs regisztrálva | INFRA-113: `ALTER DATABASE SET` |
| 4 | `22P02 invalid uuid ""` | TenantSessionInterceptor nem fut le | CUTTING-008: Interceptor implementáció |
| 5 | `500 DI hiba` | `IInventoryProvider` nincs regisztrálva | CUTTING-010: HttpAdapter DI |
| 6 | `500 connection affinity` | GUC + DELETE nem ugyanazon connection | CUTTING-015: OpenConnectionAsync explicit |

**Tanulság:** Új modul aktiválásakor mindig végig kell ellenőrizni a teljes "JWT → Grant → GUC → Interceptor → Provider → Connection affinity" láncot.

---

## DEV-006 — E2E seed fix: adminTenantId szétszórt forrása

**Tünet (MSG-E2E-001-DONE v3):** Az E2E tesztek `beforeAll`-ban új tenant-et hoztak létre, de a Keycloak token mindig a hardcoded Doorstar tenant-et adta vissza → a tesztek nem az általuk létrehozott tenant-et kérdezték le.

**Root cause:** A `spaceos_tenants` Keycloak claim a felhasználó csoport attribútumából jön. A `test-admin` user a `doorstar-kft` csoportban van, tehát mindig `a1b2c3d4-e5f6-7890-abcd-ef1234567890` UUID-t kap.

**Megoldás:** Az E2E tesztek a seed által létrehozott tenant helyett a Keycloak tokenből jövő **hardcoded Doorstar tenant-et** használják. A seed adatok mindig ezen tenant kontextusában jönnek létre.

**Globális adminTenantId forrása:**
```typescript
// global-setup.ts
const authResponse = await fetch('/bff/auth/me', {...});
const { tenantId } = await authResponse.json();
// tenantId = a1b2c3d4-e5f6-7890-abcd-ef1234567890 (Doorstar, hardcoded Keycloak-ban)
```

---

## DEV-007 — ASPNETCORE_ENVIRONMENT hiánya Production hibákat okoz

**Tünet:** E2E suite `429 Too Many Requests` + `InvalidOperationException: Crypto:SigningKey is required in non-development environments`.

**Root cause:** `/etc/spaceos/kernel.env` nem tartalmazott `ASPNETCORE_ENVIRONMENT` értéket → Kernel Production módban futott.

**Következmények:**
1. Rate limit: Production = 20 write/perc (E2E: 500 kérés percenként)
2. `Crypto__SigningKey` Production módban kötelező — ha nincs beállítva → startup exception

**Fix:**
```bash
echo 'ASPNETCORE_ENVIRONMENT=Development' >> /etc/spaceos/kernel.env
systemctl restart spaceos-kernel
```

**Referencia:** [MSG-INFRA-059]
