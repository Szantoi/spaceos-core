# SpaceOS Deploy Log — 2026-04-06

**Elvégző:** Claude (claude-sonnet-4-6)
**Időpont:** 2026-04-06
**Szerver:** spaceos (109.122.222.198)

---

## 1. spaceos-kernel service aktiválás

### Problémák és megoldások

**dotnet elérési út**
A dotnet csak `/root/.dotnet/dotnet`-ben volt meg. A service `spaceos` userként fut, amelyhez `ProtectHome=true` tiltja a `/root/` elérést. Megoldás: a teljes dotnet runtime átmásolva `/opt/dotnet/`-be, symlink létrehozva `/usr/bin/dotnet` → `/opt/dotnet/dotnet`.

**PostgreSQL port**
A szerver 5433-as porton fut (nem 5432 — valószínűleg párhuzamos telepítés miatt). A `/etc/spaceos/kernel.env` fájl üres volt. Beállítva:
```
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=spaceos;Username=spaceos;Password=spaceos_db_pass
ASPNETCORE_URLS=http://127.0.0.1:5001
```

**PostgreSQL user és adatbázis**
Létrehozva: `CREATE USER spaceos WITH PASSWORD '...'; CREATE DATABASE spaceos OWNER spaceos;`

**EF Core migrációk**
A `dotnet-ef` CLI tool telepítve (`/root/.dotnet/tools/dotnet-ef`). Két migráció javítást igényelt:

- `20260327200101_SpaceLayerJsonbConfig.cs` — a `text → jsonb` cast `USING` kulcsszó nélkül sikertelen PostgreSQL-en. Megoldás: `AlterColumn` helyett raw SQL: `ALTER TABLE "SpaceLayers" ALTER COLUMN "IntentDataJson" TYPE jsonb USING "IntentDataJson"::jsonb;`

- `20260402085938_AddIsArchivedToAllEntities.cs` — ez a migráció SQLite-ra lett generálva (TEXT/INTEGER típusok), de PostgreSQL-en futott. Az összes `AlterColumn` (uuid→TEXT, boolean→INTEGER, jsonb→TEXT stb.) kihagyva; csak az érdemi `AddColumn<bool> IsArchived` oszlopok alkalmazva, helyes PostgreSQL `boolean` típussal.

**ModulesDbContext schema**
A `ModulesDbContext`-nek nincsenek EF migrációi; a `modules` sémát csak development módban hozza létre `EnsureCreated` hívással. Production módban manuálisan alkalmazva: `dotnet-ef dbcontext script | psql`.
Létrehozott táblák: `modules.FlowTasks`, `FlowMilestones`, `FlowProjects`, `FlowPrograms`, `OfflineSyncQueue`.

**Type=notify → Type=simple**
A service fájlban `Type=notify` volt beállítva, de az alkalmazásban nincs `UseSystemd()` / `builder.Host.UseSystemd()` hívás. A systemd időtúllépés után leállította a service-t. Megoldás: `Type=simple`.

**Port ütközés**
A `gabor` user egy fejlesztői SpaceOS példányt futtat a 5000-es porton. Az `appsettings.Production.json` hardcode-olta `"Urls": "http://127.0.0.1:5000"`. Módosítva 5001-re.

### Végállapot
```
● spaceos-kernel.service — active (running)
  Now listening on: http://127.0.0.1:5001
  Hosting environment: Production
```

---

## 2. deploy-spaceos user (T-09)

```bash
adduser deploy-spaceos --disabled-password --gecos ''
mkdir -p /home/deploy-spaceos/.ssh
chmod 700 /home/deploy-spaceos/.ssh
```

`/etc/sudoers.d/spaceos-deploy`:
```
deploy-spaceos ALL=(root) NOPASSWD: /bin/systemctl restart spaceos-kernel
deploy-spaceos ALL=(root) NOPASSWD: /bin/systemctl restart spaceos-orchestrator
```

---

## 3. UFW — 443/tcp megnyitva

```bash
ufw allow 443/tcp
```

---

## 4. SSL / HTTPS (Let's Encrypt)

### Certbot telepítés
```bash
apt install -y certbot python3-certbot-nginx
```

### Tanúsítvány
Cert neve: `joinerytech.hu`
Domének (SAN): `joinerytech.hu`, `www.joinerytech.hu`, `asztalostech.hu`, `www.asztalostech.hu`
Lejárat: 2026-07-05
Elérési út: `/etc/letsencrypt/live/joinerytech.hu/`

Megjegyzés: az első `certbot --nginx` futtatáskor a conf `server_name _;` miatt certbot nem tudta azonosítani a server blockot, és a cert `joinerytech.hu-0001` névvel, csak 1 domainre jött létre. Megoldás: `server_name` frissítve az nginx konfigban, majd `certbot certonly --nginx --expand` az összes domainre.

### Nginx konfig (`/etc/nginx/sites-available/spaceos`)

Javítások a `/opt/spaceos/nginx-spaceos-ssl.conf` alkalmazásakor:
- `listen 443 ssl http2` (deprecated) → `listen 443 ssl; http2 on;`
- `server_names_hash_bucket_size 64;` bekapcsolva az `/etc/nginx/nginx.conf`-ban (hosszú domainnevek miatt)
- `ssl_stapling` warning: ECDSA certeknél normális, nem kritikus

### Ellenőrzés
```
https://joinerytech.hu   → HTTP/2 200 + HSTS + security headerek ✓
https://asztalostech.hu  → HTTP/2 200 + HSTS + security headerek ✓
http://joinerytech.hu    → 301 Moved Permanently → HTTPS ✓
```

### Auto-megújítás
```
● certbot.timer — active (waiting), naponta kétszer fut
certbot renew --dry-run → Congratulations, all simulated renewals succeeded ✓
```

---

## Módosított fájlok

| Fájl | Változás |
|------|----------|
| `/etc/systemd/system/spaceos-kernel.service` | `Type=notify→simple`, `DOTNET_ROOT` env hozzáadva |
| `/etc/spaceos/kernel.env` | connection string + port + ASPNETCORE_URLS beállítva |
| `/opt/spaceos/spaceos-kernel/publish/appsettings.Production.json` | port 5000→5001 |
| `/etc/nginx/sites-available/spaceos` | SSL konfig alkalmazva (nginx-spaceos-ssl.conf) |
| `/etc/nginx/nginx.conf` | `server_names_hash_bucket_size 64` bekapcsolva |
| `/opt/spaceos/SpaceOS.Kerner/SpaceOS.Infrastructure/Migrations/20260327200101_SpaceLayerJsonbConfig.cs` | jsonb cast javítva |
| `/opt/spaceos/SpaceOS.Kerner/SpaceOS.Infrastructure/Migrations/20260402085938_AddIsArchivedToAllEntities.cs` | SQLite AlterColumn-ok eltávolítva, csak IsArchived bool oszlopok alkalmazva |
| `/etc/sudoers.d/spaceos-deploy` | deploy-spaceos sudo jogok |
| `/usr/bin/dotnet` | symlink → /opt/dotnet/dotnet |

---

## Ismert technikai adósság

- A `20260402085938_AddIsArchivedToAllEntities` migráció `Down()` metódusa nem lett javítva — rollback esetén hibát adna. Az összes `AlterColumn` ott is SQLite-specifikus.
- A `joinerytech.hu-0001` cert (1 domain, nem használt) törölhető: `certbot delete --cert-name joinerytech.hu-0001`
- A `spaceos-kernel` service `UseSystemd()` hívás nélkül fut (`Type=simple`) — érdemes lenne hozzáadni a `Program.cs`-hez a `builder.Host.UseSystemd()` sort a teljes systemd integráció érdekében.
- A `gabor` user fejlesztői példánya a 5000-es porton fut — ez potenciálisan ütközhet, ha a production portot visszaállítják.
