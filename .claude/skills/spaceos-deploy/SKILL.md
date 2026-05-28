---
name: spaceos-deploy
description: >
  SpaceOS VPS deploy skill. Használd amikor service-t kell deployolni a VPS-re:
  új service telepítés, migration futtatás, publish + systemd restart, nginx konfig.
  Tartalmazza az összes ismert gotcha-t és a bevált diagnózis mintákat.
---

# SpaceOS VPS Deploy — Bevált Eljárás

> Minden lépés kötelező sorrendben. A gotcha szekciók éles incidensekből épültek.
> Részletes diagnózisok: `docs/knowledge/deployment/KNOWN_GOTCHAS.md`

---

## Előfeltételek ellenőrzés

```bash
# PostgreSQL elérhető-e? (5433, NEM 5432)
sudo -u postgres psql -p 5433 -c "SELECT version();" | head -2

# Szabad port?
ss -tlnp | grep <port>

# Env fájl könyvtár
ls /etc/spaceos/
```

---

## 1. Új service telepítése

### 1a. DB szerepkörök + jelszavak

```bash
APP_PASS=$(openssl rand -hex 20)
WORKER_PASS=$(openssl rand -hex 20)
echo "app: $APP_PASS"
echo "worker: $WORKER_PASS"

sudo -u postgres psql -p 5433 -d spaceos <<SQL
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_<modul>_app') THEN
    CREATE ROLE spaceos_<modul>_app LOGIN NOINHERIT PASSWORD '$APP_PASS';
  ELSE
    ALTER ROLE spaceos_<modul>_app PASSWORD '$APP_PASS';
  END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_<modul>_worker') THEN
    CREATE ROLE spaceos_<modul>_worker LOGIN NOINHERIT PASSWORD '$WORKER_PASS';
  ELSE
    ALTER ROLE spaceos_<modul>_worker PASSWORD '$WORKER_PASS';
  END IF;
END \$\$;
GRANT CONNECT ON DATABASE spaceos TO spaceos_<modul>_app, spaceos_<modul>_worker;
SQL
```

> ⚠️ **Gotcha #6:** A migration `IF NOT EXISTS CREATE ROLE` jelszó nélkül hoz létre role-t.
> Az `ALTER ROLE ... PASSWORD` itt, a migration ELŐTT kell, hogy az env fájl jelszava érvényes legyen.

### 1b. Env fájl

```bash
sudo tee /etc/spaceos/<modul>.env > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:<port>
ConnectionStrings__<Modul>Db=Host=localhost;Port=5433;Database=spaceos;Username=spaceos_<modul>_app;Password=$APP_PASS
ConnectionStrings__<Modul>Db_Worker=Host=localhost;Port=5433;Database=spaceos;Username=spaceos_<modul>_worker;Password=$WORKER_PASS
SpaceOS__InternalSecret=<shared_secret>
Auth__Authority=https://joinerytech.hu/auth/realms/spaceos
Auth__Issuer=https://joinerytech.hu/auth/realms/spaceos
AllowedHosts=*
EOF

sudo chmod 640 /etc/spaceos/<modul>.env
sudo chown root:spaceos /etc/spaceos/<modul>.env
```

> ⚠️ **Gotcha #4:** A kulcsnak pontosan egyeznie kell a `Program.cs`-sel.
> Ellenőrizd: `grep -n "Authority\|Jwt\|Auth" src/*/Program.cs`
> `.NET env → konfig:` `Auth__Authority` → `Auth:Authority`

### 1c. Build + publish

```bash
cd /opt/spaceos/backend/<repo>
dotnet publish src/<Modul>.Api -c Release -o /tmp/<modul>-publish/ 2>&1 | tail -5
sudo mkdir -p /opt/spaceos/backend/<repo>/publish
sudo cp -r /tmp/<modul>-publish/. /opt/spaceos/backend/<repo>/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/backend/<repo>/publish
```

### 1d. Migrációk futtatása (MANUÁLIS — dotnet-ef v10 nem kompatibilis .NET 8-cal)

> ⚠️ **Gotcha #1+2:** `dotnet ef database update` NEM megbízható. A model snapshot
> lehet stale, az eszköz lehet inkompatibilis. **Mindig manuális SQL.**

```bash
# 1. Olvasd el a migration fájlokat
ls src/*/Persistence/Migrations/*.cs | sort

# 2. Minden migration SQL blokkját futtasd sorban
sudo -u postgres psql -p 5433 -d spaceos <<'SQL'
-- migration tartalma (migrationBuilder.Sql(@"...") blokkokból)
SELECT '<migration-name> OK' AS result;
SQL
```

> ⚠️ **Gotcha #5:** Az első migration `CREATE SCHEMA`-t futtat. Ehhez előbb:
> ```bash
> sudo -u postgres psql -p 5433 -d spaceos \
>   -c "GRANT CREATE ON DATABASE spaceos TO spaceos_<modul>_app;"
> ```

### 1e. systemd service

```bash
sudo tee /etc/systemd/system/spaceos-modules-<modul>.service > /dev/null <<'UNIT'
[Unit]
Description=SpaceOS Modules.<Modul> API (.NET 8)
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=simple
User=spaceos
WorkingDirectory=/opt/spaceos/backend/<repo>/publish
ExecStart=/usr/bin/dotnet /opt/spaceos/backend/<repo>/publish/<Modul>.Api.dll
EnvironmentFile=/etc/spaceos/<modul>.env
Environment=DOTNET_ROOT=/opt/dotnet
Restart=always
RestartSec=5
TimeoutStopSec=30
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=/opt/spaceos/backend/<repo>/publish /var/log/spaceos
CapabilityBoundingSet=
SystemCallFilter=@system-service
SystemCallErrorNumber=EPERM
RestrictNamespaces=true
LockPersonality=true
RestrictRealtime=true
RestrictSUIDSGID=true

[Install]
WantedBy=multi-user.target
UNIT

sudo systemctl daemon-reload
sudo systemctl enable spaceos-modules-<modul>
sudo systemctl start spaceos-modules-<modul>
sleep 5
```

### 1f. nginx upstream + location

```bash
# Upstream hozzáadása (a "# --- HTTP → HTTPS redirect ---" sor elé)
sudo sed -i '/^# --- HTTP → HTTPS redirect ---/i upstream <modul>_backend {\n    server 127.0.0.1:<port> max_fails=3 fail_timeout=30s;\n}\n' \
  /etc/nginx/sites-available/joinerytech

# Location blokk hozzáadása (megfelelő helyre)
# Mintaként lásd az identity vagy sales blokk struktúráját a fájlban
sudo nano /etc/nginx/sites-available/joinerytech
```

```nginx
location /<modul>/ {
    proxy_pass http://<modul>_backend/;
    proxy_http_version 1.1;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_read_timeout 60s;
}
```

```bash
sudo nginx -t && sudo systemctl reload nginx
```

---

## 2. Meglévő service frissítése (migration + redeploy)

```bash
# 1. Új migration SQL manuálisan (lásd fent #1d)

# 2. Build + publish
cd /opt/spaceos/backend/<repo>
git pull
dotnet publish src/<Modul>.Api -c Release -o /tmp/<modul>-publish/ 2>&1 | tail -5
sudo cp -r /tmp/<modul>-publish/. /opt/spaceos/backend/<repo>/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/backend/<repo>/publish

# 3. Restart
sudo systemctl restart spaceos-modules-<modul>
sleep 4
```

---

## 3. Kernel frissítése

```bash
cd /opt/spaceos/backend/spaceos-kernel
git pull
dotnet publish SpaceOS.Kernel.Api -c Release -o /tmp/kernel-publish/ 2>&1 | tail -5
sudo cp -r /tmp/kernel-publish/. /opt/spaceos/backend/spaceos-kernel/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/backend/spaceos-kernel/publish
sudo systemctl restart spaceos-kernel
sleep 4
curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5000/healthz
```

---

## 4. Frontend deploy

```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm install
pnpm build
# Nincs másolás szükséges — nginx root közvetlenül a dist/ mappára mutat
# (Gotcha #9)
```

---

## 5. Health ellenőrzés — minden service

```bash
declare -A SERVICES=(
  [spaceos-kernel]="5000:/healthz"
  [spaceos-joinery]="5002:/health"
  [spaceos-abstractions]="5003:/health"
  [spaceos-inventory]="5004:/health"
  [spaceos-cutting-svc]="5005:/healthz"
  [spaceos-procurement]="5006:/healthz"
  [spaceos-modules-identity]="5008:/health"
  [spaceos-modules-sales]="5009:/health"
)

for svc in "${!SERVICES[@]}"; do
  IFS=: read port path <<< "${SERVICES[$svc]}"
  active=$(sudo systemctl is-active "$svc" 2>/dev/null)
  http=$(curl -s -o /dev/null -w "%{http_code}" --max-time 3 "http://127.0.0.1:$port$path")
  echo "$svc | $active | HTTP $http"
done
```

---

## 6. Diagnózis — service 500 / nem indul

```bash
# Root exception megtalálása (ne a stacktrace-t olvasd — az első "fail:" sort keresd)
sudo journalctl -u <service> --since "2 minutes ago" --no-pager \
  | grep -E "^.*fail:|Exception|Error:" | head -10

# Teljes log az utolsó indítástól
sudo journalctl -u <service> -n 80 --no-pager
```

**Leggyakoribb hibák és fix:**

| Hibaüzenet | Ok | Fix |
|---|---|---|
| `relation "schema.table" does not exist` | Migration nem futott | Manuális SQL (→ #1d) |
| `An attempt was made to use the model while it was being created` | ChangeTracker OnModelCreating-ben | Konstruktorba kell (→ KNOWN_GOTCHAS #3) |
| `MetadataAddress or Authority must use HTTPS` | Auth__Authority = null (kulcsnév mismatch) | Env kulcs javítás (→ KNOWN_GOTCHAS #4) |
| `Connection refused` | Rossz port (5432 helyett 5433) | Port=5433 (→ KNOWN_GOTCHAS #7) |
| `Role does not exist` | Role nem lett létrehozva | #1a lépés (→ KNOWN_GOTCHAS #6) |
| `permission denied for database` | GRANT CREATE hiányzik | GRANT CREATE ON DATABASE (→ KNOWN_GOTCHAS #5) |
| `Unit <name>.service could not be found` | Rossz service név | `systemctl list-units \| grep spaceos` (→ KNOWN_GOTCHAS #8) |

---

## 7. InternalSecret kezelés

Az `X-SpaceOS-Internal` header titkos. Minden service-nek ugyanaz a secret kell.

```bash
# Generálás (egyszer, a legelején)
INTERNAL_SECRET=$(openssl rand -hex 32)
echo $INTERNAL_SECRET | sudo tee /tmp/spaceos_internal_secret.txt

# Ellenőrzés melyik service-ekben van már
sudo grep -l "InternalSecret" /etc/spaceos/*.env

# Hozzáadás
echo "SpaceOS__InternalSecret=$INTERNAL_SECRET" | sudo tee -a /etc/spaceos/<service>.env
```

> Az env kulcs: `SpaceOS__InternalSecret` (→ konfig: `SpaceOS:InternalSecret`)

---

## 8. Smoke tesztek deploy után

```bash
INTERNAL_SECRET=$(cat /tmp/spaceos_internal_secret.txt)

# Kernel internal (loopbackon IGEN)
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-SpaceOS-Internal: $INTERNAL_SECRET" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  http://127.0.0.1:5000/api/internal/tenants/63ef28b6-a43b-4d3f-a076-759a47911559
# → 200 vagy 404 (tenant not found = OK, 401 = secret hibás)

# Service health-ek kívülről
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/joinery/health  # 200
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/sales/health    # 200

# Internal endpoint NEM elérhető kívülről
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/sales/internal/anything
# → 404 (nginx nem routeolja)

# Frontend
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/  # 200
```
