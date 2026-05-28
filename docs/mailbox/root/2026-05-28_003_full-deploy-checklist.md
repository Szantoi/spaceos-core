---
id: MSG-ROOT-003
from: root
to: operator
type: task
priority: high
status: READ
created: 2026-05-28
---

# Teljes VPS deploy — 2026-05-28

Kernel ADR-039 + Joinery J-003 + Sales modul (új) + Frontend frissítés.

**Sorrend:** Kernel → Joinery → Sales → FE (frontend bármelyikkel párhuzamosan)

---

## 0. Shared InternalSecret generálása

**EGYSZER, a legelején — ez a secret Kernel + Joinery + Sales között közös.**

```bash
INTERNAL_SECRET=$(openssl rand -hex 32)
echo "Generált secret (mentsd el!): $INTERNAL_SECRET"
```

---

## 1. Kernel — Migration 0031 + InternalSecret

### 1a. Env frissítés
```bash
echo "SpaceOS__InternalSecret=$INTERNAL_SECRET" | sudo tee -a /etc/spaceos/kernel.env
```

### 1b. Build + publish
```bash
cd /opt/spaceos/backend/spaceos-kernel
git pull
dotnet publish Kernel.Api -c Release -o /tmp/kernel-publish/
sudo cp -r /tmp/kernel-publish/. /opt/spaceos/backend/spaceos-kernel/publish/
```

### 1c. Migration 0031 (InternalAccessAuditLog)
```bash
# Kapcsolódj a kernel DB-hez (ellenőrizd a connection string-et a kernel.env-ben)
KERNEL_CONN=$(grep "ConnectionStrings__DefaultConnection" /etc/spaceos/kernel.env | cut -d= -f2-)
dotnet ef database update \
  --project SpaceOS.Infrastructure \
  --startup-project Kernel.Api \
  --connection "$KERNEL_CONN"
```

### 1d. Restart + ellenőrzés
```bash
sudo systemctl restart spaceos-kernel
sudo systemctl status spaceos-kernel
sudo journalctl -u spaceos-kernel -n 20 | grep -i "error\|warn\|started"
```

---

## 2. Joinery — Migration J-003 + InternalSecret

### 2a. Env frissítés
```bash
echo "SpaceOS__InternalSecret=$INTERNAL_SECRET" | sudo tee -a /etc/spaceos/joinery.env
```

### 2b. Build + publish
```bash
cd /opt/spaceos/backend/spaceos-modules-joinery
git pull
dotnet publish SpaceOS.Modules.Joinery.Api -c Release -o /tmp/joinery-publish/
sudo cp -r /tmp/joinery-publish/. /opt/spaceos/backend/spaceos-modules-joinery/publish/
```

### 2c. Restart (Migration J-003 automatikusan fut startup-kor)
```bash
sudo systemctl restart spaceos-joinery
sleep 4
sudo journalctl -u spaceos-joinery -n 30 | grep -i "migration\|error\|started"
```

### 2d. Ellenőrzés
```bash
sudo -u postgres psql -p 5433 -d spaceos_joinery -c "\dt" | grep -E "DoorOrderConvertedLines|DoorOrders"
```

---

## 3. Sales modul — ÚJ service telepítés

### 3a. DB szerepkörök létrehozása

A migration automatikusan létrehozza a szerepköröket, de jelszót nem ad hozzuk. Előbb manuálisan:

```bash
SALES_APP_PASS=$(openssl rand -hex 20)
SALES_WORKER_PASS=$(openssl rand -hex 20)
echo "sales_app jelszó: $SALES_APP_PASS"
echo "sales_worker jelszó: $SALES_WORKER_PASS"

sudo -u postgres psql -p 5433 -d spaceos <<SQL
-- Szerepkörök jelszóval (migration a DO \$\$ blokkban re-futtatja de IF NOT EXISTS miatt skip)
DO \$\$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sales_app') THEN
    CREATE ROLE spaceos_sales_app LOGIN NOINHERIT PASSWORD '$SALES_APP_PASS';
  ELSE
    ALTER ROLE spaceos_sales_app PASSWORD '$SALES_APP_PASS';
  END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_sales_worker') THEN
    CREATE ROLE spaceos_sales_worker LOGIN NOINHERIT PASSWORD '$SALES_WORKER_PASS';
  ELSE
    ALTER ROLE spaceos_sales_worker PASSWORD '$SALES_WORKER_PASS';
  END IF;
END \$\$;

-- Connect jog az adatbázisra
GRANT CONNECT ON DATABASE spaceos TO spaceos_sales_app, spaceos_sales_worker;
SQL
```

### 3b. Env fájl létrehozása

```bash
sudo tee /etc/spaceos/sales.env > /dev/null <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5009
ConnectionStrings__SalesDb=Host=localhost;Port=5433;Database=spaceos;Username=spaceos_sales_app;Password=$SALES_APP_PASS
ConnectionStrings__SalesDb_Worker=Host=localhost;Port=5433;Database=spaceos;Username=spaceos_sales_worker;Password=$SALES_WORKER_PASS
SpaceOS__InternalSecret=$INTERNAL_SECRET
Jwt__Authority=https://joinerytech.hu/auth/realms/spaceos
Jwt__Audience=sales-api
AllowedHosts=*
EOF

sudo chmod 640 /etc/spaceos/sales.env
sudo chown root:spaceos /etc/spaceos/sales.env
```

### 3c. Build + publish

```bash
cd /opt/spaceos/backend/spaceos-modules-sales
git pull 2>/dev/null || true   # új repo, nincs remote — skip ha nincs
dotnet publish src/SpaceOS.Modules.Sales.Api -c Release -o /tmp/sales-publish/
sudo mkdir -p /opt/spaceos/backend/spaceos-modules-sales/publish
sudo cp -r /tmp/sales-publish/. /opt/spaceos/backend/spaceos-modules-sales/publish/
sudo chown -R spaceos:spaceos /opt/spaceos/backend/spaceos-modules-sales/publish
```

### 3d. systemd service

```bash
sudo tee /etc/systemd/system/spaceos-modules-sales.service > /dev/null <<'UNIT'
[Unit]
Description=SpaceOS Modules.Sales API (.NET 8)
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=simple
User=spaceos
WorkingDirectory=/opt/spaceos/backend/spaceos-modules-sales/publish
ExecStart=/usr/bin/dotnet /opt/spaceos/backend/spaceos-modules-sales/publish/SpaceOS.Modules.Sales.Api.dll
EnvironmentFile=/etc/spaceos/sales.env
Environment=DOTNET_ROOT=/opt/dotnet
Restart=always
RestartSec=5
TimeoutStopSec=30
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=/opt/spaceos/backend/spaceos-modules-sales/publish /var/log/spaceos
ProtectKernelTunables=true
ProtectKernelModules=true
ProtectControlGroups=true
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
sudo systemctl enable spaceos-modules-sales
```

### 3e. Első indítás (migrációk automatikusan futnak)

```bash
sudo systemctl start spaceos-modules-sales
sleep 5
sudo journalctl -u spaceos-modules-sales -n 40 | grep -i "migration\|error\|started\|listening"
```

Ha migrációs hiba (`permission denied`):
```bash
# spaceos_sales_app-nak kell a schema create jog az első migráción
sudo -u postgres psql -p 5433 -d spaceos -c "GRANT CREATE ON DATABASE spaceos TO spaceos_sales_app;"
sudo systemctl restart spaceos-modules-sales
```

### 3f. Szerepkörök jelszó ellenőrzés (migráció után)

A migration az `ALTER ROLE` -t is futtatja ha a role már létezik (IF NOT EXISTS skip) — a jelszavak megmaradnak.

### 3g. nginx — upstream + location hozzáadása

```bash
sudo nano /etc/nginx/sites-available/joinerytech
```

**Upstream szekció végére** (a `# --- HTTP → HTTPS redirect ---` sor elé):
```nginx
upstream sales_backend {
    server 127.0.0.1:5009 max_fails=3 fail_timeout=30s;
}
```

**Location blokkok közé** (pl. identity után):
```nginx
location /sales/ {
    proxy_pass http://sales_backend/;
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

## 4. Frontend — build + deploy

```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm install
pnpm build

# dist másolás (nginx a /var/www/joinerytech-t vagy az src/dist-t szolgálja ki — ellenőrizd)
DIST_TARGET=$(grep "root " /etc/nginx/sites-available/joinerytech | head -1 | awk '{print $2}' | tr -d ';')
echo "nginx root: $DIST_TARGET"

sudo cp -r dist/. "$DIST_TARGET/"
# VAGY ha a nginx közvetlenül a dist/-t nézi:
# sudo cp -r dist /opt/spaceos/frontend/joinerytech-portal/
```

---

## 5. Nginx — internal route BLOKKOLÁSA (biztonsági ellenőrzés)

Az `/api/internal/*` és `/joinery/internal/*` és `/sales/internal/*` route-ok **nem szabad** kifelé exponálva legyenek:

```bash
# Ha ilyen location blokk lenne, töröld:
grep -n "internal" /etc/nginx/sites-available/joinerytech
# Helyes eredmény: 0 találat (a belső endpoint-ok nem jelennek meg nginx-ben)
```

---

## 6. Smoke tesztek

```bash
# Kernel internal — loopbackon belülről IGEN
curl -s -o /dev/null -w "%{http_code}" \
  -H "X-SpaceOS-Internal: $INTERNAL_SECRET" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  http://127.0.0.1:5000/api/internal/tenants/63ef28b6-a43b-4d3f-a076-759a47911559
# → 200 vagy 404 (tenant nem found = OK, 401 = secret hibás)

# Joinery health
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/joinery/health
# → 200

# Sales health
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/sales/health
# → 200

# Sales internal — kívülről NEM elérhető
curl -s -o /dev/null -w "%{http_code}" \
  https://joinerytech.hu/sales/internal/orders/from-quote
# → 404 (nginx nem routeolja ki)

# FE
curl -s -o /dev/null -w "%{http_code}" https://joinerytech.hu/
# → 200
```

---

## 7. Ha valami nem indul el

```bash
# Részletes napló
sudo journalctl -u spaceos-modules-sales -n 50 --no-pager

# Leggyakoribb hibák:
# "Connection refused" → PostgreSQL port (5433 nem 5432!)
# "Role does not exist" → 3a. lépés kihagyva
# "Permission denied for schema" → GRANT CREATE ON DATABASE spaceos TO spaceos_sales_app;
# "InvalidOperationException: InternalSecret" → SpaceOS__InternalSecret hiányzik az env-ből
# "401 Unauthorized" → Jwt__Authority rossz URL
```
