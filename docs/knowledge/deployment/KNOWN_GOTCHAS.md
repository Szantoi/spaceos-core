# SpaceOS — Ismert csapdák és diagnózisok

> Ez a fájl élő tapasztalatból épül. Minden VPS deploy vagy service indítás után
> frissítsd, ha új csapdát találsz.
>
> **Forrás:** `/spaceos-deploy` skill és valós deploy incidensek.

---

## 1. dotnet-ef tool verziókonfliktusd

**Tünet:**
```
The EF Core tools version '10.x.x' is newer than that of the runtime '8.x.x'.
```
vagy egyszerűen: `dotnet ef` parancs futtatása után semmi nem történik / tool not found.

**Ok:** `~/.dotnet/tools/dotnet-ef` v10-es, de a projekt .NET 8 targetű. A v10 eszköz
nem kompatibilis .NET 8 projektekkel.

**Fix — minden .NET 8 migrációt manuális SQL-lel kell alkalmazni:**
```bash
# 1. Extractáld az SQL-t a migration .cs fájlból
cat src/SpaceOS.Modules.X.Infrastructure/Persistence/Migrations/YYYYMMDD_MigrationName.cs

# 2. Futtasd psql-ben (5433 a natív PostgreSQL port, NEM 5432!)
sudo -u postgres psql -p 5433 -d <adatbázis_neve> <<'SQL'
  -- migration SQL tartalma ide
  SELECT 'migration-name OK' AS result;
SQL

# 3. Ha szükséges: rögzítsd a migration history-ban
sudo -u postgres psql -p 5433 -d <adatbázis_neve> <<SQL
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YYYYMMDDHHMMSS_MigrationName', '8.0.x');
SQL
```

**Érintett service-ek:** Identity, Joinery (J-003), Sales (S-001/002/003) — mind manuális SQL-lel lett alkalmazva.

---

## 2. Üres / stale model snapshot — EF "already up to date" hamisan

**Tünet:**
```
No migrations were applied. The database is already up to date.
```
...de a táblák nem léteznek a DB-ben.

**Ok:** A `*DbContextModelSnapshot.cs` fájl üres vagy nem lett frissítve a migration generálásakor.
Az EF Core a snapshot alapján dönti el, hogy van-e pending migration — ha a snapshot nem tartalmazza
a migration-t, az EF nem látja pending-ként.

**Diagnózis:**
```bash
# Ellenőrizd a snapshot méretét
wc -l src/*/Persistence/Migrations/*ModelSnapshot.cs
# Ha < 50 sor → valószínűleg üres/stale

# Ellenőrizd, hogy a migration DLL-ben van-e
strings /tmp/publish/SpaceOS.Modules.X.Infrastructure.dll | grep "MigrationName"
# Ha megvan a DLL-ben de EF mégis "up to date" → snapshot probléma
```

**Fix:** A migration SQL-t manuálisan kell futtatni (→ lásd #1 gotcha).

---

## 3. ChangeTracker tilos OnModelCreating-ben

**Tünet:**
```
System.InvalidOperationException: An attempt was made to use the model while it was being created.
A DbContext instance cannot be used inside 'OnModelCreating' in any way that makes use of the model
that is being created.
```
Service elindul, health 500, outbox worker folyamatosan fail-el.

**Ok:** A `DbContext.OnModelCreating()` futása közben az EF Core a model-t még építi.
Minden olyan hívás, ami a model-t igényli (beleértve a `ChangeTracker` elérését), kivételt dob.

**Fix — event subscription a konstruktorba:**
```csharp
// ❌ TILOS
protected override void OnModelCreating(ModelBuilder mb)
{
    mb.ApplyConfigurationsFromAssembly(...);
    ChangeTracker.Tracked += (_, e) => { ... }; // CRASH
}

// ✅ HELYES
public sealed class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
        ChangeTracker.Tracked += (_, e) => { ... }; // konstruktorban OK
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(...);
        // ChangeTracker IDE NEM kerülhet
    }
}
```

---

## 4. Env kulcs ≠ Program.cs konfig kulcs

**Tünet:**
```
System.InvalidOperationException: The MetadataAddress or Authority must use HTTPS
unless disabled for development by setting RequireHttpsMetadata=false.
```
...annak ellenére, hogy az authority URL valóban HTTPS.

**Ok:** Az env fájlban lévő kulcsnév nem egyezik azzal, amit a `Program.cs` olvas.
Pl. env-ben `Jwt__Authority`, de a kód `builder.Configuration["Auth:Authority"]`-t kér.
Az ASP.NET Core nem dob kivételt, ha egy konfig kulcs `null` — az `opts.Authority = null`
esetén viszont a JWT middleware HTTPS nélkül próbál discovery-t végezni, ami crashel.

**Diagnózis:**
```bash
# Melyik kulcsot olvassa a Program.cs?
grep -n "Authority\|Jwt\|Auth\|Issuer" src/*/Program.cs

# Mit tartalmaz az env fájl?
sudo grep -i "auth\|jwt\|issuer" /etc/spaceos/<service>.env
```

**Fix:** Az env kulcsot a Program.cs-hez igazítsd. A .NET env → konfig leképzés:
`Auth__Authority` → `Auth:Authority`, `Jwt__Authority` → `Jwt:Authority`.

**Érintett:** Sales (`Jwt__Authority` → `Auth__Authority` javítás, 2026-05-28).

---

## 5. GRANT CREATE ON DATABASE — első migráció schema-létrehozáshoz

**Tünet:**
```
ERROR: permission denied for database spaceos
```
Az első migration futásakor, amikor a service megpróbálja létrehozni a saját sémáját.

**Ok:** A `CREATE SCHEMA` jog nincs alapból az app role-on. A PostgreSQL-ben a séma létrehozáshoz
`CREATE` jog kell az adatbázisra, nem csak a sémára.

**Fix — első indítás előtt:**
```bash
sudo -u postgres psql -p 5433 -d spaceos \
  -c "GRANT CREATE ON DATABASE spaceos TO spaceos_<modul>_app;"

sudo systemctl restart spaceos-modules-<modul>
```

**Mikor kell:** Minden új service első deployjakor, amelyik saját sémát hoz létre
(`CREATE SCHEMA IF NOT EXISTS spaceos_<modul>`).

---

## 6. Migration-ban létrehozott role-ok jelszó nélkül születnek

**Tünet:** Service elindul, de DB kapcsolat authentication error-ral meghiúsul,
annak ellenére, hogy az env fájlban jelszó van megadva.

**Ok:** Az EF Core migration `DO $$ BEGIN IF NOT EXISTS CREATE ROLE ... END $$` blokkban
a role-t jelszó nélkül hozza létre (csak `LOGIN NOINHERIT`). Az env fájlban lévő jelszó
nincs érvényes, mert a role-on nincs beállítva.

**Fix — migration után:**
```bash
sudo -u postgres psql -p 5433 -d spaceos <<SQL
ALTER ROLE spaceos_<modul>_app PASSWORD '<app_password>';
ALTER ROLE spaceos_<modul>_worker PASSWORD '<worker_password>';
SQL
```

**Sorrend:** Jelszó generálás → env fájl írás → migration futtatás → ALTER ROLE → service start.

---

## 7. PostgreSQL port: 5433, NEM 5432

**Tünet:** `Connection refused` a connection string-ben `Port=5432`-vel.

**Ok:** A VPS-en a PostgreSQL natív telepítéssel fut (nem Docker), és a 5433-as porton
hallgat. A 5432 üres.

**Fix:** Minden connection string-ben és psql hívásban: `-p 5433` / `Port=5433`.

```bash
# Ellenőrzés
sudo -u postgres psql -p 5433 -c "\l"  # ✅
sudo -u postgres psql -p 5432 -c "\l"  # ❌ connection refused
```

---

## 8. systemd service néveltérések

**Tünet:** `Unit spaceos-cutting.service could not be found.`

**Valós service nevek** (2026-05-28 állapot):

| Név amit gondolnál | Valós systemd neve |
|---|---|
| `spaceos-cutting` | `spaceos-cutting-svc` |
| `spaceos-sales` | `spaceos-modules-sales` |
| `spaceos-identity` | `spaceos-modules-identity` |

**Ellenőrzés:**
```bash
sudo systemctl list-units --type=service | grep spaceos
```

---

## 9. nginx dist root = már a dist/ mappa

**Tünet:** `sudo cp -r dist/. "$DIST_TARGET/"` hibát dob: "same file".

**Ok:** A joinerytech.hu nginx config `root` direktívája közvetlenül a
`/opt/spaceos/frontend/joinerytech-portal/dist` mappára mutat.
A `pnpm build` ugyanebbe a mappába ír.

**Fix:** `pnpm build` után **nincs másolás szükséges**. A dist tartalom azonnal él.
```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm build
# kész — nginx automatikusan az új fájlokat szolgálja ki
```

---

## 10. journalctl — root exception kiemelése stacktrace-özönből

A .NET service-ek crashkor több száz sor stacktrace-t írnak. A tényleges hiba az első
`Exception` típus és üzenet — általában az első `fail:` sor után.

```bash
# Root exception megtalálása
sudo journalctl -u <service> --since "2 minutes ago" --no-pager \
  | grep -E "Exception|Error|fail:" | head -10

# Teljes log az utolsó indítástól
sudo journalctl -u <service> --since "$(sudo systemctl show <service> \
  --property=ActiveEnterTimestamp --value | cut -d' ' -f1,2)" --no-pager | head -60
```

**Pattern:** Ha a health endpoint 500-at ad, de a service `active (running)`:
1. Keresd meg az első `fail:` sort
2. Az alatta lévő sor a root exception típusa és üzenete
3. A stacktrace csak a hívási lánc — nem a hiba oka
